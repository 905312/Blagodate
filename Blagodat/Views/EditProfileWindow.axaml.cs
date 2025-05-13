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
            var userNameText = this.FindControl<TextBlock>("UserNameText");
            var userRoleText = this.FindControl<TextBlock>("UserRoleText");
            var userImage = this.FindControl<Image>("UserImage");
            var profileImage = this.FindControl<Image>("ProfileImage");
            var statusBlock = this.FindControl<TextBlock>("StatusBlock");
            
            userNameText.Text = user.FullName;
            userRoleText.Text = user.Role;
            statusBlock.Text = "Редактирование личных данных";
            
            var loginTextBox = this.FindControl<TextBox>("LoginTextBox");
            var fullNameTextBox = this.FindControl<TextBox>("FullNameTextBox");
            
            loginTextBox.Text = user.Login;
            fullNameTextBox.Text = user.FullName;
            
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");
                if (File.Exists(logoPath))
                {
                    userImage.Source = new Bitmap(logoPath);
                    
                    if (string.IsNullOrEmpty(user.PhotoPath))
                    {
                        profileImage.Source = new Bitmap(logoPath);
                    }
                }
                
                if (!string.IsNullOrEmpty(user.PhotoPath))
                {
                    string photoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", user.PhotoPath);
                    if (File.Exists(photoPath))
                    {
                        profileImage.Source = new Bitmap(photoPath);
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
                    
                    // Update profile image preview
                    var profileImage = this.FindControl<Image>("ProfileImage");
                    profileImage.Source = new Bitmap(selectedPath);
                    
                    var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                    statusBlock.Text = "Новое фото выбрано. Нажмите 'Сохранить изменения' для применения.";
                }
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка при выборе фото: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось выбрать фото: {ex.Message}");
            }
        }

        private async void OnSaveChangesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var fullNameTextBox = this.FindControl<TextBox>("FullNameTextBox");
                var currentPasswordTextBox = this.FindControl<TextBox>("CurrentPasswordTextBox");
                var newPasswordTextBox = this.FindControl<TextBox>("NewPasswordTextBox");
                var confirmPasswordTextBox = this.FindControl<TextBox>("ConfirmPasswordTextBox");
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                
                // Validation
                if (string.IsNullOrWhiteSpace(fullNameTextBox.Text))
                {
                    statusBlock.Text = "Ошибка: Введите ФИО";
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
                
                bool changingPassword = !string.IsNullOrEmpty(newPasswordTextBox.Text);
                
                if (changingPassword)
                {
                    if (string.IsNullOrEmpty(currentPasswordTextBox.Text))
                    {
                        statusBlock.Text = "Ошибка: Введите текущий пароль";
                        return;
                    }
                    
                    if (currentPasswordTextBox.Text != user.Password)
                    {
                        statusBlock.Text = "Ошибка: Неверный текущий пароль";
                        return;
                    }
                    
                    if (newPasswordTextBox.Text != confirmPasswordTextBox.Text)
                    {
                        statusBlock.Text = "Ошибка: Пароли не совпадают";
                        return;
                    }
                    
                    if (newPasswordTextBox.Text.Length < 6)
                    {
                        statusBlock.Text = "Ошибка: Пароль должен содержать не менее 6 символов";
                        return;
                    }
                }
                
                user.FullName = fullNameTextBox.Text;
                
                if (changingPassword)
                {
                    user.Password = newPasswordTextBox.Text;
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
                            // Обновляем данные пользователя
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
                            
                            // Обновляем фото пользователя, если оно было изменено
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
                            
                            // Обновляем данные сотрудника, если пользователь связан с сотрудником
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
                                
                                // Обновляем фото сотрудника, если оно было изменено
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
                
                var userNameText = this.FindControl<TextBlock>("UserNameText");
                userNameText.Text = user.FullName;
                
                await MessageBox.Show(this, "Успех", "Личные данные успешно обновлены");
                statusBlock.Text = "Личные данные успешно обновлены";
                
                currentPasswordTextBox.Text = "";
                newPasswordTextBox.Text = "";
                confirmPasswordTextBox.Text = "";
            }
            catch (Exception ex)
            {
                var statusBlock = this.FindControl<TextBlock>("StatusBlock");
                statusBlock.Text = $"Ошибка: {ex.Message}";
                await MessageBox.Show(this, "Ошибка", $"Не удалось обновить личные данные: {ex.Message}");
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
