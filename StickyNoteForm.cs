using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PostNote;

public class StickyNoteForm : Form
{
    private const int BorderPadding = 6;
    private const int TitleBarHeight = 32;
    private const int MinWidth = 180;
    private const int MinHeight = 150;

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HT_CAPTION = 2;
    private const int HT_NOWHERE = 0;
    private const int HT_TOP = 12;
    private const int HT_BOTTOM = 15;
    private const int HT_LEFT = 10;
    private const int HT_RIGHT = 11;
    private const int HT_TOPLEFT = 13;
    private const int HT_TOPRIGHT = 14;
    private const int HT_BOTTOMLEFT = 16;
    private const int HT_BOTTOMRIGHT = 17;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeBtn = null!;
    private Button _pinBtn = null!;
    private RichTextBox _richTextBox = null!;
    private ContextMenuStrip _contextMenu = null!;

    private bool _isPinned;
    private Color _noteColor;

    public string NoteId { get; private set; }

    public StickyNoteForm(NoteData data)
    {
        NoteId = data.Id;
        _noteColor = Color.FromArgb(data.BackColorArgb);
        _isPinned = data.AlwaysOnTop;

        InitializeComponent();
        ApplyNoteData(data);
        BindEvents();
    }

    public StickyNoteForm()
        : this(new NoteData())
    {
    }

    private void InitializeComponent()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        BackColor = _noteColor;
        MinimumSize = new Size(MinWidth, MinHeight);
        Opacity = 0.92;
        ShowInTaskbar = true;
        DoubleBuffered = true;
        Padding = new Padding(BorderPadding);

