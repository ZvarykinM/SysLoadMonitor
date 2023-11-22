using Avalonia.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using CPUandMemoTest;
using Avalonia.Interactivity;
using System.Diagnostics;
using Avalonia;
using System.Threading;
using Avalonia.Threading;

namespace AvaloniaSample;

public partial class MainWindow : Window
{
    private readonly List<DateTimePoint> _values1 = new();

    private readonly List<DateTimePoint> _values2 = new();
    private readonly DateTimeAxis _customAxis;

    private UnixSystemMonitoringVirtualDevice SysDev;
    public ObservableCollection<ISeries> SeriesProc { get; set; }

    public ObservableCollection<ISeries> SeriesMem { get; set; }
    public Axis[] ProcXAxes { get; set; }

    public Axis[] MemXAxes { get; set; }

    public object SyncProc { get; } = new object();

    public object SyncMem { get; } = new object();

    public bool IsReading { get; set; } = true;

    private static Border MakeTextBlock(string Str)
    {
        var B = new Border
        {
            BorderBrush = Avalonia.Media.Brushes.Green,
            BorderThickness = new Thickness(1, 1, 1, 1)
        };
        var TBlock = new TextBlock
        {
            Text = Str,
            Foreground = Avalonia.Media.Brushes.Green,
            Background = Avalonia.Media.Brushes.Black,
            Height = TableStringHeight,
            Width = TablePanelFieldSize,
            FontSize = 22
        };
        B.Child = TBlock;
        return B;
    }
    private List<string> AllInfo;
    private const int TablePanelFieldSize = 150;
    private const int TableStringHeight = 40;
    private static StackPanel MakeStackPanelForTableRow(string[] Strings)
    {
        var SPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal
        };
        foreach(var s in Strings)
            SPanel.Children.Add(MakeTextBlock(s));
        return SPanel;

    }

    public void KillProcess(object? sender, RoutedEventArgs args)
    {
        try
        {
            Process.GetProcessById(Convert.ToInt32(PIDTextBox.Text)).Kill();
        }
        catch(Exception e)
        {
            PIDTextBox.Foreground = Avalonia.Media.Brushes.Red;
        }
    }
    public void ReadInfoAboutProcesses(object? sender)
    {
        lock(SyncProc)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AllInfo = ProcessGetter.GetProcessInfo();
                var ExistingPanelsCollection = new List<StackPanel>();
                foreach(var elem in LBox.Items)
                    ExistingPanelsCollection.Add(elem as StackPanel);
                if(ExistingPanelsCollection.Count > AllInfo.Count)
                    ExistingPanelsCollection.RemoveRange(ExistingPanelsCollection.Count - AllInfo.Count - 1, ExistingPanelsCollection.Count);
                var i = 0;
                foreach(var SPanel in ExistingPanelsCollection)
                {
                    SPanel.Children.Clear();
                    foreach(var s in AllInfo[i].Split(';'))
                        SPanel.Children.Add(MakeTextBlock(s));
                    i++;
                }
                AmountOfProcessesInfoTextBox.Text = $"Quantity of all processes in the system: {AllInfo.Count}";
                PIDTextBox.Foreground = Avalonia.Media.Brushes.White;
            });
        }
    }
    private Timer timer1;

    private async Task ReadData()
    {
        while (IsReading)
        {
            await Task.Delay(100);
            lock (SyncProc)
            {
                var SysInfo = SysDev.Renew; 
                
                _values1.Add(new DateTimePoint(DateTime.Now, SysInfo[0] * 100));
                if (_values1.Count > 250) _values1.RemoveAt(0);

                _values2.Add(new DateTimePoint(DateTime.Now, SysInfo[1]));
                if (_values2.Count > 250) _values2.RemoveAt(0);

                _customAxis.CustomSeparators = GetSeparators();
            }
        }
    }

    private double[] GetSeparators()
    {
        var now = DateTime.Now;

        return new double[]
        {
            now.AddSeconds(-25).Ticks,
            now.AddSeconds(-20).Ticks,
            now.AddSeconds(-15).Ticks,
            now.AddSeconds(-10).Ticks,
            now.AddSeconds(-5).Ticks,
            now.Ticks
        };
    }

    private static string Formatter(DateTime date)
    {
        var secsAgo = (DateTime.Now - date).TotalSeconds;

        return secsAgo < 1
            ? "now"
            : $"{secsAgo:N0}s ago";
    }

    public SolidColorPaint ProcLegendTextPaint { get; set; } = new SolidColorPaint 
        { 
            Color = new SKColor(50, 50, 50), 
            SKTypeface = SKTypeface.FromFamilyName("Courier New") 
        }; 

    public SolidColorPaint MemLegendTextPaint { get; set; } = new SolidColorPaint 
        { 
            Color = new SKColor(50, 50, 50), 
            SKTypeface = SKTypeface.FromFamilyName("Courier New") 
        }; 
    public MainWindow()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            SysDev = new();

        DataContext = this;
        this.AttachDevTools();
        SeriesProc = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = _values1,
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                Name = "CPU Load"
            }
        };

        SeriesMem = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = _values2,
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                Name = "Memory Load"
            }
        };
        _customAxis = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
        {
            CustomSeparators = GetSeparators(),
            AnimationsSpeed = TimeSpan.FromMilliseconds(0),
            SeparatorsPaint = new SolidColorPaint(SKColors.Black.WithAlpha(100))
        };

        ProcXAxes = [_customAxis];
        MemXAxes = [_customAxis];
        
        _ = ReadData();
        
        //ИНИЦИАЛИЗАЦИЯ КОМПОНЕНТА АВАЛИОНИИ
        InitializeComponent();

        GridForTable.Width = 600;
        Knopka.Background = Avalonia.Media.Brushes.DarkRed;
        AmountOfProcessesInfoTextBox.Foreground = Avalonia.Media.Brushes.DarkGreen;
        AmountOfProcessesInfoTextBox.FontSize = 25;
        ScrlViewer.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
        AllInfo = ProcessGetter.GetProcessInfo();
        foreach(var s in AllInfo)
            LBox.Items.Add(MakeStackPanelForTableRow(s.Split(';')));
        timer1 = new Timer(ReadInfoAboutProcesses, null, 0, 500);
    }
}