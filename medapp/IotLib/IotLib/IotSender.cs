using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotLib
{
    public class IotSender
    {
        private IConfig m_config;
        public IotSender(IConfig config)
        {
            m_config = config;
            m_cache = new Dictionary<string, DeviceClient>();
            foreach(var item in m_config.GetDevices())
            {
                m_cache.Add(item.ObjectId, DeviceClient.CreateFromConnectionString(m_config.GetConnectionForObjectId(item.ObjectId), Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only));
            }
           
        }

        private Dictionary<string, DeviceClient> m_cache;
        
        /// <summary>
        /// Send alert 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="alertType"></param>
        public async Task SendAlertAsync(string objectId,string alertType)
        {
            var deviceClient = m_cache[objectId];
            var msg = new Message();
            msg.Properties.Add(alertType, "1");
            await deviceClient.SendEventAsync(msg);
        }
        public async Task SendAlertNoCacheAsync(string objectId, string alertType)
        {
            //Much slower!
            var deviceClient = DeviceClient.CreateFromConnectionString(m_config.GetConnectionForObjectId(objectId), Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only);
            //var deviceClient = m_cache[objectId];
            var msg = new Message();
            msg.Properties.Add(alertType, "1");
            await deviceClient.SendEventAsync(msg);
        }

        public async Task SendEventAsync(string objectId, string jsonPayload)
        {
            //var deviceClient = DeviceClient.CreateFromConnectionString(m_config.GetConnectionForObjectId(objectId), Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only);
            var deviceClient = m_cache[objectId];
            var msg = new Message(UTF8Encoding.UTF8.GetBytes(jsonPayload));
            await deviceClient.SendEventAsync(msg);
        }
        public async Task SendEventBatchAsync(string objectId, IEnumerable<Message> msg)
        {
            //var deviceClient = DeviceClient.CreateFromConnectionString(m_config.GetConnectionForObjectId(objectId), Microsoft.Azure.Devices.Client.TransportType.Amqp_Tcp_Only);
            var deviceClient = m_cache[objectId];
            await deviceClient.SendEventBatchAsync(msg);
        }
        public async Task UploadToBlobAsync(string objectId, string blobName, Stream image)
        {
            var deviceClient = m_cache[objectId];
            await deviceClient.UploadToBlobAsync(blobName,image);
        }
    }
}
