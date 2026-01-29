using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HousingManagementApp
{
    public partial class RequestsWindow : Window
    {
        public RequestsWindow()
        {
            InitializeComponent();
            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                using (var context = HousingStockManagementDBEntities.GetNewContext())
                {
                    var payments = context.Payments.ToList();

                    var requests = payments.Select(p =>
                    {
                        var city = context.City.Find(p.CityID);
                        var street = context.Street.Find(p.StreetID);
                        var houseNumber = context.HouseNumber.Find(p.HouseNumberID);
                        var owner = context.Owner.Find(p.OwnerID);

                        var arrears = context.Arrears.FirstOrDefault(a =>
                            a.OwnerID == p.OwnerID &&
                            a.CityID == p.CityID &&
                            a.StreetID == p.StreetID &&
                            a.HouseNumberID == p.HouseNumberID &&
                            a.Flat == p.Flat)
                        ?? context.Arrears.FirstOrDefault(a =>
                            a.OwnerID == p.OwnerID &&
                            a.CityID == p.CityID &&
                            a.StreetID == p.StreetID &&
                            a.HouseNumberID == p.HouseNumberID)
                        ?? context.Arrears.FirstOrDefault(a => a.OwnerID == p.OwnerID);

                        string status = "Новая";
                        string periodText = p.Period ?? "";

                        if (periodText.Contains("|"))
                        {
                            var parts = periodText.Split('|');
                            if (parts.Length > 1)
                            {
                                status = parts[1].Trim();
                            }
                        }

                        if (status == "Новая" && !string.IsNullOrEmpty(periodText) && !periodText.Contains("|"))
                        {
                            bool isPaid = p.Paid.HasValue && p.Paid > 0;
                            bool hasAccrual = p.Accrual.HasValue && p.Accrual > 0;

                            if (isPaid && hasAccrual && p.Paid >= p.Accrual)
                            {
                                status = "Выполнена";
                            }
                            else if (hasAccrual && (!isPaid || (p.Paid.HasValue && p.Paid < p.Accrual)))
                            {
                                status = "В работе";
                            }
                            else
                            {
                                status = "Новая";
                            }
                        }

                        string fullAddress = $"{(city?.CityName?.Trim() ?? "")}, {(street?.StreetName?.Trim() ?? "")}, " +
                                           $"д.{(houseNumber?.HouseNumber1?.Trim() ?? "")}";

                        if (p.Flat.HasValue)
                            fullAddress += $", кв.{p.Flat}";

                        return new
                        {
                            RequestId = p.PaymentID,
                            FullAddress = fullAddress,
                            ApplicantName = owner?.OwnerFIO ?? "Не указан",
                            Phone = arrears?.Phone ?? "не указан",
                            ExecutorName = owner?.OwnerFIO ?? "Не указан",
                            Status = status,
                            CreatedDate = GetCreatedDate(p.PaymentID),
                            CityID = p.CityID,
                            StreetID = p.StreetID,
                            HouseNumberID = p.HouseNumberID,
                            OwnerID = p.OwnerID,
                            Flat = p.Flat
                        };
                    }).ToList();

                    RequestsGrid.ItemsSource = requests;
                    StatusText.Text = $"Загружено {requests.Count} заявок";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}\n\nТип ошибки: {ex.GetType().Name}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DateTime GetCreatedDate(int paymentId)
        {
            return DateTime.Now.AddDays(-paymentId);
        }

        private dynamic GetSelectedRequest()
        {
            return RequestsGrid.SelectedItem;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void AddRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new RequestEditWindow();
            editWindow.ShowDialog();
            LoadRequests();
        }

        private void EditRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRequest = GetSelectedRequest();
            if (selectedRequest == null)
            {
                MessageBox.Show("Выберите заявку для редактирования",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new RequestEditWindow(selectedRequest.RequestId);
            editWindow.ShowDialog();
            LoadRequests();
        }

        private void DeleteRequestButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRequest = GetSelectedRequest();
            if (selectedRequest == null)
            {
                MessageBox.Show("Выберите заявку для удаления",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Удалить заявку #{selectedRequest.RequestId}?\n{selectedRequest.FullAddress}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = HousingStockManagementDBEntities.GetNewContext())
                    {
                        var paymentToDelete = context.Payments.Find(selectedRequest.RequestId);

                        if (paymentToDelete != null)
                        {
                            context.Payments.Remove(paymentToDelete);

                            context.SaveChanges();

                            MessageBox.Show($"Заявка #{selectedRequest.RequestId} успешно удалена!",
                                "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadRequests();
                        }
                        else
                        {
                            MessageBox.Show($"Заявка #{selectedRequest.RequestId} не найдена в базе данных",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
                {
                    MessageBox.Show($"Нельзя удалить заявку #{selectedRequest.RequestId}, так как с ней связаны другие данные.\n" +
                                  $"Детали: {dbEx.InnerException?.Message ?? dbEx.Message}",
                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}\n" +
                                  $"Тип ошибки: {ex.GetType().Name}\n" +
                                  $"Детали: {ex.InnerException?.Message ?? "Нет дополнительной информации"}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RequestsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditRequestButton_Click(sender, e);
        }
    }
}