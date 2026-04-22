using System.Text;
using System.Text.Json;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Adts.TokenCompiler <input-tokens.json> <output-axaml>");
    return 1;
}

var inputPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input token file not found: {inputPath}");
    return 2;
}

using var document = JsonDocument.Parse(File.ReadAllText(inputPath));
var root = document.RootElement;

if (!root.TryGetProperty("themes", out var themes) || !themes.TryGetProperty("base", out var baseTheme))
{
    Console.Error.WriteLine("Token file must contain themes.base.");
    return 3;
}

if (!baseTheme.TryGetProperty("resources", out var resources))
{
    Console.Error.WriteLine("themes.base.resources is required.");
    return 4;
}

var aliases = root.TryGetProperty("aliases", out var aliasesNode) ? aliasesNode : default;
var styles = baseTheme.TryGetProperty("styles", out var stylesNode) ? stylesNode : default;

var sb = new StringBuilder();
sb.AppendLine("<Styles xmlns=\"https://github.com/avaloniaui\"");
sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
sb.AppendLine("  <Styles.Resources>");
sb.AppendLine("    <ResourceDictionary>");

var variantDictionaries = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
if (baseTheme.TryGetProperty("variants", out var variantsNode) && variantsNode.ValueKind == JsonValueKind.Object)
{
    foreach (var variant in variantsNode.EnumerateObject())
    {
        var byKey = new Dictionary<string, string>(StringComparer.Ordinal);
        if (variant.Value.TryGetProperty("resources", out var variantResources) &&
            variantResources.ValueKind == JsonValueKind.Object)
        {
            foreach (var resource in variantResources.EnumerateObject())
            {
                byKey[resource.Name] = ResolveTokenValue(
                    resource.Value.GetProperty("$value"),
                    resources,
                    aliases,
                    null);
            }
        }

        variantDictionaries[variant.Name] = byKey;
    }
}

if (variantDictionaries.Count > 0)
{
    sb.AppendLine("      <ResourceDictionary.ThemeDictionaries>");
    foreach (var variant in variantDictionaries)
    {
        sb.AppendLine($"        <ResourceDictionary x:Key=\"{EscapeXml(variant.Key)}\">");
        foreach (var entry in variant.Value)
        {
            sb.AppendLine(
                $"          <SolidColorBrush x:Key=\"adts.{EscapeXml(entry.Key)}\">{EscapeXml(entry.Value)}</SolidColorBrush>");
        }

        sb.AppendLine("        </ResourceDictionary>");
    }

    sb.AppendLine("      </ResourceDictionary.ThemeDictionaries>");
}

foreach (var resource in resources.EnumerateObject())
{
    var type = resource.Value.GetProperty("$type").GetString() ?? string.Empty;
    var value = ResolveTokenValue(resource.Value.GetProperty("$value"), resources, aliases, type);

    var key = $"adts.{resource.Name}";
    switch (type)
    {
        case "color":
        case "brush":
            sb.AppendLine($"      <SolidColorBrush x:Key=\"{EscapeXml(key)}\">{EscapeXml(value)}</SolidColorBrush>");
            break;
        case "double":
            sb.AppendLine($"      <x:Double x:Key=\"{EscapeXml(key)}\">{EscapeXml(value)}</x:Double>");
            break;
        case "string":
            sb.AppendLine($"      <x:String x:Key=\"{EscapeXml(key)}\">{EscapeXml(value)}</x:String>");
            break;
    }
}

sb.AppendLine("    </ResourceDictionary>");
sb.AppendLine("  </Styles.Resources>");

if (styles.ValueKind == JsonValueKind.Array)
{
    foreach (var style in styles.EnumerateArray())
    {
        var selector = style.GetProperty("selector").GetString() ?? "";
        sb.AppendLine($"  <Style Selector=\"{EscapeXml(selector)}\">");
        if (style.TryGetProperty("setters", out var setters) && setters.ValueKind == JsonValueKind.Object)
        {
            foreach (var setter in setters.EnumerateObject())
            {
                var resolved = ResolvePropertyValue(setter.Value, resources, aliases);
                sb.AppendLine(
                    $"    <Setter Property=\"{EscapeXml(setter.Name)}\" Value=\"{EscapeXml(resolved)}\" />");
            }
        }

        sb.AppendLine("  </Style>");
    }
}

sb.AppendLine("  <Style Selector=\"Border.card\">");
sb.AppendLine("    <Setter Property=\"Padding\" Value=\"20\" />");
sb.AppendLine("    <Setter Property=\"Background\" Value=\"{DynamicResource adts.color.surface.card}\" />");
sb.AppendLine("  </Style>");
sb.AppendLine("  <Style Selector=\"Button.primary\">");
sb.AppendLine("    <Setter Property=\"Background\" Value=\"{DynamicResource adts.color.brand.primary}\" />");
sb.AppendLine("    <Setter Property=\"Foreground\" Value=\"White\" />");
sb.AppendLine("    <Setter Property=\"Padding\" Value=\"14,8\" />");
sb.AppendLine("  </Style>");
sb.AppendLine("</Styles>");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, sb.ToString());
Console.WriteLine($"Generated: {outputPath}");
return 0;

static string ResolvePropertyValue(
    JsonElement value,
    JsonElement resources,
    JsonElement aliases)
{
    if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("ref", out var refNode))
    {
        var reference = refNode.GetString() ?? string.Empty;
        return "{DynamicResource adts." + reference + "}";
    }

    return value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        _ => value.GetRawText()
    };
}

static string ResolveTokenValue(
    JsonElement value,
    JsonElement resources,
    JsonElement aliases,
    string? typeHint)
{
    if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("ref", out var refNode))
    {
        var reference = refNode.GetString() ?? string.Empty;

        if (reference.StartsWith("aliases.", StringComparison.Ordinal) &&
            aliases.ValueKind == JsonValueKind.Object)
        {
            var aliasKey = reference["aliases.".Length..];
            if (aliases.TryGetProperty(aliasKey, out var aliasValue))
            {
                return aliasValue.ValueKind switch
                {
                    JsonValueKind.Number => aliasValue.GetRawText(),
                    JsonValueKind.String => aliasValue.GetString() ?? "",
                    _ => aliasValue.GetRawText()
                };
            }
        }

        if (resources.ValueKind == JsonValueKind.Object && resources.TryGetProperty(reference, out var resourceNode))
        {
            return ResolveTokenValue(resourceNode.GetProperty("$value"), resources, aliases, typeHint);
        }

        // fall back to token key if unresolved (keeps generation deterministic)
        return reference;
    }

    return value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Number => value.GetRawText(),
        _ => value.GetRawText()
    };
}

static string EscapeXml(string input)
{
    return input
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}
