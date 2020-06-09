using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;


namespace DemoCogSvcVision
{
    class Program
    {
        static bool runVision = false;
        static bool runOcr = false;


        static async Task Main(string[] args)
        {
            Console.WriteLine("** Demo Cognitive Service Vision");
            if (args.Length < 1)
            {
                Console.Error.WriteLine("ERROR argument missing : add image path as parameters");
                return;
            }
            Console.WriteLine($"Vision ENDPOINT : {DemoSettings.csVisionEndpoint}");
            Console.WriteLine($"Vision LOCATION : {DemoSettings.csVisionLocation}");
            Console.WriteLine($"Vision KEY      : {DemoSettings.csVisionKey.Substring(0,12)}....");

            foreach(var a in args)
            {
                switch(a.ToLower())
                {
                    case "-ocr":
                        runOcr = true;
                        break;
                    case "-vision":
                        runVision = true;
                        break;
                 
                }
            }


            if (runVision)
                await AnalyzeImage(args[0]);

            if (runOcr)
                await OcrImage(args[0]);


#if DEBUG
            //Console.WriteLine("\n==> Press enter to exit");
            //Console.ReadLine();
#endif
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
                }
                else Console.WriteLine(" ERROR !");
            }

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


        static string FormatListOf<T>(IList<T> listOf, Func<T,string> extractor )
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


        static string FormatListOfString(IList<string> listOfString)
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
