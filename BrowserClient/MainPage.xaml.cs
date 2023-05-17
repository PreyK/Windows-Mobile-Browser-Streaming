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
using Windows.Storage;

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
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("LastServerUrl"))
            {
                Debug.WriteLine("Has key");
                Debug.WriteLine(localSettings.Values["LastServerUrl"] as string);
                serverAddress.Text = localSettings.Values["LastServerUrl"] as string;
            }
            else
            {
                Debug.WriteLine("No known server");
            }

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
                var url = urlField.Text;
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
           
        }

        private void Test_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var x = e.GetCurrentPoint(null).Position.X / ScaleRect.ActualWidth;
            var y = e.GetCurrentPoint(null).Position.Y / ScaleRect.ActualHeight;
            ds.TouchDown(new Point(x, y), e.Pointer.PointerId);
        }

        private void Test_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var x = e.GetCurrentPoint(null).Position.X / ScaleRect.ActualWidth;
            var y = e.GetCurrentPoint(null).Position.Y / ScaleRect.ActualHeight;
            ds.TouchUp(new Point(x, y), e.Pointer.PointerId);
        }

        private void Test_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var x = e.GetCurrentPoint(null).Position.X / ScaleRect.ActualWidth;
            var y = e.GetCurrentPoint(null).Position.Y / ScaleRect.ActualHeight;
            ds.TouchMove(new Point(x, y), e.Pointer.PointerId);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Connect(serverAddress.Text.Replace("tcp://", "ws://"));
            ConnectPage.Visibility = Visibility.Collapsed;
        }

        public void Connect(string endpoint)
        {
            ds = new WebBrowserDataSource();
            ds.FrameRecived += (s, o) =>
            {
                test.Source = o;
            };
            ds.StartRecive(endpoint);
            ds.TextPacketRecived += (s, o) =>
            {
                switch (o.PType)
                {
                    case TextPacketType.NavigatedUrl:
                        urlField.Text = o.text;
                        //hack to get proper size on first launch
                        ds.SizeChange(new Size { Width = ScaleRect.ActualWidth, Height = ScaleRect.ActualHeight });
                        break;

                    case TextPacketType.TextInputContent:
                        NavbarGrid.Visibility = Visibility.Collapsed;
                        TextInput.Visibility = Visibility.Visible;
                        websiteTextBox.Text = o.text;
                        websiteTextBox.Select(websiteTextBox.Text.Length, 0);
                        websiteTextBox.Focus(FocusState.Programmatic);
                        break;

                    case TextPacketType.TextInputSend:
                        break;

                    case TextPacketType.TextInputCancel:
                        TextInput.Visibility = Visibility.Collapsed;
                        NavbarGrid.Visibility = Visibility.Visible;
                        websiteTextBox.Text = "";
                        break;
                }
            };            
        }

        private void WebsiteTextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            ds.SendKey(e);
         //   Debug.WriteLine(e.Key);

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ds.SendText(websiteTextBox.Text);
                TextInput.Visibility = Visibility.Collapsed;
                NavbarGrid.Visibility = Visibility.Visible;
                websiteTextBox.Text = "";

                e.Handled = true; LoseFocus(sender);
            }
        }
        private void SendText_Click(object sender, RoutedEventArgs e)
        {
            ds.SendText(websiteTextBox.Text);
            TextInput.Visibility = Visibility.Collapsed;
            NavbarGrid.Visibility = Visibility.Visible;
            websiteTextBox.Text = "";
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void Browser_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);
            Debug.WriteLine("AD"+ScaleRect.ActualWidth + " " + ScaleRect.ActualHeight);

            Debug.WriteLine("SIZE!!!!");
            if (ds != null)
            {
                var s = e.NewSize;
                s.Width = ScaleRect.ActualWidth;
                s.Height = ScaleRect.ActualHeight;

                ds.SizeChange(s);
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
                        UdpDiscoveryTimer.Dispose();
                        serverDatagramSocket.Dispose();


                        Connect("ws://" + packet.ServerAddress + ":8081");
                        /*
                        ds = new WebBrowserDataSource();
                        ds.DataRecived += (s, o) =>
                        {
                            test.Source = ConvertToBitmapImage(o).Result;
                            // ds.ACKRender();
                        };
                        */
                        ConnectPage.Visibility = Visibility.Collapsed;
                        DiscoveryPage.Visibility = Visibility.Collapsed;
                        NavbarGrid.Visibility = Visibility.Visible;
                       // ds.StartRecive("ws://" + packet.ServerAddress + ":8081");
                        
                    });
                    break;
                default:
                    break;
            }
        }

        private void NavigateBack_Click(object sender, RoutedEventArgs e)
        {
            ds.NavigateBack();
        }

        private void NavigateForward_Click(object sender, RoutedEventArgs e)
        {
            ds.NavigateForward();
        }
    }
}
