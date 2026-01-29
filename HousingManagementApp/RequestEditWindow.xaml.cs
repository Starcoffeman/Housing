using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HousingManagementApp
{
    public partial class RequestEditWindow : Window
    {
        private int? _requestId = null;

        public RequestEditWindow(int? requestId = null)
        {
            InitializeComponent();
            _requestId = requestId;

            if (_requestId.HasValue)
            {
                TitleText.Text = "Редактирование заявки";
                Title = "Редактирование заявки";
            }
            else
            {
                TitleText.Text = "Новая заявка";
                Title = "Новая заявка";
            }

            LoadComboBoxes();
            LoadRequestData();
        }

        private void LoadComboBoxes()
        {
            try
            {
                var context = HousingStockManagementDBEntities.GetContext();

                // Загрузка адресов (жилой фонд)
                var dbAddresses = (from hs in context.HousingStock
                                   join c in context.City on hs.CityID equals c.CityID
                                   join s in context.Street on hs.StreetID equals s.StreetID
                                   join hn in context.HouseNumber on hs.HouseNumberID equals hn.HouseNumberID
                                   select new
                                   {
                                       CityID = c.CityID,
                                       CityName = c.CityName,
                                       StreetID = s.StreetID,
                                       StreetName = s.StreetName,
                                       HouseNumberID = hn.HouseNumberID,
                                       HouseNumber = hn.HouseNumber1
                                   }).ToList();

                // Форматируем в памяти
                var addresses = dbAddresses.Select(a => new
                {
                    a.CityID,
                    a.StreetID,
                    a.HouseNumberID,
                    FullAddress = $"{a.CityName?.Trim() ?? ""}, {a.StreetName?.Trim() ?? ""}, д.{a.HouseNumber?.Trim() ?? ""}"
                }).ToList();

                AddressComboBox.ItemsSource = addresses;
                AddressComboBox.DisplayMemberPath = "FullAddress";
                AddressComboBox.SelectedValuePath = "HouseNumberID";

                // Загрузка владельцев (они же могут быть заявителями)
                var owners = context.Owner.ToList();
                ExecutorComboBox.ItemsSource = owners;
                ExecutorComboBox.DisplayMemberPath = "OwnerFIO";
                ExecutorComboBox.SelectedValuePath = "OwnerID";

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRequestData()
        {
            try
            {
                if (!_requestId.HasValue)
                {
                    StatusComboBox.SelectedIndex = 0;
                    FlatTextBox.Text = "";
                    return;
                }

                var context = HousingStockManagementDBEntities.GetContext();
                var payment = context.Payments.Find(_requestId.Value);

                if (payment == null)
                {
                    MessageBox.Show($"Заявка #{_requestId.Value} не найдена",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                FlatTextBox.Text = payment.Flat?.ToString() ?? "";

                var owner = context.Owner.Find(payment.OwnerID);
                if (owner != null)
                {
                    ApplicantTextBox.Text = owner.OwnerFIO;
                }

                var arrears = context.Arrears.FirstOrDefault(a =>
                    a.OwnerID == payment.OwnerID &&
                    a.CityID == payment.CityID &&
                    a.StreetID == payment.StreetID &&
                    a.HouseNumberID == payment.HouseNumberID &&
                    a.Flat == payment.Flat);

                if (arrears == null)
                {
                    arrears = context.Arrears.FirstOrDefault(a =>
                        a.OwnerID == payment.OwnerID &&
                        a.CityID == payment.CityID &&
                        a.StreetID == payment.StreetID &&
                        a.HouseNumberID == payment.HouseNumberID);
                }

                if (arrears == null)
                {
                    arrears = context.Arrears.FirstOrDefault(a => a.OwnerID == payment.OwnerID);
                }

                PhoneTextBox.Text = arrears?.Phone ?? "";

                ProblemTextBox.Text = payment.Period ?? "";

                ExecutorComboBox.SelectedValue = payment.OwnerID;

                var address = AddressComboBox.Items.Cast<dynamic>()
                    .FirstOrDefault(a => a.HouseNumberID == payment.HouseNumberID);

                if (address != null)
                {
                    AddressComboBox.SelectedItem = address;
                }

                string periodText = payment.Period ?? "";
                string status = "Новая";

                if (periodText.Contains("Выполнена")) status = "Выполнена";
                else if (periodText.Contains("В работе")) status = "В работе";
                else if (periodText.Contains("Отменена")) status = "Отменена";

                for (int i = 0; i < StatusComboBox.Items.Count; i++)
                {
                    if (StatusComboBox.Items[i] is ComboBoxItem item && item.Content.ToString() == status)
                    {
                        StatusComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных заявки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(FlatTextBox.Text, out int flatNumber) || flatNumber <= 0)
            {
                MessageBox.Show("Введите корректный номер квартиры (целое положительное число)",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AddressComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите адрес",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(ApplicantTextBox.Text))
            {
                MessageBox.Show("Введите ФИО заявителя",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ExecutorComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите исполнителя",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProblemTextBox.Text))
            {
                MessageBox.Show("Введите описание проблемы",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = HousingStockManagementDBEntities.GetNewContext())
                {
                    var selectedAddress = (dynamic)AddressComboBox.SelectedItem;
                    var selectedExecutor = (Owner)ExecutorComboBox.SelectedItem;

                    Owner applicantOwner = null;
                    string applicantName = ApplicantTextBox.Text.Trim();

                    applicantOwner = context.Owner.FirstOrDefault(o => o.OwnerFIO == applicantName);

                    if (applicantOwner == null)
                    {
                        applicantOwner = new Owner
                        {
                            OwnerFIO = applicantName
                        };
                        context.Owner.Add(applicantOwner);
                        context.SaveChanges();
                    }

                    if (_requestId.HasValue)
                    {
                        var payment = context.Payments.Find(_requestId.Value);
                        if (payment != null)
                        {
                            payment.OwnerID = selectedExecutor.OwnerID;
                            payment.CityID = selectedAddress.CityID;
                            payment.StreetID = selectedAddress.StreetID;
                            payment.HouseNumberID = selectedAddress.HouseNumberID;
                            payment.Flat = flatNumber;
                            payment.Period = ProblemTextBox.Text;

                            string status = "Новая";
                            if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem)
                            {
                                status = selectedItem.Content?.ToString() ?? "Новая";
                            }
                            else if (StatusComboBox.SelectedItem != null)
                            {
                                status = StatusComboBox.SelectedItem.ToString();
                            }

                            if (status == "Выполнена")
                            {
                                payment.Accrual = 1000;
                                payment.Paid = 1000;
                            }
                            else if (status == "В работе")
                            {
                                payment.Accrual = 1000;
                                payment.Paid = 0;
                            }
                            else if (status == "Новая")
                            {
                                payment.Accrual = 0;
                                payment.Paid = 0;
                            }
                            else 
                            {
                                payment.Accrual = 0;
                                payment.Paid = 0;
                            }

                            context.SaveChanges();

                            UpdateOwnerPhone(applicantOwner.OwnerID, selectedAddress.CityID,
                                           selectedAddress.StreetID, selectedAddress.HouseNumberID,
                                           PhoneTextBox.Text, flatNumber);

                            MessageBox.Show($"Заявка #{_requestId.Value} успешно обновлена!",
                                "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        var newPayment = new Payments
                        {
                            OwnerID = selectedExecutor.OwnerID,
                            CityID = selectedAddress.CityID,
                            StreetID = selectedAddress.StreetID,
                            HouseNumberID = selectedAddress.HouseNumberID,
                            Flat = flatNumber,
                            Period = ProblemTextBox.Text,
                            Accrual = 0,
                            Paid = 0
                        };

                        string status = "Новая";
                        if (StatusComboBox.SelectedItem is ComboBoxItem selectedItem)
                        {
                            status = selectedItem.Content?.ToString() ?? "Новая";
                        }
                        else if (StatusComboBox.SelectedItem != null)
                        {
                            status = StatusComboBox.SelectedItem.ToString();
                        }

                        if (status == "Выполнена")
                        {
                            newPayment.Accrual = 1000;
                            newPayment.Paid = 1000;
                        }
                        else if (status == "В работе")
                        {
                            newPayment.Accrual = 1000;
                            newPayment.Paid = 0;
                        }

                        context.Payments.Add(newPayment);
                        context.SaveChanges();

                        UpdateOwnerPhone(applicantOwner.OwnerID, selectedAddress.CityID,
                                       selectedAddress.StreetID, selectedAddress.HouseNumberID,
                                       PhoneTextBox.Text, flatNumber);

                        MessageBox.Show($"Новая заявка успешно создана! ID: {newPayment.PaymentID}",
                            "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateOwnerPhone(int ownerId, int cityId, int streetId, int houseNumberId, string phone, int? flat = null)
        {
            try
            {
                var context = HousingStockManagementDBEntities.GetContext();

                Arrears arrears = null;

                if (flat.HasValue)
                {
                    arrears = context.Arrears.FirstOrDefault(a =>
                        a.OwnerID == ownerId &&
                        a.CityID == cityId &&
                        a.StreetID == streetId &&
                        a.HouseNumberID == houseNumberId &&
                        a.Flat == flat.Value);
                }

                if (arrears == null)
                {
                    arrears = context.Arrears.FirstOrDefault(a =>
                        a.OwnerID == ownerId &&
                        a.CityID == cityId &&
                        a.StreetID == streetId &&
                        a.HouseNumberID == houseNumberId);
                }

                if (arrears == null)
                {
                    arrears = context.Arrears.FirstOrDefault(a => a.OwnerID == ownerId);
                }

                if (arrears != null)
                {
                    arrears.Phone = phone;
                    arrears.CityID = cityId;
                    arrears.StreetID = streetId;
                    arrears.HouseNumberID = houseNumberId;
                    if (flat.HasValue) arrears.Flat = flat.Value;
                }
                else
                {
                    var newArrears = new Arrears
                    {
                        OwnerID = ownerId,
                        CityID = cityId,
                        StreetID = streetId,
                        HouseNumberID = houseNumberId,
                        Phone = phone
                    };
                    if (flat.HasValue) newArrears.Flat = flat.Value;

                    context.Arrears.Add(newArrears);
                }

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения телефона: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}