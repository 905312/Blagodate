using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Blagodat.Controls;
using Blagodat.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZXing;
using ZXing.QrCode.Internal;
using ZXing.QrCode;

namespace Blagodat.Views
{
    public partial class GenerateBarcodeWindow : Window
    {
        private readonly User21Context _context;
        
        public GenerateBarcodeWindow()
        {
            InitializeComponent();
            _context = new User21Context();
        }

        private async void OnGenerateBarcodeClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(OrderIdTextBox.Text, out int orderId))
                {
                    StatusTextBlock.Text = "Ошибка: Некорректный номер заказа!";
                    return;
                }
                
                if (!int.TryParse(RentalHoursTextBox.Text, out int rentalHours) || rentalHours <= 0)
                {
                    StatusTextBlock.Text = "Ошибка: Некорректный срок проката!";
                    return;
                }
                
                var data = $"{orderId}{DateTime.Now:yyyyMMddHHmm}01{new Random().Next(100000, 999999)}";
                var writer = new QRCodeWriter();
                var matrix = writer.encode(data, BarcodeFormat.QR_CODE, 300, 300, new System.Collections.Generic.Dictionary<EncodeHintType, object> 
                    { { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H }, { EncodeHintType.MARGIN, 1 } });
                
                var bmp = new System.Drawing.Bitmap(matrix.Width, matrix.Height);
                for (int x = 0; x < matrix.Width; x++)
                    for (int y = 0; y < matrix.Height; y++)
                        bmp.SetPixel(x, y, matrix[x, y] ? System.Drawing.Color.Black : System.Drawing.Color.White);
                
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Barcodes");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"barcode_{orderId}.png");
                using var fs = new FileStream(path, FileMode.Create);
                bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                
                BarcodeImage.Source = new Bitmap(path);
                StatusTextBlock.Text = "Штрих-код успешно сгенерирован!";
                
                await SaveBarcodeToDatabase(orderId, path, rentalHours);
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
            }
        }
        
        private async Task SaveBarcodeToDatabase(int orderId, string barcodePath, int rentalHours)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString);
                await connection.OpenAsync();
                
                using var cmd = new Npgsql.NpgsqlCommand(
                    "UPDATE orders SET barcode_path = @path, rental_time = @rental_time WHERE order_id = @id", connection);
                
                cmd.Parameters.AddWithValue("path", barcodePath);
                cmd.Parameters.AddWithValue("rental_time", $"{rentalHours} hour");
                cmd.Parameters.AddWithValue("id", orderId);
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка сохранения: {ex.Message}";
            }
        }
        
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}