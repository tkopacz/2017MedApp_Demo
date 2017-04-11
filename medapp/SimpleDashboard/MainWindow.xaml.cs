using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleDashboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        SubscriptionClient m_clientAll = SubscriptionClient.Create("open", "all");
        BrokeredMessage m_msg;
        RegistryManager m_rm;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnConfirm.IsEnabled = false;
            m_rm = RegistryManager.CreateFromConnectionString(ConfigurationManager.AppSettings["ServiceConnection"]);
            Task.Factory.StartNew(waitForMessage);
        }

        private async void waitForMessage()
        {
            while (true)
            {
                m_msg = await m_clientAll.ReceiveAsync();
                if (m_msg != null)
                {
                    await this.Dispatcher.InvokeAsync(async () =>
                    {
                        string deviceId = m_msg.Properties["iothub-connection-device-id"]?.ToString();
                        var deviceTwin = await m_rm.GetTwinAsync(deviceId);
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine(deviceTwin.Tags["address"].ToString());
                        sb.AppendLine(deviceTwin.Tags["city"].ToString());
                        sb.AppendLine(deviceTwin.Tags["country"].ToString());
                        sb.AppendLine(deviceTwin.Tags["notes"].ToString());
                        txtInfo.Text = sb.ToString();
                        btnConfirm.IsEnabled = true;
                    });
                    return;
                }
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m_clientAll.Complete(m_msg.LockToken);
                txtInfo.Text = "";
            }
            catch (Exception ex)
            {
                //Too slow,lock timeout, message was processed on another station
            }
            btnConfirm.IsEnabled = false;
            Task.Factory.StartNew(waitForMessage);

        }
    }
}
