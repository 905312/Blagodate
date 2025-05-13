using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Blagodat.Models;
using System;
using System.IO;
using System.Timers;

namespace Blagodat.Views
{
    public partial class BaseWindow : Window
    {
        protected User CurrentUser { get; private set; }
        private Timer _sessionTimer;
        private Timer _updateTimer;
        private DateTime _sessionStart;

        public BaseWindow()
        {
            InitializeComponent();
            
            // Устанавливаем иконку окна программно
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    this.Icon = new WindowIcon(logoPath);
                }
                else
                {
                    Console.WriteLine($"Файл иконки не найден: {logoPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке иконки: {ex.Message}");
            }
        }

        public virtual void Initialize(User user)
        {
            CurrentUser = user;
            InitializeUserInfo();
            StartSessionTimer();
        }

        private void InitializeUserInfo()
        {
            var userImage = this.FindControl<Image>("UserImage");
            var userNameText = this.FindControl<TextBlock>("UserNameText");
            var userRoleText = this.FindControl<TextBlock>("UserRoleText");

            
            userNameText.Text = CurrentUser.FullName;
            userRoleText.Text = CurrentUser.Role;
        }

        private void StartSessionTimer()
        {
            _sessionStart = DateTime.Now;
            
            _sessionTimer = new Timer(600000); 
            _sessionTimer.Elapsed += (s, e) => Dispatcher.UIThread.InvokeAsync(EndSession);
            _sessionTimer.Start();

            _updateTimer = new Timer(1000);
            _updateTimer.Elapsed += (s, e) => Dispatcher.UIThread.InvokeAsync(UpdateTimer);
            _updateTimer.Start();
        }

        private void UpdateTimer()
        {
            var elapsed = DateTime.Now - _sessionStart;
            var remaining = TimeSpan.FromMinutes(10) - elapsed;

            if (remaining <= TimeSpan.Zero)
            {
                EndSession();
                return;
            }

            var timerBlock = this.FindControl<TextBlock>("TimerBlock");
            if (remaining <= TimeSpan.FromMinutes(5))
            {
                timerBlock.Text = $"Внимание! Сеанс завершится через: {remaining:mm\\:ss}";
            }
            else
            {
                timerBlock.Text = $"Время сеанса: {remaining:mm\\:ss}";
            }
        }

        private void EndSession()
        {
            _sessionTimer?.Stop();
            _updateTimer?.Stop();
            Close();
        }

        protected void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            EndSession();
        }
    }
}
