using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using ExaDoc.v1;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace APIExaDoc.Controllers.v1
{
    [ApiVersion("1.0")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private IConfiguration _configuration;
        private StorageService _storageService;


        public ImageController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, StorageService storageService)
        {
            _configuration = configuration;
            this._storageService = storageService;
            string orgFolder = "";
            if (!string.IsNullOrWhiteSpace(httpContextAccessor.HttpContext?.Request?.Headers["orgName"]))
            {
                orgFolder = "/" + httpContextAccessor.HttpContext?.Request?.Headers["orgName"];
            }
            _storageService.orgName = orgFolder;
        }

        /// <summary>
        /// postExadocImage
        /// </summary>
        /// <remarks>Post image to the server the server in order to include it in the final document</remarks>
        /// <param name="orgName">organization Name of the client</param> 
        /// <response code="200">Image Successfully posted, response correspond to document name on Azure storage</response>
        /// <response code="404">Bad request</response>
        [HttpPost]
        [Route("/v{v:apiVersion}/Image")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostAsync([FromHeader(Name = "orgName")] String orgName)
        {
            try
            {
                using (var mem = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(mem);
                    mem.Seek(0, SeekOrigin.Begin);
                    String fileName = StringExtensions.RandomString(10);
                    _storageService.CreateImageBlockBlob(mem, fileName + ".png");

                    return Ok(fileName);
                }  
            }

            catch (Exception e)
            {
                return BadRequest("BAD" + e.ToString());
            }

        }

    }
}
