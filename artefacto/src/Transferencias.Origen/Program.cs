using System.Text.Json;
using Microsoft.Data.SqlClient;
using Transferencias.Compartido;

namespace Transferencias.Origen;

public static class Programa
{
    private const string Cadena =
        "Server=localhost;Database=TransferenciasLab;Integrated Security=true;TrustServerCertificate=true";

    public static async Task Main()
    {
        var detenerTrasAceptar = Environment.GetEnvironmentVariable("ORIGEN_DETENER_TRAS_ACEPTAR") == "1";
        var directa = Environment.GetEnvironmentVariable("PUBLICACION") == "Directa";
        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        await ConciliarSinDespacho(conexion);
        using var broker = new Broker();
        Console.WriteLine(directa
            ? "[origen] en ejecución con publicación directa"
            : "[origen] en ejecución con trabajo pendiente");

        var pendientesEnMemoria = new List<MensajeTransferencia>();
        while (true)
        {
            var aceptadas = await AceptarSolicitudes(conexion, directa, pendientesEnMemoria);
            if (detenerTrasAceptar && aceptadas > 0)
            {
                Console.WriteLine("[origen] se detiene después de confirmar la aceptación y antes de publicar");
                return;
            }

            if (await VentanaAbierta(conexion, transaccion: null))
            {
                foreach (var mensaje in pendientesEnMemoria)
                    await PublicarDirecta(conexion, broker, mensaje);
                pendientesEnMemoria.Clear();
                await PublicarPendientes(conexion, broker);
            }

            await RecibirConfirmaciones(conexion, broker);
            await Recuperar(conexion, broker);
            await AplicarResultados(conexion, broker);
            await Task.Delay(200);
        }
    }

    private static async Task ConciliarSinDespacho(SqlConnection conexion)
    {
        await using var comando = new SqlCommand("""
            UPDATE dbo.Transferencia
            SET Estado = N'Conciliacion', ActualizadaEn = SYSUTCDATETIME()
            WHERE Estado = N'FondosRetenidos'
              AND PublicadorConfirmadoEn IS NULL
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.TrabajoPendiente p
                  WHERE p.TransferenciaId = dbo.Transferencia.TransferenciaId)
            """, conexion);
        var marcadas = await comando.ExecuteNonQueryAsync();
        if (marcadas > 0)
            Console.WriteLine($"[origen] {marcadas} solicitud(es) sin despacho pasaron a conciliación");
    }

    private static async Task<int> AceptarSolicitudes(
        SqlConnection conexion,
        bool directa,
        List<MensajeTransferencia> pendientesEnMemoria)
    {
        var aceptadas = 0;
        while (await AceptarUna(conexion, directa, pendientesEnMemoria))
            aceptadas++;
        return aceptadas;
    }

