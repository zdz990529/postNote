using System.ComponentModel;

namespace PostNote;

public class MainForm : Form
{
    public static MainForm? Instance { get; private set; }

    private DataGridView _noteGrid = null!;
    private Panel _toolbarPanel = null!;
    private Button _newBtn = null!;
    private Button _deleteBtn = null!;
    private Button _refreshBtn = null!;
    private Button _saveAllBtn = null!;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip _gridContextMenu = null!;

    public MainForm()
    {
        Instance = this;
        InitializeComponent();
        LoadSavedNotes();
    }

    private void InitializeComponent()
    {
        Text = "桌面便签管理器";
        Size = new Size(700, 480);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(500, 350);
        Font = new Font("微软雅黑", 9f);

        _toolbarPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding = new Padding(10, 8, 10, 8)
        };

        _newBtn = CreateToolButton("＋ 新建便签", Color.FromArgb(0, 150, 136), Color.White);
        _newBtn.Click += (s, e) => CreateNewNote();

        _deleteBtn = CreateToolButton("✕ 删除选中", Color.FromArgb(244, 67, 54), Color.White);
        _deleteBtn.Click += (s, e) => DeleteSelectedNotes();

        _refreshBtn = CreateToolButton("↻ 刷新列表", Color.FromArgb(100, 100, 100), Color.White);
        _refreshBtn.Click += (s, e) => RefreshNoteList();

        _saveAllBtn = CreateToolButton("💾 全部保存", Color.FromArgb(33, 150, 243), Color.White);
        _saveAllBtn.Click += (s, e) => SaveAllNotes();

        _toolbarPanel.Controls.AddRange([_newBtn, _deleteBtn, _refreshBtn, _saveAllBtn]);
        ArrangeToolButtons();

