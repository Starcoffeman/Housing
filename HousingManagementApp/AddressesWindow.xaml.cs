using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace HousingManagementApp
{
    public partial class AddressesWindow : Window
    {
        public AddressesWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = HousingStockManagementDBEntities.GetContext())
                {
                    var addresses = (from hs in context.HousingStock
                                     join c in context.City on hs.CityID equals c.CityID
                                     join s in context.Street on hs.StreetID equals s.StreetID
                                     join hn in context.HouseNumber on hs.HouseNumberID equals hn.HouseNumberID
                                     select new
                                     {
                                         CityName = c.CityName,
                                         StreetName = s.StreetName,
                                         HouseNumber = hn.HouseNumber1,
                                         hs.Floors,
                                         hs.Flats,
                                         hs.Square
                                     }).ToList();

                    AddressesGrid.ItemsSource = addresses;

                    EmployeesGrid.ItemsSource = context.Owner.ToList();

                    StatusText.Text = $"Загружено: {addresses.Count} адресов, {context.Owner.Count()} сотрудников";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}