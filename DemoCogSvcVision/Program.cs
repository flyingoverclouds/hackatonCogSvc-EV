using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;


namespace DemoCogSvcVision
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("** Demo Cognitive Service Vision");
            if (args.Length != 1)
            {
                Console.Error.WriteLine("ERROR argument missing : add image path as parameters");
                return;
            }
            Console.WriteLine($"Vision ENDPOINT : {DemoSettings.csVisionEndpoint}");
            Console.WriteLine($"Vision LOCATION : {DemoSettings.csVisionLocation}");
            Console.WriteLine($"Vision KEY      : {DemoSettings.csVisionKey}");

            await AnalyzeImage(args[0]);

#if DEBUG
            //Console.WriteLine("\n==> Press enter to exit");
            //Console.ReadLine();
#endif
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

                ImageAnalysis imgAnalysis;
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

                imgAnalysis = await visionClient.AnalyzeImageInStreamAsync(stImg, features);
                PrintAnalysisResult(imgAnalysis);
            }

        }

        static void PrintAnalysisResult(ImageAnalysis imgAn)
        {
            Console.WriteLine("Vision Analysis result : ");
            Console.WriteLine($"-- Adult content : {imgAn.Adult.IsAdultContent}");
            Console.WriteLine($"     |- Adult score : {imgAn.Adult.AdultScore}");
            Console.WriteLine($"-- Gore content : {imgAn.Adult.IsGoryContent}");
            Console.WriteLine($"     |- Gore score : {imgAn.Adult.GoreScore}");
            Console.WriteLine($"-- Racist content : {imgAn.Adult.IsRacyContent}");
            Console.WriteLine($"     |- Gore score : {imgAn.Adult.RacyScore}");

            Console.WriteLine($"-- Description : ");
            //Console.WriteLine($"     |- Captions : {FormatListOf(imgAn.Description.Captions)}");
            Console.WriteLine($"     |- Tags : {FormatListOfString(imgAn.Description.Tags)}");
        }


        static string FormatListOf<T>(IList<T> listOf)
        {
            bool notFirst = false;
            StringBuilder sb = new StringBuilder();
            foreach (var obj in listOf)
            {
                if (notFirst)
                    sb.Append(" , ");
                sb.Append(obj.ToString());
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
