namespace T05.CatchMind.Common
{
    using System;
    using System.Text;
    using System.Net;

    public enum PacketType : byte
    {
        None,
        Ping,
        Text,
        Ready,
        Draw,
        Clear,
        PlayerList
    }

    public class Packet
    {
        // FF
        public byte header;
        // id (0:server)
        public byte userId;
        // packet type
        public PacketType type;
        // 0~255 (excepts header)
        public byte length;

        public Packet(byte newUserId = 0, PacketType newType = PacketType.None)
        {
            header = 0xFF;
            userId = newUserId;
            type = newType;
            length = 0;
        }

        public virtual byte[] GetBytes()
        {
            byte[] bytes = new byte[4];
            bytes[0] = header;
            bytes[1] = userId;
            bytes[2] = (byte)type;
            bytes[3] = length;
            return bytes;
        }

        public virtual void Parse(byte[] data)
        {
            int offset = 0;
            header = data[offset++];
            userId = data[offset++];
            type = (PacketType)data[offset++];
            length = data[offset++];
        }
    }

    /// <summary>
    /// 메시지 전송
    /// </summary>
    public class TextPacket : Packet
    {
        private byte[] _textBytes;
        private string _text;
        public string text
        {
            get { return _text; }
            set
            {
                _text = value;
                if (_text != null)
                {
                    _textBytes = Encoding.UTF8.GetBytes(_text);
                    length = (byte)_textBytes.Length;
                }
            }
        }

        public TextPacket(byte newUserId = 0) : base(newUserId, PacketType.Text)
        {
            text = "";
        }

        public override byte[] GetBytes()
        {
            byte[] header = base.GetBytes();

            byte[] combined = new byte[header.Length + _textBytes.Length];
            Array.Copy(header, combined, header.Length);
            Array.Copy(_textBytes, 0, combined, header.Length, _textBytes.Length);

            return combined;
        }

        public override void Parse(byte[] data)
        {
            base.Parse(data);
            _text = Encoding.UTF8.GetString(data, 4, length);
            _textBytes = Encoding.UTF8.GetBytes(_text);
        }
    }

    /// <summary>
    /// 그림 그리기
    /// </summary>
    public class DrawPacket : Packet
    {
        public ColorType color;
        public byte thickness;
        public Phase phase;
        public short x, y;

        public DrawPacket(byte newUserId = 0) : base(newUserId, PacketType.Draw)
        {
            length = 7;
        }

        public override byte[] GetBytes()
        {
            byte[] header = base.GetBytes();
            byte[] xB = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(x));
            byte[] yB = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(y));

            byte[] combined = new byte[header.Length + 3 + xB.Length + yB.Length];
            Array.Copy(header, combined, header.Length);
            combined[4] = (byte)color;
            combined[5] = thickness;
            combined[6] = (byte)phase;
            Array.Copy(xB, 0, combined, header.Length + 3, xB.Length);
            Array.Copy(yB, 0, combined, header.Length + 3 + xB.Length, yB.Length);

            return combined;
        }

        public override void Parse(byte[] data)
        {
            base.Parse(data);
            color = (ColorType)data[4];
            thickness = data[5];
            phase = (Phase)data[6];
            x = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 7));
            y = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 9));
        }
    }

    /// <summary>
    /// 플레이어 목록
    /// </summary>
    public class PlayerListPacket : Packet
    {
        public byte[] list = new byte[4];

        public PlayerListPacket() : base(0, PacketType.PlayerList)
        {
            length = (byte)list.Length;
        }
        
        public override byte[] GetBytes()
        {
            byte[] header = base.GetBytes();
            byte[] combined = new byte[header.Length + list.Length];
            Array.Copy(header, combined, header.Length);
            Array.Copy(list, 0, combined, header.Length, list.Length);
            return combined;
        }

        public override void Parse(byte[] data)
        {
            base.Parse(data);
            Array.Copy(data, 4, list, 0, list.Length);
        }
    }

    public class ReadyPacket : Packet
    {
        private byte[] _wordBytes;
        private string _word;

        public byte turn;
        public string word
        {
            get { return _word; }
            set
            {
                _word = value;
                if (!string.IsNullOrEmpty(_word))
                    _wordBytes = Encoding.UTF8.GetBytes(_word);
                else
                    _wordBytes = new byte[0];
            }
        }
        
        public ReadyPacket(byte newTurn = 0, string newText = "") : base(0, PacketType.Ready)
        {
            turn = newTurn;
            word = newText;
            length = (byte)(_wordBytes.Length + 1);
        }
        
        public override byte[] GetBytes()
        {
            byte[] header = base.GetBytes();

            byte[] combined = new byte[header.Length + _wordBytes.Length + 1];
            Array.Copy(header, combined, header.Length);
            combined[header.Length] = turn;
            Array.Copy(_wordBytes, 0, combined, header.Length + 1, _wordBytes.Length);

            return combined;
        }

        public override void Parse(byte[] data)
        {
            base.Parse(data);
            turn = data[4];
            _word = Encoding.UTF8.GetString(data, 5, length - 1);
            _wordBytes = Encoding.UTF8.GetBytes(_word);
        }
    }
}
