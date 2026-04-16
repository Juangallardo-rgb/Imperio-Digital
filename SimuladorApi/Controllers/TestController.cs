using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("publico")]
        public IActionResult Publico()
        {
            return Ok("Endpoint público");
        }

        [Authorize]
        [HttpGet("privado")]
        public IActionResult Privado()
        {
            return Ok("Tienes acceso porque estás autenticado");
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("solo-docente")]
        public IActionResult SoloDocente()
        {
            return Ok("Tienes acceso porque eres Docente");
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("solo-estudiante")]
        public IActionResult SoloEstudiante()
        {
            return Ok("Tienes acceso porque eres Estudiante");
        }
    }
}