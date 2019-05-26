using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace T05.CatchMind.Server
{
    class ServerManager
    {
        // Properties
        public bool isActive { get { return _socket != null; } }
        public List<ClientSocket> clients { get { return _clients; } }

        // Server Socket, Thread
        public const int Port = 10000;
        private Thread _thread;
        private Socket _socket;

        // Clients
        private List<ClientSocket> _clients = new List<ClientSocket>();
        private byte _clientId = 1;

        // Lock
        public object clientSocket_Lock = new object();
        private object _lock = new object();
        
        public ServerManager()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(new IPEndPoint(IPAddress.Any, Port));
                _socket.Listen(4);
                _socket.Blocking = false;
                _socket.NoDelay = true;

                _thread = new Thread(new ThreadStart(_SocketThread));
                _thread.Start();
            }
            catch (SocketException e)
            {
                System.Console.WriteLine(e);
            }
        }

        ~ServerManager()
        {
            Close();
        }

        public void Close()
        {
            lock (clientSocket_Lock)
            {
                foreach (var c in _clients)
                    c.Disconnect();
                _clients.Clear();
            }

            if (_socket != null)
            {
                lock (_lock)
                {
                    _socket.Close();
                    _socket = null;
                }
            }

            if (_thread != null)
            {
                _thread.Join();
                _thread = null;
            }
        }

        public void RemoveClient(ClientSocket c)
        {
            lock (clientSocket_Lock)
            {
                _clients.Remove(c);
            }
            c.Disconnect();
        }

        public int GetClientIndex(int id)
        {
            int idx = -1;
            lock (clientSocket_Lock)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].userId == id)
                    {
                        idx = i;
                        break;
                    }
                }
            }
            return idx;
        }

        public void Send(int id, byte[] data)
        {
            lock (clientSocket_Lock)
            {
                int idx = -1;
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].userId == id)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0)
                    clients[idx].Send(data);
            }
        }

        private void _SocketThread()
        {
            while (isActive)
            {
                try
                {
                    Socket s = null;

                    lock (_lock)
                    {
                        if (isActive)
                            s = _socket.Accept();
                    }

                    if (!isActive) break;

                    if (s != null)
                    {
                        ClientSocket client = new ClientSocket(this, _clientId++, s);
                        lock (clientSocket_Lock)
                            _clients.Add(client);
                        client.Start();
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.WouldBlock)
                    {
                        System.Console.WriteLine(e);
                        Close();
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    System.Console.WriteLine(e);
                    Close();
                    break;
                }

                Thread.Sleep(1000);
            }
        }
    }
}
