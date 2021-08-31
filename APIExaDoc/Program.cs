using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace APIExaDoc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }


        public static IWebHostBuilder CreateWebHostBuilder(string[] args)=>
            WebHost.CreateDefaultBuilder(args)
              .ConfigureAppConfiguration((context, config) =>
              {
                  config.AddEnvironmentVariables();
                  var keyVaultEndpoint = Environment.GetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUP__KEYVAULT__CONFIGURATIONVAULT");
                  if (!string.IsNullOrEmpty(keyVaultEndpoint))
                  {
                      var azureServiceTokenProvider = new AzureServiceTokenProvider();
                      var keyVaultClient = new KeyVaultClient(
                          new KeyVaultClient.AuthenticationCallback(
                              azureServiceTokenProvider.KeyVaultTokenCallback));
                      config.AddAzureKeyVault(
                          keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager());
                  }
              }
           ).UseIIS()
            .UseStartup<Startup>();
        }
    }
