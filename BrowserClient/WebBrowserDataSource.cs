using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using Windows.UI.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace BrowserClient
{
    public class WebBrowserDataSource
    {
        ClientWebSocket sock;
        public event EventHandler<string> JSONRecived;
        public event EventHandler<BitmapImage> FrameRecived;
        public event EventHandler<TextPacket> TextPacketRecived;
        public async void StartRecive(string addr)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Create a simple setting.
            localSettings.Values["LastServerUrl"] = addr ;

            sock = new ClientWebSocket();

            await sock.ConnectAsync(new Uri(addr), CancellationToken.None);

            //2mb should be enough
            ArraySegment<byte> readbuffer = new ArraySegment<byte>(new byte[2000000]);
            
            while (sock.State == WebSocketState.Open)
            {
                //  ArraySegment<byte> readbuffer = new ArraySegment<byte>(new byte[2000000]);
                // Array.Clear(readbuffer.Array, 0, 2000000-1);
                Array.Clear(readbuffer.Array, 0, readbuffer.Array.Length);

                var res = await sock.ReceiveAsync(readbuffer, CancellationToken.None);

                switch (res.MessageType)
                {
                    case WebSocketMessageType.Binary:
                        FrameRecived?.Invoke(this, ConvertToBitmapImage(readbuffer.Array).Result);
                       
                        break;
                    case WebSocketMessageType.Close:

                        break;
                    case WebSocketMessageType.Text:
                        //text packet
                        try
                        {
                            TextPacketRecived?.Invoke(this, JsonConvert.DeserializeObject<TextPacket>(System.Text.Encoding.UTF8.GetString(readbuffer.Array)));
                        }
                        catch (Exception)
                        {

                           // throw;
                        }
                       
                        break;
                    default:
                        break;
                }
            }
        }

        public async void Navigate(string s)
        {
            var cp = new CommPacket();
            cp.PType = PacketType.Navigation;
            cp.JSONData = s;
            string PacketJSON = JsonConvert.SerializeObject(cp);
            var encoded = Encoding.UTF8.GetBytes(PacketJSON);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async void NavigateForward()
        {
            if (sock.State != WebSocketState.Open)
                return;

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new CommPacket
            {
                PType = PacketType.NavigateForward
            }));

            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async void NavigateBack()
        {
            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new CommPacket
            {
                PType = PacketType.NavigateBack
            }));

            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async void SendKey(Windows.UI.Xaml.Input.KeyRoutedEventArgs key)
        {

           
            if (sock.State != WebSocketState.Open)
                return;

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new CommPacket
            {
                PType = PacketType.SendKey,
                JSONData = JsonConvert.SerializeObject(key.Key)
            }));

            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async void SizeChange(Windows.Foundation.Size newSize)
        {

            if (sock.State != WebSocketState.Open)
                return;

            var cp = new CommPacket();
            cp.PType = PacketType.SizeChange;
            cp.JSONData = JsonConvert.SerializeObject(newSize);

            string PacketJSON = JsonConvert.SerializeObject(cp);
            var encoded = Encoding.UTF8.GetBytes(PacketJSON);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async void TouchDown(Point p, uint pointerId)
        {

            if (sock.State != WebSocketState.Open)
                return;


            var cp = new CommPacket
            {
                PType = PacketType.TouchDown,
                JSONData = JsonConvert.SerializeObject(new PointerPacket
                {
                    px = p.X,
                    py = p.Y,
                    id = pointerId
                })
            };

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cp));
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);


        }
        public async void TouchUp(Point p, uint pointerId)
        {

            if (sock.State != WebSocketState.Open)
                return;

            var cp = new CommPacket
            {
                PType = PacketType.TouchUp,
                JSONData = JsonConvert.SerializeObject(new PointerPacket
                {
                    px = p.X,
                    py = p.Y,
                    id = pointerId
                })
            };

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cp));
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);


        }


        public async void SendText(string text)
        {
            if (sock.State != WebSocketState.Open)
                return;

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new CommPacket {
                PType = PacketType.TextInputSend,
                JSONData = text
            }));

            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async void TouchMove(Point p, uint pointerId)
        {

            if (sock.State != WebSocketState.Open)
                return;


            var cp = new CommPacket
            {
                PType = PacketType.TouchMoved,
                JSONData = JsonConvert.SerializeObject(new PointerPacket
                {
                    px = p.X,
                    py = p.Y,
                    id = pointerId
                })
            };

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cp));
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            
        }

        public async void ACKRender()
        {
            if (sock.State != WebSocketState.Open)
                return;


            var cp = new CommPacket
            {
                PType = PacketType.ACK
            };

            var encoded = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cp));
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
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
    }
}
