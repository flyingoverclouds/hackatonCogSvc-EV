using System;

namespace DemoCogSvcVision
{
    class Program
    {
        static void Main(string[] args)
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

            Console.Write("Analyzing " +  args[0] + " ...");

#if DEBUG
            //Console.WriteLine("\n==> Press enter to exit");
            //Console.ReadLine();
#endif
        }
    }
}
