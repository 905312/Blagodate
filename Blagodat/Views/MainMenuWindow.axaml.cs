using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Blagodat.Controls;
using Blagodat.Models;
using System;
using System.IO;
using System.Timers;

namespace Blagodat.Views
{
    
    public partial class MainMenuWindow : BaseWindow
    {
        private Timer _sessionTimer;

        public MainMenuWindow()
        {
            InitializeComponent();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            
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
            
            StartSession();
        }
        
        private void StartSession()
        {
            _sessionTimer = new Timer(600000); 
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
                    TimerBlock.Text = $"Внимание! Сеанс завершится через: {remaining:mm':'ss}";
                }
            });
            updateTimer.Start();
        }

        private void EndSession()
        {
            _sessionTimer?.Stop();
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await MessageBox.Show(this, "Сессия", "Ваша сессия завершена из-за неактивности");
                Logout();
            });
        }

        private void Logout()
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();
            Close();
        }

     
        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void OnExitApplicationClick(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }


        private void OnAddClientClick(object sender, RoutedEventArgs e)
        {
            var window = new AddClientWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnDeleteClientClick(object sender, RoutedEventArgs e)
        {
            var window = new DeleteClientWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnEditClientClick(object sender, RoutedEventArgs e)
        {
            var window = new EditClientWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        

        private void OnAddServiceClick(object sender, RoutedEventArgs e)
        {
            var window = new AddServiceWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnDeleteServiceClick(object sender, RoutedEventArgs e)
        {
            var window = new DeleteServiceWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnEditServiceClick(object sender, RoutedEventArgs e)
        {
            var window = new EditServiceWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

    
        private void OnAddOrderClick(object sender, RoutedEventArgs e)
        {
            var window = new AddOrderWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnDeleteOrderClick(object sender, RoutedEventArgs e)
        {
            var window = new DeleteOrderWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnEditOrderClick(object sender, RoutedEventArgs e)
        {
            var window = new EditOrderWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnOrderHistoryClick(object sender, RoutedEventArgs e)
        {
            var window = new OrderHistoryWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        
        private void OnTransferPositionClick(object sender, RoutedEventArgs e)
        {
            var window = new TransferPositionWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnDeleteEmployeeClick(object sender, RoutedEventArgs e)
        {
            var window = new DeleteEmployeeWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }

        private void OnEditProfileClick(object sender, RoutedEventArgs e)
        {
            var window = new EditProfileWindow();
            window.Initialize(CurrentUser);
            window.Show();
        }
    }
}
