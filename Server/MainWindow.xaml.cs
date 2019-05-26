using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using T05.CatchMind.Server;

namespace T05.CatchMind.Server
{
    using T05.CatchMind.Common;

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        ServerManager serverMgr;
        private Polyline _line;

        public MainWindow()
        {
            InitializeComponent();
            serverMgr = new ServerManager();
            GameManager.Init(this, serverMgr);
            ShowPlayers();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            serverMgr.Close();
        }

        public void ShowMessage(string msg, int owner)
        {
            int idx = serverMgr.GetClientIndex(owner);
            switch (idx)
            {
                case 0:
                    User1Message.Content = msg;
                    break;
                case 1:
                    User2Message.Content = msg;
                    break;
                case 2:
                    User3Message.Content = msg;
                    break;
                case 3:
                    User4Message.Content = msg;
                    break;
            }
        }

        public void Draw(DrawPacket dp)
        {
            switch (dp.phase)
            {
                case Phase.Began:
                case Phase.Moved:
                    if (_line == null)
                    {
                        _line = new Polyline();
                        Color color = Colors.Black;
                        switch (dp.color)
                        {
                            case ColorType.Red: color = Colors.Red; break;
                            case ColorType.Yellow: color = Colors.Yellow; break;
                            case ColorType.Green: color = Colors.Green; break;
                            case ColorType.Blue: color = Colors.Blue; break;
                            case ColorType.White: color = Colors.White; break;
                        }

                        _line.Stroke = new SolidColorBrush(color);
                        _line.StrokeThickness = dp.thickness;
                        MyCanvas.Children.Add(_line);
                    }
                    _line.Points.Add(new Point(dp.x, dp.y));
                    break;
                case Phase.Ended:
                    if (_line != null)
                    {
                        _line.Points.Add(new Point(dp.x, dp.y));
                        _line = null;
                    }
                    break;
            }
        }

        public void ClearDrawing()
        {
            _line = null;
            MyCanvas.Children.Clear();
        }

        public void ShowPlayers()
        {
            lock (serverMgr.clientSocket_Lock)
            {
                for (int i = 0; i < Math.Max(4, serverMgr.clients.Count); i++)
                {
                    ClientSocket c = null;
                    if (serverMgr.clients.Count > i)
                        c = serverMgr.clients[i];

                    switch (i)
                    {
                        case 0:
                            User1ID.Content = c != null ? string.Format("사용자 {0}", c.userId) : "없음";
                            break;
                        case 1:
                            User2ID.Content = c != null ? string.Format("사용자 {0}", c.userId) : "없음";
                            break;
                        case 2:
                            User3ID.Content = c != null ? string.Format("사용자 {0}", c.userId) : "없음";
                            break;
                        case 3:
                            User4ID.Content = c != null ? string.Format("사용자 {0}", c.userId) : "없음";
                            break;
                    }
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GameManager.instance.Start())
            {
                MessageBox.Show("사용자 수가 부족합니다.");
                return;
            }
        }

        public void ShowWord()
        {
            WordText.Text = string.Format("문제[{0}], 사용자 {1}", GameManager.instance.word, GameManager.instance.turn);
        }
    }
}
