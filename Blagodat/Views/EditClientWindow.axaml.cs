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
    public partial class EditClientWindow : BaseWindow
    {
        private User21Context _context;
        private Client _selectedClient;

        public EditClientWindow()
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
            statusBlock.Text = "Выберите клиента для редактирования";
            
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
            
            LoadClients();
        }
        
        private void LoadClients(string searchTerm = "")
        {
            try
            {
                var clientsListBox = this.FindControl<ListBox>("ClientsListBox");
                
                var query = _context.Clients
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
                    .AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(c => 
                        c.FullName.ToLower().Contains(searchTerm) || 
                        c.Code.ToLower().Contains(searchTerm) ||
                        c.Email.ToLower().Contains(searchTerm));
                }
                
                var clients = query.OrderBy(c => c.FullName).ToList();
                clientsListBox.ItemsSource = clients;
                
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Найдено клиентов: {clients.Count}";
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            var searchTextBox = this.FindControl<TextBox>("SearchTextBox");
            LoadClients(searchTextBox.Text);
        }

        private void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is Client client)
            {
                _selectedClient = client;
                
                var codeTextBox = this.FindControl<TextBox>("CodeTextBox");
                var fullNameTextBox = this.FindControl<TextBox>("FullNameTextBox");
                var passportTextBox = this.FindControl<TextBox>("PassportTextBox");
                var birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
                var addressTextBox = this.FindControl<TextBox>("AddressTextBox");
                var emailTextBox = this.FindControl<TextBox>("EmailTextBox");
                var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
                
                codeTextBox.Text = client.Code;
                fullNameTextBox.Text = client.FullName;
                passportTextBox.Text = client.PassportData;
                birthDatePicker.SelectedDate = client.BirthDate;
                addressTextBox.Text = client.Address;
                emailTextBox.Text = client.Email;
                passwordTextBox.Text = client.Password;
                
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Редактирование клиента: {client.FullName}";
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
                return;
                
            try
            {
                var fullNameTextBox = this.FindControl<TextBox>("FullNameTextBox");
                var passportTextBox = this.FindControl<TextBox>("PassportTextBox");
                var birthDatePicker = this.FindControl<DatePicker>("BirthDatePicker");
                var addressTextBox = this.FindControl<TextBox>("AddressTextBox");
                var emailTextBox = this.FindControl<TextBox>("EmailTextBox");
                var passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                
                if (string.IsNullOrWhiteSpace(fullNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(passportTextBox.Text) ||
                    birthDatePicker.SelectedDate == null)
                {
                    statusBlock.Text = "Ошибка: заполните все обязательные поля";
                    return;
                }

                string fullName = fullNameTextBox.Text;
                string passportData = passportTextBox.Text;
                DateTime birthDate = birthDatePicker.SelectedDate?.Date ?? DateTime.Today;
                string address = addressTextBox.Text;
                string email = emailTextBox.Text;
                string password = passwordTextBox.Text;
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            // SQL запрос для обновления данных клиента
                            string updateSql = @"
                                UPDATE clients 
                                SET full_name = @fullName, 
                                    passport_data = @passportData, 
                                    birth_date = @birthDate, 
                                    address = @address, 
                                    email = @email, 
                                    password = @password 
                                WHERE client_id = @clientId";
                                
                            using (var cmd = new NpgsqlCommand(updateSql, connection))
                            {
                                cmd.Parameters.AddWithValue("fullName", fullName);
                                cmd.Parameters.AddWithValue("passportData", passportData);
                                cmd.Parameters.AddWithValue("birthDate", birthDate);
                                cmd.Parameters.AddWithValue("address", address);
                                cmd.Parameters.AddWithValue("email", email);
                                cmd.Parameters.AddWithValue("password", password);
                                cmd.Parameters.AddWithValue("clientId", _selectedClient.Id);
                                cmd.Transaction = transaction;
                                
                                await cmd.ExecuteNonQueryAsync();
                            }
                            
                            await transaction.CommitAsync();
                            
                            // Обновляем локальные данные объекта
                            _selectedClient.FullName = fullName;
                            _selectedClient.PassportData = passportData;
                            _selectedClient.BirthDate = birthDate;
                            _selectedClient.Address = address;
                            _selectedClient.Email = email;
                            _selectedClient.Password = password;
                            
                            await MessageBox.Show(this, "Успех", "Данные клиента успешно обновлены");
                            statusBlock.Text = $"Клиент {_selectedClient.FullName} успешно обновлен";
                            
                            // Перезагружаем список клиентов
                            LoadClients();
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception($"Ошибка при обновлении данных клиента: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить данные клиента: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
