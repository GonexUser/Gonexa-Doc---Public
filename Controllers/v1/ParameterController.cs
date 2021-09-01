using System;
using System.IO;
using ExaDoc.v1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using ExaDoc.v1.Services;
using ExaDoc.v1.Utility;
using System.Collections.Generic;
using System.Linq;
using ExaDoc.v1.Extensions;
using ExaDoc.v1.Models;
using ExaDoc.v1.Exceptions;

namespace APIExaDoc.Controllers.v1
{
    [ApiVersion("1.0")]
    [ApiController]
    public class ParameterController : ControllerBase
    {
        private IConfiguration _configuration;
        private IWebHostEnvironment _environment;
        private StorageService _storageService;
        private LogService _logService;
        private SecurityService _securityService;


        public ParameterController(IConfiguration configuration, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, StorageService storageService, LogService logService, SecurityService securityService)
        {
            _configuration = configuration;
            _environment = environment;
            this._storageService = storageService;
            string orgFolder = "";
            if (!string.IsNullOrWhiteSpace(httpContextAccessor.HttpContext?.Request?.Headers["orgName"]))
            {
                orgFolder = "/" + httpContextAccessor.HttpContext?.Request?.Headers["orgName"];
            }
            _storageService.orgName = orgFolder;
            this._logService = logService;
            _securityService = securityService;
        }

        /// <summary>
        /// postExadocGenerate
        /// </summary>
        /// <remarks>Post param, route used for document generation, can be used for generation and electronic signature, route for electronic signature returns the 
        /// signature position in a header</remarks>
        /// <param name="id">document on azure</param>
        /// <param name="content">Data to be used for generation</param>
        /// <param name="orgName">organization Name of the client</param>  
        ///  <response code="200">document correctly generated</response>
        /// <response code="404">Bad request</response>     
        /// <response code="500">Internal server error</response> 
        [HttpPost]
        [Route("/v{v:apiVersion}/Template/{id}/generate")]
        [Route("/v{v:apiVersion}/Template/{id}/generateforsignature")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Generate([FromRoute] string id, [FromBody] FileFeatureWrapper content, [FromHeader(Name = "orgName")] String orgName)
        {
            try
            {
                content.ConvertToCaseInsensitive();
                MemoryStream mem = new MemoryStream();
                mem = _storageService.DownloadDocument(id, out string fileExtension);
                DocumentGeneration docGen = (fileExtension.ToLower()) switch
                {
                    "pptx" => new DocumentGenerationPresentation(_storageService, _logService, _securityService, id),
                    "pdf" => new DocumentGenerationPDF(_storageService, _logService, _securityService, id),
                    "xlsx" => new DocumentGenerationExcel(_storageService, _logService, _securityService, id),
                    "xlsm" => new DocumentGenerationExcel(_storageService, _logService, _securityService, id),
                    _ => new DocumentGenerationWord(_storageService, _logService, _securityService, id),
                };
                docGen.DocumentTemplateStream = mem;
                docGen.FileFeature = content;
                docGen.CheckSignature();
                
                docGen.LaunchGeneration(_environment.IsProduction());
                if (Request.Path.Value.ToLower().EndsWith("forsignature"))
                {
                    docGen.FindSignatureInformation();
                    Response.Headers.Add("GNX-Signatures", JsonConvert.SerializeObject(docGen.SignatureInformations));
                }
                var generatedFileExtension = content.outputWanted == ".pdf" ? "pdf" : fileExtension;
                Response.Headers.Add("fileExtension", generatedFileExtension);
                foreach(KeyValuePair<string,string> kp in docGen.SaveToRemote(generatedFileExtension))
                {
                    Response.Headers.Add(kp.Key,kp.Value);
                }
                docGen.SaveToRemote(generatedFileExtension);
                //Retourne toujours le fichier généré au demandeur
                return File(docGen.ConvertedFile, "application/octet-stream");
            }catch(GonexaDocException gde)
            {
                return StatusCode(gde.ErrorCode, gde.Message);
            }
            catch(Exception e)
            {
                throw e;
            }
           

        }

    }
}