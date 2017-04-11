using IotLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
           {
               Stopwatch sw;
               IotSender iot = new IotSender(new Config(@"..\..\..\IotLib\IotLib\devices.json"));
               await iot.SendAlertAsync("AAAA", "open");
               await iot.SendAlertAsync("AAAB", "open");
               await iot.SendAlertAsync("AAAC", "open");
               Console.WriteLine("Cache");

               sw = Stopwatch.StartNew();
               for (int i = 0; i < 10; i++)
               {
                   await iot.SendAlertAsync("AAAA", "open");
                   await iot.SendAlertAsync("AAAB", "open");
                   await iot.SendAlertAsync("AAAC", "open");
               }
               sw.Stop();
               Console.WriteLine($"Cache: {sw.ElapsedMilliseconds}");



           }).Wait();
            Console.WriteLine("(enter - end)");
            Console.ReadLine();
        }
    }
}
