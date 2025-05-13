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
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите клиента для редактирования";
            
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
        }
        
        private void LoadClients(string searchTerm = "")
        {
            try
            {
                
                
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
                ClientsListBox.ItemsSource = clients;
                
                
                StatusBlock.Text = $"Найдено клиентов: {clients.Count}";
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            
            LoadClients(SearchTextBox.Text);
        }

        private void OnClientSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is Client client)
            {
                _selectedClient = client;
                
                
                
                CodeTextBox.Text = client.Code;
                FullNameTextBox.Text = client.FullName;
                PassportTextBox.Text = client.PassportData;
                BirthDatePicker.SelectedDate = client.BirthDate;
                AddressTextBox.Text = client.Address;
                EmailTextBox.Text = client.Email;
                PasswordTextBox.Text = client.Password;
                
                
                StatusBlock.Text = $"Редактирование клиента: {client.FullName}";
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
                return;
                
            try
            {
                
                
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PassportTextBox.Text) ||
                    BirthDatePicker.SelectedDate == null)
                {
                    StatusBlock.Text = "Ошибка: заполните все обязательные поля";
                    return;
                }

                string fullName = FullNameTextBox.Text;
                string passportData = PassportTextBox.Text ?? "";
                DateTime birthDate = BirthDatePicker.SelectedDate?.Date ?? DateTime.Today;
                string address = AddressTextBox.Text ?? "";
                string email = EmailTextBox.Text ?? "";
                string password = PasswordTextBox.Text ?? "";
                
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
                            StatusBlock.Text = $"Клиент {_selectedClient.FullName} успешно обновлен";
                            
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
                
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить данные клиента: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
