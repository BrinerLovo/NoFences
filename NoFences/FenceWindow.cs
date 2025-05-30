using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;
using Peter;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static NoFences.Win32.WindowUtil;

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

        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ShellContextMenu shellContextMenu = new ShellContextMenu();
        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();
        private readonly object saveLock = new object();
        private int draggedItemIndex = -1;
        private readonly int itemWidtHalf = itemWidth / 2;

        private void ReloadFonts()
        {
            var family = new FontFamily("Segoe UI");
            titleFont = new Font(family, (int)Math.Floor(logicalTitleHeight / 2.0));
            iconFont = new Font(family, 9);
        }

        public FenceWindow(FenceInfo fenceInfo)
        {
            InitializeComponent();
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
            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;

            ReloadFonts();

            AllowDrop = true;

            this.fenceInfo = fenceInfo;
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

            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.StringFormat))
                e.Effect = DragDropEffects.Move;
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            // if we are dragging a item from the box inside the box.
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                Point dropPoint = PointToClient(new Point(e.X, e.Y));
                int newIndex = GetItemIndexAtPosition(dropPoint);

                // re-order the items
                if (newIndex != -1 && newIndex != draggedItemIndex && draggedItem != null)
                {
                    var files = fenceInfo.Files.ToList();
                    files.RemoveAt(draggedItemIndex);
                    // make sure the new index in the bounds of the list
                    if (newIndex > files.Count) newIndex = files.Count;

                    if (!files.Contains(draggedItem)) files.Insert(newIndex, draggedItem);
                    fenceInfo.Files = files; // Update list
                    Save();
                }

                draggedItemIndex = -1;
                draggedItem = null;
                isDragging = false;
                return;
            }

            string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in dropped)
            {
                if (!fenceInfo.Files.Contains(file) && ItemExists(file))
                {
                    if (Properties.Settings.Default.hide_desktop_icons)
                    {
                        string filePath = HandleDraggedItem(file);
                        fenceInfo.Files.Add(filePath);
                    }
                    else
                    {
                        fenceInfo.Files.Add(file);
                    }
                }
            }

            Save();
            Refresh();
        }

        private void FenceWindow_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (e.Action == DragAction.Drop)
            {
                // check if the mouse is over the form
                if (hoveringItem != null && !ClientRectangle.Contains(PointToClient(MousePosition)))
                {
                    if (Properties.Settings.Default.hide_desktop_icons)
                    {
                        if (File.Exists(hoveringItem))
                        {
                            MoveFileToDesktop(hoveringItem);
                        }
                    }

                    RemoveSelectedItem();

                    Cursor = Cursors.Default;
                    e.Action = DragAction.Cancel;

                    Console.WriteLine("Item dropped outside the form");
                }
            }
            else if (e.Action == DragAction.Cancel)
            {
                Cursor = Cursors.Default;
                Refresh();
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

            Refresh();
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
                Refresh();
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
                // Calculate new size based on cursor position
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
                    Save();
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
            else if (!IsMinified && !lockedToolStripMenuItem.Checked)
            {
                // Update cursor based on mouse position
                this.Cursor = IsNearLeftEdge(e.Location) || IsNearRightEdge(e.Location)
                    ? Cursors.SizeWE
                    : IsNearBottomEdge(e.Location) ? Cursors.SizeNS : Cursors.Default;
            }

            Refresh();
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
            }
        }

        private void FenceWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                isDraggingWindow = false;
                isResizing = false;
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
            Refresh();
        }

        private void FenceWindow_Click(object sender, EventArgs e)
        {
            shouldUpdateSelection = true;
            Refresh();
        }

        private void FenceWindow_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Location.Y < titleHeight)
            {
                OnTitleBarDoubleClick();
                return;
            }

            shouldRunDoubleClick = true;
            Refresh();

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
            if (Application.OpenForms.Count == 0)
                Application.Exit();
        }

        private void Save()
        {
            lock (saveLock)
            {
                FenceManager.Instance.UpdateFence(fenceInfo);
                Refresh();
            }
        }

        private void Minify()
        {
            if ((minifyToolStripMenuItem.Checked || fenceInfo.CanMinify) && !IsMinified)
            {
                state = FenceState.Minified;
                Height = titleHeight;
                Refresh();
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
            Invalidate();
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

        public bool CanInteractWithContent()
        {
            return !IsMinified && !isDragging && !isDraggingWindow && !isResizing;
        }
    }
}