using NoFences.Model;
using NoFences.Interaction;
using NoFences.Layout;
using NoFences.Util;
using NoFences.Win32;
using Peter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static NoFences.Win32.WindowUtil;
using Timer = System.Windows.Forms.Timer;

namespace NoFences
{
    public partial class FenceWindow : Form
    {
        public enum FenceState
        {
            Normal,
            Minified,
            Maximized
        }

        private int logicalTitleHeight;
        private int titleHeight;
        private readonly FenceInfo fenceInfo;

        private FenceState state = FenceState.Normal;
        private string hoveringItem;
        private string draggedItem;

        private int scrollHeight;
        private int scrollOffset;
        private int totalHeight;

        private bool isDragging = false;
        private Point initialMousePosition;
        private bool isDraggingWindow = false;
        private Point dragStartPoint;
        private bool isResizing = false;
        private Size resizeStartSize;
        private string resizeEdge = "";
        private readonly Timer resizeTimer;
        private Size startSize, targetSize;
        private float animationProgress = 0; // Tracks animation progress (0 to 1)
        private readonly float animationSpeed = 0.1f; // Adjust speed (lower = slower)
        private bool isAnimating = false;

        // Add drag tracking fields
        private bool isDraggingItem = false;
        private Point dragStartPosition;
        private string dragStartItem;

        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromMilliseconds(350));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromMilliseconds(350));
        private readonly ShellContextMenu shellContextMenu = new ShellContextMenu();
        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();
        private readonly FenceDragDropController dragDropController = new FenceDragDropController();
        private readonly object saveLock = new object();
        private bool deleteFenceOnClose;

        private FileSystemWatcher fenceWatcher;
        private FileSystemWatcher desktopWatcher;
        
        private string fenceFolderPath 
        { 
            get { return FenceManager.Instance.GetContentFolderPath(fenceInfo); }
        }

        public FenceWindow(FenceInfo fenceInfo)
        {
            InitializeComponent();
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
            SettingsValidator.NormalizeFence(this.fenceInfo);
            FencePlacement.EnsureVisible(this.fenceInfo);
            InitializeFenceCommands();
            DropShadow.ApplyShadows(this);
            BlurUtil.EnableBlur(Handle, 50);
            HideFromAltTab(Handle);
            DesktopUtil.GlueToDesktop(Handle);
            //DesktopUtil.PreventMinimize(Handle);

            if (fenceInfo.UseGlobalTitleHeight)
                fenceInfo.TitleHeight = Properties.Settings.Default.title_size;
            if (fenceInfo.UseGlobalAutoMinify)
                fenceInfo.CanMinify = Properties.Settings.Default.autoMinify;
            logicalTitleHeight = fenceInfo.TitleHeight;
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            if (fenceInfo.Folded) state = FenceState.Minified;

            this.MouseWheel += FenceWindow_MouseWheel;
            this.MouseDown += FenceWindow_MouseDown;
            this.MouseUp += FenceWindow_MouseUp;
            this.KeyDown += FenceWindow_KeyDown; // Add keyboard event handler
            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;

            AllowDrop = true;
            KeyPreview = true; // Enable key events for the form

            Text = fenceInfo.Name;
            Location = new Point(fenceInfo.PosX, fenceInfo.PosY);

            Width = fenceInfo.Width;
            Height = (fenceInfo.Folded ? titleHeight : fenceInfo.Height);
            LoadSettings();
            RefreshBrushes();

            // Initialize Timer
            resizeTimer = new Timer
            {
                Interval = 16 // ~60 FPS (1000ms / 60)
            };
            resizeTimer.Tick += ResizeWindowStep;

            lockedToolStripMenuItem.Checked = fenceInfo.Locked;
            minifyToolStripMenuItem.Checked = fenceInfo.CanMinify;
            Minify();

            // --- File Watchers ---
            InitializeFileWatchersOptimized();
        }

        protected override void WndProc(ref Message m)
        {
            // Remove border
            if (m.Msg == WM_NCCALCSIZE)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Mouse leave
            var myrect = new Rectangle(Location, Size);
            if (m.Msg == 0x02a2 && !myrect.IntersectsWith(new Rectangle(MousePosition, new Size(1, 1))))
            {
                Minify();
            }

            if (m.Msg == WM_NCLBUTTONDOWN || m.Msg == WM_NCLBUTTONDBLCLK)
            {
                int hitTestResult = m.WParam.ToInt32();
                if (hitTestResult == HTCAPTION) // If clicked on the title bar
                {
                    if (m.Msg == WM_NCLBUTTONDOWN)
                    {
                        OnTitleBarClick();
                    }
                    else if (m.Msg == WM_NCLBUTTONDBLCLK)
                    {
                        OnTitleBarDoubleClick();
                    }
                }
            }

            // Detect when mouse is released after clicking the title bar
            if (m.Msg == WM_NCLBUTTONUP)
            {
                int hitTestResult = m.WParam.ToInt32();

                if (hitTestResult == HTCAPTION) // If released on the title bar
                {
                    OnTitleBarMouseUp();
                }
            }

            if (snapping && m.Msg == WM_NCLBUTTONDOWN && m.WParam.ToInt32() == HTCAPTION)
            {
                // Prevent Windows' default movement handling
                return;
            }

            // Prevent maximize
            if ((m.Msg == WM_SYSCOMMAND) && m.WParam.ToInt32() == 0xF032)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Prevent foreground
            if (m.Msg == WM_SETFOCUS)
            {
                SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                return;
            }

            // Other messages
            base.WndProc(ref m);

            // If not locked and using the left mouse button
            if (MouseButtons == MouseButtons.Right || lockedToolStripMenuItem.Checked)
                return;

            // Then, allow dragging and resizing
            if (m.Msg == WM_NCHITTEST)
            {
                var pt = PointToClient(new Point(m.LParam.ToInt32()));

                if ((int)m.Result == HTCLIENT && pt.Y < titleHeight)     // drag the form
                {
                    if (!snapping)
                    {
                        m.Result = (IntPtr)HTCAPTION;
                        FenceWindow_MouseEnter(null, null);
                    }
                }

                if (!snapping && !IsMinified)
                {
                    if (pt.X < 10 && pt.Y < 10)
                        m.Result = new IntPtr(HTTOPLEFT);
                    else if (pt.X > (Width - 10) && pt.Y < 10)
                        m.Result = new IntPtr(HTTOPRIGHT);
                    else if (pt.X < 10 && pt.Y > (Height - 10))
                        m.Result = new IntPtr(HTBOTTOMLEFT);
                    else if (pt.X > (Width - 10) && pt.Y > (Height - 10))
                        m.Result = new IntPtr(HTBOTTOMRIGHT);
                    else if (pt.Y > (Height - 10))
                        m.Result = new IntPtr(HTBOTTOM);
                    else if (pt.X < 10)
                        m.Result = new IntPtr(HTLEFT);
                    else if (pt.X > (Width - 10))
                        m.Result = new IntPtr(HTRIGHT);
                }
            }
        }

        private void FenceWindow_Resize(object sender, EventArgs e)
        {
            if (isAnimating) return;

            throttledResize.Run(() =>
            {
                fenceInfo.Width = Width;
                if (state == FenceState.Normal && Height != titleHeight)
                {
                    fenceInfo.Height = Height;
                }
                Save();
            });

            Invalidate();
        }

        private void ResizeWindowStep(object sender, EventArgs e)
        {
            isAnimating = true; // Set flag to prevent resizing while animating
            animationProgress += animationSpeed; // Increase progress
            if (animationProgress > 1) animationProgress = 1;

            float easedProgress = MathUtils.EaseOutQuint(animationProgress); // Apply easing function

            // Interpolate width & height using easing
            int newWidth = (int)(startSize.Width + ((targetSize.Width - startSize.Width) * easedProgress));
            int newHeight = (int)(startSize.Height + ((targetSize.Height - startSize.Height) * easedProgress));

            this.Size = new Size(newWidth, newHeight);

            // Stop when fully resized
            if (animationProgress >= 1)
            {
                this.Size = targetSize; // Snap to final size
                resizeTimer.Stop();
                isAnimating = false;
                Invalidate();
            }
        }

        private void StartResizeTransition(Size destinationSize)
        {
            targetSize = destinationSize;
            if (Properties.Settings.Default.reduceAnimations)
            {
                resizeTimer.Stop();
                isAnimating = false;
                Size = targetSize;
                Invalidate();
                return;
            }

            startSize = Size;
            animationProgress = 0;
            resizeTimer.Start();
        }

        private void FenceWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingWindow)
            {
                // Calculate new position based on mouse movement
                int newX = Location.X + (e.X - dragStartPoint.X);
                int newY = Location.Y + (e.Y - dragStartPoint.Y);

                // Snap to grid while moving
                newX = (int)Math.Round((double)newX / snapSize) * snapSize;
                newY = (int)Math.Round((double)newY / snapSize) * snapSize;

                Location = new Point(newX, newY);
            }
            else if (isResizing)
            {
                // Calculator new size based on cursor position
                int newWidth = resizeStartSize.Width;
                int newHeight = resizeStartSize.Height;
                int newX = this.Left;
                int newY = this.Top;

                switch (resizeEdge)
                {
                    case "left":
                        // Calculate new width and X position based on cursor X position
                        newX = this.Left + e.X;
                        newX = (int)Math.Round((double)newX / snapSize) * snapSize;
                        newWidth = this.Right - newX;
                        break;
                    case "right":
                        // Calculate new width based on cursor X position
                        newWidth = e.X;
                        newWidth = (int)Math.Round((double)newWidth / snapSize) * snapSize;
                        break;
                    case "bottom":
                        // Calculate new height based on cursor Y position
                        newHeight = e.Y;
                        newHeight = (int)Math.Round((double)newHeight / snapSize) * snapSize;
                        break;
                }

                // Clamp the size to ensure it doesn't become smaller than the grid size
                newWidth = Math.Max(newWidth, snapSize);
                newHeight = Math.Max(newHeight, snapSize);

                // Create the new size
                var newSize = new Size(newWidth, newHeight);

                // Only update if the new size is different from the current size
                if (this.Size != newSize)
                {
                    // Update size and position
                    this.SuspendLayout();
                    this.Size = newSize;
                    fenceInfo.Width = newWidth;
                    fenceInfo.Height = newHeight;
                    fenceInfo.PosX = newX;
                    fenceInfo.PosY = newY;
                    throttledResize.Run(Save);
                    if (resizeEdge == "left")
                    {
                        this.Location = new Point(newX, this.Top);
                    }
                    this.ResumeLayout();

                    // Reset the start point to the current mouse position
                    resizeStartSize = this.Size;
                }
            }
            else if (isDragging)
            {
                int deltaY = e.Y - initialMousePosition.Y;
                scrollOffset -= deltaY;
                if (scrollOffset < 0)
                    scrollOffset = 0;
                if (scrollOffset > scrollHeight)
                    scrollOffset = scrollHeight;

                initialMousePosition = e.Location;
            }
            else if (isDraggingItem && dragStartItem != null)
            {
                int deltaX = Math.Abs(e.X - dragStartPosition.X);
                int deltaY = Math.Abs(e.Y - dragStartPosition.Y);

                if (deltaX > SystemInformation.DragSize.Width / 2
                    || deltaY > SystemInformation.DragSize.Height / 2)
                {
                    FenceLayoutSnapshot layout = GetLayoutSnapshot();
                    string[] draggedPaths = dragDropController.GetSelectedInDisplayOrder(
                        layout.OrderedPaths,
                        dragStartItem);
                    if (draggedPaths.Length == 0)
                    {
                        isDraggingItem = false;
                        dragStartItem = null;
                        return;
                    }

                    draggedItem = dragStartItem;
                    DataObject data = dragDropController.CreateDragData(layout.OrderedPaths, dragStartItem);
                    var result = DoDragDrop(data, DragDropEffects.Move | DragDropEffects.Copy);

                    if (result == DragDropEffects.Move && !dragDropController.InternalDropHandled)
                    {
                        string[] previousItems = fenceInfo.Files.ToArray();
                        var handledByFence = new HashSet<string>(
                            dragDropController.GetPathsHandledByFence(data),
                            StringComparer.OrdinalIgnoreCase);
                        int removed = 0;
                        for (int index = 0; index < draggedPaths.Length; index++)
                        {
                            string path = draggedPaths[index];
                            if (handledByFence.Contains(path) || !ItemExists(path))
                            {
                                removed += fenceInfo.Files.RemoveAll(candidate =>
                                    string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase));
                            }
                        }

                        if (removed > 0)
                        {
                            if (draggedPaths.All(ItemExists))
                                RecordUndo("move items out", previousItems);
                            Save();
                        }
                    }

                    isDraggingItem = false;
                    draggedItem = null;
                    dragStartItem = null;
                    dragDropController.RetainExisting(fenceInfo.Files);
                    InvalidateFenceContent();
                }
            }
            else if (!IsMinified && !lockedToolStripMenuItem.Checked)
            {
                // Update cursor based on mouse position
                this.Cursor = IsNearLeftEdge(e.Location) || IsNearRightEdge(e.Location)
                    ? Cursors.SizeWE
                    : IsNearBottomEdge(e.Location) ? Cursors.SizeNS : Cursors.Default;
            }

            Invalidate();
        }

        private void FenceWindow_MouseDown(object sender, MouseEventArgs e)
        {
            FenceLayoutSnapshot layout = GetLayoutSnapshot();
            string hitItem = CanInteractWithContent() ? layout.HitTest(e.Location) : null;

            if (e.Button == MouseButtons.Left)
            {
                bool isLocked = lockedToolStripMenuItem.Checked;

                if (e.Y >= titleHeight)
                {
                    dragDropController.Select(
                        hitItem,
                        layout.OrderedPaths,
                        ModifierKeys.HasFlag(Keys.Control),
                        ModifierKeys.HasFlag(Keys.Shift));
                    Invalidate();
                }

                if (snapping && e.Y < titleHeight && !isLocked) // Only drag from title bar
                {
                    isDraggingWindow = true;
                    dragStartPoint = e.Location;
                }
                else if (!isResizing && !isLocked && IsNearLeftEdge(e.Location)) // Check if the mouse is near the edges to start resizing
                {
                    isResizing = true;
                    resizeEdge = "left";
                    resizeStartSize = this.Size;
                    this.Cursor = Cursors.SizeWE;
                }
                else if (!isResizing && !isLocked && IsNearRightEdge(e.Location))
                {
                    isResizing = true;
                    resizeEdge = "right";
                    resizeStartSize = this.Size;
                    this.Cursor = Cursors.SizeWE;
                }
                else if (!isResizing && !isLocked && IsNearBottomEdge(e.Location))
                {
                    isResizing = true;
                    resizeEdge = "bottom";
                    resizeStartSize = this.Size;
                    this.Cursor = Cursors.SizeNS;
                }
                else if (!isDraggingWindow && scrollHeight > 0 && draggedItem == null && hitItem == null) // content scroll dragging with left mouse button
                {
                    isDragging = true;
                    initialMousePosition = e.Location;
                }
                else if (hitItem != null && !isLocked) // Start tracking potential item drag
                {
                    isDraggingItem = true;
                    dragStartPosition = e.Location;
                    dragStartItem = hitItem;
                }
            }
            else if (e.Button == MouseButtons.Right && hitItem != null)
            {
                if (!dragDropController.IsSelected(hitItem))
                    dragDropController.Select(hitItem, layout.OrderedPaths, false, false);
                hoveringItem = hitItem;
                Invalidate();
            }
        }

        private void FenceWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                isDraggingWindow = false;
                isResizing = false;
                
                // Reset item drag state if we release without dragging
                if (isDraggingItem)
                {
                    isDraggingItem = false;
                    dragStartItem = null;
                }
                
                this.Cursor = Cursors.Default;
            }
        }

        private void FenceWindow_MouseEnter(object sender, EventArgs e)
        {
            if ((minifyToolStripMenuItem.Checked || fenceInfo.CanMinify) && IsMinified)
            {
                state = FenceState.Normal;
                Height = fenceInfo.Height;
            }
            if (overallOpacity < 1) SetOverallOpacity(1);
        }

        private void FenceWindow_MouseLeave(object sender, EventArgs e)
        {
            Minify();
            if (overallOpacity < 1) SetOverallOpacity(overallOpacity);
            Invalidate();
        }

        private void FenceWindow_Click(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void FenceWindow_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Location.Y < titleHeight)
            {
                OnTitleBarDoubleClick();
                return;
            }

            string hitItem = GetLayoutSnapshot().HitTest(e.Location);
            if (hitItem != null)
            {
                FenceEntry.FromPath(hitItem)?.Open();
                return;
            }

            if (state == FenceState.Maximized)
            {
                state = FenceState.Normal;
                // Height = fenceInfo.Height;

                StartResizeTransition(new Size(Width, fenceInfo.Height));
            }
            else if (scrollHeight > 0)
            {
                state = FenceState.Maximized;
                // Height = Math.Min(Screen.PrimaryScreen.WorkingArea.Height, totalHeight + 10); // Expand but don't exceed screen
                StartResizeTransition(new Size(
                    Width,
                    Math.Min(Screen.PrimaryScreen.WorkingArea.Height, totalHeight + 10)));
            }
        }

        private void FenceWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisposeFileWatchersOptimized();

            if (deleteFenceOnClose)
            {
                // A delayed move/resize save must not recreate metadata after the
                // user explicitly removes this fence.
                throttledMove.Cancel();
                throttledResize.Cancel();
            }
            else
            {
                throttledMove.Flush();
                throttledResize.Flush();
            }

            thumbnailProvider.IconThumbnailLoaded -= ThumbnailProvider_IconThumbnailLoaded;
            thumbnailProvider.Dispose();
            DisposeDrawingResources();
            folderSyncDebouncer.Dispose();
            desktopImportDebouncer.Dispose();
            throttledMove.Dispose();
            throttledResize.Dispose();

            if (deleteFenceOnClose)
                FenceManager.Instance.RemoveFence(fenceInfo, this);
            else
                FenceManager.Instance.RemoveFence(this);

            if (Application.OpenForms.Count == 0)
                Application.Exit();
        }

        private void Save()
        {
            lock (saveLock)
            {
                FenceManager.Instance.UpdateFence(fenceInfo);
            }
        }

        private void Minify()
        {
            if ((minifyToolStripMenuItem.Checked || fenceInfo.CanMinify) && !IsMinified)
            {
                state = FenceState.Minified;
                Height = titleHeight;
                Invalidate();
            }
        }

        private void FenceWindow_LocationChanged(object sender, EventArgs e)
        {
            throttledMove.Run(() =>
            {
                fenceInfo.PosX = Location.X;
                fenceInfo.PosY = Location.Y;
                Save();
            });
        }

        private void FenceWindow_Load(object sender, EventArgs e)
        {
            // CenterToScreen();
        }

        private void FenceWindow_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            FenceLayoutSnapshot layout = GetLayoutSnapshot();
            string hitItem = layout.HitTest(e.Location);
            if (hitItem != null)
            {
                if (!dragDropController.IsSelected(hitItem))
                    dragDropController.Select(hitItem, layout.OrderedPaths, false, false);

                string[] selected = dragDropController.GetSelectedInDisplayOrder(layout.OrderedPaths, hitItem);
                if (selected.All(File.Exists))
                    shellContextMenu.ShowContextMenu(selected.Select(path => new FileInfo(path)).ToArray(), MousePosition);
                else if (selected.All(Directory.Exists))
                    shellContextMenu.ShowContextMenu(selected.Select(path => new DirectoryInfo(path)).ToArray(), MousePosition);
                else
                    appContextMenuDark.Show(this, e.Location);
            }
            else
            {
                appContextMenuDark.Show(this, e.Location);
            }
        }

        private void FenceWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            if (scrollHeight < 1)
                return;

            scrollOffset -= Math.Sign(e.Delta) * 10;
            if (scrollOffset < 0)
                scrollOffset = 0;
            if (scrollOffset > scrollHeight)
                scrollOffset = scrollHeight;

            Invalidate();
        }

        private void ThumbnailProvider_IconThumbnailLoaded(object sender, EventArgs e)
        {
            QueueUiAction(Invalidate);
        }

        private void OnTitleBarClick()
        {
        }

        private void OnTitleBarDoubleClick()
        {
            state = state == FenceState.Normal ? FenceState.Minified : FenceState.Normal;

            if (IsMinified)
            {
                fenceInfo.Folded = true;
                // Height = titleHeight;
                targetSize = new Size(Width, titleHeight);
            }
            else
            {
                fenceInfo.Folded = false;
                targetSize = new Size(Width, fenceInfo.Height);
                // Height = fenceInfo.Height;
            }

            StartResizeTransition(targetSize);

            // Save();
        }

        private void OnTitleBarMouseUp()
        {
        }

        public bool IsMinified => state == FenceState.Minified;

        private void FenceWindow_Deactivate(object sender, EventArgs e)
        {
            Update();
        }

        public bool CanInteractWithContent()
        {
            return !IsMinified && !isDragging && !isDraggingWindow && !isResizing;
        }

        // Helper methods for edge detection
        private bool IsNearLeftEdge(Point location)
        {
            return location.X < 10;
        }

        private bool IsNearRightEdge(Point location)
        {
            return location.X > (Width - 10);
        }

        private bool IsNearBottomEdge(Point location)
        {
            return location.Y > (Height - 10);
        }

        // Missing event handlers
        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateUndoCommands();

            // Update menu item states based on current fence configuration
            bool hasWatchedExtensions = fenceInfo.WatchedExtensions != null && fenceInfo.WatchedExtensions.Count > 0;
            bool hasCustomFolder = !string.IsNullOrWhiteSpace(fenceInfo.CustomFolderPath);
            clearCustomFolderPathToolStripMenuItem.Enabled = hasCustomFolder;
            clearCustomFolderPathToolStripMenuItemDark.Enabled = hasCustomFolder;
            
            // Enable/disable scan button based on whether we have watched extensions
            if (sender == appContextMenu)
            {
                scanForWatchedItemsToolStripMenuItem.Enabled = hasWatchedExtensions;
                scanForWatchedItemsToolStripMenuItem.ToolTipText = hasWatchedExtensions 
                    ? "Scan desktop for files matching watched extensions" 
                    : "Configure watched extensions first";
            }
            else if (sender == appContextMenuDark)
            {
                scanForWatchedItemsToolStripMenuItemDark.Enabled = hasWatchedExtensions;
                scanForWatchedItemsToolStripMenuItemDark.ToolTipText = hasWatchedExtensions 
                    ? "Scan desktop for files matching watched extensions" 
                    : "Configure watched extensions first";
            }
            
            // Sync locked state between menus
            lockedTick.Checked = lockedToolStripMenuItem.Checked;
        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.confirmFenceDeletion
                && MessageBox.Show(
                    $"Remove fence '{Text}'?\n\nThe linked folder and all of its files will be kept.",
                    "Delete Fence",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            deleteFenceOnClose = true;
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void closeAppMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void FenceWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                dragDropController.SelectAll(GetLayoutSnapshot().OrderedPaths);
                Invalidate();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.Z)
            {
                UndoLastFenceChange();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Delete
                && dragDropController.SelectedPaths.Count > 0
                && !lockedToolStripMenuItem.Checked)
            {
                RemoveSelectedItem();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void scanForWatchedItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScanForWatchedItems();
        }

        private void ScanForWatchedItems()
        {
            if (fenceInfo.WatchedExtensions == null || fenceInfo.WatchedExtensions.Count == 0)
            {
                MessageBox.Show("No file extensions are being watched. Please configure watched extensions first.", 
                    "No Extensions Watched", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var foundItems = new List<string>();
                var desktopPaths = new List<string> { DesktopPath };
                
                // Also scan public desktop if different
                if (!string.Equals(DesktopPath, PublicDesktopPath, StringComparison.OrdinalIgnoreCase))
                {
                    desktopPaths.Add(PublicDesktopPath);
                }
                
                foreach (var desktopPath in desktopPaths)
                {
                    if (!Directory.Exists(desktopPath))
                        continue;
                        
                    // Scan for watched files
                    foreach (var extension in fenceInfo.WatchedExtensions)
                    {
                        var files = Directory.GetFiles(desktopPath, "*" + extension);
                        foreach (var file in files)
                        {
                            if (!fenceInfo.Files.Contains(file))
                            {
                                foundItems.Add(file);
                            }
                        }
                    }

                    // Scan for folders (always included when watching is enabled)
                    var folders = Directory.GetDirectories(desktopPath);
                    foreach (var folder in folders)
                    {
                        // Skip hidden/system folders and the hidden desktop folder we create
                        DirectoryInfo dirInfo = new DirectoryInfo(folder);
                        if ((dirInfo.Attributes & FileAttributes.Hidden) != 0 || 
                            (dirInfo.Attributes & FileAttributes.System) != 0 ||
                            folder.Equals(HiddenDesktopPath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        if (!fenceInfo.Files.Contains(folder))
                        {
                            foundItems.Add(folder);
                        }
                    }
                }

                if (foundItems.Count == 0)
                {
                    MessageBox.Show("No new items found on desktop matching watched extensions or folders.", 
                        "Scan Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"Found {foundItems.Count} items on desktop:\n\n" +
                    string.Join("\n", foundItems.Take(10).Select(Path.GetFileName)) +
                    (foundItems.Count > 10 ? $"\n... and {foundItems.Count - 10} more" : "") +
                    "\n\nDo you want to move these items to this fence?",
                    "Items Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    MoveItemsToFence(foundItems);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning for items: {ex.Message}", 
                    "Scan Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppLogger.Error("Unable to scan the desktop for watched items.", ex);
            }
        }

        private void MoveItemsToFence(List<string> items)
        {
            int movedCount = 0;
            var errors = new List<string>();

            foreach (var item in items)
            {
                try
                {
                    if (TryMoveItemToFenceFolder(item, out string destinationPath))
                    {
                        if (!fenceInfo.Files.Contains(destinationPath, StringComparer.OrdinalIgnoreCase))
                            fenceInfo.Files.Add(destinationPath);
                        movedCount++;
                    }
                    else
                    {
                        errors.Add($"{Path.GetFileName(item)}: could not be moved");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(item)}: {ex.Message}");
                    AppLogger.Error($"Unable to move watched item '{item}'.", ex);
                }
            }

            Save();
            InvalidateFenceContent();

            // Show results
            string message = $"Successfully moved {movedCount} of {items.Count} items to the fence.";
            if (errors.Count > 0)
            {
                message += $"\n\nErrors encountered:\n{string.Join("\n", errors.Take(5))}";
                if (errors.Count > 5)
                {
                    message += $"\n... and {errors.Count - 5} more errors.";
                }
            }

            MessageBox.Show(message, "Scan Results", MessageBoxButtons.OK, 
                errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
    }
}
