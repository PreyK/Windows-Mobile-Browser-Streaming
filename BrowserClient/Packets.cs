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
