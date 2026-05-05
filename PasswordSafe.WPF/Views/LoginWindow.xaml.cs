using PasswordSafe.WPF.Services;
using System.Windows;
using System.Windows.Controls;

namespace PasswordSafe.WPF.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly PasswordService _pwd = new();
        private bool _isSyncing;

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            PasswordTextBox.Text = PasswordBox.Password;
            _isSyncing = false;
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            PasswordBox.Password = PasswordTextBox.Text;
            _isSyncing = false;
        }

        private void ShowPasswordButton_Checked(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }

        private void ShowPasswordButton_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var password = PasswordBox.Password;

            if (!_pwd.IsInitialized())
            {
                // Первый вход — создаём
                if (password.Length < 8)
                {
                    MessageBox.Show("Минимум 8 символов");
                    return;
                }
                _pwd.WriteMeta(password);
            }
            else if (!_pwd.Verify(password))
            {
                MessageBox.Show("Неверный пароль");
                return;
            }

            var (salt, _) = _pwd.ReadMeta();
            var crypto = new CryptoService(password, salt);
            var db = new DatabaseService(password, salt, crypto);

            new MainWindow(db).Show();
            Close();
        }
    }
}
