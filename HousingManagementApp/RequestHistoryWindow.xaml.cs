using System;
using System.Linq;
using System.Windows;

namespace HousingManagementApp
{
    public partial class RequestHistoryWindow : Window
    {
        public RequestHistoryWindow()
        {
            InitializeComponent();
            LoadFilterData();
            LoadHistory();
        }

        private void LoadFilterData()
        {
            try
            {
                using (var context = HousingStockManagementDBEntities.GetContext())
                {
                    var allExecutors = context.Owner
                        .OrderBy(o => o.OwnerFIO)
                        .ToList();

                    ExecutorFilterComboBox.ItemsSource = allExecutors;

                    var allAddressesData = (from hs in context.HousingStock
                                            join c in context.City on hs.CityID equals c.CityID
                                            join s in context.Street on hs.StreetID equals s.StreetID
                                            join hn in context.HouseNumber on hs.HouseNumberID equals hn.HouseNumberID
                                            select new AddressItem
                                            {
                                                HousingStockID = hs.HousingStockID,
                                                CityID = c.CityID,
                                                StreetID = s.StreetID,
                                                HouseNumberID = hn.HouseNumberID,
                                                CityName = c.CityName,
                                                StreetName = s.StreetName,
                                                HouseNumber = hn.HouseNumber1
                                            })
                                           .Distinct()
                                           .ToList();

                    foreach (var address in allAddressesData)
                    {
                        address.FullAddress = $"{address.CityName?.Trim() ?? ""}, {address.StreetName?.Trim() ?? ""}, д.{address.HouseNumber?.Trim() ?? ""}";
                    }

                    var allAddresses = allAddressesData.ToList();
                    allAddresses.Insert(0, new AddressItem
                    {
                        HousingStockID = 0,
                        CityID = 0,
                        StreetID = 0,
                        HouseNumberID = 0,
                        FullAddress = "Все адреса"
                    });

                    AddressFilterComboBox.ItemsSource = allAddresses;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки фильтров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHistory()
        {
            try
            {
                using (var context = HousingStockManagementDBEntities.GetContext())
                {
                    var historyData = (from p in context.Payments
                                       join c in context.City on p.CityID equals c.CityID
                                       join s in context.Street on p.StreetID equals s.StreetID
                                       join hn in context.HouseNumber on p.HouseNumberID equals hn.HouseNumberID
                                       join o in context.Owner on p.OwnerID equals o.OwnerID
                                       join a in context.Arrears on
                                         new { p.OwnerID, p.CityID, p.StreetID, p.HouseNumberID } equals
                                         new { a.OwnerID, a.CityID, a.StreetID, a.HouseNumberID } into arrearsJoin
                                       from arrears in arrearsJoin.DefaultIfEmpty()
                                       orderby p.PaymentID descending
                                       select new
                                       {
                                           p.PaymentID,
                                           p.CityID,
                                           p.StreetID,
                                           p.HouseNumberID,
                                           p.Flat,
                                           p.OwnerID,
                                           p.Period,
                                           p.Accrual,
                                           p.Paid,
                                           CityName = c.CityName,
                                           StreetName = s.StreetName,
                                           HouseNumber = hn.HouseNumber1,
                                           OwnerFIO = o.OwnerFIO,
                                           Phone = arrears != null ? arrears.Phone : "не указан"
                                       }).Take(100).ToList();

                    var history = historyData.Select(item =>
                    {
                        string fullAddress = $"{item.CityName?.Trim() ?? ""}, {item.StreetName?.Trim() ?? ""}, д.{item.HouseNumber?.Trim() ?? ""}";
                        if (item.Flat.HasValue)
                        {
                            fullAddress += $", кв.{item.Flat}";
                        }

                        string status = "Новая";

                        double? accrualNullable = item.Accrual;
                        double? paidNullable = item.Paid;

                        bool hasAccrual = accrualNullable.HasValue;
                        bool hasPaid = paidNullable.HasValue;

                        double accrual = hasAccrual ? accrualNullable.Value : 0;
                        double paid = hasPaid ? paidNullable.Value : 0;

                        if (hasAccrual && accrual > 0)
                        {
                            if (hasPaid && paid > 0)
                            {
                                if (paid >= accrual)
                                    status = "Выполнена";
                                else
                                    status = "Частично оплачено";
                            }
                            else
                            {
                                status = "Ожидает оплаты";
                            }
                        }

                        double balance = paid - accrual;

                        return new HistoryItem
                        {
                            RequestId = item.PaymentID,
                            FullAddress = fullAddress,
                            ExecutorName = item.OwnerFIO,
                            Status = status,
                            ProblemDescription = item.Period ?? "Заявка по коммунальным услугам",
                            CreatedDate = DateTime.Now.AddDays(-item.PaymentID * 2),
                            ApplicantName = item.OwnerFIO,
                            Phone = item.Phone,
                            FlatNumber = item.Flat.HasValue ? item.Flat.Value : 0,
                            AccrualAmount = accrual,
                            PaidAmount = paid,
                            Balance = balance
                        };
                    }).ToList();

                    HistoryGrid.ItemsSource = history;
                    StatusText.Text = $"Загружено {history.Count} заявок";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки истории: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = HousingStockManagementDBEntities.GetContext())
                {
                    var query = context.Payments.AsQueryable();

                    if (ExecutorFilterComboBox.SelectedItem is Owner selectedOwner)
                    {
                        query = query.Where(p => p.OwnerID == selectedOwner.OwnerID);
                    }

                    if (AddressFilterComboBox.SelectedItem is AddressItem selectedAddress && selectedAddress.HousingStockID > 0)
                    {
                        query = query.Where(p =>
                            p.CityID == selectedAddress.CityID &&
                            p.StreetID == selectedAddress.StreetID &&
                            p.HouseNumberID == selectedAddress.HouseNumberID);
                    }

                    var filteredData = (from p in query
                                        join c in context.City on p.CityID equals c.CityID
                                        join s in context.Street on p.StreetID equals s.StreetID
                                        join hn in context.HouseNumber on p.HouseNumberID equals hn.HouseNumberID
                                        join o in context.Owner on p.OwnerID equals o.OwnerID
                                        join a in context.Arrears on
                                          new { p.OwnerID, p.CityID, p.StreetID, p.HouseNumberID } equals
                                          new { a.OwnerID, a.CityID, a.StreetID, a.HouseNumberID } into arrearsJoin
                                        from arrears in arrearsJoin.DefaultIfEmpty()
                                        orderby p.PaymentID descending
                                        select new
                                        {
                                            p.PaymentID,
                                            p.CityID,
                                            p.StreetID,
                                            p.HouseNumberID,
                                            p.Flat,
                                            p.Period,
                                            p.Accrual,
                                            p.Paid,
                                            CityName = c.CityName,
                                            StreetName = s.StreetName,
                                            HouseNumber = hn.HouseNumber1,
                                            OwnerFIO = o.OwnerFIO,
                                            Phone = arrears != null ? arrears.Phone : "не указан"
                                        }).ToList();

                    var history = filteredData.Select(item =>
                    {
                        string fullAddress = $"{item.CityName?.Trim() ?? ""}, {item.StreetName?.Trim() ?? ""}, д.{item.HouseNumber?.Trim() ?? ""}";
                        if (item.Flat.HasValue)
                        {
                            fullAddress += $", кв.{item.Flat}";
                        }

                        string status = "Новая";

                        double? accrualNullable = item.Accrual;
                        double? paidNullable = item.Paid;

                        bool hasAccrual = accrualNullable.HasValue;
                        bool hasPaid = paidNullable.HasValue;

                        double accrual = hasAccrual ? accrualNullable.Value : 0;
                        double paid = hasPaid ? paidNullable.Value : 0;

                        if (hasAccrual && accrual > 0)
                        {
                            if (hasPaid && paid > 0)
                            {
                                if (paid >= accrual)
                                    status = "Выполнена";
                                else
                                    status = "Частично оплачено";
                            }
                            else
                            {
                                status = "Ожидает оплаты";
                            }
                        }

                        double balance = paid - accrual;

                        return new HistoryItem
                        {
                            RequestId = item.PaymentID,
                            FullAddress = fullAddress,
                            ExecutorName = item.OwnerFIO,
                            Status = status,
                            ProblemDescription = item.Period ?? "Заявка",
                            CreatedDate = DateTime.Now.AddDays(-item.PaymentID * 2),
                            ApplicantName = item.OwnerFIO,
                            Phone = item.Phone,
                            FlatNumber = item.Flat.HasValue ? item.Flat.Value : 0,
                            AccrualAmount = accrual,
                            PaidAmount = paid,
                            Balance = balance
                        };
                    }).ToList();

                    HistoryGrid.ItemsSource = history;
                    StatusText.Text = $"Найдено {history.Count} заявок";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExecutorFilterComboBox.SelectedIndex = -1;
                AddressFilterComboBox.SelectedIndex = 0; 

                LoadHistory();

                StatusText.Text = "Фильтры очищены. Показаны все заявки.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при очистке фильтров: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class AddressItem
    {
        public int HousingStockID { get; set; }
        public int CityID { get; set; }
        public int StreetID { get; set; }
        public int HouseNumberID { get; set; }
        public string CityName { get; set; }
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string FullAddress { get; set; }
    }

    public class HistoryItem
    {
        public int RequestId { get; set; }
        public string FullAddress { get; set; }
        public string ApplicantName { get; set; }     
        public string Phone { get; set; }              
        public string ExecutorName { get; set; }       
        public string Status { get; set; }
        public string ProblemDescription { get; set; }
        public DateTime CreatedDate { get; set; }
        public int FlatNumber { get; set; }           
        public double AccrualAmount { get; set; }     
        public double PaidAmount { get; set; }         
        public double Balance { get; set; }         
    }
}