# NoFences design system

## Product context

NoFences is a lightweight Windows desktop organizer. Its UI should feel native, quiet, fast, and trustworthy. The primary surfaces are translucent desktop fences, global application settings, and per-fence configuration. Settings must favor clarity and safe defaults over density.

## Settings architecture

- Global settings window: left navigation with General, Behavior, Appearance, and About; one content pane; persistent footer actions.
- Per-fence settings window: Overview, Folder & Sync, Behavior, and Appearance; clearly identify the fence being edited.
- Group related controls into bordered, low-contrast sections with concise titles and one-line descriptions.
- Show current values beside sliders. Disable dependent controls instead of hiding them.
- Dangerous actions must be separated from ordinary settings and require confirmation.

## Visual tokens

- Segoe UI throughout; 16–18 pt semibold title, 10 pt section title, 9 pt body.
- Background `#1F1F1F`; navigation `#242424`; surfaces `#2A2A2A`; hover `#363636`.
- Text `#F2F2F2`; secondary `#AFAFAF`; disabled `#777777`; border `#4A4A4A`.
- Use the Windows accent sparingly for the active navigation item, focus, and primary action.
- 8 px spacing grid; 16 px control spacing; 20–24 px section padding.
- Thin 1 px borders, native/slightly rounded corners, no gradients, no tinted dark backgrounds, and no heavy shadows.

## Interaction and accessibility

- Keyboard navigation and visible focus must work across every control.
- Controls have descriptive accessible names; descriptions explain side effects such as moving files.
- Changes can preview live, but durable writes are debounced and flushed on close.
- Respect a Reduce animations toggle and never use hover effects that obscure content.
- Use explicit Apply/Cancel semantics for per-fence changes that can move files; global low-risk appearance changes may preview live.
