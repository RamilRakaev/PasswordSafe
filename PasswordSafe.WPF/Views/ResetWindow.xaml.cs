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
            // 1. Считываем всё под старым ключом
            var entries = _db.GetAll();
            var categories = _db.GetCategories();

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

            // 6. Сначала переносим категории и строим маппинг старых Id -> новых Id
            var categoryIdMap = new Dictionary<int, int>();
            foreach (var cat in categories)
            {
                var oldId = cat.Id;
                cat.Id = 0; // сброс, чтобы LiteDB сгенерировала новый
                _db.SaveCategory(cat);
                categoryIdMap[oldId] = cat.Id; // после Insert LiteDB обновит Id в объекте
            }

            // 7. Переносим записи, переназначая CategoryId по маппингу
            foreach (var entry in entries)
            {
                entry.Id = 0;

                // Перепривязываем к новой категории
                if (entry.CategoryId.HasValue &&
                    categoryIdMap.TryGetValue(entry.CategoryId.Value, out var newCatId))
                {
                    entry.CategoryId = newCatId;
                }
                else
                {
                    entry.CategoryId = null;
                }

                // Сбрасываем Id у extras, чтобы они тоже создались заново
                foreach (var ex in entry.Extras)
                    ex.Id = 0;

                _db.Save(entry);
            }
        }

    }
}
