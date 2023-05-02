using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.Graphics.Display;
using Windows.Foundation;
using Windows.UI.Input;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using BrowserClient;
using Newtonsoft.Json;
using Windows.Networking.Sockets;

namespace BrowserClient
{
    public sealed partial class MainPage : Page
    {
        WebBrowserDataSource ds;
        public UdpClient sendingClient;
        public UdpClient recivingClient;

        public string broadcastAddress = "255.255.255.255";
        Timer UdpDiscoveryTimer;
        public MainPage()
        {
            this.InitializeComponent();
            if (IsMobile)
            {
                Windows.UI.ViewManagement.StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.HideAsync();
            }

        }
        public async Task<BitmapImage> ConvertToBitmapImage(byte[] image)
        {
            BitmapImage bitmapimage = null;
            using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(ms.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes((byte[])image);
                    await writer.StoreAsync();
                }
                bitmapimage = new BitmapImage();
                bitmapimage.SetSource(ms);
            }
            return bitmapimage;
        }

        private void LoseFocus(object sender)
        {
            var control = sender as Control;
            var isTabStop = control.IsTabStop;
            control.IsTabStop = false;
            control.IsEnabled = false;
            control.IsEnabled = true;
            control.IsTabStop = isTabStop;
        }
        private void TextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var url = urlBar.Text;
                ds.Navigate(url);
                e.Handled = true; LoseFocus(sender);
            }
        }
        public static bool IsMobile
        {
            get
            {
                var qualifiers = Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().QualifierValues;
                return (qualifiers.ContainsKey("DeviceFamily") && qualifiers["DeviceFamily"] == "Mobile");
            }
        }

        private void Test_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            Debug.WriteLine("sizechange");
        }

        private void Page_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            //if(ds!=null)
            //ds.SizeChange(e.NewSize);

            // Debug.WriteLine("sizechange"+e.NewSize);
        }

        private void Test_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
            var x = e.GetCurrentPoint(null).Position.X / bounds.Width;
            var y = e.GetCurrentPoint(null).Position.Y / bounds.Height;
            ds.TouchDown(new Point(x, y), e.Pointer.PointerId);
        }

        private void Test_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
            var x = e.GetCurrentPoint(null).Position.X / bounds.Width;
            var y = e.GetCurrentPoint(null).Position.Y / bounds.Height;
            ds.TouchUp(new Point(x, y), e.Pointer.PointerId);
        }

        private void Test_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
            var x = e.GetCurrentPoint(null).Position.X / bounds.Width;
            var y = e.GetCurrentPoint(null).Position.Y / bounds.Height;
            ds.TouchMove(new Point(x, y), e.Pointer.PointerId);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            //    tcp:// = ws://
            ds = new WebBrowserDataSource();
            ds.DataRecived += (s, o) =>
            {
                test.Source = ConvertToBitmapImage(o).Result;
                // ds.ACKRender();
            };
            ConnectPage.Visibility = Visibility.Collapsed;
            ds.StartRecive(serverAddress.Text);
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void Browser_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Debug.WriteLine("SIZE!!!!");
            if (ds != null)
            {
                ds.SizeChange(e.NewSize);
            }

        }
        public bool discovering = false;

        DatagramSocket serverDatagramSocket;

        private void DiscoverBtn_Click(object sender, RoutedEventArgs e)
        {
            
            //TODO:
            //1336 & 1337 for UDP ports, 5454X is out of specon UWP?
            int udpPort = 54545;
            int udpRecPort = 54546;


            ConnectPage.Visibility = Visibility.Collapsed;
            DiscoveryPage.Visibility = Visibility.Visible;

            sendingClient = new UdpClient(udpPort);
            sendingClient.EnableBroadcast = true;


            recivingClient = new UdpClient(udpRecPort);
            


            //3 seconds
            UdpDiscoveryTimer = new Timer(state =>
            {
                try
                {
                    //datagram discovery, we broadcast that we WANT an adress
                    var packet = new DiscoveryPacket
                    {
                        PType = DiscoveryPacketType.AddressRequest,
                    };
                    var rawPacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));
                    sendingClient.SendAsync(rawPacket, rawPacket.Length, new System.Net.IPEndPoint(IPAddress.Parse("255.255.255.255"), udpPort));
                }
                catch (Exception) { }
            }, null, 0, 3000);

            discovering = true;

            serverDatagramSocket = new Windows.Networking.Sockets.DatagramSocket();

            // The ConnectionReceived event is raised when connections are received.
            serverDatagramSocket.MessageReceived += ServerDatagramSocket_MessageReceived;
        
            // Start listening for incoming TCP connections on the specified port. You can specify any port that's not currently in use.
             serverDatagramSocket.BindServiceNameAsync("1337");

        }

        private void ServerDatagramSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            string request;
            using (DataReader dataReader = args.GetDataReader())
            {
                request = dataReader.ReadString(dataReader.UnconsumedBufferLength).Trim();
            }
            Debug.WriteLine(request);

            var packet = JsonConvert.DeserializeObject<DiscoveryPacket>(request);

            switch (packet.PType)
            {
                case DiscoveryPacketType.AddressRequest:
                    break;
                case DiscoveryPacketType.ACK:
                    Debug.WriteLine("ws://" + packet.ServerAddress + ":8081");

                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                        //replace with a connect function
                        serverDatagramSocket.Dispose();

                        ds = new WebBrowserDataSource();
                        ds.DataRecived += (s, o) =>
                        {
                            test.Source = ConvertToBitmapImage(o).Result;
                            // ds.ACKRender();
                        };
                        ConnectPage.Visibility = Visibility.Collapsed;
                        DiscoveryPage.Visibility = Visibility.Collapsed;
                        urlBar.Visibility = Visibility.Visible;
                        ds.StartRecive("ws://" + packet.ServerAddress + ":8081");
                        
                    });
                    break;
                default:
                    break;
            }
        }
    }
}
