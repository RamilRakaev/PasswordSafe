using PasswordSafe.WPF.Models;
using PasswordSafe.WPF.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace PasswordSafe.WPF.Views
{
    public partial class MainWindow : Window
    {
        private DatabaseService _db;
        private List<SecretEntry> _entries = new();
        private SecretEntry? _current;
        private ObservableCollection<ExtraData> _currentExtras = new();

        public MainWindow(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            LoadEntries();
            LoadCategories();

            // Корректное закрытие БД при выходе
            Closed += (_, _) => _db.Dispose();
        }

        private void LoadEntries()
        {
            // Запоминаем выбранную запись, чтобы восстановить выделение
            var selectedId = _current?.Id;

            _entries = _db.GetAll(); // здесь уже подставляются актуальные CategoryName

            EntriesList.ItemsSource = null;            // сбрасываем view
            EntriesList.ItemsSource = _entries;

            var view = CollectionViewSource.GetDefaultView(EntriesList.ItemsSource);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("CategoryName"));
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("CategoryName", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));

            // Восстанавливаем выделение
            if (selectedId.HasValue)
                EntriesList.SelectedItem = _entries.FirstOrDefault(x => x.Id == selectedId.Value);
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
            CategoryBox.SelectedValue = _current.CategoryId ?? 0;

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

            // Установка CategoryId ДО первого сохранения
            var selectedId = (int?)CategoryBox.SelectedValue;
            if (selectedId == null || selectedId == 0)
            {
                _current.CategoryId = null;
            }
            else
            {
                _current.CategoryId = selectedId;
            }

            _db.Save(_current);
            LoadEntries(); // Только после этого обновляем список
        }


        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var pwdService = new PasswordService();
            var resetWindow = new ResetWindow(_db, pwdService)
            {
                Owner = this
            };

            if (resetWindow.ShowDialog() == true)
            {
                // Получаем обновлённый DatabaseService с новым ключом
                _db = resetWindow.UpdatedDb;
                LoadEntries();
            }
        }


        private void ClearFields()
        {
            TitleBox.Text = "";
            LoginBox.Text = "";
            SecretBox.Text = "";
            ExtrasGrid.ItemsSource = null;
            _current = null;
        }

        private void ManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var win = new CategoryWindow(_db) { Owner = this };
            win.ShowDialog();

            if (win.HasChanges)
            {
                LoadCategories();
                LoadEntries();
            }
        }


        private void LoadCategories()
        {
            var categories = _db.GetCategories();
            var list = new List<Category>
            {
                new Category { Id = 0, Name = "Без категории" }
            };
            list.AddRange(categories);

            var previouslySelected = (int?)CategoryBox.SelectedValue;

            CategoryBox.ItemsSource = list;
            CategoryBox.DisplayMemberPath = "Name";
            CategoryBox.SelectedValuePath = "Id";

            // Восстанавливаем выбор, если категория ещё существует
            if (previouslySelected.HasValue && list.Any(c => c.Id == previouslySelected.Value))
                CategoryBox.SelectedValue = previouslySelected.Value;
            else
                CategoryBox.SelectedValue = 0;
        }

        public static Dictionary<string, bool> GroupStates { get; } = new();

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is Expander ex && ex.DataContext is CollectionViewGroup g)
                GroupStates[g.Name?.ToString() ?? ""] = true;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (sender is Expander ex && ex.DataContext is CollectionViewGroup g)
                GroupStates[g.Name?.ToString() ?? ""] = false;
        }

        private void Expander_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Expander ex && ex.DataContext is CollectionViewGroup g)
            {
                var key = g.Name?.ToString() ?? "";
                if (GroupStates.TryGetValue(key, out var expanded))
                    ex.IsExpanded = expanded;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            LoadEntries();
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e) => SetAllGroups(false);
        private void ExpandAll_Click(object sender, RoutedEventArgs e) => SetAllGroups(true);

        private void SetAllGroups(bool expanded)
        {
            foreach (var ex in FindVisualChildren<Expander>(EntriesList))
                ex.IsExpanded = expanded;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var sub in FindVisualChildren<T>(child))
                    yield return sub;
            }
        }

    }
}
