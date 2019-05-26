using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace T05.CatchMind.Client
{
    /// <summary>
    /// Connection.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Connection : Page
    {
        public Connection()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string domain = DomainText.Text;
            string portStr = PortText.Text;
            int port = 0;

            if (!int.TryParse(portStr, out port))
            {
                MessageBox.Show("포트 번호를 잘못 입력하였습니다.");
                return;
            }

            if (ClientManager.instance.Connect(domain, port))
            {
                MainWindow mainWindow = (MainWindow)this.Parent;
                mainWindow.ShowContent();
            }
            else
            {
                MessageBox.Show("서버에 연결할 수 없습니다.");
            }
        }
    }
}
