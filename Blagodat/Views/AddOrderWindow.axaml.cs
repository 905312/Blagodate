using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blagodat.Views
{
    
    public partial class AddOrderWindow : BaseWindow
    {
        private User21Context _context;
        private List<ModelExtensions.OrderServiceViewModel> _orderServices = new List<ModelExtensions.OrderServiceViewModel>();
        private decimal _totalCost = 0;

        public AddOrderWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Заполните данные заказа";
            TotalCostText.Text = "0.00 ₽";
            
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    UserImage.Source = new Bitmap(logoPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
            }
            
      
            Dispatcher.UIThread.InvokeAsync(async () => 
            {
                try 
                {
                    
                    string newCode = await GenerateNewOrderCode();
                    OrderCodeTextBox.Text = newCode;
                    StatusBlock.Text = "Номер заказа сгенерирован";
                }
                catch (Exception ex)
                {
                    StatusBlock.Text = $"Ошибка генерации номера: {ex.Message}";
                    
                    OrderCodeTextBox.Text = $"ORD{100000 + new Random().Next(0, 900000)}";
                }
            });
            
            LoadClients();
            LoadServices();
        }
        
        private void LoadClients()
        {
            try
            {
                
                
                var clients = _context.Clients
                    .Select(c => new Client 
                    { 
                        Id = c.Id,
                        Code = c.Code,
                        FullName = c.FullName,
                        Email = c.Email,
                        PassportData = c.PassportData,
                        Address = c.Address,
                        BirthDate = c.BirthDate,
                        Password = c.Password
                    })
                    .OrderBy(c => c.FullName)
                    .ToList();
                    
                ClientComboBox.ItemsSource = clients;
                
                if (clients.Count > 0)
                {
                    ClientComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки клиентов: {ex.Message}";
            }
        }
        
        private void LoadServices()
        {
            try
            {
                
                var services = _context.Services.OrderBy(s => s.Name).ToList();
                ServiceComboBox.ItemsSource = services;
                
                if (services.Count > 0)
                {
                    ServiceComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки услуг: {ex.Message}";
            }
        }
        
        private async Task<string> GenerateNewOrderCode()
        {
            string prefix = "ORD";
            int nextNumber = 100000; 
            Random random = new Random();
            
            try
            {
              
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // Запрос последнего номера заказа с сортировкой по убыванию
                    string sql = "SELECT order_code FROM orders ORDER BY order_code DESC LIMIT 1";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        // Выполняем запрос и получаем последний номер заказа
                        object result = await cmd.ExecuteScalarAsync();
                        string lastOrderCode = result?.ToString();
                        
                        // Если есть последний номер заказа и он начинается с префикса
                        if (!string.IsNullOrEmpty(lastOrderCode) && lastOrderCode.StartsWith(prefix))
                        {
                            string numberPart = lastOrderCode.Substring(prefix.Length);
                            if (int.TryParse(numberPart, out int lastNumber))
                            {
                                nextNumber = lastNumber + 1;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                
                nextNumber = 100000 + random.Next(0, 900000);
            }
            
           
            if (nextNumber > 999999) {
                nextNumber = 100000; 
            }
            
            return $"{prefix}{nextNumber}";
        }
        
        private async void GenerateOrderNumber()
        {
            
            
            try
            {
                string newCode = await GenerateNewOrderCode();
                OrderCodeTextBox.Text = newCode;
                StatusBlock.Text = "Номер заказа сгенерирован";
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Ошибка генерации номера заказа: {ex.Message}";
                
                OrderCodeTextBox.Text = $"ORD{100000 + new Random().Next(0, 900000)}";
            }
        }

        private async void OnGenerateOrderNumberClick(object sender, RoutedEventArgs e)
        {
            
            
            StatusBlock.Text = "Генерация нового номера заказа...";
            
            try
            {
                // Генерируем новый номер при каждом нажатии кнопки
                string newOrderCode = await GenerateNewOrderCode();
                
               
                OrderCodeTextBox.Text = newOrderCode;
                StatusBlock.Text = $"Новый номер заказа сгенерирован: {newOrderCode}";
            }
            catch (Exception ex)
            {
                // В случае ошибки формируем резервный номер
                Random random = new Random();
                string fallbackCode = $"ORD{100000 + random.Next(0, 900000)}";
               
                OrderCodeTextBox.Text = fallbackCode;
                StatusBlock.Text = $"Новый номер заказа: {fallbackCode}. (Ошибка: {ex.Message})";
            }
        }

        private void OnAddClientClick(object sender, RoutedEventArgs e)
        {
            var window = new AddClientWindow();
            window.Initialize(CurrentUser);
            window.Closed += (s, args) => 
            {
                LoadClients();
            };
            window.Show();
        }

        private void OnAddServiceToOrderClick(object sender, RoutedEventArgs e)
        {
            
            
            if (ServiceComboBox.SelectedItem is Service selectedService)
            {
                int hours = (int)HoursUpDown.Value;
                decimal totalServiceCost = selectedService.CostPerHour * hours;
                
                var existingService = _orderServices.FirstOrDefault(os => os.Service.ServiceId == selectedService.ServiceId);
                
                if (existingService != null)
                {
                    existingService.Hours += hours;
                    existingService.TotalCost += totalServiceCost;
                }
                else
                {
                    var orderService = new ModelExtensions.OrderServiceViewModel
                    {
                        Service = selectedService,
                        ServiceId = selectedService.ServiceId,
                        Hours = hours,
                        TotalCost = totalServiceCost
                    };
                    
                    _orderServices.Add(orderService);
                }
                
                
                OrderServicesDataGrid.ItemsSource = null;
                OrderServicesDataGrid.ItemsSource = _orderServices;
                
                _totalCost = _orderServices.Sum(os => os.TotalCost);
                TotalCostText.Text = $"{_totalCost:C}";
                
                StatusBlock.Text = $"Услуга '{selectedService.Name}' добавлена в заказ";
            }
            else
            {
                StatusBlock.Text = "Ошибка: Не выбрана услуга";
            }
        }

        private void OnRemoveServiceClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is ModelExtensions.OrderServiceViewModel orderService)
            {
                _orderServices.Remove(orderService);
                
                
                OrderServicesDataGrid.ItemsSource = null;
                OrderServicesDataGrid.ItemsSource = _orderServices;
                
                _totalCost = _orderServices.Sum(os => os.TotalCost);
                TotalCostText.Text = $"{_totalCost:C}";
                
                StatusBlock.Text = $"Услуга '{orderService.Service.Name}' удалена из заказа";
            }
        }

        private async void OnCreateOrderClick(object sender, RoutedEventArgs e)
        {
            
            
            if (_orderServices.Count == 0)
            {
                StatusBlock.Text = "Ошибка: Добавьте хотя бы одну услугу в заказ";
                return;
            }
            
            if (ClientComboBox.SelectedItem is not Client selectedClient)
            {
                StatusBlock.Text = "Ошибка: Выберите клиента";
                return;
            }
            
            try
            {
                // Собираем данные для нового заказа
                string orderCode = OrderCodeTextBox.Text;
                string clientCode = selectedClient.Code;
                DateTime creationDate = DateTime.Today;
                string orderTime = DateTime.Now.TimeOfDay.ToString();
                string status = "Новый";
                int orderId = 0;
                
                // SQL запрос для создания заказа
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            string newCode = await GenerateNewOrderCode();
                            orderCode = newCode;
                            
                            OrderCodeTextBox.Text = newCode; 
                            
                            // Создаем новый заказ
                            string insertOrderSql = @"
                                INSERT INTO orders (order_code, client_code, creation_date, order_time, status, services, rental_time)
                                VALUES (@orderCode, @clientCode, @creationDate, @orderTime, @status, @services, @rentalTime)
                                RETURNING order_id";
                                
                            using (var cmd = new NpgsqlCommand(insertOrderSql, connection))
                            {
                                cmd.Parameters.AddWithValue("orderCode", orderCode);
                                cmd.Parameters.AddWithValue("clientCode", clientCode);
                                cmd.Parameters.AddWithValue("creationDate", creationDate);
                                cmd.Parameters.AddWithValue("orderTime", TimeSpan.Parse(orderTime));
                                cmd.Parameters.AddWithValue("status", status);
                                
                                // Формируем список услуг для колонки services
                                string services = string.Join(", ", _orderServices.Select(os => os.Service.Name));                                
                                cmd.Parameters.AddWithValue("services", services);
                                
                           
                                double totalHours = _orderServices.Sum(os => os.Hours);
                                int hours = (int)totalHours;
                                int minutes = (int)((totalHours - hours) * 60);
                                string rentalTime = $"{hours}:{minutes}:00";
                                cmd.Parameters.AddWithValue("rentalTime", TimeSpan.Parse(rentalTime));
                                cmd.Transaction = transaction;
                                
                                // Получаем ID нового заказа
                                orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }
                            
                            // Добавляем услуги заказа
                            string insertOrderServiceSql = @"
                                INSERT INTO order_services (order_id, service_id)
                                VALUES (@orderId, @serviceId)";
                                
                            foreach (var viewModel in _orderServices)
                            {
                                using (var cmd = new NpgsqlCommand(insertOrderServiceSql, connection))
                                {
                                    cmd.Parameters.AddWithValue("orderId", orderId);
                                    cmd.Parameters.AddWithValue("serviceId", viewModel.ServiceId);
                                    cmd.Transaction = transaction;
                                    
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            
                            await transaction.CommitAsync();
                            
                            await MessageBox.Show(this, "Успех", "Заказ успешно создан");
                            
                            // Очищаем заказ и готовимся к созданию нового
                            _orderServices.Clear();
                            
                            OrderServicesDataGrid.ItemsSource = null;
                            OrderServicesDataGrid.ItemsSource = _orderServices;
                            
                            _totalCost = 0;
                            TotalCostText.Text = "0.00 ₽";
                            
                            GenerateOrderNumber();
                            
                            StatusBlock.Text = "Заказ успешно создан. Готов к оформлению нового заказа";
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception($"Ошибка при создании заказа: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось создать заказ: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
