using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using T05.CatchMind.Common;

namespace T05.CatchMind.Client
{
    class ClientManager
    {
        // Properties
        public byte id { get; private set; }
        public bool isActive { get { return _socket != null && _socket.Connected; } }
        public string word { get; private set; }
        public byte turn { get; private set; }

        public static ClientManager instance { get; private set; }
        public byte[] players = new byte[4];
        
        // Clinet Socket, Thread
        private Thread _thread;
        private Socket _socket;

        private byte[] _buffer = new byte[400];
        private List<byte> _receivedBytes = new List<byte>();
        private List<Packet> _receivedPackets = new List<Packet>();
        private List<byte> _sendBytes = new List<byte>();
        private object _lock = new object();

        // Events
        public event Action<bool> onConnect;
        public event Action onDisconnect;

        // MainWindow
        private MainWindow _mainWindow;

        public static void Init(MainWindow mw)
        {
            if (instance == null)
                instance = new ClientManager(mw);
        }

        public static void Cleanup()
        {
            if (instance != null)
            {
                if (instance.isActive)
                    instance.Disconnect();
                instance = null;
            }
        }

        public ClientManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public bool Connect(string domain, int port)
        {
            try
            {
                if (isActive)
                {
                    System.Console.WriteLine("이미 서버에 연결되어 있음.");
                    return true;
                }

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.ReceiveTimeout = 10000;
                _socket.SendTimeout = 10000;

                // 연결은 blocking, 이후는 non-blocking으로 수행
                IPAddress[] address = Dns.GetHostAddresses(domain); // domain to ip
                _socket.Blocking = true;
                _socket.NoDelay = false;
                _socket.Connect(address[0], port);
                _socket.Blocking = false;
                _socket.NoDelay = true;

                _thread = new Thread(new ThreadStart(_SocketThread));
                _thread.Start();

                onConnect?.Invoke(true);
                return true;
            }
            catch (SocketException e)
            {
                System.Console.WriteLine(e);
                _socket = null;
                onConnect?.Invoke(false);
                return false;
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
            }

            _receivedBytes.Clear();
            _receivedPackets.Clear();
            _sendBytes.Clear();

            word = null;
            turn = 0;

            if (onDisconnect != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    onDisconnect();
                });
            }
        }

        ~ClientManager()
        {
            Disconnect();
            _mainWindow = null;
        }

        public int GetClientIndex(byte userId)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == userId)
                    return i;
            }
            return -1;
        }

        public void Send(byte[] bytes)
        {
            lock (_lock)
            {
                if (isActive)
                {
                    _sendBytes.AddRange(bytes);
                }
            }
        }

        private void _ProcessPacket(Packet p)
        {
            switch (p.type)
            {
                case PacketType.Ping:
                    {
                        // userId = 0일 경우 ping용 패킷
                        // 그 외는 아이디 할당
                        if (p.userId != 0)
                            id = p.userId;
                    }
                    break;
                case PacketType.Text:
                    {
                        TextPacket t = (TextPacket)p;
                        Application.Current.Dispatcher.Invoke(() => { _mainWindow.ShowMessage(t.text, t.userId); });
                    }
                    break;
                case PacketType.Draw:
                    {
                        DrawPacket dp = (DrawPacket)p;
                        Application.Current.Dispatcher.Invoke(() => { _mainWindow.Draw(dp); });
                    }
                    break;
                case PacketType.Clear:
                    {
                        Application.Current.Dispatcher.Invoke(() => { _mainWindow.ClearDrawing(); });
                    }
                    break;
                case PacketType.PlayerList:
                    {
                        PlayerListPacket pl = (PlayerListPacket)p;
                        players = pl.list;
                        Application.Current.Dispatcher.Invoke(() => { _mainWindow.ShowPlayers(); });
                    }
                    break;
                case PacketType.Ready:
                    {
                        ReadyPacket rp = (ReadyPacket)p;
                        turn = rp.turn;
                        word = rp.word;
                        Application.Current.Dispatcher.Invoke(() => { _mainWindow.ShowWord(); });
                    }
                    break;
            }
        }

        private void _SocketThread()
        {
            try
            {
                while (isActive)
                {
                    int received = 0, sended = 0;

                    try
                    {
                        lock (_lock)
                        {
                            if (isActive)
                                received = _socket.Receive(_buffer);
                        }
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode != SocketError.WouldBlock)
                        {
                            Disconnect();
                            break;
                        }
                    }
                    catch
                    {
                        Disconnect();
                        break;
                    }

                    if (!isActive) break;

                    if (received > 0)
                    {
                        // Append data to buffer
                        for (int i = 0; i < received; i++)
                            _receivedBytes.Add(_buffer[i]);

                        // Parse
                        int offset = 0;
                        while (offset < _receivedBytes.Count)
                        {
                            Packet p = PacketParser.Parse(_receivedBytes, offset);
                            if (p != null)
                            {
                                offset += p.length + 4;
                                _receivedPackets.Add(p);
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (offset > 0)
                            _receivedBytes.RemoveRange(0, offset);

                        // process
                        for (int i = 0; i < _receivedPackets.Count; i++)
                        {
                            Packet p = _receivedPackets[i];
                            _ProcessPacket(p);
                        }
                        _receivedPackets.Clear();
                    }

                    // send
                    int sendLength = Math.Min(_sendBytes.Count, _buffer.Length);
                    if (sendLength > 0)
                    {
                        _sendBytes.CopyTo(0, _buffer, 0, sendLength);

                        try
                        {
                            lock (_lock)
                            {
                                if (isActive)
                                    sended = _socket.Send(_buffer, sendLength, SocketFlags.None);
                            }
                        }
                        catch (SocketException e)
                        {
                            if (e.SocketErrorCode != SocketError.WouldBlock)
                            {
                                Disconnect();
                                break;
                            }
                        }
                        catch
                        {
                            Disconnect();
                            break;
                        }

                        if (!isActive) break;

                        if (sended > 0)
                        {
                            lock (_lock)
                            {
                                _sendBytes.RemoveRange(0, sended);
                            }
                        }
                    }

                    Thread.Sleep(16);
                }
            }
            catch (ThreadAbortException e)
            {
                Console.WriteLine(e);
                Disconnect();
            }
        }
    }
}
