using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Blagodat.Controls;
using Blagodat.Models;
using System;
using System.IO;
using System.Linq;

namespace Blagodat.Views
{
    
    public partial class EditServiceWindow : BaseWindow
    {
        private User21Context _context;
        private Service _selectedService;

        public EditServiceWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Выберите услугу для редактирования";
            
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
        
        private void LoadServices(string searchTerm = "")
        {
            try
            {
                
                var query = _context.Services.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(s => 
                        s.Name.ToLower().Contains(searchTerm) || 
                        s.Code.ToLower().Contains(searchTerm));
                }
                
                var services = _context.Services.OrderBy(s => s.Name).ToList();
                ServicesListBox.ItemsSource = services;
                
                StatusBlock.Text = $"Найдено услуг: {services.Count}";
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            
            LoadServices(SearchTextBox.Text);
        }

        private void OnServiceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is Service service)
            {
                _selectedService = service;
                
                
                CodeTextBox.Text = service.Code;
                NameTextBox.Text = service.Name;
                
                decimal costDecimal = service.CostPerHour;
                string costString = costDecimal.ToString("F2");
                if (decimal.TryParse(costString, out decimal parsedCost))
                {
                    CostPerHourUpDown.Text = costString;
                }
                
                StatusBlock.Text = $"Редактирование услуги: {service.Name}";
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (_selectedService == null)
                return;
                
            try
            {
                
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    StatusBlock.Text = "Ошибка: Заполните название услуги";
                    return;
                }

                _selectedService.Name = NameTextBox.Text;
                
                if (!string.IsNullOrWhiteSpace(CostPerHourUpDown.Text) && 
                    decimal.TryParse(CostPerHourUpDown.Text, out decimal cost))
                {
                    _selectedService.Cost = cost;
                }
                else
                {
                    _selectedService.Cost = 0m;
                }
                
                _context.Services.Update(_selectedService);
                await _context.SaveChangesAsync();
                
                await MessageBox.Show(this, "Успех", "Данные услуги успешно обновлены");
                StatusBlock.Text = $"Услуга {_selectedService.Name} успешно обновлена";
                
                
                int selectedIndex = ServicesListBox.SelectedIndex;
                LoadServices();
                
                if (selectedIndex >= 0 && selectedIndex < ServicesListBox.Items.Count)
                {
                    ServicesListBox.SelectedIndex = selectedIndex;
                }
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить данные услуги: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
