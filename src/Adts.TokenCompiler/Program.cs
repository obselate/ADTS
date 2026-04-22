using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

if (!root.TryGetProperty("tokens", out var tokensNode))
{
    Console.Error.WriteLine("Token file must contain tokens.");
    return 3;
}

if (!root.TryGetProperty("avalonia", out var avaloniaNode))
{
    Console.Error.WriteLine("Token file must contain avalonia mapping.");
    return 4;
}

var resourcePrefix = avaloniaNode.TryGetProperty("resourcePrefix", out var prefixNode)
    ? prefixNode.GetString() ?? "adts"
    : "adts";

var rawTokenValues = FlattenTokenValues(tokensNode);
var resolvedTokenValues = ResolveAllTokenValues(rawTokenValues);
var terminalPathByTokenPath = BuildTerminalPathMap(rawTokenValues);

var resourcesNode = avaloniaNode.TryGetProperty("resources", out var resourcesTmp) ? resourcesTmp : default;
var variantsNode = avaloniaNode.TryGetProperty("variants", out var variantsTmp) ? variantsTmp : default;
var stylesNode = avaloniaNode.TryGetProperty("styles", out var stylesTmp) ? stylesTmp : default;
var controlThemesNode = avaloniaNode.TryGetProperty("controlThemes", out var controlThemesTmp) ? controlThemesTmp : default;

var resourceEntries = BuildResourceEntries(resourcesNode, resourcePrefix, resolvedTokenValues, terminalPathByTokenPath);
var resourceKeyByTokenPath = BuildTokenToResourceKeyMap(resourceEntries);
var resourceTypeByName = resourceEntries.ToDictionary(e => e.Name, e => e.Kind, StringComparer.Ordinal);

