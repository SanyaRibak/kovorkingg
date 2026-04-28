using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace kovorkingg.Views
{
    public partial class CashierWindow : Window
    {
        private readonly praktikaEntities _context;
        private readonly users _currentUser;
        private ObservableCollection<items> _itemsList;
        private ObservableCollection<CartItem> _cartItems;
        private int? _lastBookingId = null;

        public CashierWindow(users user)
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации окна (XAML):\n{ex.Message}\n\n{ex.StackTrace}",
                                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            try
            {
                _context = new praktikaEntities();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД:\n{ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            _currentUser = user;
            txtUserInfo.Text = $"{user.full_name} (кассир)";
            _cartItems = new ObservableCollection<CartItem>();
            dgCart.ItemsSource = _cartItems;

            LoadClients();
            LoadItems();
        }

        public class CartItem : INotifyPropertyChanged
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            private int _quantity;
            public int Quantity
            {
                get => _quantity;
                set { _quantity = value; OnPropertyChanged(nameof(Quantity)); OnPropertyChanged(nameof(Total)); }
            }
            public decimal Total => Price * Quantity;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadClients()
        {
            cmbClients.ItemsSource = _context.clients.OrderBy(c => c.full_name).ToList();
            if (cmbClients.Items.Count > 0) cmbClients.SelectedIndex = 0;
        }

        private void LoadItems()
        {
            var query = _context.items.Where(i => i.is_active).AsQueryable();

            if (cmbCategoryFilter.SelectedItem is ComboBoxItem item && item.Content.ToString() != "Все категории")
                query = query.Where(i => i.category == item.Content.ToString());

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                query = query.Where(i => i.name.Contains(txtSearch.Text));

            var sortItem = cmbSort.SelectedItem as ComboBoxItem;
            switch (sortItem?.Content.ToString())
            {
                case "По названию (А-Я)": query = query.OrderBy(i => i.name); break;
                case "По названию (Я-А)": query = query.OrderByDescending(i => i.name); break;
                case "По цене (возр.)": query = query.OrderBy(i => i.price); break;
                case "По цене (убыв.)": query = query.OrderByDescending(i => i.price); break;
                default: query = query.OrderBy(i => i.name); break;
            }

            _itemsList = new ObservableCollection<items>(query.ToList());
            dgItems.ItemsSource = _itemsList;
        }

        private void Search_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => LoadItems();
        private void Filter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => LoadItems();
        private void Sort_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => LoadItems();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadItems();
        private void Items_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgItems.SelectedItem as items;
            if (selected == null)
            {
                MessageBox.Show("Выберите услугу из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var existing = _cartItems.FirstOrDefault(c => c.ItemId == selected.id);
            if (existing != null)
                existing.Quantity++;
            else
                _cartItems.Add(new CartItem { ItemId = selected.id, Name = selected.name, Price = selected.price, Quantity = 1 });
            UpdateTotal();
        }

        private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
        {
            var item = ((FrameworkElement)sender).DataContext as CartItem;
            if (item != null) _cartItems.Remove(item);
            UpdateTotal();
        }

        private void Cart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && dgCart.SelectedItem is CartItem item)
                _cartItems.Remove(item);
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            decimal total = _cartItems.Sum(i => i.Total);
            txtTotalAmount.Text = $"{total:F2} ₽";
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ClientAddWindow();
            if (dialog.ShowDialog() == true)
            {
                _context.clients.Add(dialog.NewClient);
                _context.SaveChanges();
                LoadClients();
                cmbClients.SelectedValue = dialog.NewClient.id;
            }
        }

        private void CreateBooking_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (cmbClients.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!DateTime.TryParse(dpBookingDate.Text, out DateTime bookingDate))
            {
                MessageBox.Show("Некорректная дата.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!TimeSpan.TryParse(txtStartTime.Text, out TimeSpan start) || !TimeSpan.TryParse(txtEndTime.Text, out TimeSpan end))
            {
                MessageBox.Show("Некорректное время.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (end <= start)
            {
                MessageBox.Show("Время окончания должно быть позже начала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var booking = new bookings
                    {
                        user_id = _currentUser.id,
                        client_id = (int)cmbClients.SelectedValue,
                        booking_date = bookingDate,
                        start_time = start,
                        end_time = end,
                        total_amount = _cartItems.Sum(i => i.Total),
                        status = "confirmed"
                    };
                    _context.bookings.Add(booking);
                    _context.SaveChanges();

                    foreach (var cartItem in _cartItems)
                    {
                        _context.booking_items.Add(new booking_items
                        {
                            booking_id = booking.id,
                            item_id = cartItem.ItemId,
                            quantity = cartItem.Quantity,
                            price = cartItem.Price
                        });
                    }
                    _context.SaveChanges();
                    transaction.Commit();

                    _lastBookingId = booking.id;
                    btnPrintPdf.IsEnabled = true;
                    txtStatus.Text = $"Бронирование №{booking.id} успешно создано.";
                    MessageBox.Show($"Бронирование №{booking.id} оформлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    _cartItems.Clear();
                    UpdateTotal();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_lastBookingId == null) return;
            var booking = _context.bookings.Find(_lastBookingId);
            if (booking == null) return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"Booking_{booking.id}_{booking.booking_date:yyyyMMdd}.pdf"
            };
            if (sfd.ShowDialog() != true) return;

            GeneratePdf(booking, sfd.FileName);
            Process.Start(sfd.FileName);
        }

        private void GeneratePdf(bookings booking, string filePath)
        {
            Document doc = new Document(PageSize.A4, 36, 36, 50, 50);
            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            BaseFont baseFont = BaseFont.CreateFont("c:/windows/fonts/arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            iTextSharp.text.Font titleFont = new iTextSharp.text.Font(baseFont, 18, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            iTextSharp.text.Font normalFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL);
            iTextSharp.text.Font boldFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.BOLD);

            Paragraph title = new Paragraph("КОВОРКИНГ - ПОДТВЕРЖДЕНИЕ БРОНИРОВАНИЯ", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            doc.Add(title);
            doc.Add(new Paragraph($"Бронирование № {booking.id} от {booking.booking_date:dd.MM.yyyy}", normalFont) { SpacingAfter = 20 });

            var client = _context.clients.Find(booking.client_id);
            doc.Add(new Paragraph($"Клиент: {client?.full_name}", boldFont));
            if (!string.IsNullOrEmpty(client?.phone)) doc.Add(new Paragraph($"Телефон: {client.phone}", normalFont));
            doc.Add(new Paragraph($"Дата: {booking.booking_date:dd.MM.yyyy}  Время: {booking.start_time:hh\\:mm} – {booking.end_time:hh\\:mm}", normalFont));
            doc.Add(new Paragraph($"Статус: {booking.status}", normalFont) { SpacingAfter = 20 });

            PdfPTable table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 3, 1, 1, 1 });
            table.AddCell(new PdfPCell(new Phrase("Услуга", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("Цена", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("Кол-во", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("Сумма", boldFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });

            foreach (var bi in _context.booking_items.Where(b => b.booking_id == booking.id).ToList())
            {
                var item = _context.items.Find(bi.item_id);
                table.AddCell(new Phrase(item?.name ?? "?", normalFont));
                table.AddCell(new Phrase(bi.price.ToString("F2"), normalFont));
                table.AddCell(new Phrase(bi.quantity.ToString(), normalFont));
                table.AddCell(new Phrase((bi.price * bi.quantity).ToString("F2"), normalFont));
            }
            doc.Add(table);

            Paragraph total = new Paragraph($"ИТОГО: {booking.total_amount:F2} ₽", boldFont);
            total.Alignment = Element.ALIGN_RIGHT;
            total.SpacingBefore = 20;
            doc.Add(total);

            doc.Add(new Paragraph($"\nКассир: {_currentUser.full_name}", normalFont) { SpacingBefore = 30 });
            doc.Add(new Paragraph($"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}", normalFont));

            doc.Close();
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
            
            LoadClients();
        }

        private void Bookings_Click(object sender, RoutedEventArgs e)
        {
            BookingsWindow bookingsWindow = new BookingsWindow(_currentUser);
            bookingsWindow.Owner = this;
            bookingsWindow.ShowDialog();
        }
    }
}