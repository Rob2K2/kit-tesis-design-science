using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Transferencias.Compartido;

namespace Transferencias.Ejecutor;

public static class Programa
{
    private const string CadenaMaestra =
        "Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=true";
    private const string Cadena =
        "Server=localhost;Database=TransferenciasLab;Integrated Security=true;TrustServerCertificate=true";

    public static async Task<int> Main(string[] args)
    {
        var comando = args.Length == 0 ? "verificar-v0" : args[0];
        if (comando is not ("verificar-v0" or "verificar-v1" or "verificar-v2" or "verificar-contrastes" or "verificar-sumativa"))
        {
            Console.WriteLine("Uso: Transferencias.Ejecutor verificar-v0 | verificar-v1 | verificar-v2 | verificar-contrastes | verificar-sumativa");
            return 1;
        }

        await CrearLibro();
        using (var broker = new Broker())
            broker.Vaciar();
        await Limpiar();
        var solucion = BuscarSolucion();
        var origen = Ruta("Transferencias.Origen", solucion);
        var camara = Ruta("Transferencias.Camara", solucion);

        try
        {
            if (comando == "verificar-v0")
            {
                await VerificarSeparacionDeConfirmaciones(origen, camara);
                await Limpiar();
                await VerificarReinicio(origen, camara);
                Console.WriteLine("[ejecutor] V0 verificada");
            }
            else if (comando == "verificar-v1")
            {
                await VerificarFalloLocal(origen, camara);
                Console.WriteLine("[ejecutor] V1 verificada");
            }
            else if (comando == "verificar-v2")
            {
                await VerificarRedisenio(origen, camara);
                Console.WriteLine("[ejecutor] V2 verificada");
            }
            else if (comando == "verificar-contrastes")
            {
                await VerificarContrastes(origen, camara);
                Console.WriteLine("[ejecutor] contrastes verificados");
            }
            else
            {
                await VerificarSumativa(origen, camara);
                Console.WriteLine("[ejecutor] sumativa terminada");
            }
        }
        catch (Exception excepcion)
        {
            Console.WriteLine($"[ejecutor] fallo: {excepcion.Message}");
            return 1;
        }

        return 0;
    }

    private static async Task VerificarRedisenio(string origenRuta, string camaraRuta)
    {
        const int repeticiones = 5;
        for (var i = 0; i < repeticiones; i++)
        {
            await Limpiar();
            using (var origen = Iniciar(origenRuta, detener: true, "Directa"))
            {
                await InsertarSolicitud(25m);
                await origen.EsperarSalida(TimeSpan.FromSeconds(15));
            }

            using (var reanudado = Iniciar(origenRuta, detener: false, "Directa"))
            using (Iniciar(camaraRuta, detener: false))
            {
                await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Conciliacion'", 1, TimeSpan.FromSeconds(8));
            }

            await ExigirEscalar("SELECT COUNT(*) FROM dbo.Efecto", 0);
            await ExigirEscalar("SELECT COUNT(*) FROM dbo.Retencion WHERE Vigente = 1", 1);
            await ExigirEscalar("SELECT Saldo FROM dbo.Cuenta WHERE CuentaId = N'C-100'", 975m);
        }

        for (var i = 0; i < repeticiones; i++)
        {
            await Limpiar();
            using (var origen = Iniciar(origenRuta, detener: true, "TrabajoPendiente"))
            {
                await InsertarSolicitud(25m);
                await origen.EsperarSalida(TimeSpan.FromSeconds(15));
            }

            using var reanudado = Iniciar(origenRuta, detener: false, "TrabajoPendiente");
            using var camara = Iniciar(camaraRuta, detener: false);
            await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(8));
            await ExigirEscalar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Conciliacion'", 0);
        }

