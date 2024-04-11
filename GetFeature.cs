using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;


// Add the following using statements
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
namespace OW.Global
{
    public  class GetFeature
    {
        private  IFeatureManagerSnapshot _featureManagerSnapshot;
        private  IConfigurationRefresherProvider _refresherProvider;

        public  GetFeature(IFeatureManagerSnapshot featureManagerSnapshot, IConfigurationRefresherProvider refresherProvider)
        {
            _featureManagerSnapshot = featureManagerSnapshot;
            _refresherProvider = refresherProvider;
        }
       
        [FunctionName("GetFeature")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var services = new ServiceCollection()
                .AddSingleton(_featureManagerSnapshot)
                .AddSingleton(_refresherProvider)
                .BuildServiceProvider();

            var featureManagerSnapshot = services.GetService<IFeatureManagerSnapshot>();
            var refresherProvider = services.GetService<IConfigurationRefresherProvider>();

            var refresher = _refresherProvider.Refreshers.FirstOrDefault();
            if (refresher != null)
            {
                await refresher.RefreshAsync();
            }

            var featureNames = new Dictionary<string, bool>();
          

            await foreach (string featureName in _featureManagerSnapshot.GetFeatureNamesAsync())
            {
                var enabled = await _featureManagerSnapshot.IsEnabledAsync(featureName);
               // featureNames.Add($"{featureName}=>{enabled}");
               featureNames.Add(featureName,enabled);
            }

            return new OkObjectResult(featureNames);
        }
    
    }
}