    private static async Task<bool> AceptarUna(
        SqlConnection conexion,
        bool directa,
        List<MensajeTransferencia> pendientesEnMemoria)
    {
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync();
        Guid solicitudId = Guid.Empty;
        var cuentaId = "";
        decimal monto = 0;

        var haySolicitud = false;
        await using (var leer = new SqlCommand("""
            SELECT TOP (1) SolicitudId, CuentaId, Monto
            FROM dbo.SolicitudEntrante WITH (UPDLOCK, READPAST)
            WHERE Procesada = 0
            ORDER BY SolicitudId
            """, conexion, transaccion))
        await using (var lector = await leer.ExecuteReaderAsync())
        {
            if (await lector.ReadAsync())
            {
                haySolicitud = true;
                solicitudId = lector.GetGuid(0);
                cuentaId = lector.GetString(1);
                monto = lector.GetDecimal(2);
            }
        }

        if (!haySolicitud)
        {
            await transaccion.RollbackAsync();
            return false;
        }

        decimal? saldo;
        await using (var saldoCmd = new SqlCommand("""
            SELECT Saldo FROM dbo.Cuenta WITH (UPDLOCK) WHERE CuentaId = @cuenta
            """, conexion, transaccion))
        {
            saldoCmd.Parameters.AddWithValue("@cuenta", cuentaId);
            var valor = await saldoCmd.ExecuteScalarAsync();
            saldo = valor is decimal numero ? numero : null;
        }

        var transferenciaId = Guid.NewGuid();
        var ahora = DateTime.UtcNow;
        var rechazarDuranteCierre = Environment.GetEnvironmentVariable("CIERRE") == "Rechazar";
        var ventanaAbierta = await VentanaAbierta(conexion, transaccion);
        if (saldo is null || saldo < monto || (!ventanaAbierta && rechazarDuranteCierre))
        {
            await InsertarTransferencia(conexion, transaccion, transferenciaId, cuentaId, monto, Estados.RechazadaSinRetencion, ahora);
        }
        else
        {
            await InsertarTransferencia(conexion, transaccion, transferenciaId, cuentaId, monto, Estados.FondosRetenidos, ahora);
            var carga = JsonSerializer.Serialize(new MensajeTransferencia(
                transferenciaId, cuentaId, monto, TiposMensaje.Solicitud));
            await using var retener = new SqlCommand(directa
                ? """
                UPDATE dbo.Cuenta SET Saldo = Saldo - @monto WHERE CuentaId = @cuenta;
                INSERT INTO dbo.Retencion (TransferenciaId, Monto, Vigente) VALUES (@id, @monto, 1);
                """
                : """
                UPDATE dbo.Cuenta SET Saldo = Saldo - @monto WHERE CuentaId = @cuenta;
                INSERT INTO dbo.Retencion (TransferenciaId, Monto, Vigente) VALUES (@id, @monto, 1);
                INSERT INTO dbo.TrabajoPendiente (TransferenciaId, Carga, Estado) VALUES (@id, @carga, N'Pendiente');
                """, conexion, transaccion);
            retener.Parameters.AddWithValue("@monto", monto);
            retener.Parameters.AddWithValue("@cuenta", cuentaId);
            retener.Parameters.AddWithValue("@id", transferenciaId);
            retener.Parameters.AddWithValue("@carga", carga);
            await retener.ExecuteNonQueryAsync();
            if (directa)
                pendientesEnMemoria.Add(new MensajeTransferencia(
                    transferenciaId, cuentaId, monto, TiposMensaje.Solicitud));
        }

        cronometro.Stop();
        await using (var duracion = new SqlCommand("""
            UPDATE dbo.Transferencia SET DuracionAceptacionMs = @ms WHERE TransferenciaId = @id
            """, conexion, transaccion))
        {
            duracion.Parameters.AddWithValue("@ms", (int)cronometro.ElapsedMilliseconds);
            duracion.Parameters.AddWithValue("@id", transferenciaId);
            await duracion.ExecuteNonQueryAsync();
        }

        await using var cerrar = new SqlCommand("""
            UPDATE dbo.SolicitudEntrante SET Procesada = 1 WHERE SolicitudId = @solicitud
            """, conexion, transaccion);
        cerrar.Parameters.AddWithValue("@solicitud", solicitudId);
        await cerrar.ExecuteNonQueryAsync();
        await transaccion.CommitAsync();
        Console.WriteLine($"[origen] aceptación confirmada {transferenciaId}");
        return true;
    }