var sb = new StringBuilder();
sb.AppendLine("<Styles xmlns=\"https://github.com/avaloniaui\"");
sb.AppendLine("        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
sb.AppendLine("  <Styles.Resources>");
sb.AppendLine("    <ResourceDictionary>");

if (variantsNode.ValueKind == JsonValueKind.Object)
{
    sb.AppendLine("      <ResourceDictionary.ThemeDictionaries>");
    foreach (var variant in variantsNode.EnumerateObject())
    {
        sb.AppendLine($"        <ResourceDictionary x:Key=\"{EscapeXml(variant.Name)}\">");
        if (variant.Value.TryGetProperty("resources", out var variantResources) && variantResources.ValueKind == JsonValueKind.Object)
        {
            foreach (var overrideEntry in variantResources.EnumerateObject())
            {
                var resolved = ResolveReferenceOrLiteralString(
                    overrideEntry.Value.GetString() ?? string.Empty,
                    resolvedTokenValues,
                    terminalPathByTokenPath);

                var fullResourceKey = $"{resourcePrefix}.{overrideEntry.Name}";
                var kind = resourceTypeByName.TryGetValue(overrideEntry.Name, out var existing)
                    ? existing
                    : InferResourceKind(overrideEntry.Name, resolved);
                AppendResourceNode(sb, 10, fullResourceKey, resolved, kind);
            }
        }

        sb.AppendLine("        </ResourceDictionary>");
    }

    sb.AppendLine("      </ResourceDictionary.ThemeDictionaries>");
}

foreach (var entry in resourceEntries)
{
    AppendResourceNode(sb, 6, entry.FullKey, entry.ResolvedValue, entry.Kind);
}

sb.AppendLine("    </ResourceDictionary>");
sb.AppendLine("  </Styles.Resources>");

if (stylesNode.ValueKind == JsonValueKind.Array)
{
    foreach (var style in stylesNode.EnumerateArray())
    {
        var selector = style.TryGetProperty("selector", out var selectorNode)
            ? selectorNode.GetString() ?? string.Empty
            : string.Empty;

        sb.AppendLine($"  <Style Selector=\"{EscapeXml(selector)}\">");
        if (style.TryGetProperty("setters", out var setters) && setters.ValueKind == JsonValueKind.Object)
        {
            foreach (var setter in setters.EnumerateObject())
            {
                var resolved = ResolveSetterValue(
                    setter.Value,
                    resolvedTokenValues,
                    terminalPathByTokenPath,
                    resourceKeyByTokenPath);
                sb.AppendLine($"    <Setter Property=\"{EscapeXml(setter.Name)}\" Value=\"{EscapeXml(resolved)}\" />");
            }
        }

        sb.AppendLine("  </Style>");
    }
}

if (controlThemesNode.ValueKind == JsonValueKind.Object)
{
    var controlThemeBuffer = new StringBuilder();
    controlThemeBuffer.AppendLine("      <!-- Generated control themes -->");
    foreach (var controlTheme in controlThemesNode.EnumerateObject())
    {
        var themeDef = controlTheme.Value;
        var targetType = themeDef.TryGetProperty("targetType", out var targetTypeNode)
            ? targetTypeNode.GetString() ?? "Control"
            : "Control";
        var basedOnFluent =
            (themeDef.TryGetProperty("basedOnFluent", out var basedOnFluentNode) && basedOnFluentNode.ValueKind == JsonValueKind.True)
            || (themeDef.TryGetProperty("baseFluent", out var legacyFluentNode) && legacyFluentNode.ValueKind == JsonValueKind.True);

        controlThemeBuffer.AppendLine(
            $"      <!-- {EscapeXml(controlTheme.Name)}: basedOnFluent={(basedOnFluent ? "true" : "false")} -->");
        controlThemeBuffer.AppendLine(
            $"      <ControlTheme x:Key=\"{EscapeXml(controlTheme.Name)}\" TargetType=\"{EscapeXml(targetType)}\">");

        if (themeDef.TryGetProperty("setters", out var setters) && setters.ValueKind == JsonValueKind.Object)
        {
            foreach (var setter in setters.EnumerateObject())
            {
                var resolved = ResolveSetterValue(
                    setter.Value,
                    resolvedTokenValues,
                    terminalPathByTokenPath,
                    resourceKeyByTokenPath);
                controlThemeBuffer.AppendLine($"        <Setter Property=\"{EscapeXml(setter.Name)}\" Value=\"{EscapeXml(resolved)}\" />");
            }
        }

        if (themeDef.TryGetProperty("states", out var states) && states.ValueKind == JsonValueKind.Object)
        {
            foreach (var state in states.EnumerateObject())
            {
                controlThemeBuffer.AppendLine($"        <Style Selector=\"{EscapeXml(state.Name)}\">");
                if (state.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var setter in state.Value.EnumerateObject())
                    {
                        var resolved = ResolveSetterValue(
                            setter.Value,
                            resolvedTokenValues,
                            terminalPathByTokenPath,
                            resourceKeyByTokenPath);
                        controlThemeBuffer.AppendLine($"          <Setter Property=\"{EscapeXml(setter.Name)}\" Value=\"{EscapeXml(resolved)}\" />");
                    }
                }

                controlThemeBuffer.AppendLine("        </Style>");
            }
        }

        controlThemeBuffer.AppendLine("      </ControlTheme>");
    }

    // Inject control themes into Styles.Resources dictionary before closing it.
    var marker = "    </ResourceDictionary>\n";
    var insertion = controlThemeBuffer.ToString();
    var current = sb.ToString();
    var markerIndex = current.LastIndexOf(marker, StringComparison.Ordinal);
    if (markerIndex >= 0)
    {
        current = current.Insert(markerIndex, insertion);
        sb.Clear();
        sb.Append(current);
    }
}

sb.AppendLine("</Styles>");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, sb.ToString());
Console.WriteLine($"Generated: {outputPath}");
return 0;

static Dictionary<string, string> FlattenTokenValues(JsonElement tokensNode)
{
    var map = new Dictionary<string, string>(StringComparer.Ordinal);

    static void Walk(JsonElement node, string currentPath, Dictionary<string, string> mapRef)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in node.EnumerateObject())
            {
                var nextPath = string.IsNullOrEmpty(currentPath)
                    ? property.Name
                    : $"{currentPath}.{property.Name}";
                Walk(property.Value, nextPath, mapRef);
            }

            return;
        }

        if (node.ValueKind == JsonValueKind.Array)
        {
            return;
        }

        mapRef[currentPath] = node.ValueKind switch
        {
            JsonValueKind.String => node.GetString() ?? string.Empty,
            JsonValueKind.Number => node.GetRawText(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            _ => node.GetRawText()
        };
    }

    Walk(tokensNode, string.Empty, map);
    return map;
}

