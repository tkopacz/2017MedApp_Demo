using IotLib;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoSendTelemetry
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var client01 = DeviceClient.CreateFromConnectionString(ConfigurationManager.AppSettings["Dev01Connection"], Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only);
                var client02 = DeviceClient.CreateFromConnectionString(ConfigurationManager.AppSettings["Dev02Connection"], Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only);
                Random rnd = new Random();
                for (int i = 0; i < 100; i++)
                {
                    var data = new
                    {
                        msgtype = "med",
                        devicetype = "t1",
                        devicename = "dev1", //For easy processing on stream analytics
                        val1 = rnd.Next(100),
                        val2 = rnd.NextDouble()
                    };
                    var str = JsonConvert.SerializeObject(data);
                    var msg = new Message(UTF8Encoding.UTF8.GetBytes(str));
                    msg.Properties.Add("devicename", "dev1"); //For routing on IoT Hub level
                    await client01.SendEventAsync(msg);
                }
                List<Message> msglst;
                msglst = new List<Message>();
                for (int i = 0; i < 100; i++)
                {
                    var data = new
                    {
                        msgtype = "med",
                        devicetype = "t1",
                        devicename = "dev2", //For easy processing on stream analytics
                        val1 = rnd.Next(100),
                        val2 = rnd.NextDouble()
                    };
                    var str = JsonConvert.SerializeObject(data);
                    var msg = new Message(UTF8Encoding.UTF8.GetBytes(str));
                    msg.Properties.Add("devicename", "dev2"); //For routing on IoT Hub level
                    msglst.Add(msg);
                }
                await client01.SendEventBatchAsync(msglst);

                msglst = new List<Message>();
                for (int i = 0; i < 100; i++)
                {
                    var data = new
                    {
                        msgtype = "med",
                        devicetype = "t1",
                        devicename = "dev2", //For easy processing on stream analytics
                        val1 = rnd.Next(100),
                        val2 = rnd.NextDouble()
                    };
                    var str = JsonConvert.SerializeObject(data);
                    var msg = new Message(UTF8Encoding.UTF8.GetBytes(str));
                    msg.Properties.Add("devicename", "dev2"); //For routing on IoT Hub level
                    msglst.Add(msg);
                }
                await client02.SendEventBatchAsync(msglst);

                //Using 
                IotSender iot = new IotSender(new Config(@"C:\TS\ASCEND2017\Medapp\medapp\IotLib\IotLib\devices.json"));

                await iot.SendEventBatchAsync("AAAC", msglst);

                await iot.SendEventAsync("AAAA",
                        JsonConvert.SerializeObject(new
                        {
                            msgtype = "med",
                            devicetype = "t1",
                            devicename = "dev2", //For easy processing on stream analytics
                            val1 = rnd.Next(100),
                            val2 = rnd.NextDouble()
                        }
                ));

                //Images
                var dynArr = new byte[10000]; dynArr[0] = dynArr[dynArr.Length - 1] = 255;
                await iot.UploadToBlobAsync("AAAA", "first1", new MemoryStream(dynArr));
                await iot.UploadToBlobAsync("AAAA", "first1", new MemoryStream(dynArr));
                await iot.UploadToBlobAsync("AAAB", "first2", new MemoryStream(dynArr));
                await iot.UploadToBlobAsync("AAAB", "first3", new MemoryStream(dynArr));

            }).Wait();
            Console.WriteLine("(enter)");
            Console.ReadLine();
        }
    }
}
