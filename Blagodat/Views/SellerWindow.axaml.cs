using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Data;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace Blagodat.Views
{
    
    public partial class SellerWindow : BaseWindow
    {
        private readonly User21Context _db;
        private List<OrderService> _services;
        private decimal _total;

        public SellerWindow()
        {
            InitializeComponent();
            _db = new User21Context();
            _services = new List<OrderService>();
            LoadData();
        }

        private async void LoadData()
        {
            var conn = new NpgsqlConnection(_db.Database.GetDbConnection().ConnectionString);
            await conn.OpenAsync();
            try
            {
                var clients = new List<Client>();
                using var cmd1 = new Npgsql.NpgsqlCommand("SELECT client_id,code,full_name,address,birth_date,passport_data,email,password FROM clients", conn);
                using var r1 = await cmd1.ExecuteReaderAsync();
                while (await r1.ReadAsync())
                    clients.Add(new Client {
                        Id = r1.GetInt32(0), Code = r1.GetString(1),
                        LastName = r1.GetString(2).Split(' ')[0],
                        FirstName = r1.GetString(2).Split(' ').Length > 1 ? r1.GetString(2).Split(' ')[1] : "",
                        Address = r1.GetString(3), BirthDate = r1.GetDateTime(4),
                        Passport = r1.GetString(5), Email = r1.GetString(6), Password = r1.GetString(7)
                    });
                
                ClientComboBox.ItemsSource = clients;
                ClientComboBox.DisplayMemberBinding = new Binding("FullName");

                using var cmd2 = new Npgsql.NpgsqlCommand("SELECT service_id,code,cost_per_hour,name FROM services", conn);
                var services = new List<Service>();
                using var r2 = await cmd2.ExecuteReaderAsync();
                while (await r2.ReadAsync())
                    services.Add(new Service {
                        Id = r2.GetInt32(0), Code = r2.GetString(1),
                        Cost = r2.GetDecimal(2), Title = r2.GetString(3)
                    });
                
                ServiceComboBox.ItemsSource = services;
                ServiceComboBox.DisplayMemberBinding = new Binding("Title");

                using var cmd3 = new Npgsql.NpgsqlCommand("SELECT order_id,order_code,creation_date,client_code,closing_date,status FROM orders ORDER BY creation_date DESC", conn);
                var orders = new List<Order>();
                using var r3 = await cmd3.ExecuteReaderAsync();
                while (await r3.ReadAsync())
                    orders.Add(new Order {
                        Id = r3.GetInt32(0), OrderCode = r3.GetString(1), OrderDate = r3.GetDateTime(2),
                        ClientCode = r3.GetString(3),
                        ClosingDate = !r3.IsDBNull(4) ? DateOnly.FromDateTime(r3.GetDateTime(4)) : null,
                        Status = !r3.IsDBNull(5) ? r3.GetString(5) : "", TotalCost = 0
                    });
                
                OrdersHistoryGrid.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка загрузки данных: {ex.Message}");
            }

            try
            {
                using var cmd = new Npgsql.NpgsqlCommand("SELECT order_id FROM order_details ORDER BY order_id DESC LIMIT 1", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                int lastId = await reader.ReadAsync() ? reader.GetInt32(0) + 1 : 1;
                
                OrderNumberBox.Text = lastId.ToString();
            }
            catch
            {
                OrderNumberBox.Text = "1";
            }
        }

        private void OnOrderNumberKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) GenerateBarcode(); }

        private void OnGenerateBarcodeClick(object sender, RoutedEventArgs e) => GenerateBarcode();

        private void GenerateBarcode()
        {
            
            if (!int.TryParse(OrderNumberBox.Text, out int orderId))
            {
                ShowError("Введите корректный номер заказа");
                return;
            }
            
            var data = $"{orderId}{DateTime.Now:yyyyMMddHHmm}02{new Random().Next(100000, 999999)}";
            var writer = new QRCodeWriter();
            var matrix = writer.encode(data, BarcodeFormat.QR_CODE, 300, 300, new Dictionary<EncodeHintType, object> 
                { { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H }, { EncodeHintType.MARGIN, 1 } });
            
            var bmp = new System.Drawing.Bitmap(matrix.Width, matrix.Height);
            for (int x = 0; x < matrix.Width; x++)
                for (int y = 0; y < matrix.Height; y++)
                    bmp.SetPixel(x, y, matrix[x, y] ? System.Drawing.Color.Black : System.Drawing.Color.White);
            
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Barcodes");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"barcode_{orderId}.png");
            using var fs = new FileStream(path, FileMode.Create);
            bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
            
            ShowMessage("Штрих-код сгенерирован", $"Штрих-код для заказа {orderId} сохранен");
        }

        private void OnAddClientClick(object sender, RoutedEventArgs e)
        {
            
            ClientPanel.IsVisible = true;
        }

        private async void OnSaveClientClick(object sender, RoutedEventArgs e)
        {
            
            var email = ClientEmailBox.Text;
            var firstName = ClientFirstNameBox.Text;
            var lastName = ClientLastNameBox.Text;
            var passport = ClientPassportBox.Text;
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(passport))
            {
                await ShowError("Заполните обязательные поля");
                return;
            }

            var fullName = $"{lastName} {firstName}";
            
            var address = ClientAddressBox.Text ?? "";
            var birthDate = ClientBirthDatePicker.SelectedDate?.DateTime ?? DateTime.Now;

            try
            {
                using var conn = new Npgsql.NpgsqlConnection(_db.Database.GetConnectionString());
                await conn.OpenAsync();
                var code = $"CLT{new Random().Next(100, 1000)}";
                
                using var cmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO clients (client_id,code,full_name,passport_data,birth_date,address,email,password) VALUES (DEFAULT,@c,@fn,@pp,@bd,@ad,@em,@pw) RETURNING client_id", conn);
                
                cmd.Parameters.AddWithValue("c", code);
                cmd.Parameters.AddWithValue("fn", fullName);
                cmd.Parameters.AddWithValue("pp", passport);
                cmd.Parameters.AddWithValue("bd", birthDate);
                cmd.Parameters.AddWithValue("ad", address);
                cmd.Parameters.AddWithValue("em", email);
                cmd.Parameters.AddWithValue("pw", "password");
                
                int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                
                LoadData();
                
                ClientPanel.IsVisible = false;
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void OnCancelClientClick(object sender, RoutedEventArgs e) => ClientPanel.IsVisible = false; 

        private void OnAddServiceClick(object sender, RoutedEventArgs e)
        {
            
            var srv = ServiceComboBox.SelectedItem as Service;
            if (srv == null)
            {
                ShowError("\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u0443\u0441\u043b\u0443\u0433\u0443");
                return;
            }

            if (!int.TryParse(HoursBox.Text, out int h) || h <= 0)
            {
                ShowError("Введите корректное количество часов");
                return;
            }

            _services.Add(new OrderService { Service = srv, ServiceId = srv.Id, Hours = h, Cost = srv.Cost * h });
            _total = _services.Sum(s => s.Cost);
            
            
            OrderServicesGrid.ItemsSource = _services;
            TotalCostText.Text = $"{_total:C}";
        }

        private void OnRemoveServiceClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderService svc)
            {
                _services.Remove(svc);
                _total = _services.Sum(s => s.Cost);
                
                
                OrderServicesGrid.ItemsSource = _services;
                TotalCostText.Text = $"{_total:C}";
            }
        }

        private async void OnCreateOrderClick(object sender, RoutedEventArgs e)
        {
            
            if (!int.TryParse(OrderNumberBox.Text, out int orderId))
            {
                await ShowError("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u043a\u043e\u0440\u0440\u0435\u043a\u0442\u043d\u044b\u0439 \u043d\u043e\u043c\u0435\u0440 \u0437\u0430\u043a\u0430\u0437\u0430");
                return;
            }

            var client = ClientComboBox.SelectedItem as Client;
            if (client == null)
            {
                await ShowError("Выберите клиента");
                return;
            }

            if (_services.Count == 0)
            {
                await ShowError("Добавьте хотя бы одну услугу");
                return;
            }

            try
            {
                using var conn = new Npgsql.NpgsqlConnection(_db.Database.GetConnectionString());
                await conn.OpenAsync();
                var code = $"ORD{new Random().Next(100000, 999999)}";
                
                using var cmd = new Npgsql.NpgsqlCommand(
                    "INSERT INTO orders (order_id,order_code,creation_date,order_time,client_code,services,status,rental_time) VALUES (DEFAULT,@c,@cd,@ot,@cc,@s,@st,@rt) RETURNING order_id", conn);
                
                cmd.Parameters.AddWithValue("c", code);
                cmd.Parameters.AddWithValue("cd", DateTime.Now.Date);
                cmd.Parameters.AddWithValue("ot", DateTime.Now.TimeOfDay);
                cmd.Parameters.AddWithValue("cc", client.Code);
                cmd.Parameters.AddWithValue("s", string.Join(", ", _services.Select(s => s.Service.Title)));
                cmd.Parameters.AddWithValue("st", "New");
                cmd.Parameters.AddWithValue("rt", "1 hour");
                
                int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                
                foreach (var svc in _services)
                {
                    using var scmd = new Npgsql.NpgsqlCommand(
                        "INSERT INTO order_services (id,order_id,service_id) VALUES (DEFAULT,@oid,@sid) RETURNING id", conn);
                    scmd.Parameters.AddWithValue("oid", id);
                    scmd.Parameters.AddWithValue("sid", svc.ServiceId);
                    svc.Id = Convert.ToInt32(await scmd.ExecuteScalarAsync());
                }
                
                var order = new Order { Id = id, OrderDate = DateTime.Now, TotalCost = _total, Client = client };
                
                SaveOrderFiles(order);
                
                await ShowMessage("Заказ создан", $"Заказ №{id} успешно создан");
                
                _services.Clear();
                _total = 0;
                LoadData();
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка: {ex.Message}");
            }
        }
        
        private void SaveOrderFiles(Order order)
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orders"));
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Links"));
            
            var sb = new StringBuilder();
            sb.AppendLine($"Заказ №{order.Id}");
            sb.AppendLine($"Дата: {order.OrderDate}");
            sb.AppendLine($"Клиент: {order.Client.FullName}");
            sb.AppendLine($"Адрес: {order.Client.Address}");
            sb.AppendLine("Услуги:");
            
            foreach (var s in _services)
                sb.AppendLine($"- {s.Service.Title}: {s.Hours} ч. = {s.Cost:C}");
            
            sb.AppendLine($"Итого: {order.TotalCost:C}");
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Orders", $"order_{order.Id}.pdf"), sb.ToString());
            
            var data = Convert.ToBase64String(Encoding.UTF8.GetBytes($"дата_заказа={order.OrderDate:yyyy-MM-ddTHH:mm:ss}&номер_заказа={order.Id}"));
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Links", $"order_{order.Id}_link.txt"), $"https://wsrussia.ru/?data={data}");
        }

        private async void OnViewOrderDetailsClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Order order)
            {
                var details = await _db.OrderServices
                    .Include(os => os.Service)
                    .Where(os => os.OrderId == order.Id)
                    .ToListAsync();

                var msg = new StringBuilder();
                msg.AppendLine($"Заказ №{order.Id}");
                msg.AppendLine($"Дата: {order.OrderDate}");
                msg.AppendLine($"Клиент: {order.Client?.FullName ?? order.ClientCode}");
                msg.AppendLine("Услуги:");
                
                foreach (var d in details)
                    msg.AppendLine($"- {d.Service.Title}: {d.Cost:C}");
                
                msg.AppendLine($"Итого: {order.TotalCost:C}");

                await MessageBox.Show(this, $"Детали заказа №{order.Id}", msg.ToString());
            }
        }

        private async Task ShowError(string message) => await MessageBox.Show(this, "Ошибка", message);

        private async Task ShowMessage(string title, string message) => await MessageBox.Show(this, title, message);
    }
}
