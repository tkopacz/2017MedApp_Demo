using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.ServicebusHttpClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPDashboard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();


        }
        HttpClientHelper hc;
        RegistryManager m_rm;
        ServiceBusHttpMessage m_msg;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            hc = new HttpClientHelper("[servicebusname]", "RootManageSharedAccessKey", "[key]");
            //http{s}://{serviceNamespace}.servicebus.windows.net/{topicPath}/subscriptions/{subscriptionName}/messages/head
            btnConfirm.IsEnabled = false;
            m_rm = RegistryManager.CreateFromConnectionString("HostName=[iothubname].azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=[key]");
            await Task.Factory.StartNew(waitForMessage);

            base.OnNavigatedTo(e);
        }
        private async void waitForMessage()
        {
            while (true)
            {
                m_msg = await hc.ReceiveMessage("https://[servicebusname].servicebus.windows.net/open/subscriptions/all");
                if (m_msg != null)
                {
                    await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (m_msg.customProperties.ContainsKey("iothub-connection-device-id")) {
                            string deviceId = m_msg.customProperties["iothub-connection-device-id"]?.ToString().Replace("\"", String.Empty);
                            var deviceTwin = await m_rm.GetTwinAsync(deviceId);
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(deviceTwin.Tags["address"].ToString());
                            sb.AppendLine(deviceTwin.Tags["city"].ToString());
                            sb.AppendLine(deviceTwin.Tags["country"].ToString());
                            sb.AppendLine(deviceTwin.Tags["notes"].ToString());
                            txtInfo.Text = sb.ToString();
                        }
                        btnConfirm.IsEnabled = true;
                    });
                    return;
                }
            }
        }

        private async void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await hc.DeleteMessage("https://[servicebusname].servicebus.windows.net/open/subscriptions/all", m_msg.brokerProperties.MessageId, m_msg.brokerProperties.LockToken.Value);
                txtInfo.Text = "";
            }
            catch (Exception ex)
            {
                //Too slow,lock timeout, message was processed on another station
            }
            btnConfirm.IsEnabled = false;
            await Task.Factory.StartNew(waitForMessage);
        }
    }
}
