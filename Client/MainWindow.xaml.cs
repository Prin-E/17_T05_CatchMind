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
using T05.CatchMind.Common;

namespace T05.CatchMind.Client
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public object ContentView;

        private int _thickness;
        private ColorType _color;
        private SolidColorBrush _brush = new SolidColorBrush(Colors.Black);
        private Polyline _line;

        System.Timers.Timer _pingTimer;

        public MainWindow()
        {
            InitializeComponent();

            ClientManager.Init(this);
            ClientManager.instance.onConnect += _OnConnect;
            ClientManager.instance.onDisconnect += _OnDisconnect;

            ContentView = this.Content;
            ShowConnection();
        }

        public void ShowContent()
        {
            this.Content = ContentView;
            WordText.Text = "게임 시작 전입니다.";
            User1ID.Content = "";
            User1Message.Content = "";
            User2ID.Content = "";
            User2Message.Content = "";
            User3ID.Content = "";
            User3Message.Content = "";
            User4ID.Content = "";
            User4Message.Content = "";
            MyCanvas.Children.Clear();
            MessageText.Clear();
        }

        public void ShowConnection()
        {
            this.Content = new Connection();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            ClientManager.instance.onDisconnect -= _OnDisconnect;
            ClientManager.Cleanup();
        }

        private void _OnConnect(bool success)
        {
            if (!success) return;

            if (_pingTimer == null)
            {
                _pingTimer = new System.Timers.Timer(1000);
                _pingTimer.Elapsed += new System.Timers.ElapsedEventHandler((sender, e) =>
                {
                    if (ClientManager.instance == null) return;
                    if (ClientManager.instance.id != 0 && ClientManager.instance.isActive)
                    {
                        ClientManager.instance.Send(new Packet(ClientManager.instance.id, PacketType.Ping).GetBytes());
                    }
                });
                _pingTimer.Start();
            }
        }

        private void _OnDisconnect()
        {
            ShowConnection();
            _pingTimer.Stop();
            _pingTimer = null;
        }

        #region Draw (Local)
        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _thickness = (int)(ThicknessSlider.Value + 0.5);
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ClientManager.instance.turn == ClientManager.instance.id)
            {
                _line = new Polyline();
                _line.StrokeThickness = _thickness;
                _line.Stroke = _brush;
                MyCanvas.Children.Add(_line);
                Point p = e.GetPosition(MyCanvas);
                _line.Points.Add(p);
                _SendDrawPacket(p, Phase.Began);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (ClientManager.instance.turn == ClientManager.instance.id)
            {
                if (_line != null)
                {
                    Point p = e.GetPosition(MyCanvas);
                    _line.Points.Add(p);
                    _SendDrawPacket(p, Phase.Moved);
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ClientManager.instance.turn == ClientManager.instance.id)
            {
                if (_line != null)
                {
                    Point p = e.GetPosition(MyCanvas);
                    _line.Points.Add(p);
                    _SendDrawPacket(p, Phase.Ended);
                    _line = null;
                }
            }
        }

        private void _SendDrawPacket(Point p, Phase phase)
        {
            DrawPacket packet = new DrawPacket(ClientManager.instance.id);
            packet.color = _color;
            packet.thickness = (byte)_thickness;
            packet.x = (short)p.X;
            packet.y = (short)p.Y;
            packet.phase = phase;
            ClientManager.instance.Send(packet.GetBytes());
        }

        private void Color1_Click(object sender, RoutedEventArgs e)
        {
            _color = ColorType.Black;
            _brush = new SolidColorBrush(Colors.Black);
        }

        private void Color2_Click(object sender, RoutedEventArgs e)
        {
            _color = ColorType.Red;
            _brush = new SolidColorBrush(Colors.Red);
        }

        private void Color3_Click(object sender, RoutedEventArgs e)
        {
            _color = ColorType.Yellow;
            _brush = new SolidColorBrush(Colors.Yellow);
        }

        private void Color4_Click(object sender, RoutedEventArgs e)
        {
            _color = ColorType.Green;
            _brush = new SolidColorBrush(Colors.Green);
        }

        private void Color5_Click(object sender, RoutedEventArgs e)
        {
            _color = ColorType.Blue;
            _brush = new SolidColorBrush(Colors.Blue);
        }

        private void Color6_Click(object sender, RoutedEventArgs e)
        {
            _color = ColorType.White;
            _brush = new SolidColorBrush(Colors.White);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (ClientManager.instance.turn == ClientManager.instance.id)
            {
                ClearDrawing();
                Packet packet = new Packet(ClientManager.instance.id, PacketType.Clear);
                ClientManager.instance.Send(packet.GetBytes());
            }
        }
        #endregion

        #region Draw (Remote)
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
        #endregion

        #region Message
        private void MessageButton_Click(object sender, RoutedEventArgs e)
        {
            _SendMessage();
        }

        private void MessageText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                _SendMessage();
            }
        }

        private void _SendMessage()
        {
            string text = MessageText.Text;
            ClientManager cm = ClientManager.instance;

            if (!string.IsNullOrEmpty(text))
            {
                TextPacket packet = new TextPacket(cm.id);
                packet.text = text;
                cm.Send(packet.GetBytes());

                ShowMessage(text, cm.id);
                MessageText.Clear();
            }

            if (cm.turn != 0 && cm.turn != cm.id && text.Equals(cm.word))
            {
                MessageBox.Show("정답!");
            }
        }
        #endregion

        public void ShowMessage(string msg, byte owner)
        {
            int idx = ClientManager.instance.GetClientIndex(owner);
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

        public void ShowPlayers()
        {
            for (int i = 0; i < ClientManager.instance.players.Length; i++)
            {
                byte userId = ClientManager.instance.players[i];
                Label lbl = null;
                
                switch (i)
                {
                    case 0: lbl = User1ID; break;
                    case 1: lbl = User2ID; break;
                    case 2: lbl = User3ID; break;
                    case 3: lbl = User4ID; break;
                }

                lbl.Content = userId != 0 ? string.Format("사용자 {0}", userId) : "없음";
                if (userId == ClientManager.instance.id)
                    lbl.Foreground = new SolidColorBrush(Colors.Blue);
                else
                    lbl.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        public void ShowWord()
        {
            var cm = ClientManager.instance;
            string word = cm.word;
            byte turn = cm.turn;

            if (turn == cm.id)
            {
                WordText.Text = string.Format("문제:[{0}]", word);
            }
            else
            {
                WordText.Text = string.Format("문제를 풀어보세요!");
            }
        }
    }
}
