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
namespace BrowserClient
{
    public sealed partial class MainPage : Page
    {
        WebBrowserDataSource ds;

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
           // Debug.WriteLine("sizechange");
        }

        private void Page_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            if(ds!=null)
            ds.SizeChange(e.NewSize);

            Debug.WriteLine("sizechange"+e.NewSize);
        }

        private void Test_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
           
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);

            //var p =  e.GetCurrentPoint(test);
            //e.GetCurrentPoint(null)



            
            //e.GetCurrentPoint(null)
            var x = e.GetCurrentPoint(null).Position.X/bounds.Width;
            var y = e.GetCurrentPoint(null).Position.Y/bounds.Height;




            //e.poi
          //  Debug.WriteLine( x* (size.Width/2) +" "+y*   (size.Height/2)  );
         

            ds.TouchDown(new Point(x, y), e.Pointer.PointerId);
        }

        private void Test_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);

            //var p =  e.GetCurrentPoint(test);
            //e.GetCurrentPoint(null)




            //e.GetCurrentPoint(null)
            var x = e.GetCurrentPoint(null).Position.X / bounds.Width;
            var y = e.GetCurrentPoint(null).Position.Y / bounds.Height;




            //e.poi
           // Debug.WriteLine(x * size.Width + " " + y * size.Width);

            ds.TouchUp(new Point(x, y), e.Pointer.PointerId);
        }

        private void Test_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(bounds.Width * scaleFactor, bounds.Height * scaleFactor);

            //var p =  e.GetCurrentPoint(test);
            //e.GetCurrentPoint(null)




            //e.GetCurrentPoint(null)
            var x = e.GetCurrentPoint(null).Position.X / bounds.Width;
            var y = e.GetCurrentPoint(null).Position.Y / bounds.Height;




            //e.poi
            //Debug.WriteLine(x+" "+y);
            ds.TouchMove(new Point(x, y), e.Pointer.PointerId);

         //   ds.TouchMove(new Point(x * (size.Width / 2), y * (size.Height / 2)), 0);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ds = new WebBrowserDataSource();
            ds.DataRecived += (s, o) =>
            {
                test.Source = ConvertToBitmapImage(o).Result;
               // ds.ACKRender();
            };
            ds.StartRecive(serverAddress.Text);
            connectRect.Visibility = Visibility.Collapsed;
            serverAddress.Visibility = Visibility.Collapsed;
            connectBtn.Visibility = Visibility.Collapsed;
        }
    }
}
