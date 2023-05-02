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
using System.Collections;
using System.Linq;

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
                //send first frame
                // browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, 70).ContinueWith(t => {
                //     server.WebSocketServices.Broadcast(t.Result);
                //  }
                //  );

            }
            protected override void OnMessage(MessageEventArgs e)
            {
                var packet = JsonConvert.DeserializeObject<CommPacket>(e.Data);
                switch (packet.PType)
                {

                    case PacketType.ACK:
                        Console.WriteLine("ACK");
                        //  browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, 70).ContinueWith(t => {
                        //     server.WebSocketServices.Broadcast(t.Result);
                        // });

                        break;
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

                        browser.Size = new System.Drawing.Size(w * ScalingFactor, h * ScalingFactor);

                        Console.WriteLine("windows resized" + w + " " + h);


                        break;


                        //stale multitouch, track touches on cliend only and forward them....
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

                        const string script =
                 @"(function ()
                    {
                        var isText = false;
                        var activeElement = document.activeElement;
                        if (activeElement) {
                            if (activeElement.tagName.toLowerCase() === 'textarea') {
                                isText = true;
                            } else {
                                if (activeElement.tagName.toLowerCase() === 'input') {
                                    if (activeElement.hasAttribute('type')) {
                                        var inputType = activeElement.getAttribute('type').toLowerCase();
                                        if (inputType === 'text' || inputType === 'email' || inputType === 'password' || inputType === 'tel' || inputType === 'number' || inputType === 'range' || inputType === 'search' || inputType === 'url') {
                                            isText = true;
                                        }
                                    }
                                }
                            }
                        }
                        if(isText){

                        }
                        return isText;
                    })();";

                        var response = browser.EvaluateScriptAsync(script).ContinueWith(t =>
                        {
                         

                            
                                Console.WriteLine(t.Result.Result.ToString());
                           
                        });




                       

                       

                        /*

                        // var result = browser.EvaluateScriptAsync("(() => { var element = document.activeElement; return element.localName; })();").ContinueWith(t =>
                        var result = browser.EvaluateScriptAsync("return 'asd';").ContinueWith(t =>
                        {

                            string[] arr = ((IEnumerable)t.Result.Result).Cast<object>()
                                .Select(c => c.ToString())
                                .ToArray();

                            //Console.WriteLine(t.Result.Result);
                            //server.WebSocketServices.Broadcast();
                        }
                        );

    */

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
                       // Console.WriteLine(move.Id);
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

            //https://gist.github.com/jankurianski/f3419d4580517516c24b?

            settings.CefCommandLineArgs["touch-events"] = "enabled";
            settings.LogSeverity = LogSeverity.Disable;
            settings.MultiThreadedMessageLoop = true;
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            browser = new ChromiumWebBrowser(testUrl);
            browser.Size = new System.Drawing.Size(1440 / 2, 1248);
            browser.RenderProcessMessageHandler = new RenderProcessMessageHandler();
            //  browser.Paint += CefPaint;


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
            try
            {
                browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, 70).ContinueWith(t =>
                {
                    server.WebSocketServices.Broadcast(t.Result);
                }
                );
            }
            catch (Exception)
            {
                //die silently
            }
        }

        static int frameNum = 0;
        private static void CefPaint(object sender, OnPaintEventArgs e)
        {
            frameNum++;
            // Console.WriteLine("RENDER FRAME"+frameNum.ToString());

            //var browserImage = new Bitmap(e.Width, e.Height, 4 * e.Width, System.Drawing.Imaging.PixelFormat.Format32bppRgb, e.BufferHandle);
            var browserImage = new Bitmap(e.Width, e.Height, 4 * e.Width, System.Drawing.Imaging.PixelFormat.Format32bppRgb, e.BufferHandle);
            byte[] bufferBytes;
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
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

    public class RenderProcessMessageHandler : IRenderProcessMessageHandler
    {
        // Wait for the underlying `Javascript Context` to be created, this is only called for the main frame.
        // If the page has no javascript, no context will be created.
        void IRenderProcessMessageHandler.OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            const string script = "document.addEventListener('DOMContentLoaded', function(){ alert('DomLoaded'); });";

            frame.ExecuteJavaScriptAsync(script);
        }

        void IRenderProcessMessageHandler.OnFocusedNodeChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IDomNode node)
        {
            var message = node == null ? "lost focus" : node.ToString();

            Console.WriteLine("OnFocusedNodeChanged() - " + message);
        }

        void IRenderProcessMessageHandler.OnContextReleased(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
            //The V8Context is about to be released, use this notification to cancel any long running tasks your might have
        }

        void IRenderProcessMessageHandler.OnUncaughtException(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, JavascriptException exception)
        {
            Console.WriteLine("OnUncaughtException() - " + exception.Message);
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
        TouchMoved,
        ACK
    }
}
