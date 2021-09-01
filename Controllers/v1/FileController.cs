using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using ExaDoc.v1;
using Microsoft.Extensions.Configuration;

using System.Threading.Tasks;
using ExaDoc.v1.Services;
using ExaDoc.v1.Utility;
using System.Text;
using System.Security.Cryptography;

namespace APIExaDoc.Controllers.v1
{
    [ApiVersion("1.0")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private IConfiguration _configuration;
        private StorageService _storageService;
        private SecurityService _securityService;

        public FileController(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, StorageService storageService,SecurityService securityService)
        {
            _configuration = configuration;
            this._storageService = storageService;
            string orgFolder = "";
            if (!string.IsNullOrWhiteSpace(httpContextAccessor.HttpContext?.Request?.Headers["orgName"]))
            {
                orgFolder = "/" + httpContextAccessor.HttpContext?.Request?.Headers["orgName"];
            }
            _storageService.orgName = orgFolder;
            _securityService = securityService;
        }

        /// <summary>
        /// postExadocTemplate
        /// </summary>
        /// <remarks>postfile</remarks>
        /// <param name="previousFile">If exists the previous file ID of the file</param>
        /// <param name="orgName">organization Name of the client</param>
        /// <param name="fileExtension">fileExtension of the sended file (.pdf,.docx)</param> 
        /// <param name="encryptSignature">encrypt the signature of the template</param> 
        /// <response code="200">document successfully posted, docID correspond to document name on azure storage account</response>
        /// <response code="404">Bad request</response>     
        /// <response code="500">Internal server error</response> 
        [HttpPost]
        [Route("/v{v:apiVersion}/Template")]
        [ProducesResponseType(typeof(FileInformation), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostAsync([FromHeader(Name = "previousFileName")] String previousFile, [FromHeader(Name = "orgName")] String orgName, [FromHeader(Name = "fileExtension")] String fileExtension,[FromHeader(Name = "encryptSignature")] Boolean encryptSignature)
        {
            DocumentGeneration docgen;
            try
            {
                if (String.IsNullOrWhiteSpace(fileExtension))
                {
                    throw new Exception("fileExtension is mandatory");
                }
                using (var mem = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(mem);
                    mem.Seek(0, SeekOrigin.Begin);

                    String fileName = StringExtensions.RandomString(10);

                    docgen = (fileExtension.ToLower()) switch
                    {
                        "pptx" => new DocumentGenerationPresentation(_storageService, _securityService, fileName),
                        "pdf" => new DocumentGenerationPDF(_storageService, _securityService, fileName),
                        "xlsx" => new DocumentGenerationExcel(_storageService, _securityService, fileName),
                        "xlsm" => new DocumentGenerationExcel(_storageService, _securityService, fileName),
                        _ => new DocumentGenerationWord(_storageService, _securityService, fileName),
                    };
                    docgen.DocumentTemplateStream = mem;
                    docgen.UploadTemplate(fileExtension, previousFile);
                    docgen.ExtractFileInformation();
                    string sha256;
                    using (SHA256 SHA256 = SHA256Managed.Create())
                    {
                        sha256 = Convert.ToBase64String(SHA256.ComputeHash(_storageService.DownloadDocument(docgen.FileInformation.docID, out string fileExt).ToArray()));
                        if ( encryptSignature)
                        {
                            sha256 = _securityService.EncryptSignature(sha256);
                        }
                        Response.Headers.Add("sha256Signature", sha256);
                    }
                }
               
                return Ok(docgen.FileInformation);
            }

            catch (Exception e)
            {
                return BadRequest("BAD" + e.ToString());
            }
        }

        /// <summary>
        /// post exa doc assembly
        /// </summary>
        /// <remarks>post assmebly</remarks>
        /// <param name="previousFile">If exists the previous file ID of the file</param>
        /// <param name="orgName">organization Name of the client</param>
        /// <param name="id">template name which needs an assembly on Azure </param>
        /// <response code="200">assembly successfully posted</response>
        /// <response code="500">Internal server error</response> 
        [HttpPost]
        [Route("/v{v:apiVersion}/Template/{id}/assembly")]
        [ProducesResponseType(typeof(FileInformation), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostAssemblyAsync([FromRoute] string id, [FromHeader(Name = "previousFileName")] String previousFile, [FromHeader(Name = "orgName")] String orgName)
        {
            try
            {
                string code;
                using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                {
                    code = await reader.ReadToEndAsync();
                    MemoryStream ass = CompileOneTheFly.CreateAssembly(code);
                    _storageService.UploadAssembly(ass, id + ".dll", previousFile);
                }
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest("BAD" + e.ToString());
            }
        }
    }
}
