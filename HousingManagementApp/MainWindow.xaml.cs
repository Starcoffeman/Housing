using System.Windows;

namespace HousingManagementApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RequestsButton_Click(object sender, RoutedEventArgs e)
        {
            var requestsWindow = new RequestsWindow();
            requestsWindow.Show();
            this.Close();
        }

        private void AddressesButton_Click(object sender, RoutedEventArgs e)
        {
            var addressesWindow = new AddressesWindow();
            addressesWindow.Show();
            this.Close();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var historyWindow = new RequestHistoryWindow();
            historyWindow.Show();
            this.Close();
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            var reportsWindow = new RequestReportsWindow();
            reportsWindow.Show();
            this.Close();
        }
    }
}