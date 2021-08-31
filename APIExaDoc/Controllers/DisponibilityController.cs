using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace APIExaDoc.Controllers
{
    [ApiVersionNeutral]
    [ApiController]
    public class DisponibilityController : Controller
    {
        private IConfiguration _configuration;

        public DisponibilityController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("/disponibility")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {

                return Ok();
           
        }
    }
}