static Dictionary<string, string> ResolveAllTokenValues(Dictionary<string, string> rawTokenValues)
{
    var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var path in rawTokenValues.Keys)
    {
        ResolveTokenValue(path, rawTokenValues, resolved, new HashSet<string>(StringComparer.Ordinal));
    }

    return resolved;
}

static string ResolveTokenValue(
    string path,
    Dictionary<string, string> rawTokenValues,
    Dictionary<string, string> resolvedTokenValues,
    HashSet<string> visiting)
{
    if (resolvedTokenValues.TryGetValue(path, out var cached))
    {
        return cached;
    }

    if (!rawTokenValues.TryGetValue(path, out var raw))
    {
        return path;
    }

    if (!visiting.Add(path))
    {
        return raw;
    }

    if (TryParseReference(raw, out var referencePath))
    {
        var resolvedReference = ResolveTokenValue(referencePath, rawTokenValues, resolvedTokenValues, visiting);
        resolvedTokenValues[path] = resolvedReference;
        visiting.Remove(path);
        return resolvedReference;
    }

    resolvedTokenValues[path] = raw;
    visiting.Remove(path);
    return raw;
}

static Dictionary<string, string> BuildTerminalPathMap(Dictionary<string, string> rawTokenValues)
{
    var terminal = new Dictionary<string, string>(StringComparer.Ordinal);

    string ResolveTerminalPath(string path, HashSet<string> visiting)
    {
        if (terminal.TryGetValue(path, out var cached))
        {
            return cached;
        }

        if (!rawTokenValues.TryGetValue(path, out var raw) || !TryParseReference(raw, out var referencePath))
        {
            terminal[path] = path;
            return path;
        }

        if (!visiting.Add(path))
        {
            terminal[path] = path;
            return path;
        }

        var resolved = ResolveTerminalPath(referencePath, visiting);
        visiting.Remove(path);
        terminal[path] = resolved;
        return resolved;
    }

    foreach (var path in rawTokenValues.Keys)
    {
        ResolveTerminalPath(path, new HashSet<string>(StringComparer.Ordinal));
    }

    return terminal;
}

static List<ResourceEntry> BuildResourceEntries(
    JsonElement resourcesNode,
    string resourcePrefix,
    Dictionary<string, string> resolvedTokenValues,
    Dictionary<string, string> terminalPathByTokenPath)
{
    var entries = new List<ResourceEntry>();
    if (resourcesNode.ValueKind != JsonValueKind.Object)
    {
        return entries;
    }

    foreach (var resource in resourcesNode.EnumerateObject())
    {
        if (resource.Value.ValueKind != JsonValueKind.String)
        {
            continue;
        }

        var sourceValue = resource.Value.GetString() ?? string.Empty;
        string? tokenPath = null;
        string? terminalPath = null;
        string resolvedValue;

        if (TryParseReference(sourceValue, out var referencePath))
        {
            tokenPath = referencePath;
            if (terminalPathByTokenPath.TryGetValue(referencePath, out var terminal))
            {
                terminalPath = terminal;
            }

            if (resolvedTokenValues.TryGetValue(referencePath, out var tokenResolved))
            {
                resolvedValue = tokenResolved;
            }
            else
            {
                resolvedValue = referencePath;
            }
        }
        else
        {
            resolvedValue = sourceValue;
        }

        entries.Add(new ResourceEntry(
            Name: resource.Name,
            FullKey: $"{resourcePrefix}.{resource.Name}",
            SourceRaw: sourceValue,
            TokenPath: tokenPath,
            TerminalTokenPath: terminalPath,
            ResolvedValue: resolvedValue,
            Kind: InferResourceKind(resource.Name, resolvedValue)));
    }

    return entries;
}

static Dictionary<string, string> BuildTokenToResourceKeyMap(IEnumerable<ResourceEntry> resourceEntries)
{
    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var entry in resourceEntries)
    {
        if (!string.IsNullOrWhiteSpace(entry.TokenPath))
        {
            map[entry.TokenPath!] = entry.FullKey;
        }

        if (!string.IsNullOrWhiteSpace(entry.TerminalTokenPath))
        {
            map[entry.TerminalTokenPath!] = entry.FullKey;
        }
    }

    return map;
}

