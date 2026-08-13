# Theme

## Compact token summary

- Platform: native Windows Forms, .NET Framework 4.8, Per-Monitor V2 DPI.
- Font: Segoe UI; compact 9–10 pt body, 15–18 pt window heading.
- Window background: `#1F1F1F`.
- Surface/card background: `#2A2A2A`.
- Hover surface: `#363636`.
- Border: `#4A4A4A`, 1 px.
- Primary text: `#F2F2F2`; secondary text should use `#AFAFAF`.
- Accent: restrained Windows neutral/accent color only for selection and focus.
- Spacing: 8 px base; 12–16 px control gaps; 20–24 px section padding.
- Corners: native or slightly rounded; no pills, gradients, or heavy shadows.
- Motion: restrained 150–220 ms fades only; honor a global animation toggle.

## Raw source

The authoritative current theme implementation is reproduced in `.superdesign/init/components.md` from `NoFences/Control/UiTheme.cs`. Fence colors are user-configurable through `headerColor`, `headerAlpha`, `windowColor`, `opacity`, and `overallOpacity` application settings.
