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
            
            var userNameText = this.FindControl<TextBlock>("UserNameText");
            var userRoleText = this.FindControl<TextBlock>("UserRoleText");
            var userImage = this.FindControl<Image>("UserImage");
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            
            userNameText.Text = user.FullName;
            userRoleText.Text = user.Role;
            statusBlock.Text = "Готов к добавлению нового клиента";
            
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
            
            // уникальный код через прямой SQL-запрос
            var codeTextBox = this.FindControl<TextBox>("CodeTextBox");
            
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
                            codeTextBox.Text = (code + 1).ToString("D8");
                        }
                        else
                        {
                            codeTextBox.Text = "00000001";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating client code: {ex.Message}");
                codeTextBox.Text = "00000001";
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var codeTextBox = this.FindControl<TextBox>("CodeTextBox");
                var fullNameTextBox = this.FindControl<TextBox>("FullNameTextBox");
                var passportTextBox = this.FindControl<TextBox>("PassportTextBox");
                var birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
                var addressTextBox = this.FindControl<TextBox>("AddressTextBox");
                var emailTextBox = this.FindControl<TextBox>("EmailTextBox");
                var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");

                // Validation
                if (string.IsNullOrWhiteSpace(fullNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(passportTextBox.Text) ||
                    birthDatePicker.SelectedDate == null)
                {
                    statusBlock.Text = "Ошибка: Заполните все обязательные поля";
                    return;
                }
                
                string code = codeTextBox.Text;
                string fullName = fullNameTextBox.Text;
                string passportData = passportTextBox.Text ?? "";
                DateTime birthDate = birthDatePicker.SelectedDate?.Date ?? DateTime.Today;
                string email = emailTextBox.Text ?? "";
                string phone = ""; // Пустое поле телефона
                string address = addressTextBox.Text ?? "";
                string password = passwordTextBox.Text ?? "";
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var cmdCheck = new NpgsqlCommand("SELECT COUNT(*) FROM clients WHERE code = @code", connection))
                    {
                        cmdCheck.Parameters.AddWithValue("code", code);
                        int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                        
                        if (count > 0)
                        {
                            statusBlock.Text = "Ошибка: Клиент с таким кодом уже существует";
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
                            
                            fullNameTextBox.Text = "";
                            passportTextBox.Text = "";
                            birthDatePicker.SelectedDate = null;
                            addressTextBox.Text = "";
                            emailTextBox.Text = "";
                            passwordTextBox.Text = "";
                            
                            using (var cmdLastCode = new NpgsqlCommand("SELECT code FROM clients ORDER BY code DESC LIMIT 1", connection))
                            {
                                var result = await cmdLastCode.ExecuteScalarAsync();
                                string lastCode = result?.ToString();
                                
                                if (!string.IsNullOrEmpty(lastCode) && int.TryParse(lastCode, out int lastCodeInt))
                                {
                                    codeTextBox.Text = (lastCodeInt + 1).ToString("D8");
                                }
                                else
                                {
                                    codeTextBox.Text = "00000001";
                                }
                            }
                            
                            statusBlock.Text = "Клиент успешно добавлен. Готов к добавлению нового клиента";
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
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка: {ex.Message}";
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
