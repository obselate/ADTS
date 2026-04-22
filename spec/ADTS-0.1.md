# ADTS 0.1 (Draft)

Avalonia Design Token Schema (ADTS) defines a machine-friendly token contract for generating consistent Avalonia 11 XAML styles and themes.

## Scope

- Target framework: Avalonia UI `11.x` only.
- Output target: XAML resources, styles, and control themes.
- Primary goal: unambiguous, deterministic translation from token files to `App.axaml` and included style dictionaries.

## Core Design Principles

- Token keys are stable and semantic.
- `$type` drives value interpretation.
- Theme variants are explicit (`Light`, `Dark`) when present, matching Avalonia `ThemeDictionaries`.
- Styles map to Avalonia selector syntax.
- Control themes are first-class resources.

## Token Model

Top-level object:

- `$schema`: JSON schema URI.
- `adtsVersion`: ADTS spec version (for this draft, `0.1.0`).
- `metadata`: optional author/project information.
- `aliases`: reusable global aliases (e.g. spacing values).
- `themes`: one or more named theme payloads.

Each theme contains:

- `resources`: typed resource tokens (`color`, `brush`, `double`, `thickness`, `string`).
- `styles`: selector-based setters for Avalonia styles.
- `controlThemes`: named control themes with setter and optional state blocks.
- `variants` (optional): variant-specific `resources` for `Light` and/or `Dark`.

## Avalonia 11 Mapping Rules

### 1) Resources

- `color` token -> `<Color x:Key="...">#RRGGBB</Color>`
- `brush` token -> `<SolidColorBrush x:Key="...">` or reference another token.
- `double` token -> scalar values for font sizes, radii, etc.
- `thickness` token -> `Thickness` XAML string form (`left,top,right,bottom`).

References use `{ref: "token.path"}` and should be emitted as `DynamicResource` for theme-dependent values.

### 2) Theme Variants (Optional)

`themes.<name>.variants.Light|Dark` maps to:

- `<ResourceDictionary.ThemeDictionaries>`
  - `<ResourceDictionary x:Key="Light">...</ResourceDictionary>`
  - `<ResourceDictionary x:Key="Dark">...</ResourceDictionary>`

This follows Avalonia 11 theme-variant behavior.

### 3) Styles

`styles` entries use Avalonia selector syntax (`Button.primary:pointerover`, etc.) and setter dictionaries.

Each setter maps to:

- `<Setter Property="..." Value="..."/>`
- If value is token reference, emit `{DynamicResource KeyName}`.

### 4) Control Themes

`controlThemes` entries map to `<ControlTheme>` resources:

- Key in `controlThemes` object -> `x:Key`.
- `targetType` -> `TargetType`.
- `setters` -> `<Setter .../>`.
- Optional `states` object where keys are nested selectors (for example `^:pointerover`) and values are setter maps.

## Authoring Constraints for Agents

- Do not emit Avalonia 12-only APIs.
- Prefer `DynamicResource` for theme-sensitive colors/brushes.
- Keep token names semantic and domain-driven (`color.surface.primary`, not `blue500` only).
- Avoid selector ambiguity: include control type where reasonable.

## Example Workflow

1. Author `starter.tokens.json`.
2. Validate against `adts.schema.json`.
3. Transform into:
   - `App.axaml` resources + merged dictionaries.
   - Included styles file for selectors.
4. Build and run Avalonia app to visually validate results.
