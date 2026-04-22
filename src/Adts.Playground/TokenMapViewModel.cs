namespace Adts.Playground;

public sealed class TokenMapViewModel
{
    public string MappingEvidence { get; } =
        "$ schema and resources are authored in spec/examples/starter.tokens.json\n" +
        "Token compiler emits src/Adts.Playground/Generated/Adts.Generated.axaml\n" +
        "App.axaml includes generated resources via <StyleInclude Source=\"/Generated/Adts.Generated.axaml\" />\n" +
        "MainWindow consumes keys and selectors: adts.color.*, adts.spacing.100, TextBlock.heading, Button.primary:pointerover";

    public string ThemeEvidence { get; } =
        "Three non-Fluent ADTS themes are generated from JSON:\n" +
        "- theme_mono_ink.tokens.json\n" +
        "- theme_amber_terminal.tokens.json\n" +
        "- theme_oceanic_glow.tokens.json\n" +
        "Each includes explicit state selectors and custom control theme examples.";
}
