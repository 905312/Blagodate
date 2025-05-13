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
    
    public partial class DeleteServiceWindow : BaseWindow
    {
        private User21Context _context;

        public DeleteServiceWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите услугу для удаления";
            
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
            
            LoadServices();
        }
        
        private void LoadServices()
        {
            try
            {
                
                
                var services = _context.Services
                    .OrderBy(s => s.Name)
                    .ToList();
                    
                ServiceComboBox.ItemsSource = services;
                
                if (services.Count > 0)
                {
                    ServiceComboBox.SelectedIndex = 0;
                }
                
                StatusBlock.Text = $"Найдено услуг: {services.Count}";
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
                
                var service = ServiceComboBox.SelectedItem as Service;
                
                if (service == null)
                {
                    await MessageBox.Show(
                        this,
                        "Предупреждение",
                        "Выберите услугу из списка для удаления");
                    return;
                }
                
                var result = await MessageBox.Show(
                    this,
                    "Подтверждение удаления",
                    $"Вы действительно хотите удалить услугу {service.Name}?",
                    Controls.MessageBoxButtons.YesNo);
                    
                if (result == Controls.MessageBoxResult.Yes)
                {
                    var hasOrders = await _context.OrderServices
                        .AnyAsync(os => os.ServiceId == service.ServiceId);
                        
                    if (hasOrders)
                    {
                        await MessageBox.Show(
                            this,
                            "Ошибка",
                            "Невозможно удалить услугу, т.к. она используется в заказах");
                        return;
                    }
                    
                    // Удаляем услугу
                    _context.Services.Remove(service);
                    await _context.SaveChangesAsync();
                    
                    
                    StatusBlock.Text = $"Услуга {service.Name} успешно удалена";
                    
                    // Обновляем список услуг
                    LoadServices();
                }
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка при удалении: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось удалить услугу: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
