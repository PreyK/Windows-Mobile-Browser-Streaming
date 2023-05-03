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
using System.Net;
using System.Net.Sockets;
using System.Text;
using CefSharp.Enums;

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

                    case PacketType.TextInputSend:
                        Console.WriteLine(packet.JSONData);
                        var textscript = @"(function (){document.activeElement.value='"+packet.JSONData+"'})();";

                        var textres = browser.EvaluateScriptAsync(textscript).ContinueWith(t =>{
                            
                            browser.GetBrowserHost().SendKeyEvent(new KeyEvent {
                                WindowsKeyCode = 0x0D,
                                FocusOnEditableField = true,
                                IsSystemKey = false,
                                Type = KeyEventType.RawKeyDown
                            });
                           
                        });

                        break;


                    case PacketType.ACK:
                        Console.WriteLine("ACK");
                        //  browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, 70).ContinueWith(t => {
                        //     server.WebSocketServices.Broadcast(t.Result);
                        // });

                        break;
                    case PacketType.Navigation:


                        Console.WriteLine(NetworkManager.IsUrl(packet.JSONData));
                        if (NetworkManager.IsUrl(packet.JSONData))
                        {
                            browser.LoadUrl(packet.JSONData);
                        }
                        else
                        {
                            browser.LoadUrl("https://www.google.com/search?q=" + packet.JSONData);
                        }
                        /*
                        if (packet.JSONData.Contains("http") || packet.JSONData.Contains("https") || packet.JSONData.Contains("www") || packet.JSONData.Contains("chrome://") || packet.JSONData.Contains(".com"))
                        {
                            browser.LoadUrl(packet.JSONData);
                        }
                        else
                        {
                            browser.LoadUrl("https://www.google.com/search?q=" + packet.JSONData);
                        }
                       
                        */

                        break;

                    case PacketType.NavigateBack:
                        if (browser.CanGoBack) browser.Back();
                        break;
                    case PacketType.NavigateForward:
                        if (browser.CanGoForward) browser.Forward();
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
            const string testUrl = "https://www.google.com/";
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
            browser.LoadingStateChanged += Browser_LoadingStateChanged;
            browser.RenderHandler = new TestRHI(browser);
            //browser.
            //browser.RenderProcessMessageHandler = new RenderProcessMessageHandler();
            //  browser.Paint += CefPaint;


            Console.Clear();
            Console.WriteLine("Browser server is now running, you can connect to it via ws://" + NetworkManager.GetLocalIPAddress() + ":8081");
            Console.WriteLine("Or click the Discovery button in the UWP app to autimatically find the server on your local network");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Alternatively you can set up ngrok to acess the server over internet, to do this follow the steps below");
            Console.WriteLine("1. Set up a ngrok account at https://ngrok.com/");
            Console.WriteLine("2. download ngrok (it's just one self-contained .exe file)");
            Console.WriteLine("3. open a command prompt (cmd.exe) in the location where you have the ngrok.exe file");
            Console.WriteLine("4. open https://dashboard.ngrok.com/get-started/setup and run the command with your ngrok auth token under section 2. Connect your account");
            Console.WriteLine("5. you should get back \"Authtoken saved to configuration file\"");
            Console.WriteLine("6. run the following command: ngrok tcp 8081");
            Console.WriteLine("7. you'll need the url starting with tcp://");
            Console.WriteLine("8. enter the url in the UWP application as the server adress and connect.");
            Console.WriteLine("9. congratulations! you just connected over the internet");
            //Console.WriteLine("3. add your ngrok auth-token https://dashboard.ngrok.com/get-started/setup");
            //Console.WriteLine("for example in your command ");

            NetworkManager.StartUdpDiscoveryServer();
            var timer = new Timer(Callback, null, 0, 50);

            //Dispose the timer


            Console.ReadKey();
            Cef.Shutdown();
            timer.Dispose();
        }

        private static void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading)
            {
                var cp = new TextPacket
                {
                    PType = TextPacketType.NavigatedUrl,
                    text = browser.Address
                };
                server.WebSocketServices.Broadcast(JsonConvert.SerializeObject(cp));
            }
        }

        //todo: some smarter connection
        //client ACK the image and sends a req for the next.


        static void Callback(object state)
        {
            try
            {

                browser.CaptureScreenshotAsync(CefSharp.DevTools.Page.CaptureScreenshotFormat.Jpeg, 70).ContinueWith(t =>
                {
                    /*
                    var cp = new CommPacket
                    {
                        PType = PacketType.Frame,
                        rawData = t.Result
                    };
                    var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cp));
                    server.WebSocketServices.Broadcast(encoded);
                    */
                    server.WebSocketServices.Broadcast(t.Result);
                    //Console.WriteLine("broadcast2");
                }
                );
            }
            catch (Exception)
            {
                throw;
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


        //TODO: accelerated Draw
        // Forward the renderbuffer from here instead of screenshot?
        public class TestRHI : DefaultRenderHandler
        {
            private ChromiumWebBrowser browser;

            public TestRHI(ChromiumWebBrowser browser) : base(browser)
            {
                this.browser = browser;
            }

            public override void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
            {
                base.OnVirtualKeyboardRequested(browser, inputMode);


                Console.WriteLine("Virtual Keyboard Requested for "+inputMode);
                if (inputMode == TextInputMode.None)
                {
                    server.WebSocketServices.Broadcast(JsonConvert.SerializeObject(new TextPacket
                    {
                        PType = TextPacketType.TextInputCancel
                    }));
                }
                else
                {
                    var response = browser.EvaluateScriptAsync(JavascriptFunctions.GetActiveElementText).ContinueWith(t =>
                    {

                        https://learn.microsoft.com/hu-hu/windows/win32/api/winuser/nf-winuser-vkkeyscanexa?redirectedfrom=MSDN

                        /*
                        Console.WriteLine((string)t.Result.Result);
                        this.browser.GetBrowserHost().SendKeyEvent(new KeyEvent
                        {
                            WindowsKeyCode = 0x41,
                            FocusOnEditableField = false,
                            IsSystemKey = false,
                            Type = KeyEventType.Char
                        });
                        Console.WriteLine("sent key");
                        */
                        server.WebSocketServices.Broadcast(JsonConvert.SerializeObject(new TextPacket
                        {
                            PType = TextPacketType.TextInputContent,
                            text = (string)t.Result.Result
                        }));
                    });
                }
            }
        }

    }
}
