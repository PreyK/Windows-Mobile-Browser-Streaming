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
using NgrokApi;
using System.Threading;

namespace BrowserServer
{
    class Program
    {
        static ChromiumWebBrowser browser;
        public class test : WebSocketBehavior
        {
            //tested on 950XL
            public static int ScalingFactor = 2;

            protected override void OnOpen()
            {
                browser.Reload();
            }
            protected override void OnMessage(MessageEventArgs e)
            {
                var packet = JsonConvert.DeserializeObject<CommPacket>(e.Data);
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

                        browser.Size = new System.Drawing.Size(w*ScalingFactor, h*ScalingFactor);

                        Console.WriteLine("windows resized" +w + " " + h);


                        break;

                    case PacketType.TouchDown:

                        var t_down = JsonConvert.DeserializeObject<PointerPacket>(packet.JSONData);
                        var press = new TouchEvent()
                        {
                            Id = (int)0,
                            X = (float)t_down.px * browser.Size.Width,
                            Y = (float)t_down.py * browser.Size.Height,
                            PointerType = CefSharp.Enums.PointerType.Touch,
                            Pressure = 0,
                            Type = CefSharp.Enums.TouchEventType.Pressed,
                        };
                        browser.GetBrowser().GetHost().SendTouchEvent(press);
                        break;

                    case PacketType.TouchUp:


                        var t_up = JsonConvert.DeserializeObject<PointerPacket>(packet.JSONData);
                        var up = new TouchEvent()
                        {
                            Id = (int)0,
                            X = (float)t_up.px * browser.Size.Width,
                            Y = (float)t_up.py * browser.Size.Height,
                            PointerType = CefSharp.Enums.PointerType.Touch,
                            Pressure = 0,
                            Type = CefSharp.Enums.TouchEventType.Released,
                        };
                        browser.GetBrowser().GetHost().SendTouchEvent(up);
                        break;
                    case PacketType.TouchMoved:

                        var t_move = JsonConvert.DeserializeObject<PointerPacket>(packet.JSONData);
                        var move = new TouchEvent()
                        {
                            Id = (int)0,
                            X = (float)t_move.px * browser.Size.Width,
                            Y = (float)t_move.py * browser.Size.Height,
                            PointerType = CefSharp.Enums.PointerType.Touch,
                            Pressure = 0,
                            Type = CefSharp.Enums.TouchEventType.Moved,
                        };
                        browser.GetBrowser().GetHost().SendTouchEvent(move);
                        break;

                    default:
                        break;
                }
            }
        }
        static WebSocketServer server;
        static IFrame mainFrame;
        static void Main(string[] margs)
        {
            server = new WebSocketServer("ws://0.0.0.0:8081");
            //ngrok compatible ngrok.exe tcp 8081 -> 
            server.AllowForwardedRequest = true;
            server.AddWebSocketService<test>("/");
            server.Start();

            //var ngrok = new Ngrok("");



            //https://www.snappymaria.com/misc/TouchEventTest.html
            const string testUrl = "https://www.snappymaria.com/misc/TouchEventTest.html";
            var settings = new CefSettings()
            {
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
            };
            
            settings.CefCommandLineArgs["touch-events"] = "enabled";
            settings.LogSeverity = LogSeverity.Disable;
            settings.MultiThreadedMessageLoop = true;

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            browser = new ChromiumWebBrowser(testUrl);

            browser.Size = new System.Drawing.Size(1440 / 2, 1248);
            //browser.Paint += CefPaint;


            Console.Clear();
            Console.WriteLine("Browser server is now running, you can connect to it via ws://");

            var timer = new Timer(Callback, null, 0, 50);

            //Dispose the timer
           

            Console.ReadKey();
            Cef.Shutdown();
            timer.Dispose();
        }
        //todo: some smarter connection
        //client ACK the image and sends a req for the next.
        static void Callback(object state)
        {
           // Console.WriteLine("33ms");
            //Your code here.
            browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, 70).ContinueWith(t => {
                server.WebSocketServices.Broadcast(t.Result);
                /*
                using (FileStream fs = new FileStream(@"D:\\" + DateTime.Now.Ticks + ".jpg", FileMode.Create, FileAccess.ReadWrite))
                {
                    byte[] b = t.Result;
                    fs.Write(b, 0, b.Length);
                }
                */

            }
            );
        }

        static int frameNum = 0;
        private static void CefPaint(object sender, OnPaintEventArgs e)
        {
            frameNum++;
            Console.WriteLine("RENDER FRAME"+frameNum.ToString());

            var browserImage = new Bitmap(e.Width, e.Height, 4 * e.Width, System.Drawing.Imaging.PixelFormat.Format32bppRgb, e.BufferHandle);
            byte[] bufferBytes;
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 10L);
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
    }

    public struct PointerPacket
    {
        public double px;
        public double py;
        public uint id;
    }

    public struct CommPacket
    {
        public PacketType PType;
        //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string JSONData;
        //public byte[] rawData;
    }
    public enum PacketType
    {
        Navigation,
        SizeChange,
        TouchDown,
        TouchUp,
        TouchMoved
    }

    /*
      public static System.Timers.Timer SetInterval(Action Act, int Interval)
      {
          System.Timers.Timer tmr = new System.Timers.Timer();
          tmr.Elapsed += (sender, args) => Act();
          tmr.AutoReset = true;
          tmr.Interval = Interval;
          tmr.Start();

          return tmr;
      }
      */
}
