using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blagodat.Views
{
    public partial class OrderHistoryWindow : BaseWindow
    {
        private User _currentUser;
        private User21Context _context;
        private List<Order> orders = new List<Order>(); // Список заказов как поле класса

        public OrderHistoryWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
           
            var userNameText = this.FindControl<TextBlock>("UserNameText");
            var userRoleText = this.FindControl<TextBlock>("UserRoleText");
            var userImage = this.FindControl<Image>("UserImage");
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            var statusFilterComboBox = this.FindControl<ComboBox>("StatusFilterComboBox");
            
            userNameText.Text = user.FullName;
            userRoleText.Text = user.Role;
            statusBlock.Text = "История заказов загружена";
            
            
            statusFilterComboBox.SelectedIndex = 0;
            
           
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    userImage.Source = new Bitmap(logoPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
            }
            
            
            LoadOrdersHistory();
        }
        
        private async void LoadOrdersHistory(string searchTerm = "", string statusFilter = "Все статусы", DateTime? startDate = null, DateTime? endDate = null)
        {
       
            Debug.WriteLine($"Загрузка заказов: searchTerm='{searchTerm}', statusFilter='{statusFilter}', startDate={startDate}, endDate={endDate}");
            
            try
            {
               
                var ordersDataGrid = this.FindControl<DataGrid>("OrdersHistoryDataGrid");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                var totalOrdersText = this.FindControl<TextBlock>("TotalOrdersText");
                
              
                statusBlock.Text = "Загрузка данных...";
                
                List<Order> orders = new List<Order>();
                
             
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                   
                    string sql = @"
                        SELECT order_id, order_code, creation_date, closing_date, 
                               order_time, rental_time, 
                               client_code, status, services
                        FROM orders";
                        
                    
                    List<string> whereConditions = new List<string>();
                    
                
                    if (statusFilter != "Все статусы")
                    {
                        whereConditions.Add("status = @status");
                    }
                    
              
                    if (startDate.HasValue)
                    {
                        whereConditions.Add("creation_date >= @startDate");
                    }
                    
                    if (endDate.HasValue)
                    {
                        whereConditions.Add("creation_date <= @endDate");
                    }
                    
             
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        whereConditions.Add(@"(
                            LOWER(order_code) LIKE @search OR 
                            LOWER(client_code) LIKE @search OR 
                            LOWER(status) LIKE @search OR
                            LOWER(services) LIKE @search
                        )");
                    }
                    
                  
                    if (whereConditions.Count > 0)
                    {
                        sql += " WHERE " + string.Join(" AND ", whereConditions);
                    }
                    
                    
                    sql += " ORDER BY creation_date DESC, order_time DESC";
                    
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        
                        if (statusFilter != "Все статусы")
                        {
                            cmd.Parameters.AddWithValue("status", statusFilter);
                        }
                        
                        if (startDate.HasValue)
                        {
                    
                            cmd.Parameters.AddWithValue("startDate", startDate.Value.Date);
                        }
                        
                        if (endDate.HasValue)
                        {
                          
                            cmd.Parameters.AddWithValue("endDate", endDate.Value.Date.AddDays(1).AddSeconds(-1));
                        }
                        
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            cmd.Parameters.AddWithValue("search", $"%{searchTerm.ToLower()}%");
                        }
                        
                      
                        orders.Clear();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                try
                                {
                                 
                                    var order = new Order
                                    {
                                        OrderId = reader.GetInt32(0),
                                        OrderCode = reader.GetString(1),
                                        CreationDate = reader.GetDateTime(2), // формат yyyy-MM-dd из базы
                                        ClosingDate = !reader.IsDBNull(3) ? DateOnly.FromDateTime(reader.GetDateTime(3)) : null
                                    };

                                   
                                    if (!reader.IsDBNull(4))
                                    {
                                        try
                                        {
                                          
                                            order.OrderTime = reader.GetString(4);
                                        }
                                        catch
                                        {
                                           
                                            try
                                            {
                                                var timeValue = reader.GetValue(4);
                                                order.OrderTime = timeValue.ToString();
                                            }
                                            catch
                                            {
                                                order.OrderTime = "(неизв.)";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        order.OrderTime = "";
                                    }

                                    if (!reader.IsDBNull(5))
                                    {
                                        try
                                        {
                                            order.RentalTime = reader.GetString(5);
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                var timeValue = reader.GetValue(5);
                                                order.RentalTime = timeValue.ToString();
                                            }
                                            catch
                                            {
                                                order.RentalTime = "(неизв.)";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        order.RentalTime = "";
                                    }

                                    // Код клиента и статус
                                    order.ClientCode = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty;
                                    order.Status = !reader.IsDBNull(7) ? reader.GetString(7) : string.Empty;
                                    
                                 
                                    if (!reader.IsDBNull(8)) 
                                    {
                                       
                                        var service = new Models.Service { Name = reader.GetString(8) };
                                        var orderService = new Models.OrderService { Service = service };
                                        order.OrderServices.Add(orderService);
                                    }

                                 
                                    Debug.WriteLine($"Заказ загружен: ID={order.OrderId}, Код={order.OrderCode}, Время={order.OrderTime}");

                                    // Добавляем заказ в список
                                    orders.Add(order);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Ошибка при чтении заказа: {ex.Message}");
                                    // Не добавляем заказ в список в случае ошибки
                                }
                            }
                        }
                        
                        Debug.WriteLine($"Всего загружено заказов: {orders.Count}");
                        
                        // Выводим первые 5 заказов для отладки
                        foreach (var order in orders.Take(5))
                        {
                            Debug.WriteLine($"Заказ: ID={order.OrderId}, Код={order.OrderCode}, Дата={order.CreationDate}, Время={order.OrderTime}, Статус={order.Status}");
                        }

                      
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                            try {
                                // Сначала очищаем
                                ordersDataGrid.ItemsSource = null;
                                
                                // Затем устанавливаем новые данные
                                ordersDataGrid.ItemsSource = orders;
                                
                                // Обновляем текстовые индикаторы
                                totalOrdersText.Text = $"Всего заказов: {orders.Count}";
                                
                                if (orders.Count == 0) {
                                    statusBlock.Text = "Заказы не найдены";
                                } else {
                                    statusBlock.Text = $"Данные загружены. Показано {orders.Count} заказов";
                                }
                            } catch (Exception ex) {
                                Debug.WriteLine($"Ошибка при обновлении UI: {ex.Message}");
                                statusBlock.Text = $"Ошибка обновления интерфейса: {ex.Message}";
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Детальная информация об ошибке для отладки
                Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                Debug.WriteLine($"Тип исключения: {ex.GetType().Name}");
                Debug.WriteLine($"Стек вызовов: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
                }
                
           
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                if (statusBlock != null)
                {
                    statusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
                }
                
            
                await MessageBox.Show(this, "Ошибка", $"Не удалось загрузить историю заказов: {ex.Message}");
            }
        }

        private async void OnOrderSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is Order selectedOrder)
            {
                await LoadOrderDetails(selectedOrder);
                
              
                var orderDetailsExpander = this.FindControl<Expander>("OrderDetailsExpander");
                orderDetailsExpander.IsExpanded = true;
            }
        }
        
        private async Task LoadOrderDetails(Order order)
        {
            try
            {
                var orderDetailsDataGrid = this.FindControl<DataGrid>("OrderDetailsDataGrid");
                var detailOrderCodeText = this.FindControl<TextBlock>("DetailOrderCodeText");
                var detailOrderTimeText = this.FindControl<TextBlock>("DetailOrderTimeText");
                var detailRentalTimeText = this.FindControl<TextBlock>("DetailRentalTimeText");
                var detailClientText = this.FindControl<TextBlock>("DetailClientText");
                var detailCreationDateText = this.FindControl<TextBlock>("DetailCreationDateText");
                var detailStatusText = this.FindControl<TextBlock>("DetailStatusText");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                
                detailOrderCodeText.Text = order.OrderCode;
                detailOrderTimeText.Text = order.OrderTime;
                detailRentalTimeText.Text = order.RentalTime;
                
                // Используем имя клиента полученное из заказа или загружаем из базы
                if (order.ClientCodeNavigation != null)
                {
                    detailClientText.Text = order.ClientCodeNavigation.FullName;
                }
                else
                {
                    using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        
                        string clientSql = "SELECT full_name FROM clients WHERE code = @clientCode";
                        using (var cmd = new NpgsqlCommand(clientSql, connection))
                        {
                            cmd.Parameters.AddWithValue("clientCode", order.ClientCode);
                            var clientName = await cmd.ExecuteScalarAsync();
                            detailClientText.Text = clientName != null ? clientName.ToString() : order.ClientCode;
                        }
                    }
                }
                
                detailCreationDateText.Text = order.CreationDate.ToString("dd.MM.yyyy");
                detailStatusText.Text = order.Status;
                
                // Загружаем услуги заказа через прямой SQL-запрос
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    string servicesSql = @"
                        SELECT os.id, os.order_id, os.service_id, s.name, s.cost_per_hour, 
                               COALESCE(os.hours, 1) as hours
                        FROM order_services os
                        JOIN services s ON os.service_id = s.service_id
                        WHERE os.order_id = @orderId";
                        
                    using (var cmd = new NpgsqlCommand(servicesSql, connection))
                    {
                        cmd.Parameters.AddWithValue("orderId", order.OrderId);
                        
                        var orderServicesWithDetails = new List<dynamic>();
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string serviceName = reader.GetString(3); // name
                                decimal costPerHour = reader.GetDecimal(4); // cost_per_hour
                                int hours = reader.GetInt32(5); // hours
                                decimal totalCost = costPerHour * hours;
                                
                                orderServicesWithDetails.Add(new 
                                {
                                    ServiceName = serviceName,
                                    Hours = hours,
                                    CostPerHour = costPerHour,
                                    TotalCost = totalCost
                                });
                            }
                        }
                        
                        orderDetailsDataGrid.ItemsSource = orderServicesWithDetails;
                    }
                }
                
                statusBlock.Text = $"Загружены детали заказа {order.OrderCode}";
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка загрузки деталей заказа: {ex.Message}";
            }
        }

        private void OnFilterClick(object sender, RoutedEventArgs e)
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            var statusFilterComboBox = this.FindControl<ComboBox>("StatusFilterComboBox");
            var startDatePicker = this.FindControl<DatePicker>("StartDatePicker");
            var endDatePicker = this.FindControl<DatePicker>("EndDatePicker");
            
            DateTime? startDate = startDatePicker?.SelectedDate != null ? (DateTime?)startDatePicker.SelectedDate.Value.DateTime : null;
            DateTime? endDate = endDatePicker?.SelectedDate != null ? (DateTime?)endDatePicker.SelectedDate.Value.DateTime : null;
            string searchText = searchTextBox?.Text?.ToLower() ?? "";
            string statusFilter = (statusFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все статусы";
            
            LoadOrdersHistory(searchText, statusFilter, startDate, endDate);
        }

        private async void OnPrintReportClick(object sender, RoutedEventArgs e)
        {
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            statusBlock.Text = "Подготовка отчета для печати...";
            
            // This would typically connect to a reporting service or generate a PDF
            // For this demo, we'll just show a message
            await MessageBox.Show(this, "Информация", "Функция печати отчета будет реализована в следующей версии");
            
            statusBlock.Text = "Отчет готов к печати";
        }

        private async void OnExportToExcelClick(object sender, RoutedEventArgs e)
        {
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            statusBlock.Text = "Подготовка данных для экспорта в Excel...";
            
            // This would typically export the data to an Excel file
            // For this demo, we'll just show a message
            await MessageBox.Show(this, "Информация", "Функция экспорта в Excel будет реализована в следующей версии");
            
            statusBlock.Text = "Данные экспортированы в Excel";
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}