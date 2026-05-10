using Newtonsoft.Json;

namespace PostNote;

public class NoteData
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = "新建便签";
    public string RtfContent { get; set; } = "";
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 300;
    public int BackColorArgb { get; set; } = unchecked((int)0xFFFFF9C4);
    public bool AlwaysOnTop { get; set; } = false;
    public double Opacity { get; set; } = 0.92;
}

public static class NoteManager
{
    private static readonly string DataFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PostNote", "notes.json");

    private static readonly List<StickyNoteForm> _openNotes = [];

    public static List<NoteData> LoadNotes()
    {
        try
        {
            if (File.Exists(DataFile))
            {
                var json = File.ReadAllText(DataFile);
                return JsonConvert.DeserializeObject<List<NoteData>>(json) ?? [];
            }
        }
        catch
        {
        }
        return [];
    }

    public static void SaveNotes(List<NoteData> notes)
    {
        try
        {
            var dir = Path.GetDirectoryName(DataFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonConvert.SerializeObject(notes, Formatting.Indented);
            File.WriteAllText(DataFile, json);
        }
        catch
        {
        }
    }

    public static List<StickyNoteForm> OpenNotes => _openNotes;

    public static void OpenNote(NoteData data)
    {
        var existing = _openNotes.FirstOrDefault(n => n.NoteId == data.Id);
        if (existing != null && !existing.IsDisposed)
        {
            existing.BringToFront();
            existing.Activate();
            return;
        }

        var note = new StickyNoteForm(data);
        note.FormClosed += (s, e) =>
        {
            _openNotes.Remove(note);
            MainForm.Instance?.RefreshNoteList();
        };
        _openNotes.Add(note);
        note.Show();
    }

    public static void CloseAllNotes()
    {
        foreach (var note in _openNotes.ToList())
        {
            if (!note.IsDisposed)
                note.Close();
        }
        _openNotes.Clear();
    }

    public static List<NoteData> CollectDataFromOpenNotes()
    {
        var allNotes = new List<NoteData>();
        foreach (var note in _openNotes)
        {
            if (!note.IsDisposed)
                allNotes.Add(note.ToNoteData());
        }
        // 也合并那些已关闭但可能存在于已保存列表中的便签
        // 这里简化处理：用当前打开的覆盖
        var saved = LoadNotes();
        var result = new List<NoteData>();
        var openIds = new HashSet<string>(allNotes.Select(n => n.Id));

        result.AddRange(allNotes);
        foreach (var s in saved)
        {
            if (!openIds.Contains(s.Id))
                result.Add(s);
        }
        return result;
    }

    public static void SaveCurrentState()
    {
        var notes = new List<NoteData>();
        foreach (var note in _openNotes)
        {
            if (!note.IsDisposed)
                notes.Add(note.ToNoteData());
        }
        SaveNotes(notes);
    }

    public static List<NoteData> CollectAllNotes()
    {
        var all = new List<NoteData>();
        foreach (var note in _openNotes)
        {
            if (!note.IsDisposed)
                all.Add(note.ToNoteData());
        }
        return all;
    }
}
