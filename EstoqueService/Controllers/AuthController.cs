using Microsoft.AspNetCore.Mvc;
using EstoqueService.Services;
using Shared;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserLogin login)
        {
            if (login.Username == "admin" && login.Password == "123")
            {
                var token = _jwtService.GenerateToken(login.Username);
                return Ok(new { token });
            }
            return Unauthorized();
        }
    }
}