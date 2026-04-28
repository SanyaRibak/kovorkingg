using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace kovorkingg.Views
{
    public partial class BookingsWindow : Window
    {
        private readonly praktikaEntities _context;
        private readonly users _currentUser;
        private readonly bool _isAdmin;
        private ObservableCollection<bookings> _bookingsList;
        private bookings _selectedBooking;

        public BookingsWindow(users currentUser)
        {
            InitializeComponent();
            _context = new praktikaEntities();
            _currentUser = currentUser;
            _isAdmin = currentUser.role == "admin";
            pnlAdminActions.Visibility = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
            LoadBookings();
            dpFilterDate.SelectedDate = DateTime.Today;
        }

        private void LoadBookings()
        {
            var query = _context.bookings.AsQueryable();
            if (dpFilterDate.SelectedDate.HasValue)
            {
                var date = dpFilterDate.SelectedDate.Value.Date;
                query = query.Where(b => b.booking_date == date);
            }
            if (cmbFilterStatus.SelectedItem is ComboBoxItem statusItem && statusItem.Content.ToString() != "Все статусы")
            {
                string status = statusItem.Content.ToString();
                query = query.Where(b => b.status == status);
            }
            if (!_isAdmin)
            {
                query = query.Where(b => b.user_id == _currentUser.id);
            }

            _bookingsList = new ObservableCollection<bookings>(query.OrderByDescending(b => b.booking_date).ThenBy(b => b.start_time).ToList());
            dgBookings.ItemsSource = _bookingsList;
            txtStatus.Text = $"Загружено бронирований: {_bookingsList.Count}";
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadBookings();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadBookings();
            ClearDetails();
        }

        private void Bookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedBooking = dgBookings.SelectedItem as bookings;
            if (_selectedBooking != null)
            {
                ShowBookingDetails(_selectedBooking);
            }
            else
            {
                ClearDetails();
            }
        }

        private void ShowBookingDetails(bookings booking)
        {
            txtClientInfo.Text = $"Клиент: {booking.clients.full_name}\nТелефон: {booking.clients.phone ?? "—"}\nEmail: {booking.clients.email ?? "—"}";
            txtDateTimeInfo.Text = $"Дата: {booking.booking_date:dd.MM.yyyy}\nВремя: {booking.start_time:hh\\:mm} – {booking.end_time:hh\\:mm}";
            txtStatusInfo.Text = $"Статус: {booking.status} | Кассир: {booking.users.full_name}";
            var items = _context.booking_items.Where(bi => bi.booking_id == booking.id).ToList();
            dgBookingItems.ItemsSource = items;
            decimal total = items.Sum(i => i.price * i.quantity);
            txtTotalInfo.Text = $"Итого: {total:F2} ₽";
            if (_isAdmin)
            {
                cmbChangeStatus.SelectedItem = cmbChangeStatus.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == booking.status);
            }

            lblDetailsTitle.Text = $"Бронирование №{booking.id}";
        }

        private void ClearDetails()
        {
            txtClientInfo.Text = "";
            txtDateTimeInfo.Text = "";
            txtStatusInfo.Text = "";
            dgBookingItems.ItemsSource = null;
            txtTotalInfo.Text = "";
            lblDetailsTitle.Text = "Детали бронирования";
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBooking == null)
            {
                MessageBox.Show("Выберите бронирование.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_isAdmin) return;

            string newStatus = ((ComboBoxItem)cmbChangeStatus.SelectedItem).Content.ToString();
            if (newStatus == _selectedBooking.status)
            {
                MessageBox.Show("Статус не изменился.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                _selectedBooking.status = newStatus;
                _context.SaveChanges();
                LoadBookings();
                ShowBookingDetails(_selectedBooking);
                txtStatus.Text = $"Статус бронирования №{_selectedBooking.id} изменён на \"{newStatus}\"";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}