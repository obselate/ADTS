namespace Adts.Playground;

public sealed class TokenMapViewModel
{
    public string MappingEvidence { get; } =
        "$ schema and resources are authored in spec/examples/starter.tokens.json\n" +
        "Token compiler emits src/Adts.Playground/Generated/Adts.Generated.axaml\n" +
        "App.axaml includes generated resources via <StyleInclude Source=\"/Generated/Adts.Generated.axaml\" />\n" +
        "MainWindow consumes keys and selectors: adts.color.*, adts.spacing.100, TextBlock.heading, Button.primary:pointerover";
}
