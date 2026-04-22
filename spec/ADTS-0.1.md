# ADTS 0.3 (Draft)

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
- `adtsVersion`: ADTS spec version (for this draft, `0.3.0`).
- `metadata`: optional author/project information.
- `tokens`: platform-neutral design token domains (colors, typography, spacing, rounded, components).
- `avalonia`: platform projection for resource/style/theme mapping with per-theme control theme overrides.

`tokens` contains:

- `colors`: semantic color tokens.
- `typography`: named typography objects (`fontFamily`, `fontSize`, `fontWeight`, `lineHeight`, `letterSpacing`).
- `spacing`: spacing scale values.
- `rounded`: corner radius scale values.
- `components`: semantic component contracts, including custom controls and explicit states.

`avalonia` contains:

- `resources`: token -> Avalonia resource key projection.
- `variants` (optional): `Light`/`Dark` resource key overrides and per-theme `controlThemes`.
- `styles`: selector-based setters for Avalonia styles.
- `controlThemes`: named control themes with optional fluent behavior hints.

## Avalonia 11 Mapping Rules

### 1) Resources

- `avalonia.resources` maps token paths to concrete resource keys (`adts.*`).
- color-like resources should emit brushes in `<Styles.Resources>`.
- numeric resources should emit `<x:Double ...>`.
- references use `{ref: "tokens...."}`.

### 2) Theme Variants (Optional)

`avalonia.variants.Light|Dark.resources` maps to:

- `<ResourceDictionary.ThemeDictionaries>`
  - `<ResourceDictionary x:Key="Light">...</ResourceDictionary>`
  - `<ResourceDictionary x:Key="Dark">...</ResourceDictionary>`

This follows Avalonia 11 theme-variant behavior.

`avalonia.variants.<Variant>.controlThemes` can override control theme setters/states for that specific theme variant.

### 3) Styles

`avalonia.styles` entries use Avalonia selector syntax (`Button.primary:pointerover`, etc.) and setter dictionaries.

Each setter maps to:

- `<Setter Property="..." Value="..."/>`
- If value is token reference, emit `{DynamicResource KeyName}`.

### 4) Control Themes

`avalonia.controlThemes` entries map to `<ControlTheme>` resources:

- Key in `controlThemes` object -> `x:Key`.
- `targetType` -> `TargetType`.
- `setters` -> `<Setter .../>`.
- Optional `states` object where keys are explicit nested selectors (for example `^:pointerover`, `^:pressed`, `^:disabled`) and values are setter maps.
- Optional `basedOnFluent` indicates the generated theme should keep Fluent defaults and overlay targeted setters/states.
- Optional `xKey` allows custom control themes (for example `xKey: "Adts.TagPill"`), while `fluentOverride` allows overriding Fluent default control theme when desired.

## Three-tier Precision Model

ADTS 0.3 uses three precision tiers:

1. **Foundation tokens (strict):** `tokens.colors`, `tokens.typography`, `tokens.spacing`, `tokens.rounded`.
2. **Component contracts (strict-ish):** `tokens.components.<component>.<variant|state>` for semantic UI behavior and custom controls.
3. **Avalonia projection (explicit):** `avalonia.resources`, `avalonia.styles`, `avalonia.controlThemes`, plus per-variant overrides.

## Authoring Constraints for Agents

- Do not emit Avalonia 12-only APIs.
- Prefer `DynamicResource` for theme-sensitive colors/brushes.
- Keep token names semantic and domain-driven (`color.surface.primary`, not `blue500` only).
- Avoid selector ambiguity: include control type where reasonable.

## Example Workflow

1. Author `starter.tokens.json` with domain tokens and Avalonia mapping.
2. Validate against `adts.schema.json`.
3. Transform into:
   - `App.axaml` resources + merged dictionaries.
   - Included styles file for selectors.
4. Build and run Avalonia app to visually validate results.
