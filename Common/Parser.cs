using System;
using System.Collections.Generic;
using System.Text;

namespace T05.CatchMind.Common
{
    public class PacketParser
    {
        private static byte[] _buffer = new byte[256];

        public static Packet Parse(List<byte> bytes, int offset = 0)
        {
            if (bytes == null || bytes.Count < offset + 4)
                return null;
            byte length = bytes[offset + 3];
            if (bytes.Count < offset + 4 + length)
                return null;
            bytes.CopyTo(offset, _buffer, 0, 4 + length);
            return Parse(_buffer);
        }

        public static Packet Parse(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return null;
            PacketType type = (PacketType)bytes[2];

            Packet packet = null;
            switch (type)
            {
                case PacketType.Text:
                    packet = new TextPacket();
                    break;
                case PacketType.Draw:
                    packet = new DrawPacket();
                    break;
                case PacketType.PlayerList:
                    packet = new PlayerListPacket();
                    break;
                case PacketType.Ready:
                    packet = new ReadyPacket();
                    break;
                default:
                    packet = new Packet();
                    break;
            }

            if (packet != null)
                packet.Parse(bytes);

            return packet;
        }
    }
}
