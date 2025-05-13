using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace Blagodat.Controls
{
    public enum MessageBoxButtons
    {
        Ok,
        YesNo
    }

    public enum MessageBoxResult
    {
        None,
        Yes,
        No,
        Ok
    }

    public class MessageBox
    {
        public static async Task<MessageBoxResult> Show(Window parent, string title, string message, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>();

            var dialog = new Window
            {
                Title = title,
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 15),
                Spacing = 15
            };

            if (buttons == MessageBoxButtons.YesNo)
            {
                var yesButton = new Button
                {
                    Content = "Да",
                    Width = 80
                };

                var noButton = new Button
                {
                    Content = "Нет",
                    Width = 80
                };

                yesButton.Click += (s, e) =>
                {
                    tcs.SetResult(MessageBoxResult.Yes);
                    dialog.Close();
                };

                noButton.Click += (s, e) =>
                {
                    tcs.SetResult(MessageBoxResult.No);
                    dialog.Close();
                };

                buttonPanel.Children.Add(yesButton);
                buttonPanel.Children.Add(noButton);
            }
            else
            {
                var okButton = new Button
                {
                    Content = "OK",
                    Width = 80
                };

                okButton.Click += (s, e) =>
                {
                    tcs.SetResult(MessageBoxResult.Ok);
                    dialog.Close();
                };

                buttonPanel.Children.Add(okButton);
            }

            var panel = new StackPanel
            {
                Margin = new Avalonia.Thickness(10)
            };
            panel.Children.Add(messageText);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;

            dialog.Closed += (s, e) =>
            {
                if (!tcs.Task.IsCompleted)
                    tcs.SetResult(MessageBoxResult.None);
            };

            await dialog.ShowDialog(parent);
            return await tcs.Task;
        }
    }
}
