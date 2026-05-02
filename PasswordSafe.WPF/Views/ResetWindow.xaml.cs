using System;
using System.IO;
using System.Windows;
using PasswordSafe.WPF.Services;

namespace PasswordSafe.WPF.Views
{
    public partial class ResetWindow : Window
    {
        private readonly PasswordService _pwd;
        private DatabaseService _db;

        /// <summary>
        /// Возвращает обновлённый DatabaseService после смены пароля,
        /// чтобы MainWindow мог продолжить работу с новым ключом.
        /// </summary>
        public DatabaseService UpdatedDb => _db;

        public ResetWindow(DatabaseService db, PasswordService pwd)
        {
            InitializeComponent();
            _db = db;
            _pwd = pwd;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var oldPwd = OldPasswordBox.Password;
            var newPwd = NewPasswordBox.Password;
            var repeatPwd = RepeatPasswordBox.Password;

            // Валидация
            if (string.IsNullOrEmpty(oldPwd) ||
                string.IsNullOrEmpty(newPwd) ||
                string.IsNullOrEmpty(repeatPwd))
            {
                MessageBox.Show("Заполните все поля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPwd.Length < 8)
            {
                MessageBox.Show("Новый пароль должен содержать минимум 8 символов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPwd != repeatPwd)
            {
                MessageBox.Show("Новый пароль и повтор не совпадают",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (oldPwd == newPwd)
            {
                MessageBox.Show("Новый пароль должен отличаться от старого",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_pwd.Verify(oldPwd))
            {
                MessageBox.Show("Старый пароль введён неверно",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var confirm = MessageBox.Show(
                "Сменить мастер-пароль? Все записи будут перешифрованы.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                ChangeDbPassword(oldPwd, newPwd);
                MessageBox.Show("Пароль успешно изменён", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ChangeDbPassword(string oldPwd, string newPwd)
        {
            // 1. Считываем все записи (расшифровываются старым ключом)
            var entries = _db.GetAll();

            // 2. Закрываем текущее соединение
            _db.Dispose();

            // 3. Удаляем старый файл БД и meta
            if (File.Exists("vault.db")) File.Delete("vault.db");
            if (File.Exists("vault.meta")) File.Delete("vault.meta");

            // 4. Создаём новый meta с новым паролем
            _pwd.WriteMeta(newPwd);
            var (salt, _) = _pwd.ReadMeta();

            // 5. Новый CryptoService и DatabaseService
            var newCrypto = new CryptoService(newPwd, salt);
            _db = new DatabaseService(newPwd, salt, newCrypto);

            // 6. Перезаписываем все записи (зашифруются новым ключом)
            foreach (var entry in entries)
            {
                entry.Id = 0; // сбрасываем ID, чтобы LiteDB сгенерировал новый
                _db.Save(entry);
            }
        }
    }
}
