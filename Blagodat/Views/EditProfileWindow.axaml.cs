using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Blagodat.Views
{
    
    public partial class EditProfileWindow : BaseWindow
    {
        private User _user;
        private User21Context _context;
        private string _selectedPhotoPath;
        
        private User _currentUser;

        public EditProfileWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        public override void Initialize(User user)
        {
            base.Initialize(user);
            
            _context = new User21Context();
            
            _currentUser = user;
            
            LoadUserProfile(user);
        }

        private void LoadUserProfile(User user)
        {
            
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;
            StatusBlock.Text = "Редактирование личных данных";
            
            
            
           
            LoginTextBox.Text = user.Login;
            FullNameTextBox.Text = user.FullName;
            
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    UserImage.Source = new Bitmap(logoPath);
                    
                    if (string.IsNullOrEmpty(user.PhotoPath))
                    {
                        ProfileImage.Source = new Bitmap(logoPath);
                    }
                }
                
                if (!string.IsNullOrEmpty(user.PhotoPath))
                {
                    string photoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", user.PhotoPath);
                    if (File.Exists(photoPath))
                    {
                        ProfileImage.Source = new Bitmap(photoPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
            }
        }

        private async void OnChangePhotoClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Выберите изображение",
                    Filters = new System.Collections.Generic.List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Изображения", Extensions = { "jpg", "jpeg", "png", "bmp" } }
                    },
                    AllowMultiple = false
                };

                var result = await dialog.ShowAsync(this);
                
                if (result != null && result.Length > 0)
                {
                    string selectedPath = result[0];
                    _selectedPhotoPath = selectedPath;
                    
                    
                    ProfileImage.Source = new Bitmap(selectedPath);
                    StatusBlock.Text = "Новое фото выбрано. Нажмите 'Сохранить изменения' для применения.";
                }
            }
            catch (Exception ex)
            {
               
                StatusBlock.Text = $"Ошибка при выборе фото: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось выбрать фото: {ex.Message}");
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                
                
                // Validation
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                {
                    StatusBlock.Text = "Ошибка: Введите ФИО";
                    return;
                }
                
                var user = new User
                {
                    UserId = CurrentUser.UserId,
                    Login = CurrentUser.Login,
                    Password = CurrentUser.Password,
                    FullName = CurrentUser.FullName,
                    Role = CurrentUser.Role,
                    PhotoPath = CurrentUser.PhotoPath,
                    EmployeeId = CurrentUser.EmployeeId
                };
                
                bool changingPassword = !string.IsNullOrEmpty(NewPasswordTextBox.Text);
                
                if (changingPassword)
                {
                    if (string.IsNullOrEmpty(CurrentPasswordTextBox.Text))
                    {
                        StatusBlock.Text = "Ошибка: Введите текущий пароль";
                        return;
                    }
                    
                    if (CurrentPasswordTextBox.Text != user.Password)
                    {
                        StatusBlock.Text = "Ошибка: Неверный текущий пароль";
                        return;
                    }
                    
                    if (NewPasswordTextBox.Text != ConfirmPasswordTextBox.Text)
                    {
                        StatusBlock.Text = "Ошибка: Пароли не совпадают";
                        return;
                    }
                    
                    if (NewPasswordTextBox.Text.Length < 6)
                    {
                        StatusBlock.Text = "Ошибка: Пароль должен содержать не менее 6 символов";
                        return;
                    }
                }
                
                user.FullName = FullNameTextBox.Text;
                
                if (changingPassword)
                {
                    user.Password = NewPasswordTextBox.Text;
                }
                
                if (!string.IsNullOrEmpty(_selectedPhotoPath))
                {
                    string fileName = Path.GetFileName(_selectedPhotoPath);
                    string targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
                    
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
                    
                    File.Copy(_selectedPhotoPath, targetPath, true);
                    
                    user.PhotoPath = fileName;
                }
                
                _currentUser.Login = user.Login;
                _currentUser.FullName = user.FullName;
                
                if (!string.IsNullOrEmpty(user.Password))
                {
                    _currentUser.Password = user.Password;
                }
                
                if (!string.IsNullOrEmpty(user.PhotoPath))
                {
                    _currentUser.PhotoPath = user.PhotoPath;
                }
                
                
                using (var connection = new NpgsqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            
                            string updateUserSql = @"
                                UPDATE users
                                SET full_name = @fullName,
                                    password = @password
                                WHERE user_id = @userId";
                                
                            using (var cmd = new NpgsqlCommand(updateUserSql, connection))
                            {
                                cmd.Transaction = transaction;
                                cmd.Parameters.AddWithValue("userId", user.UserId);
                                cmd.Parameters.AddWithValue("fullName", user.FullName);
                                cmd.Parameters.AddWithValue("password", user.Password);
                                
                                await cmd.ExecuteNonQueryAsync();
                            }
                            
                            
                            if (!string.IsNullOrEmpty(user.PhotoPath))
                            {
                                string updatePhotoSql = @"
                                    UPDATE users
                                    SET photo_path = @photoPath
                                    WHERE user_id = @userId";
                                    
                                using (var cmd = new NpgsqlCommand(updatePhotoSql, connection))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.AddWithValue("userId", user.UserId);
                                    cmd.Parameters.AddWithValue("photoPath", user.PhotoPath);
                                    
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            
                           
                            if (user.EmployeeId.HasValue)
                            {
                                string updateEmployeeSql = @"
                                    UPDATE employees
                                    SET full_name = @fullName,
                                        login = @login,
                                        password = @password
                                    WHERE employee_id = @employeeId";
                                    
                                using (var cmd = new NpgsqlCommand(updateEmployeeSql, connection))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.AddWithValue("employeeId", user.EmployeeId.Value);
                                    cmd.Parameters.AddWithValue("fullName", user.FullName);
                                    cmd.Parameters.AddWithValue("login", user.Login);
                                    cmd.Parameters.AddWithValue("password", user.Password);
                                    
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                
                               
                                if (!string.IsNullOrEmpty(user.PhotoPath))
                                {
                                    string updateEmployeePhotoSql = @"
                                        UPDATE employees
                                        SET photo = @photoPath
                                        WHERE employee_id = @employeeId";
                                        
                                    using (var cmd = new NpgsqlCommand(updateEmployeePhotoSql, connection))
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.Parameters.AddWithValue("employeeId", user.EmployeeId.Value);
                                        cmd.Parameters.AddWithValue("photoPath", user.PhotoPath);
                                        
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            
                            await transaction.CommitAsync();
                            
                            _currentUser.FullName = user.FullName;
                            _currentUser.Password = user.Password;
                            _currentUser.PhotoPath = user.PhotoPath;
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception($"Ошибка при обновлении данных: {ex.Message}");
                        }
                    }
                }
                
               
                UserNameText.Text = user.FullName;
                
                await MessageBox.Show(this, "Успех", "Личные данные успешно обновлены");
                StatusBlock.Text = "Личные данные успешно обновлены";
                
                CurrentPasswordTextBox.Text = "";
                NewPasswordTextBox.Text = "";
                ConfirmPasswordTextBox.Text = "";
            }
            catch (Exception ex)
            {
                
                StatusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить личные данные: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
