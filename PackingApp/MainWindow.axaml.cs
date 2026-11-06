using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using PackingLib;

namespace PackingApp;

public partial class MainWindow : Window
{
    private const int GenerationsPerUpdate = 10;

    private readonly List<ServiceInfo> services = new List<ServiceInfo>();

    private DistributedAlgorithm? algorithm;
    private Individual? best;
    private bool isRunning;

    public MainWindow()
    {
        InitializeComponent();

        FieldCanvas.SizeChanged += (sender, e) => DrawBest();
        Closing += (sender, e) => isRunning = false;

        services.Add(new ServiceInfo("127.0.0.1:5000"));
        ShowServices();
    }

    private async void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        int count1 = (int)(Count1Box.Value ?? 0);
        int count2 = (int)(Count2Box.Value ?? 0);
        int count3 = (int)(Count3Box.Value ?? 0);
        int populationSize = (int)(PopulationBox.Value ?? 500);
        double mutationProbability = (double)(MutationBox.Value ?? 0.1m);

        if (count1 + count2 + count3 == 0)
        {
            StatusText.Text = "Нужен хотя бы один квадрат";
            return;
        }

        if (services.Count == 0)
        {
            StatusText.Text = "Добавьте хотя бы один вычислительный сервис";
            return;
        }

        foreach (ServiceInfo service in services)
        {
            service.IsWorking = true;
            service.Error = "";
        }
        ShowServices();

        algorithm = null;
        best = null;
        GenerationText.Text = "";
        AreaText.Text = "";
        DrawBest();

        SetControlsEnabled(false);
        StatusText.Text = "Идёт поиск...";
        TotalText.Text = "Сумма площадей квадратов: " + (count1 + 4 * count2 + 9 * count3);
        isRunning = true;

        DistributedAlgorithm ga = new DistributedAlgorithm(count1, count2, count3, populationSize, mutationProbability, services);

        try
        {
            await Task.Run(() => ga.CreatePopulationAsync());
            algorithm = ga;
            ShowBest();

            while (isRunning)
            {
                await Task.Run(async () =>
                {
                    for (int i = 0; i < GenerationsPerUpdate; i++)
                        await ga.NextGenerationAsync();
                });
                ShowBest();
                ShowServices();
            }

            StatusText.Text = "Поиск остановлен. Показано лучшее найденное решение";
        }
        catch (NoServicesException)
        {
            ShowBest();
            if (algorithm == null)
                StatusText.Text = "Поиск не начат: нет доступных вычислительных сервисов";
            else
                StatusText.Text = "Поиск прерван: нет доступных вычислительных сервисов. Показано лучшее найденное решение";
        }

        isRunning = false;
        ShowServices();
        SetControlsEnabled(true);
    }

    private void StopButton_Click(object? sender, RoutedEventArgs e)
    {
        isRunning = false;
    }

    private void AddServiceButton_Click(object? sender, RoutedEventArgs e)
    {
        string ipText = (IpBox.Text ?? "").Trim();
        if (!IPAddress.TryParse(ipText, out IPAddress? ip))
        {
            ServicesText.Text = "Неверный IP-адрес";
            return;
        }

        if (PortBox.Value == null)
        {
            ServicesText.Text = "Не задан порт";
            return;
        }
        int port = (int)PortBox.Value;

        string address = new IPEndPoint(ip, port).ToString();

        foreach (ServiceInfo service in services)
        {
            if (service.Address == address)
            {
                ServicesText.Text = "Такой сервис уже есть в списке";
                return;
            }
        }

        services.Add(new ServiceInfo(address));
        ShowServices();
    }

    private void RemoveServiceButton_Click(object? sender, RoutedEventArgs e)
    {
        int index = ServicesList.SelectedIndex;
        if (index < 0)
        {
            ServicesText.Text = "Выберите сервис в списке";
            return;
        }

        services.RemoveAt(index);
        ShowServices();
    }

    private void SetControlsEnabled(bool enabled)
    {
        StartButton.IsEnabled = enabled;
        StopButton.IsEnabled = !enabled;
        AddServiceButton.IsEnabled = enabled;
        RemoveServiceButton.IsEnabled = enabled;
    }

    private void ShowServices()
    {
        List<string> lines = new List<string>();
        string errors = "";

        foreach (ServiceInfo service in services)
        {
            if (service.IsWorking)
            {
                lines.Add(service.Address);
            }
            else
            {
                lines.Add(service.Address + " - ошибка");
                errors += service.Address + ": " + service.Error + "\n";
            }
        }

        ServicesList.ItemsSource = lines;
        ServicesText.Text = errors;
    }

    private void ShowBest()
    {
        if (algorithm == null)
            return;

        best = algorithm.Best;
        algorithm.GetBounds(best, out int minX, out int minY, out int maxX, out int maxY);

        GenerationText.Text = "Поколение: " + algorithm.Generation;
        AreaText.Text = "Площадь: " + best.Area + " (" + (maxX - minX) + " × " + (maxY - minY) + ")";
        DrawBest();
    }

    private void DrawBest()
    {
        FieldCanvas.Children.Clear();
        if (algorithm == null || best == null)
            return;

        int[] sizes = algorithm.Sizes;
        algorithm.GetBounds(best, out int minX, out int minY, out int maxX, out int maxY);
        int width = maxX - minX;
        int height = maxY - minY;

        double margin = 20;
        double scale = Math.Min((FieldCanvas.Bounds.Width - 2 * margin) / width,
                                (FieldCanvas.Bounds.Height - 2 * margin) / height);
        if (scale <= 0)
            return;

        double offsetX = (FieldCanvas.Bounds.Width - width * scale) / 2;
        double offsetY = (FieldCanvas.Bounds.Height - height * scale) / 2;

        AddRectangle(offsetX, offsetY, width * scale, height * scale, "#F0F0F0", "#808080");

        for (int i = 0; i < sizes.Length; i++)
        {
            string fill = "#DAE8FC", stroke = "#6C8EBF";
            if (sizes[i] == 2) { fill = "#D5E8D4"; stroke = "#82B366"; }
            if (sizes[i] == 3) { fill = "#FFE6CC"; stroke = "#D79B00"; }

            double left = offsetX + (best.X[i] - minX) * scale;
            double top = offsetY + (maxY - best.Y[i] - sizes[i]) * scale;
            AddRectangle(left, top, sizes[i] * scale, sizes[i] * scale, fill, stroke);
        }
    }

    private void AddRectangle(double left, double top, double width, double height, string fill, string stroke)
    {
        Rectangle rectangle = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brush.Parse(fill),
            Stroke = Brush.Parse(stroke),
            StrokeThickness = 1
        };
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
        FieldCanvas.Children.Add(rectangle);
    }
}
