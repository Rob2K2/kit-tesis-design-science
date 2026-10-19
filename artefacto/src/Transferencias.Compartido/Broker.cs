using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Transferencias.Compartido;

public sealed class Broker : IDisposable
{
    private readonly IConnection _conexion;
    private readonly IModel _canal;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Broker()
    {
        var fabrica = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };
        _conexion = fabrica.CreateConnection();
        _canal = _conexion.CreateModel();
        _canal.ConfirmSelect();
        foreach (var cola in Colas.Todas)
            _canal.QueueDeclare(cola, durable: true, exclusive: false, autoDelete: false);
    }

    public void Publicar(string cola, MensajeTransferencia mensaje)
    {
        var cuerpo = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mensaje, Json));
        var propiedades = _canal.CreateBasicProperties();
        propiedades.Persistent = true;
        propiedades.ContentType = "application/json";
        _canal.BasicPublish("", cola, propiedades, cuerpo);
        _canal.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
    }

    public MensajeTransferencia? Tomar(string cola)
    {
        var entrega = _canal.BasicGet(cola, autoAck: false);
        if (entrega is null)
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(entrega.Body.ToArray());
            var mensaje = JsonSerializer.Deserialize<MensajeTransferencia>(json, Json)
                ?? throw new InvalidOperationException("El mensaje no tiene la forma esperada.");
            _canal.BasicAck(entrega.DeliveryTag, multiple: false);
            return mensaje;
        }
        catch
        {
            _canal.BasicNack(entrega.DeliveryTag, multiple: false, requeue: true);
            throw;
        }
    }

    public void Vaciar()
    {
        foreach (var cola in Colas.Todas)
            _canal.QueuePurge(cola);
    }

    public void Dispose()
    {
        _canal.Dispose();
        _conexion.Dispose();
    }
}