static string ResolveSetterValue(
    JsonElement valueNode,
    Dictionary<string, string> resolvedTokenValues,
    Dictionary<string, string> terminalPathByTokenPath,
    Dictionary<string, string> resourceKeyByTokenPath)
{
    var raw = valueNode.ValueKind switch
    {
        JsonValueKind.String => valueNode.GetString() ?? string.Empty,
        JsonValueKind.Number => valueNode.GetRawText(),
        JsonValueKind.True => "True",
        JsonValueKind.False => "False",
        _ => valueNode.GetRawText()
    };

    if (!TryParseReference(raw, out var referencePath))
    {
        return raw;
    }

    var terminalPath = terminalPathByTokenPath.TryGetValue(referencePath, out var terminal)
        ? terminal
        : referencePath;

    if (resourceKeyByTokenPath.TryGetValue(terminalPath, out var resourceKey)
        && ResourceKeyCanBeDynamicReference(resourceKey))
    {
        return "{DynamicResource " + resourceKey + "}";
    }

    if (resolvedTokenValues.TryGetValue(referencePath, out var resolved))
    {
        return resolved;
    }

    if (resolvedTokenValues.TryGetValue(terminalPath, out var terminalResolved))
    {
        return terminalResolved;
    }

    return referencePath;
}

static bool ResourceKeyCanBeDynamicReference(string resourceKey)
{
    // Avalonia cannot bind numeric resources like x:Double directly to typed setters such as
    // CornerRadius or Thickness via DynamicResource. Keep those as resolved literals.
    if (resourceKey.StartsWith("adts.spacing.", StringComparison.Ordinal)
        || resourceKey.StartsWith("adts.radius.", StringComparison.Ordinal)
        || resourceKey.StartsWith("adts.typography.weight.", StringComparison.Ordinal))
    {
        return false;
    }

    return true;
}

static string ResolveReferenceOrLiteralString(
    string raw,
    Dictionary<string, string> resolvedTokenValues,
    Dictionary<string, string> terminalPathByTokenPath)
{
    if (!TryParseReference(raw, out var referencePath))
    {
        return raw;
    }

    if (resolvedTokenValues.TryGetValue(referencePath, out var resolved))
    {
        return resolved;
    }

    if (terminalPathByTokenPath.TryGetValue(referencePath, out var terminalPath) &&
        resolvedTokenValues.TryGetValue(terminalPath, out var terminalResolved))
    {
        return terminalResolved;
    }

    return referencePath;
}

static void AppendResourceNode(StringBuilder sb, int indent, string fullKey, string value, ResourceKind kind)
{
    var prefix = new string(' ', indent);
    switch (kind)
    {
        case ResourceKind.Brush:
            sb.AppendLine($"{prefix}<SolidColorBrush x:Key=\"{EscapeXml(fullKey)}\">{EscapeXml(value)}</SolidColorBrush>");
            break;
        case ResourceKind.Double:
            sb.AppendLine($"{prefix}<x:Double x:Key=\"{EscapeXml(fullKey)}\">{EscapeXml(value)}</x:Double>");
            break;
        case ResourceKind.String:
            sb.AppendLine($"{prefix}<x:String x:Key=\"{EscapeXml(fullKey)}\">{EscapeXml(value)}</x:String>");
            break;
    }
}

static ResourceKind InferResourceKind(string name, string resolvedValue)
{
    if (name.StartsWith("color.", StringComparison.Ordinal))
    {
        return ResourceKind.Brush;
    }

    if (name.StartsWith("spacing.", StringComparison.Ordinal)
        || name.StartsWith("radius.", StringComparison.Ordinal)
        || name.StartsWith("typography.weight.", StringComparison.Ordinal))
    {
        return ResourceKind.Double;
    }

    if (Regex.IsMatch(resolvedValue, "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$"))
    {
        return ResourceKind.Brush;
    }

    if (double.TryParse(resolvedValue, out _))
    {
        return ResourceKind.Double;
    }

    return ResourceKind.String;
}

static bool TryParseReference(string value, out string path)
{
    var match = Regex.Match(value, "^\\{([A-Za-z0-9._-]+)\\}$");
    if (match.Success)
    {
        path = match.Groups[1].Value;
        return true;
    }

    path = string.Empty;
    return false;
}

static string EscapeXml(string input)
{
    return input
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}

enum ResourceKind
{
    Brush,
    Double,
    String
}

readonly record struct ResourceEntry(
    string Name,
    string FullKey,
    string SourceRaw,
    string? TokenPath,
    string? TerminalTokenPath,
    string ResolvedValue,
    ResourceKind Kind);
