# ADTS

Avalonia Design Token Schema (ADTS) for Avalonia UI 11.x.

## Repository structure

- `spec/ADTS-0.1.md`: ADTS draft spec.
- `spec/adts.schema.json`: JSON schema for ADTS 0.1.
- `spec/examples/starter.tokens.json`: starter token set.
- `src/Adts.TokenCompiler`: JSON -> AXAML token compiler.
- `src/Adts.Playground`: Avalonia 11 playground consuming generated tokens/styles.

## JSON -> AXAML flow

1. Edit token input: `spec/examples/starter.tokens.json`
2. Generate AXAML dictionary:
   - `dotnet run --project "src/Adts.TokenCompiler/Adts.TokenCompiler.csproj" "spec/examples/starter.tokens.json" "src/Adts.Playground/Generated/Adts.Generated.axaml"`
3. Run playground:
   - `dotnet run --project "src/Adts.Playground/Adts.Playground.csproj"`

The generated dictionary is included by `src/Adts.Playground/App.axaml`, and showcase controls in `MainWindow.axaml` consume those resources/selectors.

## Hot reload workflow

For UI iteration, use:

- `dotnet watch run --project "src/Adts.Playground/Adts.Playground.csproj"`

This enables rapid AXAML/style updates while iterating on token mappings and visual output.
