using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;

namespace DemoCogSvcVision
{
    class Program
    {
        static bool runVision = false;
        static bool runOcr = false;
        static bool runForms = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("** Demo Cognitive Service Vision");
            Console.WriteLine("Usage : DemoCogSvcVision.exe FULLPATHTOIMAGEFILE [-ocr] [-vision] [-forms]");
            Console.WriteLine("        -vision : analyze a picture and return informations on it (description, tabs, adult content, ...)");
            Console.WriteLine("        -ocr    : Optical Character Regnition on the file with VisionCS, then chained with a translation to french using translatorCS");
            Console.WriteLine("        -forms   : extract Forms data from the file using the PREVIEW FormsRecognizerCS");
            if (args.Length < 1)
            {
                Console.Error.WriteLine("ERROR argument missing : add image path as parameters");
                return;
            }


            // If you have compilation erreur on the DemoSettings. properties : 
            //     - in your project copye the DemoSettings.cs file as DemoSettings.local.cs (gitignored file)
            //     - uncomment the 9 const declaration and set the values (key, endpoint and region) in order to target YOUR cognitives service instances

            Console.WriteLine($"Vision ENDPOINT : {DemoSettings.csVisionEndpoint}");
            Console.WriteLine($"Vision LOCATION : {DemoSettings.csVisionLocation}");
            Console.WriteLine($"Vision KEY      : {DemoSettings.csVisionKey.Substring(0,12)}....");
            Console.WriteLine($"Translator ENDPOINT : {DemoSettings.csTranslatorEndpoint}");
            Console.WriteLine($"Translator LOCATION : {DemoSettings.csTranslatorLocation}");
            Console.WriteLine($"Translator KEY      : {DemoSettings.csTranslatorKey.Substring(0, 12)}....");
            Console.WriteLine($"Forms Recognizer ENDPOINT : {DemoSettings.csFormsRecognizerEnpoint}");
            Console.WriteLine($"Forms Recognizer LOCATION : {DemoSettings.csFormsRecognizerLocation}");
            Console.WriteLine($"Forms Recognizer KEY      : {DemoSettings.csFormsRecognizerKey.Substring(0, 12)}....");

            foreach (var a in args)
            {
                switch(a.ToLower())
                {
                    case "-ocr":
                        runOcr = true;
                        break;
                    case "-vision":
                        runVision = true;
                        break;
                    case "-forms":
                        runForms = true;
                        break;
                 
                }
            }


            if (runVision)
                await AnalyzeImage(args[0]);

            if (runOcr)
                await OcrImage(args[0]);


            if (runForms)
                await FormsExtraction(args[0]);

#if DEBUG
            //Console.WriteLine("\n==> Press enter to exit");
            //Console.ReadLine();
#endif
        }


        static async Task FormsExtraction(string imgPath)
        {
            // based on https://docs.microsoft.com/en-us/azure/cognitive-services/form-recognizer/quickstarts/client-library c# version

            Console.Write("Extracting forms from " + imgPath + " ...");
            using (var stImg = new FileStream(imgPath, FileMode.Open))
            {
                var recognizerClient = new FormRecognizerClient(
                                            new Uri(DemoSettings.csFormsRecognizerEnpoint), 
                                            new Azure.AzureKeyCredential(DemoSettings.csFormsRecognizerKey));

                var imgForms = await recognizerClient.StartRecognizeContent(stImg  /*, new RecognizeOptions() { IncludeTextContent = true }*/ ).WaitForCompletionAsync();

                if (imgForms.Value != null)
                    PrintFormsExtractionResult(imgForms.Value);
            }
        }

        static void PrintFormsExtractionResult(FormPageCollection imgForms)
        {
            Console.WriteLine("*** FormsExtraction result : ");
            Console.WriteLine($"Forms count : {imgForms.Count}");
            Console.WriteLine();

            int formNum = 0;
            foreach(FormPage form in imgForms)
            {
                formNum++;
                Console.WriteLine($"FORM #{formNum} on page {form.PageNumber} / pos=({form.Width} , {form.Height} ) in {form.Unit} / TextAngle={form.TextAngle} / {form.Tables.Count} table(s) / {form.Lines.Count} line(s)");
                
                int tableNum = 0;
                foreach (FormTable table in form.Tables)
                {
                    tableNum++;
                    Console.WriteLine($"--TABLE #{tableNum} on page {table.PageNumber} / {table.ColumnCount} col(s) / {table.RowCount} row(s)");
                    foreach(var cell in table.Cells)
                    {
                        Console.WriteLine($"    [{cell.ColumnIndex},{cell.RowIndex}]={cell.Text}");
                    }
                }


                int lineNum = 0;
                foreach (FormLine line in form.Lines)
                {
                    lineNum++;
                    Console.WriteLine($"--LINE #{lineNum} : {line.Text}");
                }

                
            }
            
        }

