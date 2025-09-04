using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EstoqueService.Data;
using EstoqueService.Models;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔐 Protegido por JWT
    public class ProdutosController : ControllerBase
    {
        private readonly EstoqueContext _context;

        public ProdutosController(EstoqueContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult CadastrarProduto([FromBody] Produto produto)
        {
            _context.Produtos.Add(produto);
            _context.SaveChanges();
            return CreatedAtAction(nameof(ConsultarProduto), new { id = produto.Id }, produto);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetProduto(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null)
                return NotFound();

            return Ok(produto);
        }
        public IActionResult ConsultarProduto(int id)
        {
            var produto = _context.Produtos.Find(id);
            if (produto == null) return NotFound();
            return Ok(produto);
        }

        [HttpGet]
        public IActionResult ListarProdutos()
        {
            return Ok(_context.Produtos.ToList());
        }
    }
}