using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace kovorkingg.Views
{
    public partial class AdminWindow : Window
    {
        private readonly praktikaEntities _context;
        private readonly users _currentUser;
        private ObservableCollection<items> _itemsList;
        private items _selectedItem;

        public AdminWindow(users user)
        {
            InitializeComponent();
            _context = new praktikaEntities();
            _currentUser = user;
            txtUserInfo.Text = $"{user.full_name} (администратор)";

            LoadItems();
            cmbCategory.SelectedIndex = 0;
            cmbUnit.SelectedIndex = 0;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadItems();
        }

        private void LoadItems()
        {
            var query = _context.items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                query = query.Where(i => i.name.Contains(txtSearch.Text));
            _itemsList = new ObservableCollection<items>(query.OrderBy(i => i.name).ToList());
            dgItems.ItemsSource = _itemsList;
            txtStatus.Text = $"Найдено услуг: {_itemsList.Count}";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
            ClearForm();
            txtStatus.Text = "Список обновлён";
        }

        private void Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedItem = dgItems.SelectedItem as items;
            if (_selectedItem != null)
            {
                txtName.Text = _selectedItem.name;
                txtDescription.Text = _selectedItem.description;
                cmbCategory.SelectedItem = cmbCategory.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == _selectedItem.category);
                txtPrice.Text = _selectedItem.price.ToString("F2");
                cmbUnit.SelectedItem = cmbUnit.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == _selectedItem.unit);
                chkIsActive.IsChecked = _selectedItem.is_active;

                lblFormTitle.Text = $"✏️ Редактирование: {_selectedItem.name}";
                btnAdd.IsEnabled = false;
                btnSave.IsEnabled = true;
                btnDelete.IsEnabled = true;
                txtStatus.Text = $"Выбрана услуга ID: {_selectedItem.id}";
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtName.Text = "";
            txtDescription.Text = "";
            cmbCategory.SelectedIndex = 0;
            txtPrice.Text = "";
            cmbUnit.SelectedIndex = 0;
            chkIsActive.IsChecked = true;
            _selectedItem = null;

            lblFormTitle.Text = "➕ Добавление услуги";
            btnAdd.IsEnabled = true;
            btnSave.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название услуги.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену (положительное число).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                var newItem = new items
                {
                    name = txtName.Text.Trim(),
                    description = txtDescription.Text.Trim(),
                    category = ((ComboBoxItem)cmbCategory.SelectedItem).Content.ToString(),
                    price = decimal.Parse(txtPrice.Text),
                    unit = ((ComboBoxItem)cmbUnit.SelectedItem).Content.ToString(),
                    is_active = chkIsActive.IsChecked ?? true
                };

                _context.items.Add(newItem);
                _context.SaveChanges();

                _itemsList.Add(newItem);
                dgItems.ItemsSource = null;
                dgItems.ItemsSource = _itemsList;

                txtStatus.Text = $"Услуга \"{newItem.name}\" успешно добавлена (ID: {newItem.id})";
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            if (!ValidateForm()) return;

            try
            {
                _selectedItem.name = txtName.Text.Trim();
                _selectedItem.description = txtDescription.Text.Trim();
                _selectedItem.category = ((ComboBoxItem)cmbCategory.SelectedItem).Content.ToString();
                _selectedItem.price = decimal.Parse(txtPrice.Text);
                _selectedItem.unit = ((ComboBoxItem)cmbUnit.SelectedItem).Content.ToString();
                _selectedItem.is_active = chkIsActive.IsChecked ?? true;

                _context.SaveChanges();

                dgItems.ItemsSource = null;
                dgItems.ItemsSource = _itemsList;

                txtStatus.Text = $"Услуга \"{_selectedItem.name}\" обновлена";
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var hasBookings = _context.booking_items.Any(bi => bi.item_id == _selectedItem.id);
            if (hasBookings)
            {
                MessageBox.Show("Нельзя удалить услугу, так как она используется в существующих бронированиях. Вы можете сделать её неактивной.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить услугу \"{_selectedItem.name}\"?", "Подтверждение",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string name = _selectedItem.name;
                    _context.items.Remove(_selectedItem);
                    _context.SaveChanges();

                    _itemsList.Remove(_selectedItem);
                    txtStatus.Text = $"Услуга \"{name}\" удалена";
                    ClearForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            txtStatus.Text = "Редактирование отменено";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }

        private void Clients_Click(object sender, RoutedEventArgs e)
        {
            ClientsWindow clientsWindow = new ClientsWindow();
            clientsWindow.Owner = this;
            clientsWindow.ShowDialog();
        }

        private void Bookings_Click(object sender, RoutedEventArgs e)
        {
            BookingsWindow bookingsWindow = new BookingsWindow(_currentUser);
            bookingsWindow.Owner = this;
            bookingsWindow.ShowDialog();
        }

        private void Employees_Click(object sender, RoutedEventArgs e)
        {
            EmployeesWindow employeesWindow = new EmployeesWindow();
            employeesWindow.Owner = this;
            employeesWindow.ShowDialog();
        }
    }
}