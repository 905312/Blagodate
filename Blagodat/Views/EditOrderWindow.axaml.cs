using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blagodat.Views
{
    
    public partial class EditOrderWindow : BaseWindow
    {
        private User21Context _context;
        private Order _selectedOrder;

        public EditOrderWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите заказ для редактирования";
            
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
            
            LoadClients();
            
            LoadOrders();
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
                
                if (_selectedOrder != null)
                {
                    var selectedClient = clients.FirstOrDefault(c => c.Code == _selectedOrder.ClientCode);
                    if (selectedClient != null)
                    {
                        ClientComboBox.SelectedItem = selectedClient;
                    }
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки клиентов: {ex.Message}";
            }
        }
        

        private async void LoadOrders(string searchTerm = "")
        {
            try
            {
                
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    //  SQL-запрос для получения заказов с информацией о клиентах
                   
                    string sql = @"
                        SELECT o.order_id, o.order_code, o.client_code, o.creation_date, o.status, 
                               o.order_time, 
                               o.closing_date, 
                               o.rental_time,
                               c.client_id, c.full_name as client_name
                        FROM orders o
                        LEFT JOIN clients c ON o.client_code = c.code";
                    
                    // Добавляем условие поиска, если указан поисковый запрос
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower();
                        sql += @"
                            WHERE LOWER(o.order_code) LIKE @searchTerm 
                            OR LOWER(c.full_name) LIKE @searchTerm 
                            OR LOWER(o.status) LIKE @searchTerm";
                    }
                    
                    // Добавляем сортировку по дате создания (от новых к старым)
                    sql += " ORDER BY o.creation_date DESC";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        // Добавляем параметр поиска
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            cmd.Parameters.AddWithValue("searchTerm", $"%{searchTerm}%");
                        }
                        
                        // Создаем список для хранения заказов
                        var orders = new List<Order>();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Создаем объект заказа
                                var order = new Order
                                {
                                    OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
                                    OrderCode = reader.GetString(reader.GetOrdinal("order_code")),
                                    ClientCode = reader.GetString(reader.GetOrdinal("client_code")),
                                    CreationDate = reader.GetDateTime(reader.GetOrdinal("creation_date")),
                                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status"))
                                };
                                
                                
                                if (!reader.IsDBNull(reader.GetOrdinal("order_time")))
                                {
                                   
                                    try
                                    {
                                        string columnType = reader.GetDataTypeName(reader.GetOrdinal("order_time"));
                                        if (columnType.Contains("time") && !columnType.Contains("character"))
                                        {
                               
                                            var timeValue = reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("order_time"));
                                            order.OrderTime = timeValue.ToString(@"hh\:mm");
                                        }
                                        else
                                        {
                                      
                                            order.OrderTime = reader.GetValue(reader.GetOrdinal("order_time")).ToString();
                                        }
                                    }
                                    catch
                                    {
                                      
                                        object value = reader.GetValue(reader.GetOrdinal("order_time"));
                                        order.OrderTime = value?.ToString() ?? "";
                                    }
                                }
                                
                                if (!reader.IsDBNull(reader.GetOrdinal("closing_date")))
                                {
                                    order.ClosingDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("closing_date")));
                                }
                                
                                if (!reader.IsDBNull(reader.GetOrdinal("rental_time")))
                                {
                                    
                                    try
                                    {
                                        string columnType = reader.GetDataTypeName(reader.GetOrdinal("rental_time"));
                                        if (columnType.Contains("time") && !columnType.Contains("character"))
                                        {
                                         
                                            var timeValue = reader.GetFieldValue<TimeSpan>(reader.GetOrdinal("rental_time"));
                                            order.RentalTime = timeValue.ToString(@"hh\:mm");
                                        }
                                        else
                                        {
                                          
                                            order.RentalTime = reader.GetValue(reader.GetOrdinal("rental_time")).ToString();
                                        }
                                    }
                                    catch
                                    {
                                       
                                        object value = reader.GetValue(reader.GetOrdinal("rental_time"));
                                        order.RentalTime = value?.ToString() ?? "";
                                    }
                                }
                                
                              
                                // Создаем навигационное свойство клиента
                                if (!reader.IsDBNull(reader.GetOrdinal("client_name")))
                                {
                                    order.ClientCodeNavigation = new Client
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("client_id")),
                                        Code = order.ClientCode,
                                        FullName = reader.GetString(reader.GetOrdinal("client_name"))
                                    };
                                }
                                
                                orders.Add(order);
                            }
                        }
                        
                        // Обновляем источник данных для ListBox
                        OrdersListBox.ItemsSource = orders;
                        
                        // Обновляем статус
                        StatusBlock.Text = $"Найдено заказов: {orders.Count}";
                    }
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            
            LoadOrders(SearchTextBox.Text);
        }

        private void OnOrderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is Order order)
            {
                _selectedOrder = order;
                
                LoadOrderDetails(order);
            }
        }
        
        private async void LoadOrderDetails(Order order)
        {
            try
            {
                // Сохраняем выбранный заказ
                _selectedOrder = order;
                
                // Загружаем данные заказа в элементы управления
                
                
                
                
                
                
                // Устанавливаем код заказа
                OrderCodeTextBox.Text = order.OrderCode;
                
                // Выбираем клиента в выпадающем списке
                for (int i = 0; i < ClientComboBox.Items.Count; i++)
                {
                    if (ClientComboBox.Items[i] is Client c && c.Code == order.ClientCode)
                    {
                        ClientComboBox.SelectedIndex = i;
                        break;
                    }
                }
                
                // Выбираем статус в выпадающем списке
                if (!string.IsNullOrEmpty(order.Status))
                {
                    for (int i = 0; i < StatusComboBox.Items.Count; i++)
                    {
                        if (StatusComboBox.Items[i] is ComboBoxItem item && 
                            item.Content.ToString() == order.Status)
                        {
                            StatusComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
                
             
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
          
                    string sql = @"
                        SELECT os.id, os.order_id, os.service_id,
                               1 as hours, -- используем константное значение 1
                               s.name, s.cost_per_hour
                        FROM order_services os
                        JOIN services s ON os.service_id = s.service_id
                        WHERE os.order_id = @orderId";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("orderId", order.OrderId);
                        
                        var orderServices = new List<OrderService>();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                
                                var service = new Service
                                {
                                    ServiceId = reader.GetInt32(reader.GetOrdinal("service_id")),
                                    Name = reader.GetString(reader.GetOrdinal("name")),
                                    CostPerHour = reader.GetDecimal(reader.GetOrdinal("cost_per_hour"))
                                };
                                
                               
                                var orderService = new OrderService
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
                                    ServiceId = reader.GetInt32(reader.GetOrdinal("service_id")),
                                    Hours = reader.GetInt32(reader.GetOrdinal("hours")),
                                    Service = service
                                };
                                
                                orderServices.Add(orderService);
                            }
                        }
                        
                        
                        OrderServicesDataGrid.ItemsSource = orderServices;
                    }
                }
                
                StatusBlock.Text = $"Редактирование заказа: {order.OrderCode}";
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки данных заказа: {ex.Message}";
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
                return;
                
            try
            {
                
                
                
                
                if (ClientComboBox.SelectedItem is not Client selectedClient)
                {
                    StatusBlock.Text = "Ошибка: Выберите клиента";
                    return;
                }
                
                string selectedStatus = StatusComboBox.SelectedItem as string;
                if (string.IsNullOrEmpty(selectedStatus))
                {
                    StatusBlock.Text = "Ошибка: Выберите статус заказа";
                    
                    
                    
                    _selectedOrder.OrderCode = OrderCodeTextBox?.Text ?? _selectedOrder.OrderCode;
                    _selectedOrder.ClientCode = selectedClient.Code;
                    
                    _selectedOrder.CreationDate = OrderDatePicker?.SelectedDate.HasValue == true ? OrderDatePicker.SelectedDate.Value.Date : DateTime.Today;
                    _selectedOrder.Status = selectedStatus ?? _selectedOrder.Status;
                }
                
                _selectedOrder.Status = selectedStatus ?? _selectedOrder.Status;
                _selectedOrder.ClientCode = selectedClient.Code;
                
                
                
                _selectedOrder.OrderCode = OrderCodeTextBox?.Text ?? _selectedOrder.OrderCode;
                
                DateOnly? closingDate = null;
                if (selectedStatus == "Выполнен")
                {
                    closingDate = DateOnly.FromDateTime(DateTime.Today);
                }
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    string updateSql = @"
                        UPDATE orders SET 
                            status = @status,
                            client_code = @clientCode,
                            order_code = @orderCode";
                    
                    if (closingDate.HasValue)
                    {
                        updateSql += ", closing_date = @closingDate";
                    }
                    
                    updateSql += " WHERE order_id = @orderId";
                    
                    using (var cmd = new NpgsqlCommand(updateSql, connection))
                    {
                        // Добавляем параметры
                        cmd.Parameters.AddWithValue("status", _selectedOrder.Status);
                        cmd.Parameters.AddWithValue("clientCode", _selectedOrder.ClientCode);
                        cmd.Parameters.AddWithValue("orderCode", _selectedOrder.OrderCode);
                        cmd.Parameters.AddWithValue("orderId", _selectedOrder.OrderId);
                        
                        if (closingDate.HasValue)
                        {
                            cmd.Parameters.AddWithValue("closingDate", closingDate.Value);
                        }
                        
                        // Выполняем запрос
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                
                await MessageBox.Show(this, "Успех", "Данные заказа успешно обновлены");
                StatusBlock.Text = $"Заказ {_selectedOrder.OrderCode} успешно обновлен";
                
                LoadOrders();
                
                
                for (int i = 0; i < OrdersListBox.Items.Count; i++)
                {
                    if (OrdersListBox.Items[i] is Order o && o.OrderId == _selectedOrder.OrderId)
                    {
                        OrdersListBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить данные заказа: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private async void OnGenerateBarcodeClick(object sender, RoutedEventArgs e)
        {
            if (_selectedOrder == null)
                return;
            
            // Генерируем штрих-код напрямую
            var data = $"{_selectedOrder.OrderId}{DateTime.Now:yyyyMMddHHmm}01{new Random().Next(100000, 999999)}";
            var writer = new ZXing.QrCode.QRCodeWriter();
            var matrix = writer.encode(data, ZXing.BarcodeFormat.QR_CODE, 300, 300);
            
            var bmp = new System.Drawing.Bitmap(matrix.Width, matrix.Height);
            for (int x = 0; x < matrix.Width; x++)
                for (int y = 0; y < matrix.Height; y++)
                    bmp.SetPixel(x, y, matrix[x, y] ? System.Drawing.Color.Black : System.Drawing.Color.White);
            
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Barcodes");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"barcode_{_selectedOrder.OrderId}.png");
            using var fs = new FileStream(path, FileMode.Create);
            bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
            
            StatusBlock.Text = $"Штрих-код для заказа {_selectedOrder.OrderId} сохранен";
            await MessageBox.Show(this, "Успех", $"Штрих-код для заказа {_selectedOrder.OrderId} сохранен в папке {dir}");
        }
    }
}
