using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia;

namespace Blagodat
{

    public class BarcodeGenerator
    {
        private const double SymbolHeight = 25.93;
        private const double BarHeight = 22.85;
        private const double LeftMargin = 3.63;
        private const double RightMargin = 2.31;
        private const double GuardExtension = 1.65;
        private const double DigitHeight = 2.75;
        private const double MinGapFromDigitToBar = 0.165;
        private const double BarWidthFactor = 0.15;
        private const double ZeroBarWidth = 1.35;
        private const double BarGap = 0.2;


        private static double MmToPixels(double mm)
        {
            return mm * 96 / 25.4;
        }


        public static Canvas GenerateBarcodeCanvas(string barcodeData)
        {
            try
            {
                int[] digits = new int[barcodeData.Length];
                for (int i = 0; i < barcodeData.Length; i++)
                {
                    digits[i] = int.Parse(barcodeData[i].ToString());
                }

                double totalWidth = LeftMargin + RightMargin;
                for (int i = 0; i < digits.Length; i++)
                {
                    if (digits[i] == 0)
                    {
                        totalWidth = totalWidth + ZeroBarWidth;
                    }
                    else
                    {
                        totalWidth = totalWidth + (BarWidthFactor * digits[i]);
                    }
                    totalWidth = totalWidth + BarGap;
                }
                
                double pixelWidth = MmToPixels(totalWidth);
                double pixelHeight = MmToPixels(SymbolHeight);
                
                Canvas canvas = new Canvas();
                canvas.Width = pixelWidth;
                canvas.Height = pixelHeight;
                canvas.Background = Brushes.White;
                
                double xPos = MmToPixels(LeftMargin);
                int centerIndex = digits.Length / 2;
                
                for (int i = 0; i < digits.Length; i++)
                {
                    int digit = digits[i];
                    
                    if (i == 0 || i == centerIndex || i == digits.Length - 1)
                    {
                        Rectangle guardRect = new Rectangle();
                        guardRect.Width = MmToPixels(BarWidthFactor);
                        guardRect.Height = MmToPixels(BarHeight + GuardExtension);
                        guardRect.Fill = Brushes.Black;
                        
                        Canvas.SetLeft(guardRect, xPos);
                        Canvas.SetTop(guardRect, 0);
                        canvas.Children.Add(guardRect);
                        
                        xPos = xPos + MmToPixels(BarWidthFactor + BarGap);
                        continue;
                    }
                    
                    if (digit > 0)
                    {
                        Rectangle barRect = new Rectangle();
                        barRect.Width = MmToPixels(BarWidthFactor * digit);
                        barRect.Height = MmToPixels(BarHeight);
                        barRect.Fill = Brushes.Black;
                        
                        Canvas.SetLeft(barRect, xPos);
                        Canvas.SetTop(barRect, 0);
                        canvas.Children.Add(barRect);
                        
                        TextBlock digitText = new TextBlock();
                        digitText.Text = digit.ToString();
                        digitText.FontSize = MmToPixels(DigitHeight);
                        digitText.Foreground = Brushes.Black;
                        digitText.TextAlignment = TextAlignment.Center;
                        digitText.Width = MmToPixels(BarWidthFactor * digit);
                        
                        Canvas.SetLeft(digitText, xPos);
                        Canvas.SetTop(digitText, MmToPixels(BarHeight + MinGapFromDigitToBar));
                        canvas.Children.Add(digitText);
                        
                        xPos = xPos + MmToPixels(BarWidthFactor * digit);
                    }
                    else
                    {
                        TextBlock zeroText = new TextBlock();
                        zeroText.Text = "0";
                        zeroText.FontSize = MmToPixels(DigitHeight);
                        zeroText.Foreground = Brushes.Black;
                        zeroText.TextAlignment = TextAlignment.Center;
                        zeroText.Width = MmToPixels(ZeroBarWidth);
                        
                        Canvas.SetLeft(zeroText, xPos);
                        Canvas.SetTop(zeroText, MmToPixels(BarHeight + MinGapFromDigitToBar));
                        canvas.Children.Add(zeroText);
                        
                        xPos = xPos + MmToPixels(ZeroBarWidth);
                    }
                    
                    xPos = xPos + MmToPixels(BarGap);
                }
                
                return canvas;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ошибка: " + ex.Message);
                return new Canvas();
            }
        }


        public static string GenerateBarcodeData(int orderId, DateTime creationDate, int rentalHours)
        {
            StringBuilder sb = new StringBuilder();
            
           
            sb.Append(orderId.ToString("D4"));
            
           // дата создания (день, месяц, год)
            sb.Append(creationDate.Day.ToString("D2"));
            sb.Append(creationDate.Month.ToString("D2"));
            sb.Append(creationDate.Year.ToString().Substring(2, 2));
            
            // время создания (час, минута)
            sb.Append(creationDate.Hour.ToString("D2"));
            sb.Append(creationDate.Minute.ToString("D2"));
            
            // срок проката в часах
            sb.Append(rentalHours.ToString("D3"));
            
            // уникальный код 
            Random random = new Random();
            for (int i = 0; i < 6; i++)
            {
                sb.Append(random.Next(0, 10).ToString());
            }
            
            return sb.ToString();
        }


