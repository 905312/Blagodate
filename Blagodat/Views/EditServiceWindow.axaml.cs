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
            
            var userNameText = this.FindControl<TextBlock>("UserNameText");
            var userRoleText = this.FindControl<TextBlock>("UserRoleText");
            var userImage = this.FindControl<Image>("UserImage");
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            
            userNameText.Text = user.FullName;
            userRoleText.Text = user.Role;
            statusBlock.Text = "Выберите услугу для редактирования";
            
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
            
            LoadServices();
        }
        
        private void LoadServices(string searchTerm = "")
        {
            try
            {
                var servicesListBox = this.FindControl<ListBox>("ServicesListBox");
                
                var query = _context.Services.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(s => 
                        s.Name.ToLower().Contains(searchTerm) || 
                        s.Code.ToLower().Contains(searchTerm));
                }
                
                var services = _context.Services.OrderBy(s => s.Name).ToList();
                servicesListBox.ItemsSource = services;
                
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Найдено услуг: {services.Count}";
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
            LoadServices(searchTextBox.Text);
        }

        private void OnServiceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is Service service)
            {
                _selectedService = service;
                
                var codeTextBox = this.FindControl<TextBox>("CodeTextBox");
                var nameTextBox = this.FindControl<TextBox>("NameTextBox");
                var costPerHourUpDown = this.FindControl<NumericUpDown>("CostPerHourUpDown");
                
                codeTextBox.Text = service.Code;
                nameTextBox.Text = service.Name;
                
                decimal costDecimal = service.CostPerHour;
                string costString = costDecimal.ToString("F2");
                if (decimal.TryParse(costString, out decimal parsedCost))
                {
                    costPerHourUpDown.Text = costString;
                }
                
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Редактирование услуги: {service.Name}";
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (_selectedService == null)
                return;
                
            try
            {
                var nameTextBox = this.FindControl<TextBox>("NameTextBox");
                var costPerHourUpDown = this.FindControl<NumericUpDown>("CostPerHourUpDown");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    statusBlock.Text = "Ошибка: Заполните название услуги";
                    return;
                }

                _selectedService.Name = nameTextBox.Text;
                
                if (!string.IsNullOrWhiteSpace(costPerHourUpDown.Text) && 
                    decimal.TryParse(costPerHourUpDown.Text, out decimal cost))
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
                statusBlock.Text = $"Услуга {_selectedService.Name} успешно обновлена";
                
                var servicesListBox = this.FindControl<ListBox>("ServicesListBox");
                int selectedIndex = servicesListBox.SelectedIndex;
                LoadServices();
                
                if (selectedIndex >= 0 && selectedIndex < servicesListBox.Items.Count)
                {
                    servicesListBox.SelectedIndex = selectedIndex;
                }
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить данные услуги: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
