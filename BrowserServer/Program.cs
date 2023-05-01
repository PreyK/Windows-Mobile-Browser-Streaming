using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Drawing;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Server;
using CefSharp.Structs;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;

namespace BrowserServer
{
    class Program
    {
        static ChromiumWebBrowser browser;
        public class test : WebSocketBehavior
        {

            protected override void OnOpen()
            {
                browser.Reload();
            }
            protected override void OnMessage(MessageEventArgs e)
            {
                var packet = JsonConvert.DeserializeObject<CommPacket>(e.Data);

                //var packet = FromBytes<CommPacket>(e.RawData);
                switch (packet.PType)
                {
                    case PacketType.Navigation:
                        if (packet.JSONData.Contains("http") || packet.JSONData.Contains("https") || packet.JSONData.Contains("www") || packet.JSONData.Contains("chrome://") || packet.JSONData.Contains(".com"))
                        {
                            browser.LoadUrl(packet.JSONData);
                        }
                        else
                        {
                            browser.LoadUrl("https://www.google.com/search?q=" + packet.JSONData);
                        }
                        break;
                    case PacketType.SizeChange:
                        var jsonObject = JObject.Parse(packet.JSONData);
                        var w = jsonObject.Value<int>("Width");
                        var h = jsonObject.Value<int>("Height");
                        break;

                    case PacketType.TouchDown:
                        var td_o = JObject.Parse(packet.JSONData);
                        // Console.WriteLine(packet.JSONData);
                        var press = new TouchEvent()
                        {
                            Id = 0,
                            X = (float)td_o.Value<float>("X"),
                            Y = (float)td_o.Value<float>("Y"),
                            PointerType = CefSharp.Enums.PointerType.Touch,
                            Pressure = 0,
                            Type = CefSharp.Enums.TouchEventType.Pressed,

                        };

                        browser.GetBrowser().GetHost().SendTouchEvent(press);
                        break;

                    case PacketType.TouchUp:
                        var tu_o = JObject.Parse(packet.JSONData);
                        //  Console.WriteLine(packet.JSONData);
                        var tu_press = new TouchEvent()
                        {
                            Id = 0,
                            X = (float)tu_o.Value<float>("X"),
                            Y = (float)tu_o.Value<float>("Y"),
                            PointerType = CefSharp.Enums.PointerType.Touch,
                            Pressure = 0,
                            Type = CefSharp.Enums.TouchEventType.Released,

                        };

                        browser.GetBrowser().GetHost().SendTouchEvent(tu_press);
                        break;


                    case PacketType.TouchMoved:
                        var tm_o = JObject.Parse(packet.JSONData);
                        //    Console.WriteLine(packet.JSONData);
                        var m_press = new TouchEvent()
                        {
                            Id = 0,
                            X = (float)tm_o.Value<float>("X"),
                            Y = (float)tm_o.Value<float>("Y"),
                            PointerType = CefSharp.Enums.PointerType.Touch,
                            Pressure = 0,
                            Type = CefSharp.Enums.TouchEventType.Moved,

                        };

                        browser.GetBrowser().GetHost().SendTouchEvent(m_press);
                        break;

                    default:
                        break;
                }
                /*
                Console.WriteLine(e.Data);
                //Send(e.Data);
                if (e.Data.Contains("http") || e.Data.Contains("https") || e.Data.Contains("www") || e.Data.Contains(".com"))
                {
                    browser.LoadUrl(e.Data);
                }
                else{
                    browser.LoadUrl("https://www.google.com/search?q=" + e.Data);
                }
                */
            }
        }

        public static System.Timers.Timer SetInterval(Action Act, int Interval)
        {
            System.Timers.Timer tmr = new System.Timers.Timer();
            tmr.Elapsed += (sender, args) => Act();
            tmr.AutoReset = true;
            tmr.Interval = Interval;
            tmr.Start();

            return tmr;
        }


        static WebSocketServer server;
        static IFrame mainFrame;
        static void Main(string[] margs)
        {


            server = new WebSocketServer("ws://0.0.0.0:8081");
            server.AddWebSocketService<test>("/");
            server.Start();


            //https://www.snappymaria.com/misc/TouchEventTest.html
            const string testUrl = "https://www.snappymaria.com/misc/TouchEventTest.html";
            var settings = new CefSettings()
            {
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
                // BrowserSubprocessPath = "CefSharp.BrowserSubprocess.exe",

            };
            settings.CefCommandLineArgs["touch-events"] = "enabled";

            //browser.GetBrowser().GetHost().SendMouseClickEvent()



            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            browser = new ChromiumWebBrowser(testUrl);
            browser.Size = new System.Drawing.Size(1440 / 2, 1248);
            browser.Paint += CefPaint;

            //Wait for the MainFrame to finish loading
            browser.FrameLoadEnd += (sender, args) =>
            {
                //Wait for the MainFrame to finish loading
                //  if (args.Frame.IsMain)
                //  {
                //       args.Frame.ExecuteJavaScriptAsync("alert('MainFrame finished loading');");
                //  }
            };
            /*
            SetInterval(async delegate {

                var data = await browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg);
                if(server!=null)
                 server.WebSocketServices.Broadcast(data);


            }, 33);
            */

            Console.ReadKey();
            Cef.Shutdown();
        }
        private static void CefPaint(object sender, OnPaintEventArgs e)
        {
            /*

            var press = new TouchEvent()
            {
                Id = 0,
                X = (float)(1440 / 2) / 2,
                Y = (float)(2560 / 2) / 2,
                PointerType = CefSharp.Enums.PointerType.Touch,
                Pressure = 0,
                Type = CefSharp.Enums.TouchEventType.Pressed,

            };

            browser.GetBrowser().GetHost().SendTouchEvent(press);
            */

            var browserImage = new Bitmap(e.Width, e.Height, 4 * e.Width, System.Drawing.Imaging.PixelFormat.Format32bppRgb, e.BufferHandle);

            byte[] bufferBytes;

            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);
            //  bmp.Save(filePathAndName, GetEncoder(ImageFormat.Jpeg), encoderParameters);
            /*
              using (MemoryStream stream = new MemoryStream())
              {
                  browserImage.Save(stream, GetEncoder(ImageFormat.Jpeg), encoderParameters);
                  bufferBytes = stream.ToArray();
              }*/


            using (MemoryStream stream = new MemoryStream())
            {
                browserImage.Save(stream, GetEncoder(ImageFormat.Jpeg), encoderParameters);
                bufferBytes = stream.ToArray();
            }


            server.WebSocketServices.Broadcast(bufferBytes);


        }
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static byte[] GetBytes(CommPacket str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        public static T FromBytes<T>(byte[] arr) where T : struct
        {
            T str = default(T);

            GCHandle h = default(GCHandle);

            try
            {
                h = GCHandle.Alloc(arr, GCHandleType.Pinned);

                str = Marshal.PtrToStructure<T>(h.AddrOfPinnedObject());

            }
            finally
            {
                if (h.IsAllocated)
                {
                    h.Free();
                }
            }

            return str;
        }


    }

    public struct CommPacket
    {
        public PacketType PType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string JSONData;
        public byte[] rawData;
    }
    public enum PacketType
    {
        Navigation,
        SizeChange,
        TouchDown,
        TouchUp,
        TouchMoved
    }
}
