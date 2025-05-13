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
    
    public partial class AddServiceWindow : BaseWindow
    {
        private User21Context _context;

        public AddServiceWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Готов к добавлению новой услуги";
            
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
            
            
            var lastCode = _context.Services
                .OrderByDescending(s => s.Code)
                .Select(s => s.Code)
                .FirstOrDefault();
                
            if (!string.IsNullOrEmpty(lastCode))
            {
                string numericPart = new string(lastCode.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(numericPart) && int.TryParse(numericPart, out int code))
                {
                    string prefix = new string(lastCode.Where(c => !char.IsDigit(c)).ToArray());
                    
                    CodeTextBox.Text = $"{prefix}{(code + 1):D2}";
                }
                else
                {
                    
                    CodeTextBox.Text = "SRV01";
                }
            }
            else
            {
                
                CodeTextBox.Text = "SRV01";
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 
                // 
                if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(CodeTextBox.Text))
                {
                    StatusBlock.Text = "Ошибка: Заполните все обязательные поля";
                    return;
                }

                if (_context.Services.Any(s => s.Code == CodeTextBox.Text))
                {
                    StatusBlock.Text = "Ошибка: Услуга с таким кодом уже существует";
                    return;
                }

                var service = new Service
                {
                    Name = NameTextBox.Text,
                    Code = CodeTextBox.Text,
                    CostPerHour = (decimal)CostPerHourUpDown.Value
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                await MessageBox.Show(this, "Успех", "Услуга успешно добавлена");
                
                NameTextBox.Text = "";
                CostPerHourUpDown.Value = 0;
                
                var lastCode = _context.Services
                    .OrderByDescending(s => s.Code)
                    .Select(s => s.Code)
                    .FirstOrDefault();
                    
                if (!string.IsNullOrEmpty(lastCode))
                {
                    string numericPart = new string(lastCode.Where(char.IsDigit).ToArray());
                    if (!string.IsNullOrEmpty(numericPart) && int.TryParse(numericPart, out int code))
                    {
                        string prefix = new string(lastCode.Where(c => !char.IsDigit(c)).ToArray());
                        CodeTextBox.Text = $"{prefix}{(code + 1):D2}";
                    }
                }
                
                StatusBlock.Text = "Услуга успешно добавлена. Готов к добавлению новой услуги";
            }
            catch (Exception ex)
            {
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось добавить услугу: {ex.Message}");
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
