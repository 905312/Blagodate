using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging; 
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Blagodat.Views
{
    
    public partial class AddClientWindow : BaseWindow
    {
        private User21Context _context;

        public AddClientWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Готов к добавлению нового клиента";
            
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
            
            // уникальный код через прямой SQL-запрос
            
            
            try
            {
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("SELECT code FROM clients ORDER BY code DESC LIMIT 1", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        string lastCode = result?.ToString();
                        
                        if (!string.IsNullOrEmpty(lastCode) && int.TryParse(lastCode, out int code))
                        {
                            CodeTextBox.Text = (code + 1).ToString("D8");
                        }
                        else
                        {
                            CodeTextBox.Text = "00000001";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating client code: {ex.Message}");
                CodeTextBox.Text = "00000001";
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                

                // Validation
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PassportTextBox.Text) ||
                    BirthDatePicker.SelectedDate == null)
                {
                    StatusBlock.Text = "Ошибка: Заполните все обязательные поля";
                    return;
                }
                
                string code = CodeTextBox.Text ?? "";
                string fullName = FullNameTextBox.Text ?? "";
                string passportData = PassportTextBox.Text ?? "";
                DateTime birthDate = BirthDatePicker.SelectedDate?.Date ?? DateTime.Today;
                string email = EmailTextBox.Text ?? "";
                string phone = ""; // Пустое поле телефона
                string address = AddressTextBox.Text ?? "";
                string password = PasswordTextBox.Text ?? "";
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var cmdCheck = new NpgsqlCommand("SELECT COUNT(*) FROM clients WHERE code = @code", connection))
                    {
                        cmdCheck.Parameters.AddWithValue("code", code);
                        int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                        
                        if (count > 0)
                        {
                            StatusBlock.Text = "Ошибка: Клиент с таким кодом уже существует";
                            return;
                        }
                    }
                    
                    // Начинаем транзакцию
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // Создаем клиента через SQL-запрос с правильными названиями колонок
                            string insertSql = @"
                                INSERT INTO clients (code, full_name, passport_data, birth_date, email, address, password)
                                VALUES (@code, @fullName, @passportData, @birthDate, @email, @address, @password)";
                            
                            using (var cmd = new NpgsqlCommand(insertSql, connection))
                            {
                                cmd.Parameters.AddWithValue("code", code);
                                cmd.Parameters.AddWithValue("fullName", fullName);
                                cmd.Parameters.AddWithValue("passportData", passportData);
                                cmd.Parameters.AddWithValue("birthDate", birthDate);
                                cmd.Parameters.AddWithValue("email", email);
                                cmd.Parameters.AddWithValue("address", address);
                                cmd.Parameters.AddWithValue("password", password);
                                cmd.Transaction = transaction;
                                
                                await cmd.ExecuteNonQueryAsync();
                            }
                            
                            // Если все выполнено успешно, фиксируем транзакцию
                            await transaction.CommitAsync();
                            
                            await MessageBox.Show(this, "Успех", "Клиент успешно добавлен");
                            
                            
                            FullNameTextBox.Text = "";
                            PassportTextBox.Text = "";
                            BirthDatePicker.SelectedDate = null;
                            AddressTextBox.Text = "";
                            EmailTextBox.Text = "";
                            PasswordTextBox.Text = "";
                            
                            using (var cmdLastCode = new NpgsqlCommand("SELECT code FROM clients ORDER BY code DESC LIMIT 1", connection))
                            {
                                var result = await cmdLastCode.ExecuteScalarAsync();
                                string lastCode = result?.ToString();
                                
                                if (!string.IsNullOrEmpty(lastCode) && int.TryParse(lastCode, out int lastCodeInt))
                                {
                                    CodeTextBox.Text = (lastCodeInt + 1).ToString("D8");
                                }
                                else
                                {
                                    CodeTextBox.Text = "00000001";
                                }
                            }
                            
                            StatusBlock.Text = "Клиент успешно добавлен. Готов к добавлению нового клиента";
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception($"Ошибка при добавлении клиента: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось добавить клиента: {ex.Message}");
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
