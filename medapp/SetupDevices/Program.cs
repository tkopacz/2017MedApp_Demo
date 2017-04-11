using IotLib;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetupDevicesTags
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                try
                {
                    IotSender iot = new IotSender(new Config(@"..\..\..\IotLib\IotLib\devices.json"));
                    string tags = @"..\..\..\SetupDevices\setupdevices.json";
                    RegistryManager rm = RegistryManager.CreateFromConnectionString(ConfigurationManager.AppSettings["ServiceConnection"]);

                    var obj = JObject.Parse(File.ReadAllText(tags));
                    JArray devices = (JArray)obj["devicesTags"];
                    foreach (var item in devices)
                    {
                        var devName = c.GetDeviceInfoForObjectId(item["objectid"].ToString()).IotDeviceName;
                        var devTwin = await rm.GetTwinAsync(devName);
                        var devPatchTags = new
                        {
                            tags = new
                            {
                                address = item["address"].ToString(),
                                city = item["city"].ToString(),
                                country = item["country"].ToString(),
                                notes = item["notes"].ToString()
                            }
                        };
                        await rm.UpdateTwinAsync(devName, JsonConvert.SerializeObject(devPatchTags), devTwin.ETag);

                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Invalid configuration!", ex);
                }
            }).Wait();
            Console.WriteLine("Twins updated (enter - end)");
            Console.ReadLine();
        }
    }
}
