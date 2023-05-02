using System.Text;
using System;
using System.Net;
using System.Diagnostics;
#if UNITY_EDITOR
using System.Net.Sockets;
using System.Threading;
#else
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.IO;
using Windows.Networking;
#endif


#if UNITY_EDITOR

public class BroadcastReceiver : IDisposable
{
    //OnMessageReceived
    public delegate void AddOnMessageReceivedDelegate(string message, IPEndPoint remoteEndpoint);
    public event AddOnMessageReceivedDelegate MessageReceived;
    private void OnMessageReceivedEvent(string message, IPEndPoint remoteEndpoint)
    {
        if (MessageReceived != null)
            MessageReceived(message, remoteEndpoint);
    }

    private Thread _ReadThread;
    private UdpClient _Socket;


    public void Receive(int port)
    {
        // create thread for reading UDP messages
        _ReadThread = new Thread(new ThreadStart(delegate
        {
            try
            {
                _Socket = new UdpClient(port);
                Debug.LogFormat("Receiving on port {0}", port);
            }
            catch (Exception err)
            {
                Debug.LogError(err.ToString());
                return;
            }
            while (true)
            {
                try
                {
                    // receive bytes
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _Socket.Receive(ref anyIP);

                    // encode UTF8-coded bytes to text format
                    string message = Encoding.UTF8.GetString(data);
                    OnMessageReceivedEvent(message, anyIP);
                }
                catch (Exception err)
                {
                    Debug.LogError(err.ToString());
                }
            }
        }));
        _ReadThread.IsBackground = true;
        _ReadThread.Start();
    }

    public void Dispose()
    {
        if (_ReadThread.IsAlive)
        {
            _ReadThread.Abort();
        }
        if (_Socket != null)
        {
            _Socket.Close();
            _Socket = null;
        }
    }
}

#else

public class BroadcastReceiver : IDisposable
{
    //OnMessageReceived
    public delegate void AddOnMessageReceivedDelegate(string message, IPEndPoint remoteEndpoint);
    public event AddOnMessageReceivedDelegate MessageReceived;
    private void OnMessageReceivedEvent(string message, IPEndPoint remoteEndpoint)
    {
        if (MessageReceived != null)
            MessageReceived(message, remoteEndpoint);
    }


    DatagramSocket _Socket = null;

    public async void Receive(int port)
    {
        string portStr = port.ToString();
        // start the client
        try
        {
            _Socket = new DatagramSocket();
            _Socket.MessageReceived += _Socket_MessageReceived;

            await _Socket.BindServiceNameAsync(portStr);

            //await _Socket.BindEndpointAsync(null, portStr);

            //await _Socket.ConnectAsync(new HostName("255.255.255.255"), portStr.ToString());


            //HostName hostname = Windows.Networking.Connectivity.NetworkInformation.GetHostNames().FirstOrDefault();
            //var ep = new EndpointPair(hostname, portStr, new HostName("255.255.255.255"), portStr);
            //await _Client.ConnectAsync(ep);

            Debug.WriteLine(string.Format("Receiving on {0}", portStr));

            await Task.Delay(3000);
            // send out a message, otherwise receiving does not work ?!
            var outputStream = await _Socket.GetOutputStreamAsync(new HostName("255.255.255.255"), portStr);
            DataWriter writer = new DataWriter(outputStream);
            writer.WriteString("Hello World!");
            await writer.StoreAsync();
        }
        catch (Exception ex)
        {
            _Socket.Dispose();
            _Socket = null;
            Debug.WriteLine(ex.ToString());
            Debug.WriteLine(Windows.Networking.Sockets.SocketError.GetStatus(ex.HResult).ToString());
        }
    }

    private async void _Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        try
        {
            Stream streamIn = args.GetDataStream().AsStreamForRead();
            StreamReader reader = new StreamReader(streamIn, Encoding.UTF8);

            string message = await reader.ReadLineAsync();
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Parse(args.RemoteAddress.RawName), Convert.ToInt32(args.RemotePort));
            OnMessageReceivedEvent(message, remoteEndpoint);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public void Dispose()
    {
        if (_Socket != null)
        {
            _Socket.Dispose();
            _Socket = null;
        }
    }
}

#endif