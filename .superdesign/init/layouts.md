# Shared layouts

NoFences has no shared page shell: each WinForms dialog is a top-level native window. `SettingsWindow` currently uses a fixed 444×492 tool window with a three-tab `TabControl` (`General`, `Visual`, `About`), a bottom-right Close button, standard checkboxes, trackbars, and small color swatches. The full current implementation is in:

- `NoFences/SettingsWindow.Designer.cs` — complete native control tree and positioning.
- `NoFences/SettingsWindow.cs` — initialization, live preview, debounced saving, startup registry action.

The fence itself is a borderless desktop window rendered manually in `NoFences/FenceWindow.Draw.cs`, with a title header and clipped icon grid.
