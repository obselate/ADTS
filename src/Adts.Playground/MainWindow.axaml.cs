using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Adts.Playground;

public partial class MainWindow : Window
{
    private readonly string _baseTitle = "ADTS Playground (Avalonia 11)";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void PrimaryActionClicked(object? sender, RoutedEventArgs e)
    {
        Title = $"{_baseTitle} - Primary action fired at {System.DateTime.Now:HH:mm:ss}";
    }

    private void ApplyPresetClicked(object? sender, RoutedEventArgs e)
    {
        Title = $"{_baseTitle} - Preset applied at {System.DateTime.Now:HH:mm:ss}";
    }
}