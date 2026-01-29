using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HousingManagementApp
{
    public partial class RequestReportsWindow : Window
    {
        public RequestReportsWindow()
        {
            InitializeComponent();
            LoadReports();
        }

        private void LoadReports()
        {
            try
            {
                using (var context = HousingStockManagementDBEntities.GetContext())
                {
                    LoadFinancialReports(context);

                    LoadHousingStockReports(context);

                    LoadDebtReports(context);

                    LoadOwnersReports(context);

                    StatusText.Text = $"Отчеты загружены {DateTime.Now:dd.MM.yyyy HH:mm}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFinancialReports(HousingStockManagementDBEntities context)
        {
            try
            {
                var payments = context.Payments.ToList();

                double totalAccrual = payments.Where(p => p.Accrual.HasValue).Sum(p => p.Accrual.Value);
                double totalPaid = payments.Where(p => p.Paid.HasValue).Sum(p => p.Paid.Value);
                double totalDebt = totalAccrual - totalPaid;
                double paymentPercent = totalAccrual > 0 ? (totalPaid / totalAccrual * 100) : 0;

                TotalAccrualText.Text = $"{totalAccrual:N2} ₽";
                TotalPaidText.Text = $"{totalPaid:N2} ₽";
                TotalDebtText.Text = $"{totalDebt:N2} ₽";
                PaymentPercentText.Text = $"{paymentPercent:N1} %";

                var recentPayments = (from p in context.Payments
                                      join o in context.Owner on p.OwnerID equals o.OwnerID
                                      join c in context.City on p.CityID equals c.CityID
                                      join s in context.Street on p.StreetID equals s.StreetID
                                      join hn in context.HouseNumber on p.HouseNumberID equals hn.HouseNumberID
                                      orderby p.PaymentID descending
                                      select new
                                      {
                                          p.PaymentID,
                                          OwnerName = o.OwnerFIO,
                                          p.Period,
                                          p.Accrual,
                                          p.Paid,
                                          CityName = c.CityName,
                                          StreetName = s.StreetName,
                                          HouseNumber = hn.HouseNumber1,
                                          p.Flat
                                      }).Take(20).ToList();

                PaymentsGrid.ItemsSource = recentPayments.Select(p => new
                {
                    p.PaymentID,
                    p.OwnerName,
                    p.Period,
                    p.Accrual,
                    p.Paid,
                    Address = FormatAddress(p.CityName, p.StreetName, p.HouseNumber, p.Flat),
                    Status = (p.Paid ?? 0) >= (p.Accrual ?? 0) ? "Оплачено" : "Задолженность"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки финансовых отчетов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHousingStockReports(HousingStockManagementDBEntities context)
        {
            try
            {
                var houses = context.HousingStock.ToList();

                int totalHouses = houses.Count;
                double totalSquare = houses.Where(h => h.Square.HasValue).Sum(h => h.Square.Value);
                int totalFlats = houses.Where(h => h.Flats.HasValue).Sum(h => h.Flats.Value);
                var years = houses.Where(h => h.Year.HasValue).Select(h => h.Year.Value).ToList();
                double avgYear = years.Any() ? years.Average() : 0;

                TotalHousesText.Text = totalHouses.ToString();
                TotalSquareText.Text = $"{totalSquare:N1} м²";
                TotalFlatsText.Text = totalFlats.ToString();
                AvgYearText.Text = avgYear.ToString("N0");

                var housesList = (from hs in context.HousingStock
                                  join c in context.City on hs.CityID equals c.CityID
                                  join s in context.Street on hs.StreetID equals s.StreetID
                                  join hn in context.HouseNumber on hs.HouseNumberID equals hn.HouseNumberID
                                  select new
                                  {
                                      CityName = c.CityName,
                                      StreetName = s.StreetName,
                                      HouseNumber = hn.HouseNumber1,
                                      hs.Year,
                                      hs.Floors,
                                      hs.Flats,
                                      hs.Square,
                                      ManagementStart = hs.BeginningManagement
                                  }).ToList();

                HousesGrid.ItemsSource = housesList.Select(h => new
                {
                    Address = FormatAddress(h.CityName, h.StreetName, h.HouseNumber, null),
                    h.Year,
                    h.Floors,
                    h.Flats,
                    h.Square,
                    ManagementStart = h.ManagementStart
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов по жилищному фонду: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDebtReports(HousingStockManagementDBEntities context)
        {
            try
            {
                var arrears = context.Arrears.ToList();

                double totalWaterDebt = arrears.Where(a => a.Water.HasValue).Sum(a => a.Water.Value);
                double totalElectricDebt = arrears.Where(a => a.ElectricPower.HasValue).Sum(a => a.ElectricPower.Value);

                TotalWaterDebtText.Text = $"{totalWaterDebt:N2} ₽";
                TotalElectricDebtText.Text = $"{totalElectricDebt:N2} ₽";

                var topDebts = (from a in context.Arrears
                                join o in context.Owner on a.OwnerID equals o.OwnerID
                                join c in context.City on a.CityID equals c.CityID
                                join s in context.Street on a.StreetID equals s.StreetID
                                join hn in context.HouseNumber on a.HouseNumberID equals hn.HouseNumberID
                                where (a.Water.HasValue && a.Water > 0) || (a.ElectricPower.HasValue && a.ElectricPower > 0)
                                select new
                                {
                                    OwnerName = o.OwnerFIO,
                                    a.Phone,
                                    CityName = c.CityName,
                                    StreetName = s.StreetName,
                                    HouseNumber = hn.HouseNumber1,
                                    a.Flat,
                                    a.Water,
                                    a.ElectricPower
                                }).ToList()
                               .Select(d => new
                               {
                                   d.OwnerName,
                                   Phone = d.Phone ?? "не указан",
                                   Address = FormatAddress(d.CityName, d.StreetName, d.HouseNumber, d.Flat),
                                   d.Flat,
                                   Water = d.Water ?? 0,
                                   ElectricPower = d.ElectricPower ?? 0,
                                   Total = (d.Water ?? 0) + (d.ElectricPower ?? 0)
                               })
                               .Where(d => d.Total > 0)
                               .OrderByDescending(d => d.Total)
                               .Take(10)
                               .ToList();

                DebtGrid.ItemsSource = topDebts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов по задолженностям: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOwnersReports(HousingStockManagementDBEntities context)
        {
            try
            {
                var owners = context.Owner.ToList();
                var ownersWithPhone = context.Arrears
                    .Where(a => a.Phone != null && a.Phone.Trim() != "")
                    .Select(a => a.OwnerID)
                    .Distinct()
                    .Count();

                TotalOwnersText.Text = owners.Count.ToString();
                OwnersWithPhoneText.Text = ownersWithPhone.ToString();

                var ownersQuery = from o in context.Owner
                                  join a in context.Arrears on o.OwnerID equals a.OwnerID into arrearsJoin
                                  from arrears in arrearsJoin.DefaultIfEmpty()
                                  join c in context.City on arrears.CityID equals c.CityID into cityJoin
                                  from city in cityJoin.DefaultIfEmpty()
                                  join s in context.Street on arrears.StreetID equals s.StreetID into streetJoin
                                  from street in streetJoin.DefaultIfEmpty()
                                  join hn in context.HouseNumber on arrears.HouseNumberID equals hn.HouseNumberID into houseJoin
                                  from houseNumber in houseJoin.DefaultIfEmpty()
                                  select new
                                  {
                                      o.OwnerFIO,
                                      Phone = arrears != null ? arrears.Phone : null,
                                      CityName = city != null ? city.CityName : null,
                                      StreetName = street != null ? street.StreetName : null,
                                      HouseNumber = houseNumber != null ? houseNumber.HouseNumber1 : null,
                                      Flat = arrears != null ? arrears.Flat : null,
                                      Water = arrears != null ? arrears.Water : null,
                                      ElectricPower = arrears != null ? arrears.ElectricPower : null
                                  };

                var ownersList = ownersQuery.ToList()
                    .GroupBy(x => x.OwnerFIO)
                    .Select(g =>
                    {
                        var first = g.First();
                        string address = "";
                        if (first.CityName != null && first.StreetName != null && first.HouseNumber != null)
                        {
                            address = FormatAddress(first.CityName, first.StreetName, first.HouseNumber, first.Flat);
                        }
                        else
                        {
                            address = "не указан";
                        }

                        double totalDebt = g.Sum(x => (x.Water ?? 0) + (x.ElectricPower ?? 0));

                        return new
                        {
                            OwnerFIO = first.OwnerFIO,
                            Phone = first.Phone ?? "не указан",
                            Address = address,
                            TotalDebt = totalDebt,
                            PaymentStatus = totalDebt > 0 ? "Есть долг" : "Нет долга"
                        };
                    })
                    .OrderByDescending(x => x.TotalDebt)
                    .ToList();

                OwnersGrid.ItemsSource = ownersList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов по собственникам: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatAddress(string cityName, string streetName, string houseNumber, int? flat)
        {
            List<string> parts = new List<string>();

            if (!string.IsNullOrEmpty(cityName))
                parts.Add(cityName.Trim());

            if (!string.IsNullOrEmpty(streetName))
                parts.Add(streetName.Trim());

            if (!string.IsNullOrEmpty(houseNumber))
                parts.Add($"д.{houseNumber.Trim()}");

            string result = string.Join(", ", parts);

            if (flat.HasValue)
                result += $", кв.{flat.Value}";

            return result;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}