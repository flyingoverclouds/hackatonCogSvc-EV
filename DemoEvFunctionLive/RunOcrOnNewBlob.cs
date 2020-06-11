using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Text;

namespace DemoEvFunctionLive
{
    public static class RunOcrOnNewBlob
    {

        [FunctionName("NewDocumentUploaded")]
        public static void Run(
            [BlobTrigger("new/{name}", Connection = "CnxStorageAccountDemo")]Stream myBlob,  // the blob that trigger the function executation
            string name,    // name of the blob (like found in container in the storage account
            [Queue("readyfortranslation")] out string messageToTranslate, // message sent from the function the the outside, using the same storage account connexion as the blob
            ILogger log // logger interface
            )
        {
            log.LogInformation($"OCRing blob [{name}]   Size: {myBlob.Length} Bytes");

            var visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(System.Environment.GetEnvironmentVariable("csVisionKey")))
            {
                Endpoint = System.Environment.GetEnvironmentVariable("csVisionEndpoint") // AppSetting are automatically mapped on EnvironmentVariable
            };

            var imgOcr =  visionClient.RecognizePrintedTextInStreamAsync(true, myBlob).GetAwaiter().GetResult();

            if (imgOcr != null)
            {
                string textToTranslate = GetTextFromOcrResult(imgOcr);
                messageToTranslate = $"{name}##{textToTranslate}"; // use a simple formatting to send the blob name AND the ocr text to the next message.
                                                                   // In real life, we must save the original text+langguage in a DB, push only the name of the blob.
                log.LogInformation("MESSAGE pushed to queue : " + messageToTranslate);
            }
            else
            {
                messageToTranslate = string.Empty;
                log.LogError($"ERROR WHILE OCRING BLOB {name}!");
            }



        }

        static string GetTextFromOcrResult(OcrResult imgOcr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var r in imgOcr.Regions)
            {
                foreach (var l in r.Lines)
                {
                    foreach (var w in l.Words)
                    {
                        sb.Append(w.Text);
                        sb.Append(" ");
                    }
                }
            }
            return sb.ToString();
        }

    }
}