        static async Task OcrImage(string imgPath)
        {
            Console.Write("OCRing " + imgPath + " ...");

            using (var stImg = new FileStream(imgPath, FileMode.Open))
            {
                var visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(DemoSettings.csVisionKey))
                {
                    Endpoint = DemoSettings.csVisionEndpoint
                };

                var imgOcr = await visionClient.RecognizePrintedTextInStreamAsync(true, stImg);

                if (imgOcr != null)
                {
                    Console.WriteLine(" Ok.");
                    PrintOcrResult(imgOcr);
                    string textToTranslate = GetTextFromOcrResult(imgOcr);
                    Console.WriteLine($"Original text : {textToTranslate}");
                    await TranslateToFrench(textToTranslate);
                }
                else Console.WriteLine(" ERROR !");
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
        static void PrintOcrResult(OcrResult imgOcr)
        {
            Console.WriteLine("*** OCR result : ");

            Console.WriteLine($"Language      : {imgOcr.Language}");
            Console.WriteLine($"Orientation   : {imgOcr.Orientation}");
            Console.WriteLine($"Text angle    : {imgOcr.TextAngle}");
            Console.WriteLine($"Regions found : {imgOcr.Regions.Count}");
            int regionNum = 0;
            foreach(var r in imgOcr.Regions)
            {
                regionNum++;
                Console.WriteLine($"**--> REGION #{regionNum} : ({r.BoundingBox})");
                foreach(var l in r.Lines)
                {
                    Console.Write($"LINE ({l.BoundingBox}) : ");
                    foreach (var w in l.Words)
                    {
                        //Console.WriteLine($"WORD: ({w.BoundingBox})");
                        Console.Write($"{w.Text} ");
                        //Console.WriteLine();
                    }
                    Console.WriteLine();
                }
            }
        }


        static async Task TranslateToFrench(string textToTranslate)
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
                request.RequestUri = new Uri(DemoSettings.csTranslatorEndpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", DemoSettings.csTranslatorKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", DemoSettings.csTranslatorLocation);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();

                Console.WriteLine("RESULT OF TRANSLATION : " + result);
            }

        }

        static async Task AnalyzeImage(string imgPath)
        {
            Console.Write("Analyzing " +  imgPath + " ...");

            using (var stImg = new FileStream(imgPath, FileMode.Open))
            {
                var visionClient = new ComputerVisionClient(new ApiKeyServiceClientCredentials(DemoSettings.csVisionKey))
                {
                    Endpoint = DemoSettings.csVisionEndpoint
                };

                
                var features = new VisualFeatureTypes[] {
                    VisualFeatureTypes.Adult,
                    VisualFeatureTypes.Brands,
                    VisualFeatureTypes.Categories,
                    VisualFeatureTypes.Color,
                    VisualFeatureTypes.Description,
                    VisualFeatureTypes.Faces,
                    VisualFeatureTypes.ImageType,
                    VisualFeatureTypes.Objects,
                    VisualFeatureTypes.Tags
                };

                var imgAnalysis = await visionClient.AnalyzeImageInStreamAsync(stImg, features);
                PrintAnalysisResult(imgAnalysis);
            }

        }


        static void PrintAnalysisResult(ImageAnalysis imgAn)
        {

            Console.WriteLine("Vision Analysis result : ");

            Console.WriteLine($"-- Description : ");
            Console.WriteLine($"     |- Captions : {FormatListOf(imgAn.Description.Captions,(ImageCaption c)=> c.Text)}");
            Console.WriteLine($"     |- Tags : {FormatListOfString(imgAn.Description.Tags)}");
            Console.WriteLine($"-- Categories : {FormatListOf(imgAn.Categories, (Category c) => $"{c.Name} [{c.Score}]")}");
            Console.WriteLine($"-- Tags : {FormatListOf(imgAn.Tags, (ImageTag t) => $"{t.Name} [{t.Confidence}]")}");
            Console.WriteLine($"-- Adult content : {imgAn.Adult.IsAdultContent}");
            Console.WriteLine($"     |- Adult score : {imgAn.Adult.AdultScore}");
            Console.WriteLine($"-- Gore content : {imgAn.Adult.IsGoryContent}");
            Console.WriteLine($"     |- Gore score : {imgAn.Adult.GoreScore}");
            Console.WriteLine($"-- Racist content : {imgAn.Adult.IsRacyContent}");
            Console.WriteLine($"     |- Gore score : {imgAn.Adult.RacyScore}");

          
        }


        static string FormatListOf<T>(IEnumerable<T> listOf, Func<T,string> extractor )
        {
            bool notFirst = false;
            StringBuilder sb = new StringBuilder();
            foreach (var obj in listOf)
            {
                if (notFirst)
                    sb.Append(" , ");
                sb.Append( extractor(obj));
                notFirst = true;
            }
            return sb.ToString();
        }



        static string FormatListOfString(IEnumerable<string> listOfString)
        {
            bool notFirst = false;
            StringBuilder sb = new StringBuilder();
            foreach(var s in listOfString)
            {
                if (notFirst)
                    sb.Append(",");
                sb.Append(s);
                notFirst= true;
            }
            return sb.ToString();
        }
    }
}
