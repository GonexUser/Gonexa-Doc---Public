using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using ExaDoc.v1;

namespace APIExaDoc.Controllers.v1
{

    [ApiVersion("1.0")]
    [ApiController]
    public class DataManagementController : ControllerBase
    {
        private IConfiguration _configuration;

        public DataManagementController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// postExadocDataManagementSalesforce
        /// </summary>
        /// <remarks>save document template in salesforce from zip archive</remarks>
        /// <param name="salesforceOrgId">Salesforce Organization ID</param>
        /// <param name="salesforceInstanceUrl">salesforce instance where is located current salesforce organization</param>
        /// <response code="200"> Redirection to authentication result page</response>
        [HttpPost]
        [Route("/v{v:apiVersion}/DataManagement/Salesforce")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json")]
        public async Task<IActionResult> ImportSalesforeConfigurationAsync([FromHeader(Name = "orgId")] String salesforceOrgId, [FromHeader(Name = "instanceUrl")] String salesforceInstanceUrl)
        {
            Stream body = Request.Body;

            String salesforceRefreshToken = _configuration["Salesforce:" + salesforceOrgId];
            String salesforceAccessToken = await AuthenticationUtility.RefreshSalesforceTokenAsync(salesforceRefreshToken, salesforceInstanceUrl);
            await DataManagementUtility.ParseBodyAndInsertToSalesforceAsync(body, salesforceInstanceUrl, salesforceAccessToken);

            return Ok();
        }

        /// <summary>
        /// getExadocDataManagementSalesforce
        /// </summary>
        /// <remarks>Get all document templates configation from externalIds</remarks>
        /// <param name="salesforceOrgId">Salesforce Organization ID</param>
        /// <param name="salesforceInstanceUrl">salesforce instance where is located current salesforce organization</param>
        /// <param name="externalIds">List of external id to extract</param>
        /// <response code="200"> Redirection to authentication result page</response>
        [HttpGet]
        [Route("/v{v:apiVersion}/DataManagement/Salesforce")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportSalesforeConfigurationAsync([FromQuery(Name = "orgId")] string salesforceOrgId, [FromQuery(Name = "instanceUrl")] String salesforceInstanceUrl, [FromQuery(Name = "externalIds")] String externalIds)
        {
            String storageConnectionString = _configuration["Application:StorageConnectionString"];

            String salesforceRefreshToken = _configuration["Salesforce:" + salesforceOrgId];

            return Ok(await DataManagementUtility.ExportSalesforeConfigurationAsync(salesforceInstanceUrl, externalIds, salesforceRefreshToken, storageConnectionString));

        }
    }
}
