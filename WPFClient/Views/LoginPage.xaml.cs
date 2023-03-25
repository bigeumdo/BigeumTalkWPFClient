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
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using BigeumTalkClient.Network;

namespace BigeumTalkClient.Pages
{
    /// <summary>
    /// SigninPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginPage : Page
    {
        internal static LoginPage loginPage;
        private SocketNetwork sn;
        public LoginPage()
        {
            InitializeComponent();
            loginPage = this;
            sn = new SocketNetwork();
        }


        private void SigninButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessLogin();
        }
        private void NicknameText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessLogin();
            }
        }

        private void ProcessLogin()
        {
            SigninError.Visibility = Visibility.Hidden;
            if (String.IsNullOrWhiteSpace(NicknameText.Text) == true)
            {
                SigninError.Text = "닉네임이 올바르지 않습니다.";
                SigninError.Visibility = Visibility.Visible;
                return;
            }
            SigninButton.IsEnabled = false;
            NicknameText.IsEnabled = false;

            sn.Send_C_LOGIN();

        }

        private void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }


        private void IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SigninError.Visibility = Visibility.Hidden;
        }

        private void PwPWBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            SigninError.Visibility = Visibility.Hidden;
        }


    }
}
