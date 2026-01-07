using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace LibraryStudio.Forms.MarcControlDialog
{
    /// <summary>
    /// 视觉风格对话框
    /// </summary>
    public partial class VisualStyleDialog : Form
    {
        // 用户订制的颜色主题。如果为 null，表示不使用任何定制的颜色主题，而是
        // 使用了 ColorThemeName 为名字的系统内置主题
        public ColorTheme CustomColorTheme { get; set; }

        public MarcControl RefControl { get; set; }

        SizeF _dpiXY;

        public VisualStyleDialog()
        {
            InitializeComponent();

            _dpiXY = DpiUtil.GetDpiXY(this);

            InitialColumnWidth();
        }

        private void VisualStyleDialog_Load(object sender, EventArgs e)
        {
            FillColorThemeList();

            {
                // 参考 RefControl 设置字体
                if (this.RefControl != null)
                {
                    this.marcControl_preview.Font = RefControl.Font;
                    this.marcControl_preview.FixedSizeFont = RefControl.FixedSizeFont;
                    this.marcControl_preview.CaptionFont = RefControl.CaptionFont;
                    this.marcControl_preview.HighlightBlankChar = RefControl.HighlightBlankChar;
                }

                this.marcControl_preview.GetFieldCaption += (field) =>
                {
                    if (field.IsHeader)
                        return $"头标区";
                    return $"字段 '{field.FieldName}' 的名称";
                };

                var content = MarcRecord.BuildContent(@"012345678901234567890123
001000000000
2001 $aAAA$fFFFFF
300  $a选择文字没有被选择的文字
801  $aCN");
                this.marcControl_preview.Content = content;
                /*
                var start = content.IndexOf("选择文字");
                this.marcControl_preview.Select(start, start + 4, 0);
                */
                SampleSelect();
            }

            // FillColors(this.comboBox_colorTheme.Text);
            OnColorThemeSelectedChanged();
        }

        void SampleSelect()
        {
            if (this.marcControl_preview.HasSelection() == false)
            {
                var content = this.marcControl_preview.Content;
                var start = content.IndexOf("选择文字");
                this.marcControl_preview.Select(start, start + 4, 0);
            }
        }

        void FillColorThemeList()
        {
            this.comboBox_colorTheme.Items.Clear();
            foreach (var name in ColorTheme.Captions("zh"))
            {
                this.comboBox_colorTheme.Items.Add(name);
            }

            this.comboBox_colorTheme.Items.Add(MarcControl.CUSTOM_THEME_CAPTION);
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            if (this.comboBox_colorTheme.Text == MarcControl.CUSTOM_THEME_CAPTION)
            {

            }
            else
            {
                if (this.CustomColorTheme != null)
                {
                    var result = MessageBox.Show(this,
                        "有一个刚设置的定制颜色主题尚未利用。对话框关闭后会丢失。\r\n\r\n请问是否要关闭对话框?",
                        "定制颜色主题尚未利用",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                    this.CustomColorTheme = null;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string ColorThemeName
        {
            get
            {
                return this.comboBox_colorTheme.Text;
            }
            set
            {
                this.comboBox_colorTheme.Text = value;
            }
        }

        private void comboBox_colorTheme_TextChanged(object sender, EventArgs e)
        {
            OnColorThemeSelectedChanged();
        }

        // 填充颜色样本 list
        void FillColors(string theme_name)
        {
            this.listView_colors.Items.Clear();
            ColorTheme theme = null;
            if (theme_name == MarcControl.CUSTOM_THEME_CAPTION)
            {
                if (this.CustomColorTheme == null)
                    this.CustomColorTheme = ColorTheme.ThemeDefault();
                theme = this.CustomColorTheme;
            }
            else
            {
                theme = ColorTheme.GetTheme(theme_name);
                if (theme == null)
                {
                    return;
                }
            }

            this.listView_colors.Tag = theme;
            foreach (var info in theme.GetColorProperties())
            {
                Debug.Assert(theme != null);

                var color_value = GetColor(info, theme);
                var item = new ListViewItem(ColorTheme.GetNameCaption(info, "zh"));
                this.listView_colors.Items.Add(item);
                item.Tag = info;
                // 颜色名
                item.SubItems.Add(new ListViewItem.ListViewSubItem());
                // 样本
                item.SubItems.Add(new ListViewItem.ListViewSubItem { Tag = color_value });
                // 按钮
                item.SubItems.Add(new ListViewItem.ListViewSubItem { Text = "修改..." });
            }
        }

        void InitialColumnWidth()
        {
            int[] width_list = new int[] { 90, 170, 50, 50, 50 };
            int i = 0;
            foreach (ColumnHeader column in this.listView_colors.Columns)
            {
                int v = width_list[i++];
                column.Width = scale(v);
            }
            int scale(int v)
            {
                return DpiUtil.GetScalingX(this._dpiXY, v);
            }
        }

        static Color GetColor(PropertyInfo info,
            ColorTheme theme)
        {
            return (Color)info.GetValue(theme);
        }

        static void SetColor(PropertyInfo info,
            ColorTheme theme,
            Color value)
        {
            info.SetValue(theme, value);
        }

        const int COLUMN_NAME = 0;
        const int COLUMN_COLOR_NAME = 1;
        const int COLUMN_SAMPLE = 2;
        const int COLUMN_BUTTON = 3;

        private void comboBox_colorTheme_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnColorThemeSelectedChanged();
        }

        void OnColorThemeSelectedChanged()
        {
            FillColors(this.comboBox_colorTheme.Text);

            var name = this.comboBox_colorTheme.Text;
            if (name == MarcControl.CUSTOM_THEME_CAPTION)
            {
                if (this.CustomColorTheme == null)
                {
                    this.CustomColorTheme = new ColorTheme();
                }

                this.marcControl_preview.SetCustomColorTheme(this.CustomColorTheme);

                SetListBackColor();
                return;
            }
            else
            {
                // this.CustomColorTheme = null;
            }

            if (ColorTheme.AllLangCaptions().Contains(name) == false)
                name = "default";
            this.marcControl_preview.ColorThemeName = name;

            SetListBackColor();
        }

        void SetListBackColor()
        {
            if (IsCustom())
            {
                this.listView_colors.BackColor = SystemColors.Window;
                this.button_clearDelimeterColor.Enabled = true;
            }
            else
            {
                this.listView_colors.BackColor = SystemColors.Control;
                this.button_clearDelimeterColor.Enabled = false;
            }
        }

        private void listView_colors_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // e.DrawDefault = true;
        }

        private void listView_colors_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        // 绘制 SubItem
        private void listView_colors_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var column_index = e.ColumnIndex;

            var info = e.Item.Tag as PropertyInfo;
            var theme = this.listView_colors.Tag as ColorTheme;

            if (column_index == COLUMN_COLOR_NAME)
            {
                var color_name = GetColor(info, theme).ToString().Replace("Color ", "").Replace("[", "").Replace("]", "");
                var is_custom = IsCustom();
                using (var brush = new SolidBrush(is_custom ? SystemColors.WindowText : SystemColors.GrayText))
                {
                    e.Graphics.DrawString(color_name,
                        this.Font,
                        brush,
                        e.Bounds.X,
                        e.Bounds.Y,
                        StringFormat.GenericDefault);
                }
                return;
            }
            else if (column_index == COLUMN_SAMPLE)
            {
                var color_value = GetColor(info, theme);
                using (var brush = new SolidBrush(color_value))
                {
                    // TODO: Color.Transparent 要绘制为马赛克，表示透明。或者直接中间出现“透明”字样
                    e.Graphics.FillRectangle(brush, e.SubItem.Bounds);
                }
                return;
            }
            else if (e.ColumnIndex == COLUMN_BUTTON)
            {
                var border = DpiUtil.GetScalingSize(_dpiXY, 4, 1);
                var rect = e.Bounds;
                rect.Inflate(-border.Width, -border.Height);

                using (var font = new Font(this.Font.FontFamily, this.Font.SizeInPoints * 0.9F, this.Font.Style, GraphicsUnit.Point))
                {
                    var is_custom = IsCustom();
                    var text = e.SubItem.Text;
                    var size = e.Graphics.MeasureString(text, font);

                    rect.Width = 2 * DpiUtil.GetScalingX(_dpiXY, 1) + (int)size.Width;
                    ControlPaint.DrawButton(e.Graphics,
                        rect,
                        is_custom == false ? ButtonState.Inactive : ButtonState.Normal);

                    using (var brush = new SolidBrush(is_custom ? SystemColors.WindowText : SystemColors.GrayText))
                    {
                        e.Graphics.DrawString(text,
                            font,
                            brush,
                            rect.X,
                            rect.Y,
                            StringFormat.GenericDefault);
                    }
                }
                return;
            }
            e.DrawDefault = true;
        }

        // 鼠标点击按钮
        private void listView_colors_MouseClick(object sender, MouseEventArgs e)
        {
            var result = this.listView_colors.HitTest(new Point(e.X, e.Y));
            int column_index = result.Item.SubItems.IndexOf(result.SubItem);
            if (column_index == COLUMN_BUTTON)
            {
                // MessageBox.Show(this, result.Item.Text);
                ChangeItemColorValue(result.Item);
            }
        }

        // 判断当前是否正在编辑定制颜色主题
        bool IsCustom()
        {
            return this.comboBox_colorTheme.Text == MarcControl.CUSTOM_THEME_CAPTION;
        }

        void ChangeItemColorValue(ListViewItem item)
        {
            if (IsCustom() == false)
                return;

            Debug.Assert(this.CustomColorTheme != null);

            var info = (PropertyInfo)item.Tag;
            var color_value = GetColor(info, this.CustomColorTheme);   // (Color)info.GetValue(this.CustomColorTheme, null);
            using (ColorDialog dlg = new ColorDialog())
            {
                //dlg.ShowHelp = true;
                //dlg.FullOpen = true;
                dlg.SolidColorOnly = false;
                dlg.AnyColor = true;
                dlg.Color = color_value;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // info.SetValue(this.CustomColorTheme, dlg.Color);
                    SetColor(info, this.CustomColorTheme, dlg.Color);
                    this.marcControl_preview.SetCustomColorTheme(this.CustomColorTheme);
                    // this.marcControl_preview.OnColorChanged(new EventArgs());

                    SampleSelect();
                }
            }
            this.listView_colors.Invalidate();
        }

        void ClearItemColorValue(ListViewItem item)
        {
            var info = item.Tag as PropertyInfo;

            SetColor(info, this.CustomColorTheme, Color.Transparent);
            this.marcControl_preview.SetCustomColorTheme(this.CustomColorTheme);
            // this.marcControl_preview.OnColorChanged(new EventArgs());

            SampleSelect();
            this.listView_colors.Invalidate();
        }

        // 清除分隔符背景色为 Transparent，并且把分隔符前景色设置为和 ForeColor 一样
        void ClearDelimeterColor()
        {
            this.CustomColorTheme.DelimeterBackColor = Color.Transparent;
            this.CustomColorTheme.DelimeterForeColor = this.CustomColorTheme.ForeColor;

            this.marcControl_preview.SetCustomColorTheme(this.CustomColorTheme);
            SampleSelect();
            this.listView_colors.Invalidate();
        }

        void CopyJson()
        {
            var theme = this.listView_colors.Tag as ColorTheme;
            Clipboard.SetText(theme.ToJson());
        }

        void PasteJson()
        {
            var text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return;
            try
            {
                this.CustomColorTheme = ColorTheme.FromJson(text);

                this.marcControl_preview.SetCustomColorTheme(this.CustomColorTheme);
                SampleSelect();
                this.listView_colors.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"JSON 内容不合法: {ex.Message}");
            }
        }

        private void listView_colors_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var selected_item = this.listView_colors
                    .SelectedItems
                    .Cast<ListViewItem>()
                    .FirstOrDefault();

            var strip = new ContextMenuStrip();

            strip.Items.Add(new ToolStripLabel($"部件 '{selected_item?.Text}'"));
            strip.Items.Add(new ToolStripSeparator());
            {
                var menu = new ToolStripMenuItem
                {
                    Text = "修改颜色 ...",
                    Enabled = selected_item != null && IsCustom(),
                };
                menu.Click += (s1, e1) =>
                {
                    ChangeItemColorValue(selected_item);
                };
                strip.Items.Add(menu);
            }
            {
                var menu = new ToolStripMenuItem
                {
                    Text = "设为透明色",
                    Enabled = selected_item != null && IsCustom(),
                };
                menu.Click += (s1, e1) =>
                {
                    ClearItemColorValue(selected_item);
                };
                strip.Items.Add(menu);
            }

            strip.Items.Add(new ToolStripSeparator());
            {
                var menu = new ToolStripMenuItem
                {
                    Text = "清除分隔符颜色",
                    Enabled = true,
                };
                menu.Click += (s1, e1) =>
                {
                    ClearDelimeterColor();
                };
                strip.Items.Add(menu);
            }

            strip.Items.Add(new ToolStripSeparator());
            {
                var menu = new ToolStripMenuItem
                {
                    Text = "复制 JSON",
                    Enabled = true,
                };
                menu.Click += (s1, e1) =>
                {
                    CopyJson();
                };
                strip.Items.Add(menu);
            }

            {
                var menu = new ToolStripMenuItem
                {
                    Text = "粘贴 JSON",
                    Enabled = IsCustom(),
                };
                menu.Click += (s1, e1) =>
                {
                    PasteJson();
                };
                strip.Items.Add(menu);
            }

            strip.Show(this.listView_colors, e.Location);
        }

        private void button_clearDelimeterColor_Click(object sender, EventArgs e)
        {
            ClearDelimeterColor();
        }
    }
}
