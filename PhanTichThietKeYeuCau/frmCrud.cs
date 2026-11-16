using Microsoft.EntityFrameworkCore;
using PhanTichThietKeYeuCau;
using PhanTichThietKeYeuCau.Models;
using System.Data;
using System.Reflection;

namespace PhanTichThietKeYeuCau;

public enum Kind { Text, Int, Dec, Date, Combo }

public class Field
{
    public string Prop { get; }
    public string Label { get; }
    public Kind Kind { get; }
    public bool IsKey { get; init; }
    public bool Required { get; init; }
    public int MaxLen { get; init; }
    public int Decimals { get; init; }
    public decimal Min { get; init; }
    public decimal Max { get; init; } = 1_000_000_000m;
    public string[]? Items { get; init; }                  // danh sách cố định
    public bool Editable { get; init; }                    // cho gõ thêm ngoài danh sách cố định
    public Func<List<ComboItem>>? Source { get; init; }    // khóa ngoại: chỉ được chọn
    public Field(string prop, string label, Kind kind = Kind.Text) { Prop = prop; Label = label; Kind = kind; }
}

public class frmCrud<T> : Form where T : class, new()
{
    private readonly Field[] _f;
    private readonly string _title;
    private readonly Action<T>? _afterSave;
    private readonly Field _key;
    private readonly Dictionary<string, PropertyInfo> _pi;
    private readonly Dictionary<string, Control> _ctl = new();
    private readonly Dictionary<string, List<ComboItem>> _src = new();
    private readonly TextBox txtTim = new();
    private GroupBox gb = null!;
    private DataGridView dgv = null!;
    private Button btnThem = null!, btnLuu = null!, btnSua = null!, btnXoa = null!, btnHuy = null!;
    private bool _them;

    public frmCrud(string title, Field[] fields, Action<T>? afterSave = null)
    {
        _title = title; _f = fields; _afterSave = afterSave;
        _key = fields.First(x => x.IsKey);
        _pi = fields.ToDictionary(f => f.Prop, f => typeof(T).GetProperty(f.Prop)
            ?? throw new InvalidOperationException(
                $"Lớp {typeof(T).Name} không có thuộc tính '{f.Prop}'. Hãy sửa tên trong DanhMuc.cs cho khớp với lớp EF."));

        Text = title.ToUpper();
        ClientSize = new Size(1030, 680);
        StartPosition = FormStartPosition.CenterParent;
        AutoScroll = true;
        BuildUi();
        Load += (_, _) => { NapNguon(); LoadData(); CheDo(false, false); };
    }

