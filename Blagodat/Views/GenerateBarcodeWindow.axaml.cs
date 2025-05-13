using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Layout;
using Blagodat.Models;
using Npgsql;
using System.Diagnostics;

namespace Blagodat.Views
{
    public partial class GenerateBarcodeWindow : Window
    {
        private readonly User21Context _context;
        private string _generatedBarcodeData;
        private Canvas _generatedBarcodeCanvas;
        
        public GenerateBarcodeWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private async void OnGenerateBarcodeClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var orderIdTextBox = this.FindControl<TextBox>("OrderIdTextBox");
                var rentalHoursTextBox = this.FindControl<TextBox>("RentalHoursTextBox");
                var statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
                var barcodeImage = this.FindControl<Image>("BarcodeImage");
                var barcodeTextBlock = this.FindControl<TextBlock>("BarcodeTextBlock");
                var savePdfButton = this.FindControl<Button>("SavePdfButton");
                var printBarcodeButton = this.FindControl<Button>("PrintBarcodeButton");
                
                // Проверка ввода
                if (string.IsNullOrWhiteSpace(orderIdTextBox.Text) || 
                    string.IsNullOrWhiteSpace(rentalHoursTextBox.Text))
                {
                    statusTextBlock.Text = "Ошибка: Пожалуйста, заполните все поля!";
                    return;
                }
                
                if (!int.TryParse(orderIdTextBox.Text, out int orderId))
                {
                    statusTextBlock.Text = "Ошибка: Некорректный номер заказа!";
                    return;
                }
                
                if (!int.TryParse(rentalHoursTextBox.Text, out int rentalHours) || rentalHours <= 0)
                {
                    statusTextBlock.Text = "Ошибка: Некорректный срок проката!";
                    return;
                }
                
              
                Order order = await GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    statusTextBlock.Text = "Ошибка: Заказ с указанным номером не найден!";
                    return;
                }
                
                // Генерируем данные штрих-кода
                _generatedBarcodeData = BarcodeGenerator.GenerateBarcodeData(
                    orderId, 
                    order.CreationDate, 
                    rentalHours);
                
                // Генерируем канвас со штрих-кодом
                _generatedBarcodeCanvas = BarcodeGenerator.GenerateBarcodeCanvas(_generatedBarcodeData);
                
                if (_generatedBarcodeCanvas != null)
                {
                    // Отображаем штрих-код
                    var barcodeContainer = this.FindControl<Panel>("BarcodeContainer");
                    if (barcodeContainer != null) {
                        // Очищаем панель и добавляем новый штрих-код
                        barcodeContainer.Children.Clear();
                        
                        // Добавляем текстовый заголовок
                        var headerText = new TextBlock
                        {
                            Text = $"Штрих-код заказа №{_generatedBarcodeData.Substring(0, 4)}",
                            FontSize = 16,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        
                        // Создаем простой штрих-код из прямоугольников
                        var barCodePanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 10, 0, 10),
                            Spacing = 2
                        };
                        
                        
                        foreach (char c in _generatedBarcodeData)
                        {
                            int digit = int.Parse(c.ToString());
                            double width = digit == 0 ? 6 : (digit * 3); 
                            
                            var bar = new Border
                            {
                                Width = width,
                                Height = 80,
                                Background = new SolidColorBrush(Colors.Black),
                                Margin = new Thickness(1, 0)
                            };
                            
                            barCodePanel.Children.Add(bar);
                        }
                        
                        // Добавляем цифры под штрих-кодом
                        var digitText = new TextBlock
                        {
                            Text = _generatedBarcodeData,
                            FontFamily = new Avalonia.Media.FontFamily("Courier New"),
                            FontSize = 16,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 10, 0, 0)
                        };
                        
                        // Создаем вертикальный стек со всеми элементами
                        var stackPanel = new StackPanel
                        {
                            Margin = new Thickness(10),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        
                        stackPanel.Children.Add(headerText);
                        stackPanel.Children.Add(barCodePanel);
                        stackPanel.Children.Add(digitText);
                        
                        // Добавляем все элементы в контейнер
                        barcodeContainer.Children.Add(stackPanel);
                    }
                    barcodeTextBlock.Text = _generatedBarcodeData;
                    
                    // Активируем кнопки
                    savePdfButton.IsEnabled = true;
                    printBarcodeButton.IsEnabled = true;
                    
                    statusTextBlock.Text = "Штрих-код успешно сгенерирован!";
                }
                else
                {
                    statusTextBlock.Text = "Ошибка при генерации изображения штрих-кода!";
                }
            }
            catch (Exception ex)
            {
                var statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
                statusTextBlock.Text = $"Ошибка: {ex.Message}";
                Debug.WriteLine($"Ошибка при генерации штрих-кода: {ex}");
            }
        }
        
        private async void OnSavePdfClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
                
                if (string.IsNullOrEmpty(_generatedBarcodeData) || _generatedBarcodeCanvas == null)
                {
                    statusTextBlock.Text = "Сначала сгенерируйте штрих-код!";
                    return;
                }
                
                // Создаем диалог сохранения файла
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Сохранить штрих-код как PDF",
                    Filters = new System.Collections.Generic.List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "PDF документы", Extensions = new System.Collections.Generic.List<string> { "pdf" } }
                    },
                    DefaultExtension = "pdf",
                    InitialFileName = $"barcode_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };
                
                var result = await saveFileDialog.ShowAsync(this);
                
                if (!string.IsNullOrEmpty(result))
                {
                    // Сохраняем штрих-код в HTML файл
                    BarcodeGenerator.SaveBarcodeToHtmlFile(_generatedBarcodeData, result);
                    statusTextBlock.Text = "Штрих-код успешно сохранен в файл!";
                }
            }
            catch (Exception ex)
            {
                var statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
                statusTextBlock.Text = $"Ошибка при сохранении PDF: {ex.Message}";
                Debug.WriteLine($"Ошибка при сохранении PDF: {ex}");
            }
        }
        
        private void OnPrintBarcodeClick(object sender, RoutedEventArgs e)
        {
            var statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
            statusTextBlock.Text = "Функция печати штрих-кода будет реализована в следующей версии";
        }
        
        private async Task<Order> GetOrderByIdAsync(int orderId)
        {
            try
            {
               
                string connectionString = "Host=localhost;Port=5432;Database=user21;Username=user21;Password=12345"; 
                
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    string sql = "SELECT order_id, creation_date FROM orders WHERE order_id = @orderId";
                    
                    using (var cmd = new NpgsqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("orderId", orderId);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Order
                                {
                                    OrderId = reader.GetInt32(0),
                                    CreationDate = reader.GetDateTime(1)
                                };
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении заказа: {ex}");
                return null;
            }
        }
    }
}
