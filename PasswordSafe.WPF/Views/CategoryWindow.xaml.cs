using PasswordSafe.WPF.Models;
using PasswordSafe.WPF.Services;
using System.Windows;

namespace PasswordSafe.WPF.Views
{
    public partial class CategoryWindow : Window
    {
        private readonly DatabaseService _db;
        public bool HasChanges { get; private set; }

        public CategoryWindow(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            LoadCategories();
        }

        private void LoadCategories()
        {
            var categories = _db.GetCategories();
            CategoriesGrid.ItemsSource = null;
            CategoriesGrid.ItemsSource = categories;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCategoryBox.Text))
            {
                MessageBox.Show("Please enter a category name", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var category = new Category { Name = NewCategoryBox.Text.Trim() };
            _db.SaveCategory(category);
            NewCategoryBox.Text = string.Empty;
            LoadCategories();
            HasChanges = true;
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is Category selected)
            {
                if (string.IsNullOrWhiteSpace(NewCategoryBox.Text))
                {
                    MessageBox.Show("Please enter a new category name", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selected.Name = NewCategoryBox.Text.Trim();
                _db.SaveCategory(selected);
                NewCategoryBox.Text = string.Empty;
                LoadCategories();
                HasChanges = true;
            }
            else
            {
                MessageBox.Show("Please select a category to update", "Selection Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesGrid.SelectedItem is Category selected)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete category '{selected.Name}'?\n\n" +
                    "Entries in this category will not be deleted, but will become uncategorized.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Сначала обнуляем CategoryId у всех записей этой категории
                    var entries = _db.GetAll().Where(e => e.CategoryId == selected.Id);
                    foreach (var entry in entries)
                    {
                        entry.CategoryId = null;
                        _db.Save(entry);
                    }

                    _db.DeleteCategory(selected.Id);
                    LoadCategories();
                    HasChanges = true;
                }
            }
            else
            {
                MessageBox.Show("Please select a category to delete", "Selection Required",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
