using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace Blagodat.Views
{
    
    public partial class DeleteClientWindow : BaseWindow
    {
        private User21Context _context;

        public DeleteClientWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите клиента для удаления";
            
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
                
                StatusBlock.Text = $"Найдено клиентов: {clients.Count}";
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
                
                var client = ClientComboBox.SelectedItem as Client;
                
                if (client == null)
                {
                    await MessageBox.Show(
                        this,
                        "Предупреждение",
                        "Выберите клиента из списка для удаления");
                    return;
                }
                
                var result = await MessageBox.Show(
                    this,
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить клиента {client.FullName}?",
                    Controls.MessageBoxButtons.YesNo);
                    
                if (result == Controls.MessageBoxResult.Yes)
                {
                    var hasOrders = await _context.Orders
                        .AnyAsync(o => o.ClientCode == client.Code);
                        
                    if (hasOrders)
                    {
                        await MessageBox.Show(
                            this,
                            "Ошибка",
                            "Невозможно удалить клиента, т.к. у него есть связанные заказы");
                        return;
                    }
                    
                    // Удаляем клиента из базы данных
                    _context.Clients.Remove(client);
                    await _context.SaveChangesAsync();
                    
                    
                    StatusBlock.Text = $"Клиент {client.FullName} успешно удален";
                    
                    // Обновляем список клиентов
                    LoadClients();
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка при удалении: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось удалить клиента: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
