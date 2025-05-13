using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
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
    
    public partial class DeleteEmployeeWindow : BaseWindow
    {
        private User21Context _context;

        public DeleteEmployeeWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите сотрудника для удаления";
            
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
            
            LoadEmployees();
        }
        
        private async void LoadEmployees()
        {
            try
            {
                
                
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    string sql = @"
                        SELECT employee_id, code, full_name, position, login, password, last_login, login_type
                        FROM employees
                        ORDER BY full_name";
                        
                    var cmd = new NpgsqlCommand(sql, connection);
                    
                    var employees = new List<Employee>();
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var employee = new Employee
                            {
                                EmployeeId = reader.GetInt32(0),
                                Code = reader.GetString(1),
                                FullName = reader.GetString(2),
                                Position = reader.GetString(3),
                                Login = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                Password = !reader.IsDBNull(5) ? reader.GetString(5) : null,
                                LastLogin = !reader.IsDBNull(6) ? reader.GetDateTime(6) : (DateTime?)null,
                                LoginType = !reader.IsDBNull(7) ? reader.GetString(7) : null
                            };
                            
                            employees.Add(employee);
                        }
                    }
                    
                    EmployeeComboBox.ItemsSource = employees;
                    EmployeeComboBox.DisplayMemberBinding = new Avalonia.Data.Binding("FullName");
                    
                    StatusBlock.Text = $"Загружено сотрудников: {employees.Count}";
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private async void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
        {
            try
            {
                
                
                
                var selectedEmployee = EmployeeComboBox.SelectedItem as Employee;
                
                if (selectedEmployee == null)
                {
                    StatusBlock.Text = "Не выбран сотрудник";
                    await MessageBox.Show(this, "Предупреждение", "Пожалуйста, выберите сотрудника для удаления");
                    return;
                }
                
                var result = await MessageBox.Show(this, 
                    "Подтверждение", 
                    $"Вы уверены, что хотите удалить сотрудника {selectedEmployee.FullName}?", 
                    MessageBoxButtons.YesNo);
                    
                if (result != Controls.MessageBoxResult.Yes)
                    return;
                    
                if (selectedEmployee.Login == CurrentUser.Login)
                {
                    await MessageBox.Show(
                        this,
                        "Ошибка",
                        "Невозможно удалить текущего пользователя");
                    return;
                }
                
                if (selectedEmployee.Position.Contains("Администратор") || 
                    selectedEmployee.Position.Contains("Директор"))
                {
                    using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                    {
                        await connection.OpenAsync();
                        
                        string countAdminSql = @"
                            SELECT COUNT(*) 
                            FROM employees 
                            WHERE position LIKE '%Администратор%' OR position LIKE '%Директор%'";
                            
                        int adminCount = 0;
                        
                        using (var cmd = new NpgsqlCommand(countAdminSql, connection))
                        {
                            adminCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            
                            if (adminCount <= 1)
                            {
                                await MessageBox.Show(
                                    this,
                                    "Ошибка",
                                    "Невозможно удалить последнего администратора системы");
                                return;
                            }
                        }
                    }
                }
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    string findUserSql = @"
                        SELECT user_id 
                        FROM users 
                        WHERE login = @login";
                        
                    int? userId = null;
                    
                    using (var cmd = new NpgsqlCommand(findUserSql, connection))
                    {
                        cmd.Parameters.AddWithValue("login", selectedEmployee.Login);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                userId = reader.GetInt32(0);
                            }
                        }
                    }
                    
                    if (userId.HasValue)
                    {
                        string deleteUserSql = @"
                            DELETE FROM users 
                            WHERE user_id = @userId";
                            
                        using (var cmd = new NpgsqlCommand(deleteUserSql, connection))
                        {
                            cmd.Parameters.AddWithValue("userId", userId.Value);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    
                    string deleteEmployeeSql = @"
                        DELETE FROM employees 
                        WHERE employee_id = @employeeId";
                        
                    using (var cmd = new NpgsqlCommand(deleteEmployeeSql, connection))
                    {
                        cmd.Parameters.AddWithValue("employeeId", selectedEmployee.EmployeeId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                
                
                StatusBlock.Text = $"Сотрудник {selectedEmployee.FullName} успешно удален";
                
                LoadEmployees();
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось удалить сотрудника: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
