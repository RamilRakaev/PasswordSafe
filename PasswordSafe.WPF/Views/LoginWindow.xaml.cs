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
