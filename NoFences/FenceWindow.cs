using NoFences.Model;
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
        private const string InternalItemDragFormat = "NoFences.InternalItemDrag";

        public enum FenceState
        {
            Normal,
            Minified,
            Maximized
        }

        private int logicalTitleHeight;
        private int titleHeight;
        private const int titleOffset = 1;
        private const int itemWidth = 75;
        private const int itemHeight = 32 + itemPadding + textHeight;
        private const int textHeight = 35;
        private const int itemPadding = 15;
        private const float shadowDist = 1f;

        private readonly FenceInfo fenceInfo;

        private Font titleFont;
        private Font iconFont;

        private FenceState state = FenceState.Normal;
        private string selectedItem;
        private string hoveringItem;
        private string draggedItem;
        private bool shouldUpdateSelection;
        private bool shouldRunDoubleClick;
        private bool hasSelectionUpdated;
        private bool hasHoverUpdated;

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
        private int dragStartItemIndex = -1;

        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromMilliseconds(350));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromMilliseconds(350));
        private readonly ShellContextMenu shellContextMenu = new ShellContextMenu();
        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();
        private readonly object saveLock = new object();
        private bool deleteFenceOnClose;
        private int draggedItemIndex = -1;
        private readonly string internalDragSourceId = Guid.NewGuid().ToString("N");
        private bool internalDropHandled;
        private readonly int itemWidtHalf = itemWidth / 2;

        private FileSystemWatcher fenceWatcher;
        private FileSystemWatcher desktopWatcher;
        
        private string fenceFolderPath 
        { 
            get { return FenceManager.Instance.GetContentFolderPath(fenceInfo); }
        }

        private void ReloadFonts()
        {
            titleFont?.Dispose();
            iconFont?.Dispose();
            using (var family = new FontFamily("Segoe UI"))
            {
                titleFont = new Font(family, (int)Math.Floor(logicalTitleHeight / 2.0));
                iconFont = new Font(family, 9);
            }
        }

        public FenceWindow(FenceInfo fenceInfo)
        {
            InitializeComponent();
            this.fenceInfo = fenceInfo ?? throw new ArgumentNullException(nameof(fenceInfo));
            InitializeFenceCommands();
            DropShadow.ApplyShadows(this);
            BlurUtil.EnableBlur(Handle, 50);
            HideFromAltTab(Handle);
            DesktopUtil.GlueToDesktop(Handle);
            //DesktopUtil.PreventMinimize(Handle);

            fenceInfo.TitleHeight = Properties.Settings.Default.title_size;
            logicalTitleHeight = fenceInfo.TitleHeight;
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            if (fenceInfo.Folded) state = FenceState.Minified;

            this.MouseWheel += FenceWindow_MouseWheel;
            this.MouseDown += FenceWindow_MouseDown;
            this.MouseUp += FenceWindow_MouseUp;
            this.KeyDown += FenceWindow_KeyDown; // Add keyboard event handler
            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;

            ReloadFonts();

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

        private void FenceWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (lockedToolStripMenuItem.Checked) return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(InternalItemDragFormat))
                e.Effect = DragDropEffects.Move;
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            string dragSourceId = e.Data.GetDataPresent(InternalItemDragFormat)
                ? e.Data.GetData(InternalItemDragFormat) as string
                : null;

            // Handle a reorder only when the drag originated from this exact window.
            // Mark it handled even when its position is unchanged.
            if (string.Equals(dragSourceId, internalDragSourceId, StringComparison.Ordinal))
            {
                internalDropHandled = true;
                e.Effect = DragDropEffects.Move;
                Point dropPoint = PointToClient(new Point(e.X, e.Y));
                int newIndex = GetItemIndexAtPosition(dropPoint);
                if (draggedItem != null)
                {
                    var files = fenceInfo.Files.ToList();
                    if (FenceItemOrder.TryMove(files, draggedItem, newIndex, out int sourceIndex))
                    {
                        RecordReorderUndo(draggedItem, sourceIndex);
                        fenceInfo.Files = files;
                        Save();
                    }
                }

                draggedItemIndex = -1;
                draggedItem = null;
                isDragging = false;
                Invalidate();
                return;
            }

            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped == null || dropped.Length == 0)
                return;

            var addedPaths = new List<string>(dropped.Length);
            int failedMoveCount = 0;
            foreach (var item in dropped)
            {
                if (!fenceInfo.Files.Contains(item, StringComparer.OrdinalIgnoreCase) && ItemExists(item))
                {
                    if (TryMoveItemToFenceFolder(item, out string itemPath))
                    {
                        if (!fenceInfo.Files.Contains(itemPath, StringComparer.OrdinalIgnoreCase))
                        {
                            fenceInfo.Files.Add(itemPath);
                            addedPaths.Add(itemPath);
                        }
                    }
                    else
                    {
                        failedMoveCount++;
                    }
                }
            }

            if (addedPaths.Count == 0)
            {
                e.Effect = DragDropEffects.None;
                if (failedMoveCount > 0)
                {
                    MessageBox.Show(
                        "The item could not be moved into the fence folder.",
                        "Move to fence",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return;
            }

            e.Effect = DragDropEffects.Move;

            Save(); // Ensure we save after adding files
            this.BeginInvoke((Action)(() =>
            {
                Invalidate();
            }));

            if (failedMoveCount > 0)
            {
                MessageBox.Show(
                    $"Moved {addedPaths.Count} item(s), but {failedMoveCount} item(s) could not be moved.",
                    "Move to fence",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void FenceWindow_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Don't interfere with the drag operation - let Windows handle the file placement
            // We'll only handle cancellation if needed
            if (e.Action == DragAction.Cancel)
            {
                Cursor = Cursors.Default;
                draggedItem = null;
                draggedItemIndex = -1;
                Invalidate();
            }
        }

        private void FenceWindow_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            Cursor = Cursors.Default;
            e.UseDefaultCursors = true;
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
                // Check if we've moved far enough to start a real drag operation
                int deltaX = Math.Abs(e.X - dragStartPosition.X);
                int deltaY = Math.Abs(e.Y - dragStartPosition.Y);
                
                if (deltaX > 8 || deltaY > 8) // Drag threshold
                {
                    // Start the actual drag operation
                    draggedItem = dragStartItem;
                    draggedItemIndex = dragStartItemIndex;
                    
                    // Keep FileDrop for Explorer and other fences, and use a private
                    // format to distinguish an in-place reorder from an external move.
                    internalDropHandled = false;
                    var data = new DataObject();
                    data.SetData(InternalItemDragFormat, internalDragSourceId);
                    data.SetData(DataFormats.FileDrop, new[] { dragStartItem });
                    var result = DoDragDrop(data, DragDropEffects.Move | DragDropEffects.Copy);
                    
                    // Handle the result of the drag operation
                    if (result == DragDropEffects.Move && !internalDropHandled)
                    {
                        // File was moved outside the fence - remove it from our list
                        fenceInfo.Files.Remove(dragStartItem);
                        Save();
                        Console.WriteLine($"File moved out of fence: {dragStartItem}");
                    }
                    
                    // Reset drag state
                    isDraggingItem = false;
                    draggedItem = null;
                    draggedItemIndex = -1;
                    dragStartItem = null;
                    dragStartItemIndex = -1;
                    Invalidate();
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
            if (e.Button == MouseButtons.Left)
            {
                bool isLocked = lockedToolStripMenuItem.Checked;

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
                else if (!isDraggingWindow && scrollHeight > 0 && draggedItem == null && hoveringItem == null) // content scroll dragging with left mouse button
                {
                    isDragging = true;
                    initialMousePosition = e.Location;
                }
                else if (hoveringItem != null && !isLocked) // Start tracking potential item drag
                {
                    isDraggingItem = true;
                    dragStartPosition = e.Location;
                    dragStartItem = hoveringItem;
                    
                    // Find the index of the item we're starting to drag
                    dragStartItemIndex = fenceInfo.Files.IndexOf(hoveringItem);
                }
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
                    dragStartItemIndex = -1;
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
            selectedItem = null;
            if (overallOpacity < 1) SetOverallOpacity(overallOpacity);
            Invalidate();
        }

        private void FenceWindow_Click(object sender, EventArgs e)
        {
            shouldUpdateSelection = true;
            Invalidate();
        }

        private void FenceWindow_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Location.Y < titleHeight)
            {
                OnTitleBarDoubleClick();
                return;
            }

            shouldRunDoubleClick = true;
            Invalidate();

            if (hoveringItem != null) return;

            if (state == FenceState.Maximized)
            {
                state = FenceState.Normal;
                // Height = fenceInfo.Height;

                targetSize = new Size(Width, fenceInfo.Height);
                startSize = this.Size;
                animationProgress = 0; // Reset progress
                resizeTimer.Start();
            }
            else if (scrollHeight > 0)
            {
                state = FenceState.Maximized;
                // Height = Math.Min(Screen.PrimaryScreen.WorkingArea.Height, totalHeight + 10); // Expand but don't exceed screen
                targetSize = new Size(Width, Math.Min(Screen.PrimaryScreen.WorkingArea.Height, totalHeight + 10));
                startSize = this.Size;
                animationProgress = 0; // Reset progress
                resizeTimer.Start();
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

            if (hoveringItem != null && !ModifierKeys.HasFlag(Keys.Shift))
            {
                shellContextMenu.ShowContextMenu(new[] { new FileInfo(hoveringItem) }, MousePosition);
            }
            else
            {
                // appContextMenu.Show(this, e.Location); // light skin context menu
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

            startSize = this.Size;
            animationProgress = 0; // Reset progress
            resizeTimer.Start();

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
            var result = MessageBox.Show(
                $"Remove fence '{Text}'?\n\nThe linked folder and all of its files will be kept.",
                "Delete Fence", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                deleteFenceOnClose = true;
                Close();
            }
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
            if (e.Control && e.KeyCode == Keys.Z)
            {
                UndoLastFenceChange();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Delete && selectedItem != null && !lockedToolStripMenuItem.Checked)
            {
                Console.WriteLine($"Delete key pressed for selected item: {selectedItem}");
                RemoveSelectedItem();
                e.Handled = true;
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
                Console.WriteLine($"Error in ScanForWatchedItems: {ex.Message}");
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
                    Console.WriteLine($"Failed to move item {item}: {ex.Message}");
                }
            }

            Save();
            Invalidate();

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
