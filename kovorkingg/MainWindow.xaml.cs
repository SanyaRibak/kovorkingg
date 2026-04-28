using System.Linq;
using System.Windows;
using System.Windows.Input;
using kovorkingg.Views;

namespace kovorkingg.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            this.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new praktikaEntities())
            {
                var user = context.users.FirstOrDefault(u => u.username == username && u.password_hash == password);
                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Window targetWindow = null;
                if (user.role == "admin")
                {
                    targetWindow = new AdminWindow(user);
                }
                else if (user.role == "cashier")
                {
                    targetWindow = new CashierWindow(user);
                    targetWindow = new CashierWindow(user);
                }
                else
                {
                    MessageBox.Show("Неизвестная роль пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                targetWindow.Show();
                this.Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}