using System.Text;

namespace PhanTichThietKeYeuCau;

public record ComboItem(string Value, string Text)
{
    public override string ToString() => Text;
}

internal static class UI
{
    public static string T(string? s) => s?.Trim() ?? "";

    public static TC At<TC>(this TC c, Control parent, int x, int y, int w = 0, int h = 0) where TC : Control
    {
        c.Left = x; c.Top = y;
        if (w > 0) c.Width = w;
        if (h > 0) c.Height = h;
        parent.Controls.Add(c);
        return c;
    }

    public static Label Lbl(Control parent, string text, int x, int y, int w = 0)
        => new Label { Text = text, AutoSize = w == 0 }.At(parent, x, y + 4, w);

    public static Button Btn(Control parent, string text, int x, int y, int w, Action onClick, int h = 32)
    {
        var b = new Button { Text = text, UseVisualStyleBackColor = true };
        b.Click += (_, _) => onClick();
        return b.At(parent, x, y, w, h);
    }

    public static ComboBox Combo() => new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public static NumericUpDown Num(int decimals = 0, decimal max = 1_000_000_000m)
        => new() { DecimalPlaces = decimals, Minimum = 0, Maximum = max, ThousandsSeparator = true };

    public static void Bind(ComboBox c, List<ComboItem> items)
    {
        c.DisplayMember = nameof(ComboItem.Text);
        c.ValueMember = nameof(ComboItem.Value);
        c.DataSource = items;
    }

    public static DataGridView Grid(Control parent, int x, int y, int w, int h)
    {
        var g = new DataGridView
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.WhiteSmoke }
        };
        return g.At(parent, x, y, w, h);
    }

    public static void Headers(DataGridView g, params (string Col, string Text)[] map)
    {
        foreach (var (col, text) in map)
            if (g.Columns.Contains(col)) g.Columns[col]!.HeaderText = text;
    }

    public static void Info(string msg) =>
        MessageBox.Show(msg, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Error(string msg) =>
        MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static bool Confirm(string msg) =>
        MessageBox.Show(msg, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    // Xuất CSV (mở được bằng Excel, giữ đúng tiếng Việt)
    public static void ExportCsv(DataGridView g, string defaultName)
    {
        using var dlg = new SaveFileDialog { Filter = "CSV (Excel)|*.csv", FileName = defaultName };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        static string Esc(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", g.Columns.Cast<DataGridViewColumn>().Select(c => Esc(c.HeaderText))));
        foreach (DataGridViewRow r in g.Rows)
            sb.AppendLine(string.Join(",", r.Cells.Cast<DataGridViewCell>()
                .Select(c => Esc(c.FormattedValue?.ToString() ?? ""))));

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        Info("Xuất file thành công.");
    }

    public static void PrintPreview(IWin32Window owner, string text)
    {
        var lines = text.Split(Environment.NewLine);
        using var pd = new System.Drawing.Printing.PrintDocument();
        pd.PrintPage += (_, e) =>
        {
            float y = 50;
            using var f = new Font("Consolas", 10);
            foreach (var line in lines) { e.Graphics!.DrawString(line, f, Brushes.Black, 50, y); y += 20; }
        };
        using var dlg = new PrintPreviewDialog { Document = pd, Width = 900, Height = 700 };
        dlg.ShowDialog(owner);
    }
}