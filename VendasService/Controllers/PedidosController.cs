using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VendasService.Data;
using VendasService.Models;
using VendasService.Services;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔐 JWT obrigatório
    public class PedidosController : ControllerBase
    {
        private readonly VendasContext _context;
        private readonly RabbitMqPublisher _publisher;

        public PedidosController(VendasContext context, RabbitMqPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        [HttpPost]
        [Authorize]
    public async Task<IActionResult> CriarPedido([FromBody] Pedido pedido)
    {
        // 1. Consultar estoque antes de salvar
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", Request.Headers["Authorization"].ToString().Replace("Bearer ", ""));

        var response = await httpClient.GetAsync(
            $"http://estoqueservice:8080/api/produtos/{pedido.ProdutoId}");

        if (!response.IsSuccessStatusCode)
        {
        return BadRequest("Não foi possível consultar o estoque.");
        }

        var produto = await response.Content.ReadFromJsonAsync<ProdutoDto>();
        if (produto == null || produto.Quantidade < pedido.Quantidade)
        {
            return BadRequest("Estoque insuficiente para realizar a venda.");
        }

        // 2. Se passou, criar o pedido
        pedido.Data = DateTime.UtcNow;
        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync();

        // 3. Publicar evento no RabbitMQ
        _channel.BasicPublish(
            exchange: "",
            routingKey: "vendas",
            basicProperties: null,
            body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pedido))
        );

        return Ok(new { mensagem = "Pedido criado com sucesso", pedidoId = pedido.Id });
    }
        public IActionResult CriarPedido([FromBody] Pedido pedido)
        {
            // 🔹 Aqui poderíamos consultar EstoqueService via HTTP antes de confirmar
            pedido.Status = "Confirmado";
            _context.Pedidos.Add(pedido);
            _context.SaveChanges();

            // 🔹 Publica evento no RabbitMQ para EstoqueService atualizar o estoque
            _publisher.PublicarVenda(new
            {
                PedidoId = pedido.Id,
                ProdutoId = pedido.ProdutoId,
                Quantidade = pedido.Quantidade
            });

            return CreatedAtAction(nameof(ConsultarPedido), new { id = pedido.Id }, pedido);
        }

        [HttpGet("{id}")]
        public IActionResult ConsultarPedido(int id)
        {
            var pedido = _context.Pedidos.Find(id);
            if (pedido == null) return NotFound();
            return Ok(pedido);
        }

        [HttpGet]
        public IActionResult ListarPedidos()
        {
            return Ok(_context.Pedidos.ToList());
        }
    }
}