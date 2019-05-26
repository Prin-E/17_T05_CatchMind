using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using T05.CatchMind.Common;
using Timer = System.Timers.Timer;

namespace T05.CatchMind.Server
{
    class ClientSocket
    {
        private ServerManager _serverMgr;

        public byte userId { get; private set; }
        public bool isActive { get { return _socket != null && _socket.Connected; } }
        public List<byte> recvBuffer { get { return _recvBuffer; } }

        private Thread _thread;
        private Socket _socket;
        private List<byte> _recvBuffer = new List<byte>();
        private List<byte> _sendBuffer = new List<byte>();
        private List<Packet> _receivedPackets = new List<Packet>();

        private Timer _pingTimer;

        private byte[] _buffer = new byte[400];

        private object _lock = new object();

        public ClientSocket(ServerManager sm, byte newId, Socket socket)
        {
            _serverMgr = sm;
            userId = newId;

            _socket = socket;
            _socket.ReceiveTimeout = 10000;
            _socket.SendTimeout = 10000;
            _socket.NoDelay = true;
            _socket.Blocking = false;

            System.Console.WriteLine("new client " + socket.RemoteEndPoint);

            // ID 할당
            Packet ping = new Packet(userId, PacketType.Ping);
            Send(ping.GetBytes());
        }
        
        public void Disconnect()
        {
            if (_pingTimer != null)
            {
                _pingTimer.Stop();
                _pingTimer = null;
            }

            lock (_lock)
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
            }

            lock (_lock)
            {
                _recvBuffer.Clear();
                _sendBuffer.Clear();
                _receivedPackets.Clear();
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.DispatchPlayerListPacket();
                GameManager.instance.ShowPlayers();
            }
        }

        ~ClientSocket()
        {
            _serverMgr = null;
        }

        public void Start()
        {
            if (_thread == null)
            {
                _thread = new Thread(new ThreadStart(_SocketThread));
                _thread.Priority = ThreadPriority.Normal;
                _thread.Start();
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.DispatchPlayerListPacket();
                GameManager.instance.ShowPlayers();
            }

            if (_pingTimer == null)
            {
                _pingTimer = new Timer(1000);
                _pingTimer.Elapsed += new System.Timers.ElapsedEventHandler((sender, e) =>
                {
                    if (isActive)
                    {
                        Send(new Packet(0, PacketType.Ping).GetBytes());
                    }
                });
            }
            _pingTimer.Start();
        }

        public void Send(byte[] data)
        {
            if (isActive)
            {
                lock (_lock)
                {
                    _sendBuffer.AddRange(data);
                }
            }
        }

        private void _SocketThread()
        {
            while (isActive)
            {
                int recv = 0, send = 0;

                try
                {
                    // receive
                    lock (_lock)
                    {
                        if (isActive)
                            recv = _socket.Receive(_buffer, _buffer.Length, SocketFlags.None);
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.WouldBlock)
                    {
                        Console.WriteLine(e);
                        _serverMgr.RemoveClient(this);
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    _serverMgr.RemoveClient(this);
                    break;
                }

                if (!isActive) return;

                if (recv > 0)
                {
                    Console.WriteLine("received from " + _socket.RemoteEndPoint + " : " + recv + "bytes.");
                    for (int i = 0; i < recv; i++)
                        _recvBuffer.Add(_buffer[i]);
                }

                // parse
                int offset = 0;
                lock (_lock)
                {
                    while (offset < _recvBuffer.Count)
                    {
                        Packet p = PacketParser.Parse(_recvBuffer, offset);
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
                        _recvBuffer.RemoveRange(0, offset);
                }

                // process
                if (GameManager.instance != null)
                {
                    lock (_lock)
                    {
                        for (int i = 0; i < _receivedPackets.Count; i++)
                        {
                            Packet p = _receivedPackets[i];
                            GameManager.instance.ProcessPacket(p);
                        }
                        _receivedPackets.Clear();
                    }
                }

                // send
                int sendLen = Math.Min(_sendBuffer.Count, _buffer.Length);
                if (sendLen > 0)
                {
                    _sendBuffer.CopyTo(0, _buffer, 0, sendLen);

                    try
                    {
                        lock (_lock)
                        {
                            if (isActive)
                                send = _socket.Send(_buffer, sendLen, SocketFlags.None);
                        }
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode != SocketError.WouldBlock)
                        {
                            Console.WriteLine(e);
                            _serverMgr.RemoveClient(this);
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        _serverMgr.RemoveClient(this);
                        break;
                    }

                    if (!isActive) break;

                    if (send > 0)
                    {
                        lock (_lock)
                        {
                            _sendBuffer.RemoveRange(0, send);
                        }
                    }
                }

                Thread.Sleep(16);
            }
        }
    }
}
