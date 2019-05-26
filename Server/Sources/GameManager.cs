using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace T05.CatchMind.Server
{
    using T05.CatchMind.Common;

    class GameManager
    {
        private MainWindow _mainWindow;
        private ServerManager _serverMgr;
        private string[] _words = new string[0];
        private int _wordIdx = 0;
        public byte turn { get; private set; }
        public string word { get; private set; }

        public static GameManager instance { get; private set; }

        public enum State
        {
            None, Lobby, Game
        }

        public static void Init(MainWindow mw, ServerManager sm)
        {
            if (instance == null)
                instance = new GameManager(mw, sm);
        }

        GameManager(MainWindow mw, ServerManager sm)
        {
            _mainWindow = mw;
            _serverMgr = sm;
            string words = System.IO.File.ReadAllText("Words.txt");
            _words = words.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        ~GameManager()
        {
            _mainWindow = null;
            _serverMgr = null;
            instance = null;
        }

        public void ProcessPacket(Packet p)
        {
            switch (p.type)
            {
                case PacketType.Text:
                    {
                        TextPacket t = (TextPacket)p;

                        // 텍스트 표시
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (_mainWindow != null)
                                    _mainWindow.ShowMessage(t.text, t.userId);
                            });
                        }
                        DispatchPacket(t);

                        // 정답 맞춘 경우
                        if (turn != p.userId && t.text.Equals(word))
                        {
                            Start();
                        }
                    }
                    break;
                case PacketType.Draw:
                    {
                        DrawPacket dp = (DrawPacket)p;
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (_mainWindow != null)
                                    _mainWindow.Draw(dp);
                            });
                        }
                        DispatchPacket(dp);
                    }
                    break;
                case PacketType.Clear:
                    {
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (_mainWindow != null)
                                    _mainWindow.ClearDrawing();
                            });
                        }
                        DispatchPacket(p);
                    }
                    break;
            }
        }

        public bool Start()
        {
            if (_serverMgr.clients.Count < 2)
            {
                // 사용자 수 부족
                turn = 0;
                word = null;
                DispatchPacket(new ReadyPacket(turn, word));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_mainWindow != null)
                    {
                        _mainWindow.ClearDrawing();
                        _mainWindow.ShowWord();
                    }
                });
                return false;
            }
            else
            {
                // 문제 선택
                int idx = _serverMgr.GetClientIndex(turn);
                lock (_serverMgr.clientSocket_Lock)
                {
                    turn = _serverMgr.clients[++idx % _serverMgr.clients.Count].userId;
                }
                word = _words[_wordIdx];
                _wordIdx = (_wordIdx + 1) % _words.Length;

                // 캔버스 지우기
                DispatchPacket(new Packet(0, PacketType.Clear));
                // 문제 출제
                DispatchPacket(new ReadyPacket(turn, word));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_mainWindow != null)
                    {
                        _mainWindow.ClearDrawing();
                        _mainWindow.ShowWord();
                    }
                });
            }
            return true;
        }

        public void DispatchPacket(Packet p)
        {
            lock (_serverMgr.clientSocket_Lock)
            {
                for (int i = 0; i < _serverMgr.clients.Count; i++)
                {
                    ClientSocket c = _serverMgr.clients[i];
                    if (c.userId != p.userId)
                        c.Send(p.GetBytes());
                }
            }
        }

        public void ShowPlayers()
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_mainWindow != null)
                        _mainWindow.ShowPlayers();
                });
            }
        }

        public void DispatchPlayerListPacket()
        {
            PlayerListPacket p = new PlayerListPacket();
            lock (_serverMgr.clientSocket_Lock)
            {
                for (int i = 0; i < _serverMgr.clients.Count; i++)
                {
                    ClientSocket c = _serverMgr.clients[i];
                    p.list[i] = c.userId;
                }

                for (int i = 0; i < _serverMgr.clients.Count; i++)
                {
                    ClientSocket c = _serverMgr.clients[i];
                    c.Send(p.GetBytes());
                }
            }
        }
    }
}
