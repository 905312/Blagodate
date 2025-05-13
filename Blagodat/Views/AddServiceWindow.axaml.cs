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
            
            var userNameText = this.FindControl<TextBlock>("UserNameText");
            var userRoleText = this.FindControl<TextBlock>("UserRoleText");
            var userImage = this.FindControl<Image>("UserImage");
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            
            userNameText.Text = user.FullName;
            userRoleText.Text = user.Role;
            statusBlock.Text = "Готов к добавлению новой услуги";
            
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
            
            var codeTextBox = this.FindControl<TextBox>("CodeTextBox");
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
                    codeTextBox.Text = $"{prefix}{(code + 1):D2}";
                }
                else
                {
                    codeTextBox.Text = "SRV01"; 
                }
            }
            else
            {
                codeTextBox.Text = "SRV01";
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var nameTextBox = this.FindControl<TextBox>("NameTextBox");
                var codeTextBox = this.FindControl<TextBox>("CodeTextBox");
                var costPerHourUpDown = this.FindControl<NumericUpDown>("CostPerHourUpDown");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");

                if (string.IsNullOrWhiteSpace(nameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(codeTextBox.Text))
                {
                    statusBlock.Text = "Ошибка: Заполните все обязательные поля";
                    return;
                }

                if (_context.Services.Any(s => s.Code == codeTextBox.Text))
                {
                    statusBlock.Text = "Ошибка: Услуга с таким кодом уже существует";
                    return;
                }

                var service = new Service
                {
                    Name = nameTextBox.Text,
                    Code = codeTextBox.Text,
                    CostPerHour = (decimal)costPerHourUpDown.Value
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                await MessageBox.Show(this, "Успех", "Услуга успешно добавлена");
                
                nameTextBox.Text = "";
                costPerHourUpDown.Value = 0;
                
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
                        codeTextBox.Text = $"{prefix}{(code + 1):D2}";
                    }
                }
                
                statusBlock.Text = "Услуга успешно добавлена. Готов к добавлению новой услуги";
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка: {ex.Message}";
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
