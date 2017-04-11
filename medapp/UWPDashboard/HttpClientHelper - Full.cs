//---------------------------------------------------------------------------------
// Copyright (c) 2014, Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.ServicebusHttpClient
{
    class HttpClientHelperFull
    {
        const string ApiVersion = "&api-version=2012-03"; // API version 2013-03 works with Azure Service Bus and all versions of Service Bus for Windows Server.

        HttpClient httpClient;
        string token;

        // Create HttpClient object, get token, attach token to HttpClient Authorization header.
        public HttpClientHelperFull(string serviceNamespace, string keyName, string key)
        {
            this.httpClient = new HttpClient();
            this.token = GetSasToken(serviceNamespace, keyName, key);
            httpClient.DefaultRequestHeaders.Add("Authorization", this.token);
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/atom+xml;type=entry;charset=utf-8");
        }

        // Create a SAS token. SAS tokens are described in http://msdn.microsoft.com/en-us/library/windowsazure/dn170477.aspx.
        public string GetSasToken(string uri, string keyName, string key)
        {
            // Set token lifetime to 20 minutes.
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = DateTime.Now.ToUniversalTime() - origin;
            uint tokenExpirationTime = Convert.ToUInt32(diff.TotalSeconds) + 20 * 60;

            string stringToSign = Uri.EscapeUriString(uri) + "\n" + tokenExpirationTime;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));

            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            string token = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                Uri.EscapeUriString(uri), Uri.EscapeDataString(signature), tokenExpirationTime, keyName);
            Debug.WriteLine(token);
            return token;
        }

        // Send a message.
        public async Task SendMessage(string address, ServiceBusHttpMessage message)
        {
            HttpContent postContent = new ByteArrayContent(message.body);

            // Serialize BrokerProperties.
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BrokerProperties));
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, message.brokerProperties);
                postContent.Headers.Add("BrokerProperties", Encoding.UTF8.GetString(ms.ToArray()));
            }

           // Add custom properties.
           foreach (string key in message.customProperties.Keys)
           {
               postContent.Headers.Add(key, message.customProperties[key].ToString());
           }

            // Send message.
            HttpResponseMessage response = null;
            try
            {
                response = await this.httpClient.PostAsync(address + "/messages" + "?timeout=60", postContent);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("SendMessage failed: " + ex.Message);
            }
        }

        public async Task<ServiceBusHttpMessage> Receive(string address, bool deleteMessage)
        {
            // Retrieve message from Service Bus.
            HttpResponseMessage response = null;
            try
            {
                if (deleteMessage)
                {
                    response = await this.httpClient.DeleteAsync(address + "/messages/head?timeout=60");
                }
                else
                {
                    response = await this.httpClient.PostAsync(address + "/messages/head?timeout=60", new ByteArrayContent(new Byte[0]));
                }
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                if (deleteMessage)
                {
                    Debug.WriteLine("ReceiveAndDeleteMessage failed: " + ex.Message);
                }
                else
                {
                    Debug.WriteLine("ReceiveMessage failed: " + ex.Message);
                }
            }

            // Check if a message was returned.
            HttpResponseHeaders headers = response.Headers;
            if (!headers.Contains("BrokerProperties"))
            {
                return null;
            }

            // Get message body.
            ServiceBusHttpMessage message = new ServiceBusHttpMessage();
            message.body = await response.Content.ReadAsByteArrayAsync();

            // Deserialize BrokerProperties.
            IEnumerable<string> brokerProperties = headers.GetValues("BrokerProperties");
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(BrokerProperties));
            foreach (string key in brokerProperties )
            {
                using (MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(key)))
                {
                    message.brokerProperties = (BrokerProperties)serializer.ReadObject(ms);
                }
            }

            // Get custom propoerties.
            foreach (var header in headers)
            {
                string key = header.Key;
                if (!key.Equals("Transfer-Encoding") && !key.Equals("BrokerProperties") && !key.Equals("ContentType") && !key.Equals("Location") && !key.Equals("Date") && !key.Equals("Server"))
                {
                    foreach (string value in header.Value)
                    {
                        //Remove " ??
                        message.customProperties.Add(key, value);
                    }
                }
            }

            // Get message URI.
            if (headers.Contains("Location"))
            {
                IEnumerable<string> locationProperties = headers.GetValues("Location");
                message.location = locationProperties.FirstOrDefault();
            }
            return message;
        }

        // Delete message with the specified MessageId and LockToken.
        public async Task DeleteMessage(string address, string messageId, Guid LockId)
        {
            string messageUri = address + "/messages/" + messageId + "/" + LockId.ToString();
            await DeleteMessage(messageUri);
        }

        // Delete message with the specified SequenceNumber and LockToken
        public async Task DeleteMessage(string address, long seqNum, Guid LockId)
        {
            string messageUri = address + "/messages/" + seqNum + "/" + LockId.ToString();
            await DeleteMessage(messageUri);
        }

        // Delete message with the specified URI. The URI is returned in the Location header of the response of the Peek request.
        public async Task DeleteMessage(string messageUri)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await this.httpClient.DeleteAsync(messageUri + "?timeout=60");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("DeleteMessage failed: " + ex.Message);
            }
        }

        // Unlock message with the specified MessageId and LockToken.
        public async Task UnlockMessage(string address, string messageId, Guid LockId)
        {
            string messageUri = address + "/messages/" + messageId + "/" + LockId.ToString();
            await UnlockMessage(messageUri);
        }

        // Unlock message with the specified SequenceNumber and LockToken
        public async Task UnlockMessage(string address, long seqNum, Guid LockId)
        {
            string messageUri = address + "/messages/" + seqNum + "/" + LockId.ToString();
            await UnlockMessage(messageUri);
        }

        // Unlock message with the specified URI. The URI is returned in the Location header of the response of the Peek request.
        public async Task UnlockMessage(string messageUri)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await this.httpClient.PutAsync(messageUri + "?timeout=60", null);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("UnlockMessage failed: " + ex.Message);
            }
        }

        // Renew lock of the message with the specified MessageId and LockToken.
        public async Task RenewLock(string address, string messageId, Guid LockId)
        {
            string messageUri = address + "/messages/" + messageId + "/" + LockId.ToString();
            await RenewLock(messageUri);
        }

        // Renew lock of the message with the specified SequenceNumber and LockToken
        public async Task RenewLock(string address, long seqNum, Guid LockId)
        {
            string messageUri = address + "/messages/" + seqNum + "/" + LockId.ToString();
            await RenewLock(messageUri);
        }

        // Renew lock of the message with the specified URI. The URI is returned in the Location header of the response of the Peek request.
        public async Task RenewLock(string messageUri)
        {
            HttpResponseMessage response = null;
            try
            {
                response = await this.httpClient.PostAsync(messageUri + "?timeout=60", null);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("RenewLock failed: " + ex.Message);
            }
        }
    }
}
