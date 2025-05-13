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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blagodat.Views
{
    
    public partial class DeleteOrderWindow : BaseWindow
    {
        private User21Context _context;

        public DeleteOrderWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите заказ для удаления";
            
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
            
            Task.Run(async () => await Dispatcher.UIThread.InvokeAsync(async () => await LoadOrdersAsync()));
        }
        
        private async Task LoadOrdersAsync()
        {
            try
            {
                
                var orders = new List<Order>();
                
                using (var connection = new Npgsql.NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    // Запрос для получения заказов с данными клиентов
                    var sql = @"
                        SELECT o.order_id, o.order_code, o.creation_date, o.status, o.client_code, o.order_time, 
                               c.full_name as client_full_name
                        FROM orders o
                        LEFT JOIN clients c ON o.client_code = c.code
                        ORDER BY o.creation_date DESC";
                    
                    using (var cmd = new Npgsql.NpgsqlCommand(sql, connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var order = new Order
                                {
                                    OrderId = reader.GetInt32(0),
                                    OrderCode = reader.GetString(1),
                                    OrderDate = reader.GetDateTime(2),
                                    Status = reader.GetString(3),
                                    ClientCode = reader.GetString(4),
                                    // Используем GetTimeSpan для поля time without time zone
                                    OrderTime = reader.GetTimeSpan(5).ToString(),
                                    ClientCodeNavigation = new Client
                                    {
                                        Code = reader.GetString(4),
                                        FullName = reader.GetString(6)
                                    }
                                };
                                
                                orders.Add(order);
                            }
                        }
                    }
                }
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    
                    OrderComboBox.ItemsSource = orders;
                    
                    if (orders.Count > 0)
                    {
                        OrderComboBox.SelectedIndex = 0;
                    }
                    
                    StatusBlock.Text = $"Найдено заказов: {orders.Count}";
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    
                    StatusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
                });
            }
        }

        private async void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
        {
            try
            {
                
                var order = OrderComboBox.SelectedItem as Order;
                
                if (order == null)
                {
                    await MessageBox.Show(
                        this,
                        "Предупреждение",
                        "Выберите заказ из списка для удаления");
                    return;
                }
                
                var result = await MessageBox.Show(
                    this,
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить заказ {order.OrderCode}?",
                    Controls.MessageBoxButtons.YesNo);
                    
                if (result == Controls.MessageBoxResult.Yes)
                {
                    using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        
                        using (var transaction = await connection.BeginTransactionAsync())
                        {
                            try
                            {
                                // Удаляем связанные услуги заказа
                                using (var cmd = new NpgsqlCommand("DELETE FROM order_services WHERE order_id = @orderId", connection))
                                {
                                    cmd.Parameters.AddWithValue("orderId", order.OrderId);
                                    cmd.Transaction = transaction;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                
                                // Удаляем сам заказ
                                using (var cmd = new NpgsqlCommand("DELETE FROM orders WHERE order_id = @orderId", connection))
                                {
                                    cmd.Parameters.AddWithValue("orderId", order.OrderId);
                                    cmd.Transaction = transaction;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                
                                // Фиксируем транзакцию
                                await transaction.CommitAsync();
                                
                                
                                var selectedOrder = OrderComboBox.SelectedItem as Order;
                                StatusBlock.Text = $"Заказ {selectedOrder.OrderCode} успешно удален";
                                
                                // Обновляем список заказов
                                await LoadOrdersAsync();
                            }
                            catch (Exception ex)
                            {
                                // Откатываем транзакцию в случае ошибки
                                await transaction.RollbackAsync();
                                throw new Exception($"Ошибка при удалении заказа: {ex.Message}");
                            }
                        }
                    }                    
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка при удалении: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось удалить заказ: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