        Console.WriteLine("[ejecutor] V2 directa: 5/5 en conciliación, con retención vigente y sin débito");
        Console.WriteLine("[ejecutor] V2 trabajo pendiente: 5/5 debitadas y ninguna en conciliación");
    }

    private static async Task VerificarFalloLocal(string origenRuta, string camaraRuta)
    {
        const int repeticiones = 5;
        var perdidasDirecta = await ContarPerdidas(origenRuta, camaraRuta, "Directa", repeticiones);
        var perdidasDurable = await ContarPerdidas(origenRuta, camaraRuta, "TrabajoPendiente", repeticiones);
        Console.WriteLine($"[ejecutor] V1 publicación directa: perdidas {perdidasDirecta}/{repeticiones}");
        Console.WriteLine($"[ejecutor] V1 trabajo pendiente: perdidas {perdidasDurable}/{repeticiones}");
        if (perdidasDirecta != repeticiones || perdidasDurable != 0)
            throw new InvalidOperationException("El punto de caída no separó las dos respuestas.");

        var directa = await MedirAceptacion(origenRuta, camaraRuta, "Directa", repeticiones);
        var durable = await MedirAceptacion(origenRuta, camaraRuta, "TrabajoPendiente", repeticiones);
        var medianaDirecta = Mediana(directa);
        var medianaDurable = Mediana(durable);
        var sobrecarga = medianaDirecta == 0 ? 0 : (medianaDurable - medianaDirecta) / medianaDirecta;
        Console.WriteLine($"[ejecutor] V1 mediana directa {medianaDirecta:0.###} ms; durable {medianaDurable:0.###} ms; sobrecarga {sobrecarga:P0}");
        Console.WriteLine(sobrecarga > 0.20m
            ? "[ejecutor] la sobrecarga formativa supera el 20% fijado en E6. El umbral no se modifica."
            : "[ejecutor] la sobrecarga formativa no supera el 20% fijado en E6.");
    }

    private static async Task<int> ContarPerdidas(string origenRuta, string camaraRuta, string publicacion, int repeticiones)
    {
        var perdidas = 0;
        for (var i = 0; i < repeticiones; i++)
        {
            await Limpiar();
            using (var origen = Iniciar(origenRuta, detener: true, publicacion))
            {
                await InsertarSolicitud(25m);
                await origen.EsperarSalida(TimeSpan.FromSeconds(15));
            }

            using var reanudado = Iniciar(origenRuta, detener: false, publicacion);
            using var camara = Iniciar(camaraRuta, detener: false);
            var debitada = await Alcanzo(
                "SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'",
                1,
                TimeSpan.FromSeconds(8));
            if (!debitada)
                perdidas++;
        }

        return perdidas;
    }

    private static async Task<List<decimal>> MedirAceptacion(string origenRuta, string camaraRuta, string publicacion, int repeticiones)
    {
        var duraciones = new List<decimal>();
        for (var i = 0; i < repeticiones; i++)
        {
            await Limpiar();
            using var origen = Iniciar(origenRuta, detener: false, publicacion);
            using var camara = Iniciar(camaraRuta, detener: false);
            await InsertarSolicitud(25m);
            await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(20));
            duraciones.Add(await Escalar("SELECT DuracionAceptacionMs FROM dbo.Transferencia"));
        }

        return duraciones;
    }

    private static decimal Mediana(List<decimal> valores)
    {
        var ordenados = valores.Order().ToArray();
        var medio = ordenados.Length / 2;
        return ordenados.Length % 2 == 1
            ? ordenados[medio]
            : (ordenados[medio - 1] + ordenados[medio]) / 2m;
    }

    private static async Task<bool> Alcanzo(string consulta, int esperado, TimeSpan plazo)
    {
        var limite = DateTime.UtcNow + plazo;
        while (DateTime.UtcNow < limite)
        {
            if (await Escalar(consulta) == esperado)
                return true;
            await Task.Delay(200);
        }

        return false;
    }

    private static async Task VerificarSeparacionDeConfirmaciones(string origenRuta, string camaraRuta)
    {
        using var origen = Iniciar(origenRuta, detener: false);
        await InsertarSolicitud(25m);
        await Esperar("""
            SELECT COUNT(*) FROM dbo.Transferencia
            WHERE PublicadorConfirmadoEn IS NOT NULL AND ConfirmacionInicialEn IS NULL
            """, 1, TimeSpan.FromSeconds(15));
        Console.WriteLine("[ejecutor] el publicador confirmó antes de la confirmación inicial");

        using var camara = Iniciar(camaraRuta, detener: false);
        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(20));
        await ExigirEstadoFinal(975m);
        Console.WriteLine("[ejecutor] el camino sin fallos terminó debitado");
    }

    private static async Task VerificarReinicio(string origenRuta, string camaraRuta)
    {
        using (var origen = Iniciar(origenRuta, detener: true))
        {
            await InsertarSolicitud(25m);
            await origen.EsperarSalida(TimeSpan.FromSeconds(15));
        }

        await ExigirEscalar("""
            SELECT COUNT(*) FROM dbo.Transferencia t
            JOIN dbo.Retencion r ON r.TransferenciaId = t.TransferenciaId
            JOIN dbo.TrabajoPendiente p ON p.TransferenciaId = t.TransferenciaId
            WHERE t.Estado = N'FondosRetenidos' AND r.Vigente = 1
              AND p.Estado = N'Pendiente' AND t.PublicadorConfirmadoEn IS NULL
            """, 1);
        Console.WriteLine("[ejecutor] el reinicio conserva la transacción confirmada");

        using var origenNuevo = Iniciar(origenRuta, detener: false);
        using var camara = Iniciar(camaraRuta, detener: false);
        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(20));
        await ExigirEstadoFinal(975m);
        Console.WriteLine("[ejecutor] después del reinicio la solicitud se publicó y se debitó");
    }

    private static async Task ExigirEstadoFinal(decimal saldoEsperado)
    {
        await ExigirEscalar("SELECT Saldo FROM dbo.Cuenta WHERE CuentaId = N'C-100'", saldoEsperado);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Efecto", 1);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Retencion WHERE Vigente = 1", 0);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.IdentidadProcesada", 1);
    }

    private static async Task VerificarContrastes(string origenRuta, string camaraRuta)
    {
        await ProbarCierreRechazo(origenRuta, camaraRuta);
        await PrepararCorrida();
        await ProbarCierreAceptado(origenRuta, camaraRuta);
        await PrepararCorrida();
        await ProbarDuplicado(origenRuta, camaraRuta, separado: false);
        await PrepararCorrida();
        await ProbarDuplicado(origenRuta, camaraRuta, separado: true);
        await PrepararCorrida();
        await ProbarConfirmacionPerdida(origenRuta, camaraRuta, consulta: true);
        await PrepararCorrida();
        await ProbarConfirmacionPerdida(origenRuta, camaraRuta, consulta: false);
    }

    private static async Task PrepararCorrida()
    {
        using (var broker = new Broker())
            broker.Vaciar();
        await Limpiar();
    }

    private static async Task ProbarCierreRechazo(string origenRuta, string camaraRuta)
    {
        await Ejecutar("UPDATE dbo.Ventana SET Abierta = 0, ReabiertaEn = NULL WHERE Id = 1");
        using var origen = Iniciar(origenRuta, detener: false, extras: new() { ["CIERRE"] = "Rechazar" });
        using var camara = Iniciar(camaraRuta, detener: false);
        await InsertarSolicitud(25m);
        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'RechazadaSinRetencion'", 1, TimeSpan.FromSeconds(10));
        await ExigirEscalar("SELECT Saldo FROM dbo.Cuenta WHERE CuentaId = N'C-100'", 1000m);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Retencion", 0);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Efecto", 0);
        Console.WriteLine("[ejecutor] durante el cierre, el rechazo no retiene fondos");
    }

    private static async Task ProbarCierreAceptado(string origenRuta, string camaraRuta)
    {
        await Ejecutar("UPDATE dbo.Ventana SET Abierta = 0, ReabiertaEn = NULL WHERE Id = 1");
        using var origen = Iniciar(origenRuta, detener: false);
        using var camara = Iniciar(camaraRuta, detener: false);
        await InsertarSolicitud(25m);
        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'FondosRetenidos'", 1, TimeSpan.FromSeconds(10));
        await Task.Delay(1500);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Transferencia WHERE PublicadorConfirmadoEn IS NULL", 1);
        await Ejecutar("UPDATE dbo.Ventana SET Abierta = 1, ReabiertaEn = SYSUTCDATETIME() WHERE Id = 1");
        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(15));
        await ExigirEscalar("""
            SELECT CASE
                WHEN DATEDIFF(MILLISECOND, v.ReabiertaEn, t.PublicadorConfirmadoEn) BETWEEN 0 AND 10000 THEN 1
                ELSE 0 END
            FROM dbo.Transferencia t CROSS JOIN dbo.Ventana v
            """, 1);
        await ExigirEstadoFinal(975m);
        Console.WriteLine("[ejecutor] la aceptación diferida se despacha dentro de los 10 segundos");
    }

    private static async Task ProbarDuplicado(string origenRuta, string camaraRuta, bool separado)
    {
        var extrasOrigen = separado
            ? new Dictionary<string, string> { ["EFECTO"] = "ComprobacionSeparada" }
            : null;
        using var origen = Iniciar(origenRuta, detener: false, extras: extrasOrigen);
        using var camara = Iniciar(camaraRuta, detener: false, extras: new() { ["CAMARA_DUPLICAR_RESULTADO"] = "1" });
        await InsertarSolicitud(25m);
        var efectos = separado ? 2 : 1;
        await Esperar("SELECT COUNT(*) FROM dbo.Efecto", efectos, TimeSpan.FromSeconds(15));
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.IdentidadProcesada", 1);
        await ExigirEscalar("SELECT Saldo FROM dbo.Cuenta WHERE CuentaId = N'C-100'", 975m);
        Console.WriteLine(separado
            ? "[ejecutor] la comprobación separada aplica el efecto dos veces"
            : "[ejecutor] la transacción única deja un solo efecto");
    }

    private static async Task ProbarConfirmacionPerdida(string origenRuta, string camaraRuta, bool consulta)
    {
        var extrasOrigen = consulta
            ? null
            : new Dictionary<string, string> { ["RECUPERACION"] = "ReenvioInmediato" };
        using var origen = Iniciar(origenRuta, detener: false, extras: extrasOrigen);
        using var camara = Iniciar(camaraRuta, detener: false, extras: new() { ["CAMARA_PERDER_1B"] = "1" });
        await InsertarSolicitud(25m);
        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(25));
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Efecto", 1);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.CamaraSolicitud", 1);
        await ExigirEscalar("SELECT Consultas FROM dbo.Transferencia", consulta ? 1 : 0);
        await ExigirEscalar("SELECT Reenvios FROM dbo.Transferencia", consulta ? 0 : 1);
        if (consulta)
        {
            await ExigirEscalar("""
                SELECT CASE
                    WHEN DATEDIFF(SECOND, PublicadorConfirmadoEn, ConsultadaEn) >= 10 THEN 1
                    ELSE 0 END
                FROM dbo.Transferencia
                """, 1);
        }
        Console.WriteLine(consulta
            ? "[ejecutor] sin confirmación inicial, la consulta evita el reenvío"
            : "[ejecutor] sin confirmación inicial, el reenvío inmediato usa la misma identidad");
    }

    private static async Task VerificarSumativa(string origenRuta, string camaraRuta)
    {
        var casos = new (string Escenario, string Brazo, Func<Task> Accion)[]
        {
            ("cierre", "rechazar", () => ProbarCierreRechazo(origenRuta, camaraRuta)),
            ("cierre", "aceptar", () => ProbarCierreAceptado(origenRuta, camaraRuta)),
            ("duplicado", "atomico", () => ProbarDuplicado(origenRuta, camaraRuta, separado: false)),
            ("duplicado", "separado", () => ProbarDuplicado(origenRuta, camaraRuta, separado: true)),
            ("confirmacion", "consulta", () => ProbarConfirmacionPerdida(origenRuta, camaraRuta, consulta: true)),
            ("confirmacion", "reenvio", () => ProbarConfirmacionPerdida(origenRuta, camaraRuta, consulta: false)),
            ("caida", "directa", () => ProbarCaida(origenRuta, camaraRuta, "Directa")),
            ("caida", "trabajo-pendiente", () => ProbarCaida(origenRuta, camaraRuta, "TrabajoPendiente"))
        };

        var destino = Path.Combine(BuscarSolucion(), "resultados", "sumativa.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        var filas = new List<string> { "escenario,brazo,repeticion,cumple,detalle" };
        foreach (var caso in casos)
        {
            var cumple = 0;
            for (var repeticion = 1; repeticion <= 30; repeticion++)
            {
                await PrepararCorrida();
                var detalle = "ok";
                try
                {
                    await caso.Accion();
                    cumple++;
                }
                catch (Exception excepcion)
                {
                    detalle = excepcion.Message.Replace(',', ';').Replace('\r', ' ').Replace('\n', ' ');
                }

                filas.Add($"{caso.Escenario},{caso.Brazo},{repeticion},{(detalle == "ok" ? 1 : 0)},{detalle}");
                Console.WriteLine($"[sumativa] {caso.Escenario}/{caso.Brazo} {repeticion}/30 {(detalle == "ok" ? "cumple" : "falla")}");
            }

            Console.WriteLine($"[sumativa] {caso.Escenario}/{caso.Brazo}: {cumple}/30");
        }

        await File.WriteAllLinesAsync(destino, filas);
        Console.WriteLine($"[sumativa] registro escrito en {destino}");
    }

    private static async Task ProbarCaida(string origenRuta, string camaraRuta, string publicacion)
    {
        using (var origen = Iniciar(origenRuta, detener: true, publicacion))
        {
            await InsertarSolicitud(25m);
            await origen.EsperarSalida(TimeSpan.FromSeconds(15));
        }

        using var reanudado = Iniciar(origenRuta, detener: false, publicacion);
        using var camara = Iniciar(camaraRuta, detener: false);
        if (publicacion == "Directa")
        {
            await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Conciliacion'", 1, TimeSpan.FromSeconds(8));
            await ExigirEscalar("SELECT COUNT(*) FROM dbo.Efecto", 0);
            await ExigirEscalar("SELECT COUNT(*) FROM dbo.Retencion WHERE Vigente = 1", 1);
            await ExigirEscalar("SELECT Saldo FROM dbo.Cuenta WHERE CuentaId = N'C-100'", 975m);
            return;
        }

        await Esperar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Debitada'", 1, TimeSpan.FromSeconds(8));
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Transferencia WHERE Estado = N'Conciliacion'", 0);
        await ExigirEscalar("SELECT COUNT(*) FROM dbo.Efecto", 1);
    }

    private static async Task Ejecutar(string sql)
    {
        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static Proceso Iniciar(
        string dll,
        bool detener,
        string publicacion = "TrabajoPendiente",
        Dictionary<string, string>? extras = null)
    {
        var inicio = new ProcessStartInfo("dotnet", $"exec \"{dll}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        inicio.Environment["PUBLICACION"] = publicacion;
        if (detener)
            inicio.Environment["ORIGEN_DETENER_TRAS_ACEPTAR"] = "1";
        if (extras is not null)
        {
            foreach (var par in extras)
                inicio.Environment[par.Key] = par.Value;
        }
        var proceso = Process.Start(inicio) ?? throw new InvalidOperationException($"No arrancó {dll}");
        proceso.OutputDataReceived += (_, evento) => { if (evento.Data is not null) Console.WriteLine(evento.Data); };
        proceso.ErrorDataReceived += (_, evento) => { if (evento.Data is not null) Console.WriteLine(evento.Data); };
        proceso.BeginOutputReadLine();
        proceso.BeginErrorReadLine();
        return new Proceso(proceso);
    }

    private static async Task CrearLibro()
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "001_libro.sql"));
        var lotes = script.Split(["\r\nGO\r\n", "\nGO\n", "\r\nGO\n", "\nGO\r\n"], StringSplitOptions.RemoveEmptyEntries);
        await using var conexion = new SqlConnection(CadenaMaestra);
        await conexion.OpenAsync();
        foreach (var lote in lotes)
        {
            var texto = lote.Trim();
            if (texto.Length == 0)
                continue;
            await using var comando = new SqlCommand(texto, conexion) { CommandTimeout = 60 };
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static async Task Limpiar()
    {
        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand("""
            DELETE FROM dbo.Efecto;
            DELETE FROM dbo.IdentidadProcesada;
            DELETE FROM dbo.TrabajoPendiente;
            DELETE FROM dbo.Retencion;
            DELETE FROM dbo.CamaraSolicitud;
            DELETE FROM dbo.Transferencia;
            DELETE FROM dbo.SolicitudEntrante;
            DELETE FROM dbo.Ejecucion;
            DELETE FROM dbo.Cuenta;
            INSERT INTO dbo.Cuenta (CuentaId, Saldo) VALUES (N'C-100', 1000);
            UPDATE dbo.Ventana SET Abierta = 1, ReabiertaEn = NULL WHERE Id = 1;
            """, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task InsertarSolicitud(decimal monto)
    {
        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand("""
            INSERT INTO dbo.SolicitudEntrante (SolicitudId, CuentaId, Monto, Procesada)
            VALUES (NEWID(), N'C-100', @monto, 0)
            """, conexion);
        comando.Parameters.AddWithValue("@monto", monto);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task Esperar(string consulta, int esperado, TimeSpan plazo)
    {
        var limite = DateTime.UtcNow + plazo;
        while (DateTime.UtcNow < limite)
        {
            if (await Escalar(consulta) == esperado)
                return;
            await Task.Delay(200);
        }

        throw new TimeoutException($"No se alcanzó {esperado} para: {consulta}");
    }

    private static async Task ExigirEscalar(string consulta, decimal esperado)
    {
        var obtenido = await Escalar(consulta);
        if (obtenido != esperado)
            throw new InvalidOperationException($"Se esperaba {esperado} y se obtuvo {obtenido} en: {consulta}");
    }

    private static async Task<decimal> Escalar(string consulta)
    {
        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(consulta, conexion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is decimal numero ? numero : Convert.ToDecimal(valor);
    }

    private static string BuscarSolucion()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            if (File.Exists(Path.Combine(directorio.FullName, "Transferencias.sln")))
                return directorio.FullName;
            directorio = directorio.Parent;
        }

        throw new InvalidOperationException("No se encontró Transferencias.sln.");
    }

    private static string Ruta(string proyecto, string solucion) =>
        Path.Combine(solucion, "src", proyecto, "bin", "Debug", "net10.0", $"{proyecto}.dll");

    private sealed class Proceso : IDisposable
    {
        private readonly Process _proceso;
        public Proceso(Process proceso) => _proceso = proceso;

        public async Task EsperarSalida(TimeSpan plazo)
        {
            using var cancelacion = new CancellationTokenSource(plazo);
            try
            {
                await _proceso.WaitForExitAsync(cancelacion.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("El origen no se detuvo después de aceptar.");
            }
        }

        public void Dispose()
        {
            if (!_proceso.HasExited)
            {
                _proceso.Kill(entireProcessTree: true);
                _proceso.WaitForExit(5000);
            }
            _proceso.Dispose();
        }
    }
}