        _titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = TitleBarHeight,
            BackColor = Color.FromArgb(60, 0, 0, 0),
            Cursor = Cursors.SizeAll
        };

        _titleLabel = new Label
        {
            Text = "新建便签",
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = new Font("微软雅黑", 9.5f, FontStyle.Regular),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            BackColor = Color.Transparent
        };

        _pinBtn = new Button
        {
            Text = "📌",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(28, 28),
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(100, 100, 100),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _pinBtn.FlatAppearance.BorderSize = 0;
        _pinBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 0, 0, 0);

        _closeBtn = new Button
        {
            Text = "✕",
            FlatStyle = FlatStyle.Flat,
            Size = new Size(28, 28),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 100, 100),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _closeBtn.FlatAppearance.BorderSize = 0;
        _closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 220, 80, 80);

        var btnPanel = new Panel
        {
            Size = new Size(60, TitleBarHeight),
            Dock = DockStyle.Right,
            BackColor = Color.Transparent
        };
        _pinBtn.Location = new Point(0, 2);
        _closeBtn.Location = new Point(30, 2);
        btnPanel.Controls.Add(_pinBtn);
        btnPanel.Controls.Add(_closeBtn);

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(btnPanel);

        _richTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = _noteColor,
            Font = new Font("微软雅黑", 10.5f),
            ForeColor = Color.FromArgb(60, 60, 60),
            AcceptsTab = true,
            EnableAutoDragDrop = true,
            AllowDrop = true,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        _contextMenu = new ContextMenuStrip();
        var changeColorItem = new ToolStripMenuItem("更改颜色");
        var yellowItem = new ToolStripMenuItem("淡黄") { Tag = Color.FromArgb(unchecked((int)0xFFFFF9C4)) };
        var pinkItem = new ToolStripMenuItem("淡粉") { Tag = Color.FromArgb(unchecked((int)0xFFFFE4E1)) };
        var blueItem = new ToolStripMenuItem("淡蓝") { Tag = Color.FromArgb(unchecked((int)0xFFE0F0FF)) };
        var greenItem = new ToolStripMenuItem("淡绿") { Tag = Color.FromArgb(unchecked((int)0xFFE0FFE0)) };
        var whiteItem = new ToolStripMenuItem("白色") { Tag = Color.White };
        yellowItem.Click += (s, e) => ChangeColor((Color)((ToolStripMenuItem)s!).Tag!);
        pinkItem.Click += (s, e) => ChangeColor((Color)((ToolStripMenuItem)s!).Tag!);
        blueItem.Click += (s, e) => ChangeColor((Color)((ToolStripMenuItem)s!).Tag!);
        greenItem.Click += (s, e) => ChangeColor((Color)((ToolStripMenuItem)s!).Tag!);
        whiteItem.Click += (s, e) => ChangeColor((Color)((ToolStripMenuItem)s!).Tag!);
        changeColorItem.DropDownItems.AddRange([yellowItem, pinkItem, blueItem, greenItem, whiteItem]);

        var pinItem = new ToolStripMenuItem("置顶显示");
        pinItem.Click += (s, e) => TogglePin();

        var opacityItem = new ToolStripMenuItem("透明度");
        var opacity90Item = new ToolStripMenuItem("90%") { Tag = 0.90 };
        var opacity80Item = new ToolStripMenuItem("80%") { Tag = 0.80 };
        var opacity70Item = new ToolStripMenuItem("70%") { Tag = 0.70 };
        var opacity60Item = new ToolStripMenuItem("60%") { Tag = 0.60 };
        opacity90Item.Click += (s, e) => SetOpacity((double)((ToolStripMenuItem)s!).Tag!);
        opacity80Item.Click += (s, e) => SetOpacity((double)((ToolStripMenuItem)s!).Tag!);
        opacity70Item.Click += (s, e) => SetOpacity((double)((ToolStripMenuItem)s!).Tag!);
        opacity60Item.Click += (s, e) => SetOpacity((double)((ToolStripMenuItem)s!).Tag!);
        opacityItem.DropDownItems.AddRange([opacity90Item, opacity80Item, opacity70Item, opacity60Item]);

        var deleteItem = new ToolStripMenuItem("删除便签");
        deleteItem.Click += (s, e) => Close();

        _contextMenu.Items.AddRange([
            changeColorItem,
            pinItem,
            opacityItem,
            new ToolStripSeparator(),
            deleteItem
        ]);

        _richTextBox.ContextMenuStrip = _contextMenu;

        Controls.Add(_richTextBox);
        Controls.Add(_titleBar);
    }

    private void BindEvents()
    {
        _closeBtn.Click += (s, e) => Close();
        _pinBtn.Click += (s, e) => TogglePin();

        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseDown += TitleBar_MouseDown;
        _richTextBox.MouseDown += RichTextBox_MouseDown;
        MouseDown += Form_MouseDown;
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            BringToFront();
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
        }
    }

    private void Form_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            BringToFront();
            var hitResult = DetectResizeEdge(e.Location);
            if (hitResult != HT_NOWHERE)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)hitResult, IntPtr.Zero);
            }
        }
    }

    private void RichTextBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var screenPoint = _richTextBox.PointToScreen(e.Location);
            var formPoint = PointToClient(screenPoint);
            var hitResult = DetectResizeEdge(formPoint);
            if (hitResult != HT_NOWHERE)
            {
                BringToFront();
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)hitResult, IntPtr.Zero);
                return;
            }
        }
    }

    private int DetectResizeEdge(Point clientPoint)
    {
        bool onLeft = clientPoint.X <= BorderPadding;
        bool onRight = clientPoint.X >= Width - BorderPadding;
        bool onTop = clientPoint.Y <= BorderPadding;
        bool onBottom = clientPoint.Y >= Height - BorderPadding;

        if (onTop && onLeft) return HT_TOPLEFT;
        if (onTop && onRight) return HT_TOPRIGHT;
        if (onBottom && onLeft) return HT_BOTTOMLEFT;
        if (onBottom && onRight) return HT_BOTTOMRIGHT;
        if (onTop) return HT_TOP;
        if (onBottom) return HT_BOTTOM;
        if (onLeft) return HT_LEFT;
        if (onRight) return HT_RIGHT;
        return HT_NOWHERE;
    }

    private void ApplyNoteData(NoteData data)
    {
        Bounds = new Rectangle(data.X, data.Y, data.Width, data.Height);
        _noteColor = Color.FromArgb(data.BackColorArgb);
        BackColor = _noteColor;
        _richTextBox.BackColor = _noteColor;
        _titleLabel.Text = data.Title;
        _isPinned = data.AlwaysOnTop;
        TopMost = _isPinned;
        Opacity = data.Opacity;
        UpdatePinButton();

        if (!string.IsNullOrEmpty(data.RtfContent))
        {
            try
            {
                _richTextBox.Rtf = data.RtfContent;
            }
            catch
            {
                _richTextBox.Text = data.RtfContent;
            }
        }
    }

    public NoteData ToNoteData()
    {
        return new NoteData
        {
            Id = NoteId,
            Title = _titleLabel.Text,
            RtfContent = _richTextBox.Rtf ?? "",
            X = Location.X,
            Y = Location.Y,
            Width = Width,
            Height = Height,
            BackColorArgb = _noteColor.ToArgb(),
            AlwaysOnTop = _isPinned,
            Opacity = Opacity
        };
    }

    private void TogglePin()
    {
        _isPinned = !_isPinned;
        TopMost = _isPinned;
        UpdatePinButton();
    }

    private void UpdatePinButton()
    {
        _pinBtn.ForeColor = _isPinned ? Color.FromArgb(200, 50, 50) : Color.FromArgb(100, 100, 100);
    }

    private void ChangeColor(Color color)
    {
        _noteColor = color;
        BackColor = color;
        _richTextBox.BackColor = color;
    }

    private void SetOpacity(double opacity)
    {
        Opacity = opacity;
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        _titleLabel.Text = Text;
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x0084;
        const int WM_GETMINMAXINFO = 0x0024;

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);

            var screenPoint = new Point(m.LParam.ToInt32() & 0xFFFF, m.LParam.ToInt32() >> 16);
            var clientPoint = PointToClient(screenPoint);

            int hitResult = DetectResizeEdge(clientPoint);
            if (hitResult != HT_NOWHERE)
            {
                m.Result = (IntPtr)hitResult;
                return;
            }

            bool inTitleBar = clientPoint.Y >= 0 && clientPoint.Y <= TitleBarHeight + BorderPadding
                && clientPoint.X >= 0 && clientPoint.X <= Width;

            if (inTitleBar)
                m.Result = (IntPtr)HT_CAPTION;

            return;
        }

        if (m.Msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(m.LParam);
            mmi.ptMinTrackSize.X = MinWidth;
            mmi.ptMinTrackSize.Y = MinHeight;
            Marshal.StructureToPtr(mmi, m.LParam, true);
            m.Result = IntPtr.Zero;
            return;
        }

        base.WndProc(ref m);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        NoteManager.SaveCurrentState();
        base.OnFormClosing(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _richTextBox.Focus();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _contextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
