# Window and navigation map

This is a native WinForms application, so there are windows/dialogs instead of URL routes.

| Window | Entry | Opened from |
|---|---|---|
| Fence desktop window | `NoFences/FenceWindow.cs` | Startup metadata load or New fence |
| Global settings | `NoFences/SettingsWindow.cs` | Fence context menu → Settings |
| Rename fence | `NoFences/EditDialog.cs` | Fence context menu → Rename |
| Watched extensions | `NoFences/WatchedExtensionsDialog.cs` | Fence context menu → Watched Extensions |
| Custom folder | Native `FolderBrowserDialog` | Fence context menu → Set Custom Folder |

The requested redesign targets Global settings and a new consolidated per-fence settings dialog.