        public static void SaveBarcodeToHtmlFile(string barcodeData, string filePath)
        {
            try
            {
                
                StringBuilder htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<!DOCTYPE html>");
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<head>");
                htmlBuilder.AppendLine("    <meta charset=\"utf-8\">");
                htmlBuilder.AppendLine("    <title>Штрих-код заказа №" + barcodeData.Substring(0, 4) + "</title>");
                htmlBuilder.AppendLine("    <style>");
                htmlBuilder.AppendLine("        body { font-family: Arial, sans-serif; text-align: center; }");
                htmlBuilder.AppendLine("        .barcode-container { margin: 50px auto; width: 80%; }");
                htmlBuilder.AppendLine("        .barcode-title { font-size: 24px; margin-bottom: 20px; }");
                htmlBuilder.AppendLine("        .barcode-info { margin: 20px 0; font-size: 16px; }");
                htmlBuilder.AppendLine("        .barcode-digits { letter-spacing: 2px; font-family: monospace; font-size: 18px; margin: 15px 0; }");
                htmlBuilder.AppendLine("        .barcode { margin: 0 auto; }");
                htmlBuilder.AppendLine("        .barcode-line { display: inline-block; background-color: #000; margin-right: 0.2mm; }");
                htmlBuilder.AppendLine("        .barcode-spacer { display: inline-block; background-color: #fff; margin-right: 0.2mm; }");
                htmlBuilder.AppendLine("        @media print {");
                htmlBuilder.AppendLine("            body { margin: 0; padding: 0; }");
                htmlBuilder.AppendLine("            .barcode-container { width: 100%; }");
                htmlBuilder.AppendLine("        }");
                htmlBuilder.AppendLine("    </style>");
                htmlBuilder.AppendLine("</head>");
                htmlBuilder.AppendLine("<body>");
                
                
                htmlBuilder.AppendLine("    <div class=\"barcode-container\">");
                htmlBuilder.AppendLine("        <div class=\"barcode-title\">Штрих-код заказа №" + barcodeData.Substring(0, 4) + "</div>");

                // цифры штрих-кода
                htmlBuilder.AppendLine("        <div class=\"barcode-digits\">");
                htmlBuilder.AppendLine("            " + barcodeData);
                htmlBuilder.AppendLine("        </div>");
                
                htmlBuilder.AppendLine("        <div class=\"barcode-info\">");
                htmlBuilder.AppendLine("            Дата создания: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                htmlBuilder.AppendLine("        </div>");
                
                // Визуальное представление штрих-кода
                htmlBuilder.AppendLine("        <div class=\"barcode\" style=\"width: " + MmToPixels(100) + "px; height: " + MmToPixels(SymbolHeight) + "px;\">");

                // левое поле
                htmlBuilder.AppendLine("            <div class=\"barcode-spacer\" style=\"width: " + MmToPixels(LeftMargin) + "px; height: " + MmToPixels(BarHeight) + "px;\"></div>");
                
                // штрихи для каждой цифры
                int[] digits = barcodeData.Select(c => int.Parse(c.ToString())).ToArray();
                int centerIndex = digits.Length / 2;
                
                for (int i = 0; i < digits.Length; i++)
                {
                    int digit = digits[i];
                    
                    if (i == 0 || i == centerIndex || i == digits.Length - 1)
                    {
                        
                        htmlBuilder.AppendLine("            <div class=\"barcode-line\" style=\"width: " + MmToPixels(BarWidthFactor) + "px; height: " + MmToPixels(BarHeight + GuardExtension) + "px;\"></div>");
                    }
                    else if (digit > 0)
                    {
                        
                        htmlBuilder.AppendLine("            <div class=\"barcode-line\" style=\"width: " + MmToPixels(BarWidthFactor * digit) + "px; height: " + MmToPixels(BarHeight) + "px;\"></div>");
                    }
                    else
                    {
                        
                        htmlBuilder.AppendLine("            <div class=\"barcode-spacer\" style=\"width: " + MmToPixels(ZeroBarWidth) + "px; height: " + MmToPixels(BarHeight) + "px;\"></div>");
                    }
                    
                   
                    htmlBuilder.AppendLine("            <div class=\"barcode-spacer\" style=\"width: " + MmToPixels(BarGap) + "px; height: " + MmToPixels(BarHeight) + "px;\"></div>");
                }
                
                
                htmlBuilder.AppendLine("            <div class=\"barcode-spacer\" style=\"width: " + MmToPixels(RightMargin) + "px; height: " + MmToPixels(BarHeight) + "px;\"></div>");
                
                htmlBuilder.AppendLine("        </div>");
                htmlBuilder.AppendLine("        <div class=\"barcode-info\">");
                htmlBuilder.AppendLine("            Благодать - прокат оборудования");
                htmlBuilder.AppendLine("        </div>");
                htmlBuilder.AppendLine("    </div>");
                
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");
                
                
                File.WriteAllText(filePath, htmlBuilder.ToString());
                
                
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", filePath);
                }
                catch
                {
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении штрих-кода: {ex.Message}");
            }
        }
    }
}
