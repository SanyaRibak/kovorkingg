using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace kovorkingg.Views
{
    public partial class EmployeesWindow : Window
    {
        private readonly praktikaEntities _context;
        private ObservableCollection<users> _employeesList;
        private users _selectedEmployee;

        public EmployeesWindow()
        {
            InitializeComponent();
            _context = new praktikaEntities();
            LoadEmployees();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            var query = _context.users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                query = query.Where(u => u.full_name.Contains(txtSearch.Text) || u.username.Contains(txtSearch.Text));

            _employeesList = new ObservableCollection<users>(query.OrderBy(u => u.full_name).ToList());
            dgEmployees.ItemsSource = _employeesList;
            txtStatus.Text = $"Сотрудников: {_employeesList.Count}";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
            ClearForm();
            txtStatus.Text = "Список обновлён";
        }

        private void Employees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedEmployee = dgEmployees.SelectedItem as users;
            if (_selectedEmployee != null)
            {
                txtUsername.Text = _selectedEmployee.username;
                txtPassword.Password = "";
                txtConfirmPassword.Password = "";
                txtFullName.Text = _selectedEmployee.full_name;
                cmbRole.SelectedItem = cmbRole.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == _selectedEmployee.role);

                lblFormTitle.Text = $"✏️ Редактирование: {_selectedEmployee.full_name}";
                txtPasswordHint.Visibility = Visibility.Visible;
                btnAdd.IsEnabled = false;
                btnSave.IsEnabled = true;
                btnDelete.IsEnabled = true;
                txtStatus.Text = $"Выбран сотрудник: {_selectedEmployee.full_name} ({_selectedEmployee.role})";
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtUsername.Text = "";
            txtPassword.Password = "";
            txtConfirmPassword.Password = "";
            txtFullName.Text = "";
            cmbRole.SelectedIndex = 0;
            _selectedEmployee = null;

            lblFormTitle.Text = "➕ Добавление сотрудника";
            txtPasswordHint.Visibility = Visibility.Collapsed;
            btnAdd.IsEnabled = true;
            btnSave.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        private bool ValidateForm(bool isNew)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Введите логин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО сотрудника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (isNew)
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Введите пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (txtPassword.Password != txtConfirmPassword.Password)
                {
                    MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(txtPassword.Password) || !string.IsNullOrWhiteSpace(txtConfirmPassword.Password))
                {
                    if (txtPassword.Password != txtConfirmPassword.Password)
                    {
                        MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }
            }

            bool loginExists = _context.users.Any(u => u.username == txtUsername.Text.Trim() && (_selectedEmployee == null || u.id != _selectedEmployee.id));
            if (loginExists)
            {
                MessageBox.Show("Сотрудник с таким логином уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(true)) return;

            try
            {
                var newUser = new users
                {
                    username = txtUsername.Text.Trim(),
                    password_hash = txtPassword.Password,
                    full_name = txtFullName.Text.Trim(),
                    role = ((ComboBoxItem)cmbRole.SelectedItem).Content.ToString()
                };

                _context.users.Add(newUser);
                _context.SaveChanges();

                _employeesList.Add(newUser);
                dgEmployees.ItemsSource = null;
                dgEmployees.ItemsSource = _employeesList;

                txtStatus.Text = $"Сотрудник \"{newUser.full_name}\" успешно добавлен (ID: {newUser.id})";
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null) return;
            if (!ValidateForm(false)) return;

            try
            {
                _selectedEmployee.username = txtUsername.Text.Trim();
                _selectedEmployee.full_name = txtFullName.Text.Trim();
                _selectedEmployee.role = ((ComboBoxItem)cmbRole.SelectedItem).Content.ToString();

                if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    _selectedEmployee.password_hash = txtPassword.Password;
                }

                _context.SaveChanges();

                dgEmployees.ItemsSource = null;
                dgEmployees.ItemsSource = _employeesList;

                txtStatus.Text = $"Данные сотрудника \"{_selectedEmployee.full_name}\" обновлены";
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null) return;

            var hasBookings = _context.bookings.Any(b => b.user_id == _selectedEmployee.id);
            if (hasBookings)
            {
                MessageBox.Show("Нельзя удалить сотрудника, так как у него есть оформленные бронирования.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить сотрудника \"{_selectedEmployee.full_name}\"?", "Подтверждение",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string name = _selectedEmployee.full_name;
                    _context.users.Remove(_selectedEmployee);
                    _context.SaveChanges();

                    _employeesList.Remove(_selectedEmployee);
                    txtStatus.Text = $"Сотрудник \"{name}\" удалён";
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}