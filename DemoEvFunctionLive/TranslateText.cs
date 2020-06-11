using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DemoEvFunctionLive
{
    public static class TranslateText
    {
        [FunctionName("TranslateDocumentFunction")]
        public static void Run(
            [QueueTrigger("readyfortranslation", Connection = "CnxStorageAccountDemo")]string myQueueItem, // Message that trigger the function execution
            [Queue("readyforfeatureextraction", Connection = "CnxStorageAccountDemo")] out string messsageForFeatureExtraction, // OUTput paremeters used bye function runtime to push a new message in the queue 'readyforfeatureextraction'
            ILogger log)
        {

            var separatorPosition = myQueueItem.IndexOf("##"); // parsing the message contente
            var blobname = myQueueItem.Substring(0, separatorPosition);
            var textToTranslate = myQueueItem.Substring(separatorPosition + 2); // IRL, the text to translate should be retrieve from the DB.

            log.LogInformation($"Blob id : {blobname} ");
            log.LogInformation($"text to translate : {textToTranslate}");
            var translationResult = TranslateToFrench(textToTranslate);

            messsageForFeatureExtraction = blobname + "##" + translationResult;
            // HACK : the previous line use a simple formatting to send the blob name AND the translated text to the next function.
            // In real life, we must save the original text+langguage in a DB, push only the name of the blob 
            // eg : we can use a CosmosDB database with json document linkied to each scanned document. this json doc will be enriching by each function (OCr, then translation, then feature extraction, ...)

            log.LogInformation($"Translation result : {messsageForFeatureExtraction}");
        }

        static string TranslateToFrench(string textToTranslate)
        {
            object[] body = new object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                // Set the method to Post.
                request.Method = HttpMethod.Post;
                // Construct the URI and add headers.

                string route = "/translate?api-version=3.0&to=fr";
                request.RequestUri = new Uri(System.Environment.GetEnvironmentVariable("csTranslatorEndpoint") + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", System.Environment.GetEnvironmentVariable("csTranslatorKey"));
                request.Headers.Add("Ocp-Apim-Subscription-Region", System.Environment.GetEnvironmentVariable("csTranslatorLocation"));

                // Send the request and get response.
                HttpResponseMessage response = client.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();

                // Read response as a string.
                string result =  response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                return result;
            }

        }
    }


}
