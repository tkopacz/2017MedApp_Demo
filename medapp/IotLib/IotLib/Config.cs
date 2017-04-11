using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace IotLib
{
    public class DeviceInfo
    {
        public string IotDeviceName { get; set; }
        public string IotDeviceKey { get; set; }
        public string ObjectId { get; set; }
    }

    public class Config : IConfig
    {
        private string m_iotHubName;

        private List<DeviceInfo> m_devices;

        /// <summary>
        /// Return connection string to particular device
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public string GetConnectionForObjectId(string objectid)
        {
            DeviceInfo di = m_devices.FirstOrDefault(p => p.ObjectId == objectid);
            return $"HostName={m_iotHubName};DeviceId={di.IotDeviceName};SharedAccessKey={di.IotDeviceKey}";
        }

        public List<DeviceInfo> GetDevices() { return m_devices; }
        public DeviceInfo GetDeviceInfoForObjectId(string objectid)
        {
            return m_devices.FirstOrDefault(p => p.ObjectId == objectid);
        }

        /*
{
  "iothubname": "<devicednsname>",
  "devices": [
    {
      "iotdevicename": "defib01",
      "iotdevicekey": "<key>",
      "objectid": "AAAA"
    },
    {
      "iotdevicename": "defib02",
      ...
    ]
}
        */
        public Config(string file)
        {
            try
            {
                var obj = JObject.Parse(File.ReadAllText(file));
                m_iotHubName = obj["iothubname"].ToString();
                JArray devices = (JArray)obj["devices"];
                m_devices = new List<DeviceInfo>();
                foreach(var item in devices)
                {
                    m_devices.Add(new DeviceInfo { IotDeviceKey = item["iotdevicekey"].ToString(), IotDeviceName = item["iotdevicename"].ToString(), ObjectId = item["objectid"].ToString() });
                }
            } catch (Exception ex)
            {
                throw new ApplicationException("Invalid configuration!",ex);
            }
        }

    }
}
