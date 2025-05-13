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
    
    public partial class TransferPositionWindow : BaseWindow
    {
        private User21Context _context;
        private Employee _selectedEmployee;

        public TransferPositionWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите сотрудника для передачи должности";
            
            NewPositionComboBox.SelectedIndex = 0;
            

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
        
        private async void LoadEmployees(string searchTerm = "")
        {
            try
            {
                
                var statusBlock = StatusBlock;
                
                var connection = new NpgsqlConnection(_context.Database.GetConnectionString());
                await connection.OpenAsync();
                
                string sql;
                NpgsqlCommand cmd;
                
                if (searchTerm == "" || searchTerm == null)
                {
                    sql = "SELECT employee_id, code, full_name, position, login, password FROM employees ORDER BY full_name";
                    cmd = new NpgsqlCommand(sql, connection);
                }
                else
                {
                    searchTerm = searchTerm.ToLower();
                    sql = "SELECT employee_id, code, full_name, position, login, password FROM employees WHERE LOWER(full_name) LIKE @search OR LOWER(position) LIKE @search OR LOWER(code) LIKE @search ORDER BY full_name";
                    cmd = new NpgsqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("search", "%" + searchTerm + "%");
                }
                
                var employees = new List<Employee>();
                
                var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var employee = new Employee();
                    employee.EmployeeId = reader.GetInt32(0);
                    employee.Code = reader.GetString(1);
                    employee.FullName = reader.GetString(2);
                    employee.Position = reader.GetString(3);
                    employee.Login = reader.GetString(4);
                    employee.Password = reader.GetString(5);
                    
                    employees.Add(employee);
                }
                
                reader.Close();
                connection.Close();
                
                EmployeesListBox.ItemsSource = employees;
                statusBlock.Text = "Найдено сотрудников: " + employees.Count;
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = "Ошибка: " + ex.Message;
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            
            LoadEmployees(SearchTextBox.Text);
        }

        private void OnEmployeeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is Employee employee)
            {
                _selectedEmployee = employee;
                

                
                EmployeeCodeText.Text = employee.Code;
                EmployeeNameText.Text = employee.FullName;
                CurrentPositionText.Text = employee.Position;
                
                StatusBlock.Text = $"Выбран сотрудник: {employee.FullName}";
            }
        }

        private async void OnTransferPositionClick(object sender, RoutedEventArgs e)
        {
            if (_selectedEmployee == null)
                return;
                
            try
            {
                
                var selectedPosition = NewPositionComboBox.SelectedItem as ComboBoxItem;
                
                
                string newPosition = selectedPosition?.Content.ToString();
                
                if (selectedPosition == null)
                {
                    StatusBlock.Text = "Ошибка: Выберите новую должность";
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(ReasonTextBox.Text))
                {
                    StatusBlock.Text = "Ошибка: Укажите причину перевода";
                    return;
                }
                
                if (newPosition == _selectedEmployee.Position)
                {
                    StatusBlock.Text = "Ошибка: Новая должность совпадает с текущей";
                    return;
                }
                

                var result = await MessageBox.Show(
                    this,
                    "Подтверждение",
                    $"Вы действительно хотите перевести сотрудника {_selectedEmployee.FullName} с должности {_selectedEmployee.Position} на должность {newPosition}?",
                    Controls.MessageBoxButtons.YesNo);
                    
                if (result != Controls.MessageBoxResult.Yes)
                    return;
                

                string oldPosition = _selectedEmployee.Position;
                

                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    

                    string updateEmployeeSql = @"
                        UPDATE employees
                        SET position = @position
                        WHERE employee_id = @employeeId";
                        
                    using (var cmd = new NpgsqlCommand(updateEmployeeSql, connection))
                    {
                        cmd.Parameters.AddWithValue("position", newPosition);
                        cmd.Parameters.AddWithValue("employeeId", _selectedEmployee.EmployeeId);
                        
                        await cmd.ExecuteNonQueryAsync();
                    }
                    

                    _selectedEmployee.Position = newPosition;
                    

                    string getUserSql = @"
                        SELECT user_id, role
                        FROM users
                        WHERE login = @login";
                        
                    int? userId = null;
                    string currentRole = null;
                    
                    using (var cmd = new NpgsqlCommand(getUserSql, connection))
                    {
                        cmd.Parameters.AddWithValue("login", _selectedEmployee.Login);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                userId = reader.GetInt32(0);
                                currentRole = reader.IsDBNull(1) ? null : reader.GetString(1);
                            }
                        }
                    }
                    

                    if (userId.HasValue)
                    {

                        string newRole = newPosition switch
                        {
                            "Продавец" => "Продавец",
                            "Администратор" => "Администратор",
                            "Старший смены" => "Старший смены",
                            "Менеджер" => "Менеджер",
                            "Директор" => "Директор",
                            _ => currentRole
                        };
                        

                        string updateUserSql = @"
                            UPDATE users
                            SET role = @role
                            WHERE user_id = @userId";
                            
                        using (var cmd = new NpgsqlCommand(updateUserSql, connection))
                        {
                            cmd.Parameters.AddWithValue("role", newRole);
                            cmd.Parameters.AddWithValue("userId", userId.Value);
                            
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                
                await MessageBox.Show(this, "Успех", $"Сотрудник {_selectedEmployee.FullName} успешно переведен с должности {oldPosition} на должность {newPosition}");
                

                
                EmployeeCodeText.Text = _selectedEmployee.Code;
                EmployeeNameText.Text = _selectedEmployee.FullName;
                CurrentPositionText.Text = _selectedEmployee.Position;
                ReasonTextBox.Text = "";
                
                StatusBlock.Text = $"Должность сотрудника {_selectedEmployee.FullName} успешно изменена на {newPosition}";
                

                LoadEmployees();
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось передать должность: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
