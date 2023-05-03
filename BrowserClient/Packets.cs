using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserClient
{
    public struct PointerPacket
    {
        public double px;
        public double py;
        public uint id;
    }

    public struct TextPacket
    {
        public TextPacketType PType;
        public string text;
    }

    public struct CommPacket
    {
        public PacketType PType;
        public string JSONData;
    }

    public enum TextPacketType
    {
        NavigatedUrl,
        TextInputContent,
        TextInputSend,
        TextInputCancel
    }

    public enum PacketType
    {
        Navigation,
        SizeChange,
        TouchDown,
        TouchUp,
        TouchMoved,
        ACK,
        Frame,
        TextInputSend,
        NavigateForward,
        NavigateBack
    }
    public struct DiscoveryPacket
    {
        public DiscoveryPacketType PType;
        public string ServerAddress;
    }
    public enum DiscoveryPacketType
    {
        AddressRequest,
        ACK
    }
}
