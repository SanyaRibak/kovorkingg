using System.Windows;

namespace kovorkingg.Views
{
    public partial class ClientAddWindow : Window
    {
        public clients NewClient { get; private set; }

        public ClientAddWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО или название.");
                return;
            }
            NewClient = new clients
            {
                full_name = txtFullName.Text.Trim(),
                phone = txtPhone.Text.Trim(),
                email = txtEmail.Text.Trim()
            };
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}