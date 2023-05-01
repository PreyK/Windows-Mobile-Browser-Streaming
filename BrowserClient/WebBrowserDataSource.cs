﻿using System;
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

namespace BrowserClient
{
    public class WebBrowserDataSource
    {
        ClientWebSocket sock;
        public event EventHandler<byte[]> DataRecived;
        public async void StartRecive(string addr)
        {
             sock = new ClientWebSocket();

            await sock.ConnectAsync(new Uri(addr), CancellationToken.None);

            //5mb should be enough
            ArraySegment<byte> readbuffer = new ArraySegment<byte>(new byte[5000000]);
            while (sock.State == WebSocketState.Open)
            {
                var res = await sock.ReceiveAsync(readbuffer, CancellationToken.None);
                DataRecived?.Invoke(this, readbuffer.Array);
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

        public async void TouchDown(Point p, int pointerId)
        {

            if (sock.State != WebSocketState.Open)
                return;

            var cp = new CommPacket();
            cp.PType = PacketType.TouchDown;
            cp.JSONData = JsonConvert.SerializeObject(p);

            string PacketJSON = JsonConvert.SerializeObject(cp);
            var encoded = Encoding.UTF8.GetBytes(PacketJSON);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async void TouchUp(Point p, int pointerId)
        {

            if (sock.State != WebSocketState.Open)
                return;

            var cp = new CommPacket();
            cp.PType = PacketType.TouchUp;
            cp.JSONData = JsonConvert.SerializeObject(p);

            string PacketJSON = JsonConvert.SerializeObject(cp);
            var encoded = Encoding.UTF8.GetBytes(PacketJSON);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async void TouchMove(Point p, int pointerId)
        {

            if (sock.State != WebSocketState.Open)
                return;

            var cp = new CommPacket();
            cp.PType = PacketType.TouchMoved;
            cp.JSONData = JsonConvert.SerializeObject(p);

            string PacketJSON = JsonConvert.SerializeObject(cp);
            var encoded = Encoding.UTF8.GetBytes(PacketJSON);
            var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
            await sock.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }


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
}