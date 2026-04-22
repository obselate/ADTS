using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia;

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
        if (Application.Current is not App app)
        {
            return;
        }

        var selectedThemeName = (themePresetCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
            ?? "Mono Ink";
        app.SetTheme(selectedThemeName);
        Title = $"{_baseTitle} - Theme {selectedThemeName} loaded at {DateTime.Now:HH:mm:ss}";
    }
}