        _noteGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true,
            MultiSelect = true,
            Font = new Font("微软雅黑", 9f)
        };

        _noteGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "便签标题",
                DataPropertyName = "Title",
                FillWeight = 35
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Position",
                HeaderText = "位置",
                DataPropertyName = "Position",
                FillWeight = 20
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Size",
                HeaderText = "尺寸",
                DataPropertyName = "Size",
                FillWeight = 15
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Pinned",
                HeaderText = "置顶",
                DataPropertyName = "Pinned",
                FillWeight = 10
            },
            new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                DataPropertyName = "Id",
                FillWeight = 20
            }
        );

        _noteGrid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0)
            {
                var noteId = _noteGrid.Rows[e.RowIndex].Cells["Id"].Value?.ToString();
                if (!string.IsNullOrEmpty(noteId))
                    OpenNoteById(noteId);
            }
        };

        _noteGrid.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Delete)
                DeleteSelectedNotes();
        };

        _gridContextMenu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("打开便签");
        openItem.Click += (s, e) =>
        {
            if (_noteGrid.SelectedRows.Count > 0)
            {
                var noteId = _noteGrid.SelectedRows[0].Cells["Id"].Value?.ToString();
                if (!string.IsNullOrEmpty(noteId))
                    OpenNoteById(noteId);
            }
        };

        var deleteItem = new ToolStripMenuItem("删除便签");
        deleteItem.Click += (s, e) => DeleteSelectedNotes();

        var pinItem = new ToolStripMenuItem("切换置顶");
        pinItem.Click += (s, e) => TogglePinSelected();

        _gridContextMenu.Items.AddRange([openItem, pinItem, new ToolStripSeparator(), deleteItem]);
        _noteGrid.ContextMenuStrip = _gridContextMenu;

        Controls.Add(_noteGrid);
        Controls.Add(_toolbarPanel);

        SetupNotifyIcon();
        FormClosing += MainForm_FormClosing;
    }

    private Button CreateToolButton(string text, Color backColor, Color foreColor)
    {
        return new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Size = new Size(120, 32),
            Cursor = Cursors.Hand,
            Font = new Font("微软雅黑", 9f),
            UseVisualStyleBackColor = false
        };
    }

    private void ArrangeToolButtons()
    {
        int x = 10;
        foreach (Control c in _toolbarPanel.Controls)
        {
            if (c is Button btn)
            {
                btn.Location = new Point(x, 8);
                x += btn.Width + 8;
            }
        }
    }

    private void SetupNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "桌面便签",
            Visible = true
        };

        var notifyMenu = new ContextMenuStrip();
        notifyMenu.Items.Add("显示管理器", null, (s, e) =>
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
        });
        notifyMenu.Items.Add("新建便签", null, (s, e) => CreateNewNote());
        notifyMenu.Items.Add(new ToolStripSeparator());
        notifyMenu.Items.Add("退出程序", null, (s, e) =>
        {
            _notifyIcon!.Visible = false;
            NoteManager.SaveCurrentState();
            NoteManager.CloseAllNotes();
            Application.Exit();
        });
        _notifyIcon.ContextMenuStrip = notifyMenu;
    }

    private void LoadSavedNotes()
    {
        var savedNotes = NoteManager.LoadNotes();
        foreach (var note in savedNotes)
        {
            NoteManager.OpenNote(note);
        }
        RefreshNoteList();
    }

    public void RefreshNoteList()
    {
        var notes = NoteManager.CollectAllNotes();
        _noteGrid.Rows.Clear();

        foreach (var note in notes)
        {
            _noteGrid.Rows.Add(
                note.Title,
                $"{note.X}, {note.Y}",
                $"{note.Width}×{note.Height}",
                note.AlwaysOnTop ? "是" : "否",
                note.Id
            );
        }
    }

    private void CreateNewNote()
    {
        var data = new NoteData
        {
            X = 150 + new Random().Next(0, 400),
            Y = 150 + new Random().Next(0, 300),
            Title = "新建便签"
        };
        NoteManager.OpenNote(data);
        RefreshNoteList();
    }

    private void DeleteSelectedNotes()
    {
        if (_noteGrid.SelectedRows.Count == 0) return;

        var result = MessageBox.Show(
            $"确定要删除选中的 {_noteGrid.SelectedRows.Count} 个便签吗？",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        var idsToDelete = new List<string>();
        foreach (DataGridViewRow row in _noteGrid.SelectedRows)
        {
            var id = row.Cells["Id"].Value?.ToString();
            if (!string.IsNullOrEmpty(id))
                idsToDelete.Add(id);
        }

        foreach (var id in idsToDelete)
        {
            var note = NoteManager.OpenNotes.FirstOrDefault(n => n.NoteId == id && !n.IsDisposed);
            note?.Close();
        }

        NoteManager.SaveCurrentState();
        RefreshNoteList();
    }

    private void OpenNoteById(string noteId)
    {
        var openNote = NoteManager.OpenNotes.FirstOrDefault(n => n.NoteId == noteId && !n.IsDisposed);
        if (openNote != null)
        {
            openNote.BringToFront();
            openNote.Activate();
            return;
        }

        var savedNotes = NoteManager.LoadNotes();
        var data = savedNotes.FirstOrDefault(n => n.Id == noteId);
        if (data != null)
        {
            NoteManager.OpenNote(data);
        }
    }

    private void TogglePinSelected()
    {
        if (_noteGrid.SelectedRows.Count == 0) return;
        var noteId = _noteGrid.SelectedRows[0].Cells["Id"].Value?.ToString();
        if (string.IsNullOrEmpty(noteId)) return;

        var note = NoteManager.OpenNotes.FirstOrDefault(n => n.NoteId == noteId && !n.IsDisposed);
        if (note != null)
        {
            note.TopMost = !note.TopMost;
        }
        RefreshNoteList();
    }

    private void SaveAllNotes()
    {
        NoteManager.SaveCurrentState();
        MessageBox.Show("所有便签已保存！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        NoteManager.SaveCurrentState();

        if (_notifyIcon != null && _notifyIcon.Visible)
        {
            e.Cancel = true;
            Hide();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon?.Dispose();
            _gridContextMenu?.Dispose();
        }
        base.Dispose(disposing);
    }
}
