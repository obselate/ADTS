using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Adts.Playground;

public partial class App : Application
{
    private readonly Dictionary<string, string> _themeStylesByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mono Ink"] = "/Generated/theme_mono_ink.axaml",
        ["Amber Terminal"] = "/Generated/theme_amber_terminal.axaml",
        ["Oceanic Glow"] = "/Generated/theme_oceanic_glow.axaml"
    };

    private StyleInclude? _activeThemeStyle;
    private string _activeThemeName = "Mono Ink";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SetTheme("Mono Ink");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ApplyDefaultThemeVariant();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyDefaultThemeVariant()
    {
        if (TryGetResource("adts.requestedThemeVariant", ThemeVariant.Default, out var variantObj)
            && variantObj is string variant)
        {
            RequestedThemeVariant = variant switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
    }

    public void SetTheme(string themeName)
    {
        if (!_themeStylesByKey.TryGetValue(themeName, out var source))
        {
            throw new ArgumentException($"Unknown theme: {themeName}", nameof(themeName));
        }

        if (_activeThemeStyle is not null)
        {
            Styles.Remove(_activeThemeStyle);
        }

        var style = new StyleInclude(new Uri("avares://Adts.Playground"))
        {
            Source = new Uri(source, UriKind.Relative)
        };

        Styles.Add(style);
        _activeThemeStyle = style;
        _activeThemeName = themeName;
    }
}