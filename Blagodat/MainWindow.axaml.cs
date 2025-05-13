using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Blagodat
{
    public partial class MainWindow : Window
    {
        private readonly User21Context _context;
        private bool _isPasswordVisible = false;
        private int _loginAttempts = 0;
        private string _currentCaptcha = "";
        private Timer _sessionTimer;
        private Timer _blockTimer;
        private bool _isBlocked = false;
        private DateTime _blockEndTime;

        public MainWindow()
        {
            InitializeComponent();
            _context = new User21Context();
            

            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    this.Icon = new WindowIcon(logoPath);
                }
                else
                {
                    Console.WriteLine($"no icon found: {logoPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
            }
            
            InitializeLoginForm();
        }

        private void InitializeLoginForm()
        {
            var loginBox = this.FindControl<TextBox>("LoginBox");
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            var captchaPanel = this.FindControl<StackPanel>("CaptchaPanel");
            var userImage = this.FindControl<Image>("UserImage");

            loginBox.Text = "";
            passwordBox.Text = "";
            captchaPanel.IsVisible = _loginAttempts >= 2;


            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    userImage.Source = new Bitmap(logoPath);
                }
                else
                {
                    Console.WriteLine($"no file: {logoPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"cant load image: {ex.Message}");
            }

            if (captchaPanel.IsVisible)
            {
                GenerateNewCaptcha();
            }
        }

        private void OnShowPasswordClick(object sender, RoutedEventArgs e)
        {
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            _isPasswordVisible = !_isPasswordVisible;
            passwordBox.PasswordChar = _isPasswordVisible ? '\0' : '●';
        }

        private void OnRefreshCaptchaClick(object sender, RoutedEventArgs e)
        {
            GenerateNewCaptcha();
        }

        private void GenerateNewCaptcha()
        {
            var canvas = this.FindControl<Canvas>("CaptchaCanvas");
            canvas.Children.Clear();

            var random = new Random();
            _currentCaptcha = "";


            for (int i = 0; i < 3; i++)
            {
                char c;
                if (random.Next(2) == 0)
                    c = (char)('A' + random.Next(26));
                else
                    c = (char)('0' + random.Next(10));

                _currentCaptcha += c;


                var textBlock = new TextBlock
                {
                    Text = c.ToString(),
                    FontSize = 36,
                    Foreground = new SolidColorBrush(Colors.Black),
                    RenderTransform = new RotateTransform(random.Next(-30, 30))
                };


                Canvas.SetLeft(textBlock, 60 + i * 40 + random.Next(-10, 10));
                Canvas.SetTop(textBlock, 20 + random.Next(-10, 10));

                canvas.Children.Add(textBlock);
            }


            for (int i = 0; i < 5; i++)
            {
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(random.Next(0, 300), random.Next(0, 80)),
                    EndPoint = new Avalonia.Point(random.Next(0, 300), random.Next(0, 80)),
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 1
                };

                canvas.Children.Add(line);
            }
        }

        private async void OnLoginClick(object sender, RoutedEventArgs e)
        {
            if (_isBlocked)
            {
                var timeLeft = _blockEndTime - DateTime.Now;
                if (timeLeft > TimeSpan.Zero)
                {
                    await ShowError("Вход заблокирован. Попробуйте через " + timeLeft.Seconds + " секунд");
                    return;
                }
                _isBlocked = false;
            }

            var loginBox = this.FindControl<TextBox>("LoginBox");
            var passwordBox = this.FindControl<TextBox>("PasswordBox");
            var captchaBox = this.FindControl<TextBox>("CaptchaBox");
            var captchaPanel = this.FindControl<StackPanel>("CaptchaPanel");

            if (loginBox.Text == "" || passwordBox.Text == "")
            {
                await ShowError("Заполните все поля");
                return;
            }

            if (captchaPanel.IsVisible)
            {
                if (captchaBox.Text == "" || 
                    captchaBox.Text.ToUpper() != _currentCaptcha)
                {
                    await ShowError("Неверный код с картинки");
                    GenerateNewCaptcha();
                    return;
                }
            }

            User user = null;
            try
            {
                var connection = new Npgsql.NpgsqlConnection(_context.Database.GetConnectionString());
                await connection.OpenAsync();
                
                var cmd = new Npgsql.NpgsqlCommand(
                    "SELECT user_id, login, password, full_name, role, photo_path FROM users WHERE login = @login AND password = @password", 
                    connection);
                
                cmd.Parameters.AddWithValue("login", loginBox.Text);
                cmd.Parameters.AddWithValue("password", passwordBox.Text);
                
                var reader = await cmd.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    user = new User();
                    user.UserId = reader.GetInt32(0);
                    user.Login = reader.GetString(1);
                    user.Password = reader.GetString(2);
                    user.FullName = reader.GetString(3);
                    user.Role = reader.GetString(4);
                    
                    if (!reader.IsDBNull(5))
                        user.PhotoPath = reader.GetString(5);
                    else
                        user.PhotoPath = null;
                }
                
                reader.Close();
                connection.Close();
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при авторизации: {ex.Message}");
                return;
            }

            
            try
            {
                using (var connection = new Npgsql.NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var cmd = new Npgsql.NpgsqlCommand(
                        "INSERT INTO login_history (login_time, user_login, success) VALUES (CURRENT_TIMESTAMP, @user_login, @success)", 
                        connection))
                    {
                        cmd.Parameters.AddWithValue("user_login", loginBox.Text);
                        cmd.Parameters.AddWithValue("success", user != null);
                        
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при сохранении истории входа: {ex.Message}");
               
            }

            if (user != null)
            {
                _loginAttempts = 0;
                await ShowUserInfo(user);
                StartSession(user);
            }
            else
            {
                _loginAttempts++;
                if (_loginAttempts >= 2)
                {
                    captchaPanel.IsVisible = true;
                    GenerateNewCaptcha();

                    if (_loginAttempts > 2)
                    {
                        _isBlocked = true;
                        _blockEndTime = DateTime.Now.AddSeconds(10);
                        await ShowError("Вход заблокирован на 10 секунд");
                        return;
                    }
                }
                await ShowError("Неверный логин или пароль");
            }
        }

        private async Task ShowUserInfo(User user)
        {
            var userImage = this.FindControl<Image>("UserImage");
            var userInfoText = this.FindControl<TextBlock>("UserInfoText");

           
            userInfoText.Text = $"{user.FullName}\n{user.Role}";
        }

        private void StartSession(User user)
        {
            
            _sessionTimer = new Timer(600000); // 10 minutes
            _sessionTimer.Elapsed += (s, e) => Dispatcher.UIThread.InvokeAsync(() => EndSession());
            _sessionTimer.Start();

            
            var updateTimer = new Timer(1000);
            var sessionStart = DateTime.Now;
            updateTimer.Elapsed += (s, e) => Dispatcher.UIThread.InvokeAsync(() =>
            {
                var elapsed = DateTime.Now - sessionStart;
                var remaining = TimeSpan.FromMinutes(10) - elapsed;

                if (remaining <= TimeSpan.FromMinutes(5))
                {
                    var timerBlock = this.FindControl<TextBlock>("TimerBlock");
                    timerBlock.Text = $"Внимание! Сеанс завершится через: {remaining:mm':'ss}";
                }
            });
            updateTimer.Start();

            
            OpenUserWindow(user);
        }

        private async void EndSession()
        {
            _sessionTimer?.Stop();
            _isBlocked = true;
            _blockEndTime = DateTime.Now.AddMinutes(3);
            await ShowError("Сеанс завершен. Вход заблокирован на 3 минуты");
            InitializeLoginForm();
        }

        private void OpenUserWindow(User user)
        {
            
            Console.WriteLine($"Роль пользователя: '{user.Role}'");
            
           
            var window = new Views.MainMenuWindow();
            window.Initialize(user);
            window.Show();
            Hide();
        }

        private async Task ShowError(string message)
        {
            await MessageBox.Show(this, "Ошибка", message);
        }
    }
}