    private static async Task InsertarTransferencia(
        SqlConnection conexion,
        SqlTransaction transaccion,
        Guid id,
        string cuentaId,
        decimal monto,
        string estado,
        DateTime ahora)
    {
        await using var comando = new SqlCommand("""
            INSERT INTO dbo.Transferencia
                (TransferenciaId, CuentaId, Monto, Estado, CreadaEn, ActualizadaEn)
            VALUES (@id, @cuenta, @monto, @estado, @ahora, @ahora)
            """, conexion, transaccion);
        comando.Parameters.AddWithValue("@id", id);
        comando.Parameters.AddWithValue("@cuenta", cuentaId);
        comando.Parameters.AddWithValue("@monto", monto);
        comando.Parameters.AddWithValue("@estado", estado);
        comando.Parameters.AddWithValue("@ahora", ahora);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task PublicarDirecta(SqlConnection conexion, Broker broker, MensajeTransferencia mensaje)
    {
        broker.Publicar(Colas.Solicitudes, mensaje);
        await using var marcar = new SqlCommand("""
            UPDATE dbo.Transferencia
            SET Estado = @estado, ActualizadaEn = SYSUTCDATETIME(), PublicadorConfirmadoEn = SYSUTCDATETIME()
            WHERE TransferenciaId = @id
            """, conexion);
        marcar.Parameters.AddWithValue("@id", mensaje.TransferenciaId);
        marcar.Parameters.AddWithValue("@estado", Estados.Enviada);
        await marcar.ExecuteNonQueryAsync();
        Console.WriteLine($"[origen] publicación directa confirmada {mensaje.TransferenciaId}");
    }

    private static async Task PublicarPendientes(SqlConnection conexion, Broker broker)
    {
        var pendientes = new List<(Guid Id, string Carga)>();
        await using (var leer = new SqlCommand("""
            SELECT TransferenciaId, Carga FROM dbo.TrabajoPendiente WHERE Estado = N'Pendiente'
            """, conexion))
        await using (var lector = await leer.ExecuteReaderAsync())
        {
            while (await lector.ReadAsync())
                pendientes.Add((lector.GetGuid(0), lector.GetString(1)));
        }

        foreach (var (id, carga) in pendientes)
        {
            var mensaje = JsonSerializer.Deserialize<MensajeTransferencia>(carga)
                ?? throw new InvalidOperationException("La carga pendiente no es legible.");
            broker.Publicar(Colas.Solicitudes, mensaje);
            await using var marcar = new SqlCommand("""
                UPDATE dbo.TrabajoPendiente SET Estado = N'Publicado' WHERE TransferenciaId = @id;
                UPDATE dbo.Transferencia
                SET Estado = @estado, ActualizadaEn = SYSUTCDATETIME(), PublicadorConfirmadoEn = SYSUTCDATETIME()
                WHERE TransferenciaId = @id
                """, conexion);
            marcar.Parameters.AddWithValue("@id", id);
            marcar.Parameters.AddWithValue("@estado", Estados.Enviada);
            await marcar.ExecuteNonQueryAsync();
            Console.WriteLine($"[origen] publicador confirmó {id}");
        }
    }

    private static async Task RecibirConfirmaciones(SqlConnection conexion, Broker broker)
    {
        while (broker.Tomar(Colas.Confirmaciones) is { } mensaje)
        {
            await using var comando = new SqlCommand("""
                UPDATE dbo.Transferencia
                SET Estado = @estado, ActualizadaEn = SYSUTCDATETIME(), ConfirmacionInicialEn = SYSUTCDATETIME()
                WHERE TransferenciaId = @id AND Estado = @enviada
                """, conexion);
            comando.Parameters.AddWithValue("@estado", Estados.ResultadoPendiente);
            comando.Parameters.AddWithValue("@enviada", Estados.Enviada);
            comando.Parameters.AddWithValue("@id", mensaje.TransferenciaId);
            await comando.ExecuteNonQueryAsync();
            Console.WriteLine($"[origen] confirmación inicial {mensaje.TransferenciaId}");
        }
    }

    private static async Task<bool> VentanaAbierta(SqlConnection conexion, SqlTransaction? transaccion)
    {
        await using var comando = new SqlCommand(
            "SELECT Abierta FROM dbo.Ventana WHERE Id = 1", conexion, transaccion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is bool abierta && abierta;
    }

    private static async Task Recuperar(SqlConnection conexion, Broker broker)
    {
        var inmediata = Environment.GetEnvironmentVariable("RECUPERACION") == "ReenvioInmediato";
        var espera = inmediata ? 1 : 10;
        var filas = new List<(Guid Id, string Cuenta, decimal Monto, int Reenvios, int Consultas, int Segundos)>();
        await using (var leer = new SqlCommand("""
            SELECT TransferenciaId, CuentaId, Monto, Reenvios, Consultas,
                   DATEDIFF(SECOND, PublicadorConfirmadoEn, SYSUTCDATETIME())
            FROM dbo.Transferencia
            WHERE Estado = N'Enviada' AND ConfirmacionInicialEn IS NULL
              AND PublicadorConfirmadoEn IS NOT NULL
            """, conexion))
        await using (var lector = await leer.ExecuteReaderAsync())
        {
            while (await lector.ReadAsync())
                filas.Add((lector.GetGuid(0), lector.GetString(1), lector.GetDecimal(2),
                    lector.GetInt32(3), lector.GetInt32(4), lector.GetInt32(5)));
        }

        foreach (var fila in filas)
        {
            if (fila.Segundos < espera)
                continue;
            if (inmediata && fila.Reenvios == 0)
                await Reenviar(conexion, broker, fila.Id, fila.Cuenta, fila.Monto);
            else if (!inmediata && fila.Consultas == 0)
                await Consultar(conexion, broker, fila.Id, fila.Cuenta, fila.Monto);
        }

        while (broker.Tomar(Colas.RespuestasConsulta) is { } respuesta)
            await AplicarRespuesta(conexion, broker, respuesta);
    }

    private static async Task Reenviar(
        SqlConnection conexion, Broker broker, Guid id, string cuenta, decimal monto)
    {
        broker.Publicar(Colas.Solicitudes, new MensajeTransferencia(id, cuenta, monto, TiposMensaje.Solicitud));
        await using var comando = new SqlCommand("""
            UPDATE dbo.Transferencia SET Reenvios = Reenvios + 1, ActualizadaEn = SYSUTCDATETIME()
            WHERE TransferenciaId = @id
            """, conexion);
        comando.Parameters.AddWithValue("@id", id);
        await comando.ExecuteNonQueryAsync();
        Console.WriteLine($"[origen] reenvío inmediato {id}");
    }

    private static async Task Consultar(
        SqlConnection conexion, Broker broker, Guid id, string cuenta, decimal monto)
    {
        broker.Publicar(Colas.Consultas, new MensajeTransferencia(id, cuenta, monto, TiposMensaje.Consulta));
        await using var comando = new SqlCommand("""
            UPDATE dbo.Transferencia
            SET Consultas = Consultas + 1, ConsultadaEn = SYSUTCDATETIME(), ActualizadaEn = SYSUTCDATETIME()
            WHERE TransferenciaId = @id
            """, conexion);
        comando.Parameters.AddWithValue("@id", id);
        await comando.ExecuteNonQueryAsync();
        Console.WriteLine($"[origen] consulta por identidad {id}");
    }

    private static async Task AplicarRespuesta(SqlConnection conexion, Broker broker, MensajeTransferencia respuesta)
    {
        if (respuesta.Tipo == TiposMensaje.ConsultaRecibida)
        {
            await using var comando = new SqlCommand("""
                UPDATE dbo.Transferencia
                SET Estado = @estado, ActualizadaEn = SYSUTCDATETIME()
                WHERE TransferenciaId = @id AND Estado = N'Enviada'
                """, conexion);
            comando.Parameters.AddWithValue("@estado", Estados.ResultadoPendiente);
            comando.Parameters.AddWithValue("@id", respuesta.TransferenciaId);
            await comando.ExecuteNonQueryAsync();
            Console.WriteLine($"[origen] la consulta encontró la solicitud {respuesta.TransferenciaId}");
            return;
        }

        if (respuesta.Tipo == TiposMensaje.ConsultaInexistente)
            await Reenviar(conexion, broker, respuesta.TransferenciaId, respuesta.CuentaId, respuesta.Monto);
    }

    private static async Task AplicarResultados(SqlConnection conexion, Broker broker)
    {
        if (Environment.GetEnvironmentVariable("EFECTO") == "ComprobacionSeparada")
        {
            await Task.WhenAll(AplicarUnoSeparado(), AplicarUnoSeparado());
            return;
        }

        while (broker.Tomar(Colas.Resultados) is { } mensaje)
        {
            await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync();
            await using var reservar = new SqlCommand("""
                INSERT INTO dbo.IdentidadProcesada (TransferenciaId) VALUES (@id);
                INSERT INTO dbo.Efecto (EfectoId, TransferenciaId, Tipo, Monto)
                VALUES (NEWID(), @id, N'Debito', @monto);
                UPDATE dbo.Retencion SET Vigente = 0 WHERE TransferenciaId = @id;
                UPDATE dbo.Transferencia
                SET Estado = @estado, ActualizadaEn = SYSUTCDATETIME()
                WHERE TransferenciaId = @id
                """, conexion, transaccion);
            reservar.Parameters.AddWithValue("@id", mensaje.TransferenciaId);
            reservar.Parameters.AddWithValue("@monto", mensaje.Monto);
            reservar.Parameters.AddWithValue("@estado", Estados.Debitada);
            try
            {
                await reservar.ExecuteNonQueryAsync();
                await transaccion.CommitAsync();
            }
            catch (SqlException excepcion) when (excepcion.Number is 2627 or 2601)
            {
                await transaccion.RollbackAsync();
                Console.WriteLine($"[origen] resultado duplicado ignorado {mensaje.TransferenciaId}");
                continue;
            }

            broker.Publicar(Colas.Acuses, mensaje with { Tipo = TiposMensaje.Acuse });
            Console.WriteLine($"[origen] resultado aplicado {mensaje.TransferenciaId}");
        }
    }

    private static async Task AplicarUnoSeparado()
    {
        using var broker = new Broker();
        if (broker.Tomar(Colas.Resultados) is not { } mensaje)
            return;

        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var existeCmd = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.IdentidadProcesada WHERE TransferenciaId = @id
            """, conexion);
        existeCmd.Parameters.AddWithValue("@id", mensaje.TransferenciaId);
        var existe = (int)(await existeCmd.ExecuteScalarAsync() ?? 0);
        await Task.Delay(400);
        if (existe != 0)
            return;

        await using var efecto = new SqlCommand("""
            INSERT INTO dbo.Efecto (EfectoId, TransferenciaId, Tipo, Monto)
            VALUES (NEWID(), @id, N'Debito', @monto);
            UPDATE dbo.Retencion SET Vigente = 0 WHERE TransferenciaId = @id;
            UPDATE dbo.Transferencia
            SET Estado = @estado, ActualizadaEn = SYSUTCDATETIME()
            WHERE TransferenciaId = @id
            """, conexion);
        efecto.Parameters.AddWithValue("@id", mensaje.TransferenciaId);
        efecto.Parameters.AddWithValue("@monto", mensaje.Monto);
        efecto.Parameters.AddWithValue("@estado", Estados.Debitada);
        await efecto.ExecuteNonQueryAsync();

        try
        {
            await using var identidad = new SqlCommand("""
                INSERT INTO dbo.IdentidadProcesada (TransferenciaId) VALUES (@id)
                """, conexion);
            identidad.Parameters.AddWithValue("@id", mensaje.TransferenciaId);
            await identidad.ExecuteNonQueryAsync();
        }
        catch (SqlException excepcion) when (excepcion.Number is 2627 or 2601)
        {
            Console.WriteLine($"[origen] la comprobación separada ya no pudo reservar {mensaje.TransferenciaId}");
        }

        Console.WriteLine($"[origen] efecto aplicado fuera de la reserva {mensaje.TransferenciaId}");
    }
}
