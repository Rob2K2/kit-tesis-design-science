namespace Transferencias.Compartido;

public static class Estados
{
    public const string Recibida = "Recibida";
    public const string RechazadaSinRetencion = "RechazadaSinRetencion";
    public const string FondosRetenidos = "FondosRetenidos";
    public const string Enviada = "Enviada";
    public const string ResultadoPendiente = "ResultadoPendiente";
    public const string Debitada = "Debitada";
    public const string RetencionLiberada = "RetencionLiberada";
    public const string Conciliacion = "Conciliacion";
}

public static class TiposMensaje
{
    public const string Solicitud = "Solicitud";
    public const string ConfirmacionInicial = "ConfirmacionInicial";
    public const string Consulta = "Consulta";
    public const string ConsultaRecibida = "ConsultaRecibida";
    public const string ConsultaInexistente = "ConsultaInexistente";
    public const string ResultadoSatisfactorio = "ResultadoSatisfactorio";
    public const string Acuse = "Acuse";
}

public static class Colas
{
    public const string Solicitudes = "transferencias.solicitudes";
    public const string Confirmaciones = "transferencias.confirmaciones";
    public const string Consultas = "transferencias.consultas";
    public const string RespuestasConsulta = "transferencias.respuestas-consulta";
    public const string Resultados = "transferencias.resultados";
    public const string Acuses = "transferencias.acuses";

    public static readonly string[] Todas =
    [
        Solicitudes,
        Confirmaciones,
        Consultas,
        RespuestasConsulta,
        Resultados,
        Acuses
    ];
}

public sealed record MensajeTransferencia(
    Guid TransferenciaId,
    string CuentaId,
    decimal Monto,
    string Tipo);
