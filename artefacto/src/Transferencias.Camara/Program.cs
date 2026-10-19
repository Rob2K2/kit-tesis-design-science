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
            await Task.Delay(200);
        }
    }

    private static async Task Recibir(SqlConnection conexion, Broker broker, MensajeTransferencia solicitud)
    {
        await using var comando = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM dbo.CamaraSolicitud WHERE TransferenciaId = @id)
                INSERT INTO dbo.CamaraSolicitud (TransferenciaId, CuentaId, Monto, RecibidaEn)
                VALUES (@id, @cuenta, @monto, SYSUTCDATETIME())
            """, conexion);
        comando.Parameters.AddWithValue("@id", solicitud.TransferenciaId);
        comando.Parameters.AddWithValue("@cuenta", solicitud.CuentaId);
        comando.Parameters.AddWithValue("@monto", solicitud.Monto);
        await comando.ExecuteNonQueryAsync();

        broker.Publicar(Colas.Confirmaciones, solicitud with { Tipo = TiposMensaje.ConfirmacionInicial });
        broker.Publicar(Colas.Resultados, solicitud with { Tipo = TiposMensaje.ResultadoSatisfactorio });
        Console.WriteLine($"[cámara] solicitud persistida y respondida {solicitud.TransferenciaId}");
    }
}
