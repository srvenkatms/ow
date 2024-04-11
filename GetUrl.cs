using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace OW.Global
{
    public class GetConfig
    {
        private readonly IConfiguration _configuration;

    public GetConfig(IConfiguration configuration)
    {
        _configuration = configuration;
    }
        
        [FunctionName("GetConfig")]
        public  async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
             var headers = req.Headers;
            if (headers.ContainsKey("GraphQLAuthToken"))
            {
                var token = headers["GraphQLAuthToken"].ToString();
                var handler = new JsonWebTokenHandler();
                var jwtToken = handler.ReadJsonWebToken(token);

                if (jwtToken.ValidTo > DateTime.UtcNow)
                {
                    Console.WriteLine("Token is valid and not expired.");
                }
                else
                {
                    Console.WriteLine("Token is expired.");
                }
            }
           string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string enviornment = data?.Enviornment;

           var config = new ConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.Connect(_configuration["ConnectionString"])
                    .Select(KeyFilter.Any, enviornment);
            })
            .Build();   
            // Iterate through all the keys
            var keyvalues = new Dictionary<string, string>();
            foreach (var keyValuePair in config.AsEnumerable())
            {
                string key = keyValuePair.Key;
                string value = keyValuePair.Value;
               if (!key.Contains(".appconfig.featureflag"))  //exclude feature flag
                {
                     keyvalues.Add(key, value);
                }
                // Do something with the key and value...
            }
          
            return new OkObjectResult(keyvalues);
        }
    }
}



