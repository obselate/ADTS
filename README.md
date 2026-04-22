# ADTS

Avalonia Design Token Schema (ADTS) for Avalonia UI 11.x.

## Repository structure

- `spec/ADTS-0.1.md`: ADTS v0.2 draft spec narrative.
- `spec/adts.schema.json`: JSON schema for ADTS v0.2.
- `spec/examples/theme_mono_ink.tokens.json`: Theme 1 (neutral high contrast).
- `spec/examples/theme_amber_terminal.tokens.json`: Theme 2 (amber terminal).
- `spec/examples/theme_oceanic_glow.tokens.json`: Theme 3 (oceanic glow).
- `src/Adts.TokenCompiler`: JSON -> AXAML token compiler.
- `src/Adts.Playground`: Avalonia 11 playground consuming generated tokens/styles.

## ADTS v0.2 model

ADTS v0.2 uses a 3-tier precision model:

1. **Foundation domains** (`tokens.*`)
   - `colors`, `typography`, `spacing`, `rounded`, `components`.
2. **Component contracts** (`tokens.components`)
   - semantic component maps such as `button.primary`, `card.default`, `badge.status`.
3. **Avalonia projection** (`avalonia.*`)
   - `resources`, `variants`, `styles`, `styleStates`, `controlThemes`.

`controlThemes` support:
- explicit state selectors (`^:pointerover`, `^:pressed`, etc.)
- Fluent override intent via `basedOnFluent: true`
- custom control examples (`StatusBadgeTheme`, `PillTagTheme`)

## JSON -> AXAML flow

1. Choose token input:
   - `spec/examples/theme_mono_ink.tokens.json`
   - `spec/examples/theme_amber_terminal.tokens.json`
   - `spec/examples/theme_oceanic_glow.tokens.json`
2. Generate AXAML dictionary:
   - `dotnet run --project "src/Adts.TokenCompiler/Adts.TokenCompiler.csproj" "<theme-file>" "src/Adts.Playground/Generated/Adts.Generated.axaml"`
3. Run playground:
   - `dotnet run --project "src/Adts.Playground/Adts.Playground.csproj"`

The playground can switch among all three generated theme files at runtime from the preset dropdown. Each theme defines explicit state selectors and custom control themes.

## Hot reload workflow

For UI iteration, use:

- `dotnet watch run --project "src/Adts.Playground/Adts.Playground.csproj"`

This enables rapid AXAML/style updates while iterating on token mappings and visual output.
