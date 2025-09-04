using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using EstoqueService.Data;

namespace EstoqueService.Services
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RabbitMqConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: "vendas",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    var venda = JsonSerializer.Deserialize<VendaMessage>(message);
                    if (venda != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

                        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Id == venda.ProdutoId);
                        if (produto != null && produto.Quantidade >= venda.Quantidade)
                        {
                            produto.Quantidade -= venda.Quantidade;
                            await db.SaveChangesAsync();
                            Console.WriteLine($"✅ Estoque atualizado: Produto {produto.Id} agora tem {produto.Quantidade} unidades.");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Estoque insuficiente para Produto {venda.ProdutoId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao processar mensagem RabbitMQ: {ex.Message}");
                }
            };

            channel.BasicConsume(queue: "vendas", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }
    }

    // DTO para mensagem recebida
    public class VendaMessage
    {
        public int PedidoId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
    }
}