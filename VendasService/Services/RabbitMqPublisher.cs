using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace VendasService.Services
{
    public class RabbitMqPublisher
    {
        public void PublicarVenda(object mensagem)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: "vendas",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            string body = JsonSerializer.Serialize(mensagem);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            channel.BasicPublish(
                exchange: "",
                routingKey: "vendas",
                basicProperties: null,
                body: bodyBytes
            );
        }
    }
}