using Microsoft.Data.SqlClient;
using Transferencias.Compartido;

namespace Transferencias.Camara;

public static class Programa
{
    private const string Cadena =
        "Server=localhost;Database=TransferenciasLab;Integrated Security=true;TrustServerCertificate=true";

    public static async Task Main()
    {
        await using var conexion = new SqlConnection(Cadena);
        await conexion.OpenAsync();
        using var broker = new Broker();
        Console.WriteLine("[cámara] en ejecución");

        while (true)
        {
            if (broker.Tomar(Colas.Solicitudes) is { } solicitud)
                await Recibir(conexion, broker, solicitud);
            if (broker.Tomar(Colas.Consultas) is { } consulta)
                await ResponderConsulta(conexion, broker, consulta);
            await Task.Delay(200);
        }
    }

    private static async Task Recibir(SqlConnection conexion, Broker broker, MensajeTransferencia solicitud)
    {
        await using var existeCmd = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.CamaraSolicitud WHERE TransferenciaId = @id
            """, conexion);
        existeCmd.Parameters.AddWithValue("@id", solicitud.TransferenciaId);
        var yaExistia = (int)(await existeCmd.ExecuteScalarAsync() ?? 0) > 0;

        if (!yaExistia)
        {
            await using var comando = new SqlCommand("""
                INSERT INTO dbo.CamaraSolicitud (TransferenciaId, CuentaId, Monto, RecibidaEn)
                VALUES (@id, @cuenta, @monto, SYSUTCDATETIME())
                """, conexion);
            comando.Parameters.AddWithValue("@id", solicitud.TransferenciaId);
            comando.Parameters.AddWithValue("@cuenta", solicitud.CuentaId);
            comando.Parameters.AddWithValue("@monto", solicitud.Monto);
            await comando.ExecuteNonQueryAsync();
        }

        var perderConfirmacion = Environment.GetEnvironmentVariable("CAMARA_PERDER_1B") == "1";
        if (perderConfirmacion && !yaExistia)
        {
            Console.WriteLine($"[cámara] solicitud persistida sin confirmación inicial {solicitud.TransferenciaId}");
            return;
        }

        if (perderConfirmacion && yaExistia)
        {
            broker.Publicar(Colas.Resultados, solicitud with { Tipo = TiposMensaje.ResultadoSatisfactorio });
            Console.WriteLine($"[cámara] reenvío de la misma identidad; se emite el resultado {solicitud.TransferenciaId}");
            return;
        }

        broker.Publicar(Colas.Confirmaciones, solicitud with { Tipo = TiposMensaje.ConfirmacionInicial });
        broker.Publicar(Colas.Resultados, solicitud with { Tipo = TiposMensaje.ResultadoSatisfactorio });
        if (Environment.GetEnvironmentVariable("CAMARA_DUPLICAR_RESULTADO") == "1")
            broker.Publicar(Colas.Resultados, solicitud with { Tipo = TiposMensaje.ResultadoSatisfactorio });
        Console.WriteLine($"[cámara] solicitud persistida y respondida {solicitud.TransferenciaId}");
    }

    private static async Task ResponderConsulta(SqlConnection conexion, Broker broker, MensajeTransferencia consulta)
    {
        await using var comando = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.CamaraSolicitud WHERE TransferenciaId = @id
            """, conexion);
        comando.Parameters.AddWithValue("@id", consulta.TransferenciaId);
        var existe = (int)(await comando.ExecuteScalarAsync() ?? 0);
        if (existe == 0)
        {
            broker.Publicar(Colas.RespuestasConsulta, consulta with { Tipo = TiposMensaje.ConsultaInexistente });
            Console.WriteLine($"[cámara] consulta sin solicitud {consulta.TransferenciaId}");
            return;
        }

        broker.Publicar(Colas.RespuestasConsulta, consulta with { Tipo = TiposMensaje.ConsultaRecibida });
        broker.Publicar(Colas.Resultados, consulta with { Tipo = TiposMensaje.ResultadoSatisfactorio });
        Console.WriteLine($"[cámara] consulta encontró la solicitud {consulta.TransferenciaId}");
    }
}
