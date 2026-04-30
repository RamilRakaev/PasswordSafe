using System.Collections.ObjectModel;
using System.Windows;
using PasswordSafe.WPF.Models;
using PasswordSafe.WPF.Services;

namespace PasswordSafe.WPF.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db;
        private List<SecretEntry> _entries = new();
        private SecretEntry? _current;
        private ObservableCollection<ExtraData> _currentExtras = new();

        public MainWindow(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            LoadEntries();

            // Корректное закрытие БД при выходе
            Closed += (_, _) => _db.Dispose();
        }

        private void LoadEntries()
        {
            _entries = _db.GetAll();
            EntriesList.ItemsSource = null;
            EntriesList.ItemsSource = _entries;
        }

        private void EntriesList_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _current = EntriesList.SelectedItem as SecretEntry;

            if (_current == null)
            {
                DetailsPanel.IsEnabled = false;
                ClearFields();
                return;
            }

            DetailsPanel.IsEnabled = true;
            TitleBox.Text = _current.Title ?? "";
            LoginBox.Text = _current.Login ?? "";
            SecretBox.Text = _current.Secret ?? "";

            _currentExtras = new ObservableCollection<ExtraData>(_current.Extras);
            ExtrasGrid.ItemsSource = _currentExtras;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var newEntry = new SecretEntry
            {
                Title = "Новая запись",
                Login = "",
                Secret = ""
            };
            _db.Save(newEntry);
            LoadEntries();
            EntriesList.SelectedItem = _entries.LastOrDefault();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;

            var result = MessageBox.Show(
                $"Удалить '{_current.Title}'?",
                "Подтверждение",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes) return;

            _db.Delete(_current.Id);
            LoadEntries();
            ClearFields();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;

            _current.Title = TitleBox.Text;
            _current.Login = LoginBox.Text;
            _current.Secret = SecretBox.Text;
            _current.Extras = _currentExtras
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToList();

            _db.Save(_current);
            LoadEntries();
            MessageBox.Show("Сохранено");
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            // TODO: открыть отдельное окно для смены пароля
            MessageBox.Show("Функция смены пароля будет реализована позже");
        }

        private void ClearFields()
        {
            TitleBox.Text = "";
            LoginBox.Text = "";
            SecretBox.Text = "";
            ExtrasGrid.ItemsSource = null;
            _current = null;
        }
    }
}
