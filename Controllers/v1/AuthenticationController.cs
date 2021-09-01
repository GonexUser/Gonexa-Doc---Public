using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ExaDoc.v1;

namespace APIExaDoc.Controllers.v1
{

    [ApiVersion("1.0")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        public AuthenticationController(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("/v{v:apiVersion}/Authentication/Salesforce/Code")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSalesforceTokenFromCode([FromQuery(Name = "code")] string salesforceCode, [FromQuery(Name = "state")] string salesforceState)
        {
            String redirectUrl = await AuthenticationUtility.GetTokenAsync(salesforceCode, salesforceState);
            IConfigurationRoot configurationRoot = (IConfigurationRoot)_configuration;
            configurationRoot.Reload();
            return Redirect(redirectUrl);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("/v{v:apiVersion}/Authentication/Salesforce")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSalesforceUser([FromQuery(Name = "orgId")] string salesforceOrgId, [FromQuery(Name = "instanceUrl")] String salesforceInstanceUrl)
        {
            String salesforceRefreshToken = _configuration["Salesforce:" + salesforceOrgId];
            String salesforceAccessToken = await AuthenticationUtility.RefreshSalesforceTokenAsync(salesforceRefreshToken, salesforceInstanceUrl);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", salesforceAccessToken);
            var response = client.GetAsync(String.Format("{0}/services/oauth2/userinfo", salesforceInstanceUrl)).Result;
            if (response.IsSuccessStatusCode)
            {
                return Ok(response);
            }
            else
            {
                return Unauthorized();
            }
        }

        /*// POST: api/File
        [HttpPost]
        [Route("/v{v:apiVersion}/Admin/Salesforce/Token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async System.Threading.Tasks.Task<IActionResult> PostSalesforceTokenAsync()
        {
            try
            {
                HttpClient client = new HttpClient();

                string code = Request.Headers["code"];
                string clientId = Request.Headers["clientId"];
                string clientSecret = Request.Headers["clientSecret"];
                string redirect_Uri = Request.Headers["redirect_Uri"];
                string domain = Request.Headers["domain"];

                var response = await client.PostAsync("https://" + domain + ".salesforce.com/services/oauth2/token?grant_type=authorization_code&client_secret=" + clientSecret + "&redirect_uri=" + redirect_Uri + "&client_id=" + clientId + "&code=" + code, null);
                var result = await response.Content.ReadAsStringAsync();
                SalesforceToken salesforceToken = JsonConvert.DeserializeObject<SalesforceToken>(result);

                var keyVaultEndpoint = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUP__KEYVAULT__CONFIGURATIONVAULT");
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(
                    new KeyVaultClient.AuthenticationCallback(
                        azureServiceTokenProvider.KeyVaultTokenCallback));
                await keyVaultClient.SetSecretAsync(keyVaultEndpoint, salesforceToken.id.Split('/')[4], salesforceToken.access_token);
                return Ok(result);
            }
            catch (Exception e)
            {
                var t = e;
                return Ok(t);
            }

        }
        */
    }
}