    private void BuildUi()
    {
        UI.Lbl(this, "Tìm kiếm:", 15, 12); txtTim.At(this, 85, 12, 260);
        UI.Btn(this, "Tìm", 355, 10, 70, LoadData);
        UI.Btn(this, "Làm mới", 435, 10, 90, () => { txtTim.Clear(); LoadData(); CheDo(false, false, true); });

        var rows = (_f.Length + 1) / 2;
        gb = new GroupBox { Text = "THÔNG TIN" }.At(this, 15, 50, 1000, 30 + rows * 36);
        for (int i = 0; i < _f.Length; i++)
        {
            var f = _f[i];
            int x = 15 + (i % 2) * 495, y = 25 + (i / 2) * 36;
            UI.Lbl(gb, f.Label + (f.Required ? " *" : "") + ":", x, y);
            Control c = f.Kind switch
            {
                Kind.Int => UI.Num(0, f.Max),
                Kind.Dec => UI.Num(f.Decimals, f.Max),
                Kind.Date => new DateTimePicker { Format = DateTimePickerFormat.Short, ShowCheckBox = !f.Required },
                Kind.Combo => MakeCombo(f),
                _ => new TextBox { MaxLength = f.MaxLen }
            };
            c.At(gb, x + 125, y, f.Kind is Kind.Text or Kind.Combo ? 335 : 180);
            _ctl[f.Prop] = c;
        }

        int by = 50 + gb.Height + 10;
        btnThem = UI.Btn(this, "Thêm", 15, by, 90, () => CheDo(true, true, true));
        btnLuu = UI.Btn(this, "Lưu", 115, by, 90, Luu);
        btnSua = UI.Btn(this, "Sửa", 215, by, 90, () =>
        {
            if (Khoa() == "") { UI.Error("Hãy chọn một dòng trong danh sách để sửa."); return; }
            CheDo(true, false);
        });
        btnXoa = UI.Btn(this, "Xóa", 315, by, 90, Xoa);
        btnHuy = UI.Btn(this, "Hủy", 415, by, 90, () => CheDo(false, false, true));
        UI.Btn(this, "Xuất CSV", 515, by, 100, () => UI.ExportCsv(dgv, _title + ".csv"));
        UI.Btn(this, "Đóng", 625, by, 90, () => (ParentForm ?? this).Close());

        dgv = UI.Grid(this, 15, by + 45, 1000, Math.Max(200, ClientSize.Height - by - 60));
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Nap(UI.T(Convert.ToString(dgv.Rows[e.RowIndex].Cells[_key.Prop].Value)));
        };
    }

    private ComboBox MakeCombo(Field f)
    {
        var c = UI.Combo();
        if (f.Items != null)
        {
            c.Items.AddRange(f.Items);
            if (f.Editable) c.DropDownStyle = ComboBoxStyle.DropDown;
        }
        return c;
    }

    private void NapNguon()
    {
        foreach (var f in _f.Where(x => x.Source != null))
        {
            var ds = f.Source!();
            _src[f.Prop] = ds;
            var cb = (ComboBox)_ctl[f.Prop];
            UI.Bind(cb, new List<ComboItem>(ds));
            cb.SelectedIndex = -1;
        }
    }

    // ---------- tiện ích ----------
    private static string Str(object? v) => Convert.ToString(v)?.Trim() ?? "";
    private string Khoa() => ((TextBox)_ctl[_key.Prop]).Text.Trim();

    private string Hien(Field f, object? v)
    {
        if (v == null) return "";
        switch (f.Kind)
        {
            case Kind.Date:
                return v switch { DateOnly d => d.ToString("dd/MM/yyyy"), DateTime t => t.ToString("dd/MM/yyyy"), _ => "" };
            case Kind.Int: return Convert.ToDecimal(v).ToString("N0");
            case Kind.Dec: return Convert.ToDecimal(v).ToString("N" + f.Decimals);
            case Kind.Combo:
                var s = Str(v);
                return _src.TryGetValue(f.Prop, out var items) ? items.FirstOrDefault(i => i.Value == s)?.Text ?? s : s;
            default: return Str(v);
        }
    }

    private void DoVaoControl(Field f, object? v)
    {
        var c = _ctl[f.Prop];
        switch (f.Kind)
        {
            case Kind.Int:
            case Kind.Dec:
                {
                    var n = (NumericUpDown)c;
                    var d = v == null ? 0m : Convert.ToDecimal(v);
                    n.Value = Math.Min(Math.Max(d, n.Minimum), n.Maximum);
                    break;
                }
            case Kind.Date:
                {
                    var dtp = (DateTimePicker)c;
                    DateTime? dt = v switch { DateOnly x => x.ToDateTime(TimeOnly.MinValue), DateTime x => x, _ => null };
                    if (dt == null) { if (dtp.ShowCheckBox) dtp.Checked = false; else dtp.Value = DateTime.Today; }
                    else { dtp.Value = dt.Value; if (dtp.ShowCheckBox) dtp.Checked = true; }
                    break;
                }
            case Kind.Combo:
                {
                    var cb = (ComboBox)c;
                    var s = Str(v);
                    if (f.Items != null) cb.Text = s;
                    else if (s == "") cb.SelectedIndex = -1;
                    else cb.SelectedValue = s;
                    break;
                }
            default: c.Text = Str(v); break;
        }
    }

    private object? DocControl(Field f)
    {
        var c = _ctl[f.Prop];
        switch (f.Kind)
        {
            case Kind.Int: return (int)((NumericUpDown)c).Value;
            case Kind.Dec: return ((NumericUpDown)c).Value;
            case Kind.Date:
                {
                    var d = (DateTimePicker)c;
                    return d.ShowCheckBox && !d.Checked ? null : DateOnly.FromDateTime(d.Value);
                }
            case Kind.Combo:
                {
                    var cb = (ComboBox)c;
                    var s = f.Items != null ? cb.Text.Trim() : (cb.SelectedItem as ComboItem)?.Value;
                    return string.IsNullOrWhiteSpace(s) ? null : s;
                }
            default: return string.IsNullOrWhiteSpace(c.Text) ? null : c.Text.Trim();
        }
    }

    private void GanProp(T e, Field f, object? v)
    {
        var pi = _pi[f.Prop];
        var under = Nullable.GetUnderlyingType(pi.PropertyType);
        var t = under ?? pi.PropertyType;
        if (v == null)
        {
            if (pi.PropertyType.IsValueType && under == null) return;
            pi.SetValue(e, null);
            return;
        }
        object val = v;
        if (t == typeof(string)) val = Convert.ToString(v)!;
        else if (t == typeof(int)) val = Convert.ToInt32(v);
        else if (t == typeof(decimal)) val = Convert.ToDecimal(v);
        else if (t == typeof(DateTime) && v is DateOnly d) val = d.ToDateTime(TimeOnly.MinValue);
        pi.SetValue(e, val);
    }

    private void CheDo(bool nhap, bool them, bool xoaTrang = false)
    {
        _them = them;
        gb.Enabled = nhap;
        ((TextBox)_ctl[_key.Prop]).ReadOnly = !them;
        btnLuu.Enabled = btnHuy.Enabled = nhap;
        btnThem.Enabled = btnSua.Enabled = btnXoa.Enabled = !nhap;
        if (xoaTrang) foreach (var f in _f) DoVaoControl(f, null);
        if (them) _ctl[_key.Prop].Focus();
    }

    // ---------- dữ liệu ----------
    private void LoadData()
    {
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var list = db.Set<T>().AsNoTracking().ToList()
                         .OrderBy(e => Str(_pi[_key.Prop].GetValue(e))).ToList();

            var cols = _f.ToList();
            var dt = new DataTable();
            foreach (var f in cols) dt.Columns.Add(f.Prop, typeof(string));

            var kw = txtTim.Text.Trim();
            foreach (var e in list)
            {
                var vals = cols.Select(f => Hien(f, _pi[f.Prop].GetValue(e))).ToArray();
                if (kw != "" && !vals.Any(v => v.Contains(kw, StringComparison.CurrentCultureIgnoreCase))) continue;
                dt.Rows.Add(vals.Cast<object>().ToArray());
            }
            dgv.DataSource = dt;
            foreach (var f in cols) dgv.Columns[f.Prop]!.HeaderText = f.Label;
        }
        catch (Exception ex) { UI.Error("Không đọc được dữ liệu: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void Nap(string key)
    {
        if (key == "") return;
        using var db = new QuanLyTieuHaoVatTuContext();
        var e = db.Set<T>().Find(key);
        if (e == null) return;
        foreach (var f in _f) DoVaoControl(f, _pi[f.Prop].GetValue(e));
        CheDo(false, false);
    }

    private void Luu()
    {
        foreach (var f in _f)
        {
            var v = DocControl(f);
            if (f.Required && v == null) { UI.Error($"{f.Label} không được để trống."); return; }
            if (f.Kind is Kind.Int or Kind.Dec && Convert.ToDecimal(v ?? 0) < f.Min)
            { UI.Error($"{f.Label} phải lớn hơn hoặc bằng {f.Min:N0}."); return; }
        }

        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var key = Str(DocControl(_key));
            T? e;
            if (_them)
            {
                if (db.Set<T>().Find(key) != null) { UI.Error("Mã này đã tồn tại."); return; }
                e = new T();
            }
            else
            {
                e = db.Set<T>().Find(key);
                if (e == null) { UI.Error("Không tìm thấy bản ghi cần sửa."); return; }
            }

            foreach (var f in _f)
            {
                if (!_them && f.IsKey) continue;
                GanProp(e, f, DocControl(f));
            }
            if (_them) db.Set<T>().Add(e);
            db.SaveChanges();
            _afterSave?.Invoke(e);

            UI.Info("Lưu thành công.");
            CheDo(false, false);
            LoadData();
        }
        catch (Exception ex) { UI.Error("Không lưu được: " + (ex.InnerException?.Message ?? ex.Message)); }
    }

    private void Xoa()
    {
        var key = Khoa();
        if (key == "") { UI.Error("Hãy chọn một dòng trong danh sách để xóa."); return; }
        if (!UI.Confirm($"Bạn có chắc muốn xóa {key}?")) return;
        try
        {
            using var db = new QuanLyTieuHaoVatTuContext();
            var e = db.Set<T>().Find(key);
            if (e != null) { db.Set<T>().Remove(e); db.SaveChanges(); }
            CheDo(false, false, true);
            LoadData();
        }
        catch (DbUpdateException)
        {
            UI.Error("Không thể xóa vì bản ghi đã được dùng ở các chứng từ khác (ràng buộc khóa ngoại).");
        }
    }
}