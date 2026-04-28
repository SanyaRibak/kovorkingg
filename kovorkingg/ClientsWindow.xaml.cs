using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace kovorkingg.Views
{
    public partial class ClientsWindow : Window
    {
        private readonly praktikaEntities _context;
        private ObservableCollection<clients> _clientsList;
        private clients _selectedClient;

        public ClientsWindow()
        {
            InitializeComponent();
            _context = new praktikaEntities();
            LoadClients();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadClients();
        }

        private void LoadClients()
        {
            var query = _context.clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string searchText = txtSearch.Text.Trim();
                query = query.Where(c => c.full_name.Contains(searchText) ||
                                         c.phone.Contains(searchText) ||
                                         c.email.Contains(searchText));
            }

            _clientsList = new ObservableCollection<clients>(query.OrderBy(c => c.full_name).ToList());
            dgClients.ItemsSource = _clientsList;
            txtStatus.Text = $"Найдено клиентов: {_clientsList.Count}";
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadClients();
            ClearForm();
            txtStatus.Text = "Список обновлён";
        }

        private void Clients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedClient = dgClients.SelectedItem as clients;
            if (_selectedClient != null)
            {
                txtFullName.Text = _selectedClient.full_name;
                txtPhone.Text = _selectedClient.phone;
                txtEmail.Text = _selectedClient.email;

                lblFormTitle.Text = $"✏️ Редактирование: {_selectedClient.full_name}";
                btnAdd.IsEnabled = false;
                btnSave.IsEnabled = true;
                btnDelete.IsEnabled = true;
                txtStatus.Text = $"Выбран клиент ID: {_selectedClient.id}";
            }
            else
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtFullName.Text = "";
            txtPhone.Text = "";
            txtEmail.Text = "";
            _selectedClient = null;

            lblFormTitle.Text = "➕ Добавление клиента";
            btnAdd.IsEnabled = true;
            btnSave.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО или название организации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                var newClient = new clients
                {
                    full_name = txtFullName.Text.Trim(),
                    phone = txtPhone.Text.Trim(),
                    email = txtEmail.Text.Trim()
                };

                _context.clients.Add(newClient);
                _context.SaveChanges();

                _clientsList.Add(newClient);
                dgClients.ItemsSource = null;
                dgClients.ItemsSource = _clientsList;

                txtStatus.Text = $"Клиент \"{newClient.full_name}\" успешно добавлен (ID: {newClient.id})";
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;
            if (!ValidateForm()) return;

            try
            {
                _selectedClient.full_name = txtFullName.Text.Trim();
                _selectedClient.phone = txtPhone.Text.Trim();
                _selectedClient.email = txtEmail.Text.Trim();

                _context.SaveChanges();

                dgClients.ItemsSource = null;
                dgClients.ItemsSource = _clientsList;

                txtStatus.Text = $"Клиент \"{_selectedClient.full_name}\" обновлён";
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null) return;

            var hasBookings = _context.bookings.Any(b => b.client_id == _selectedClient.id);
            if (hasBookings)
            {
                MessageBox.Show("Нельзя удалить клиента, так как у него есть бронирования.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить клиента \"{_selectedClient.full_name}\"?", "Подтверждение",
                                         MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string name = _selectedClient.full_name;
                    _context.clients.Remove(_selectedClient);
                    _context.SaveChanges();

                    _clientsList.Remove(_selectedClient);
                    txtStatus.Text = $"Клиент \"{name}\" удалён";
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