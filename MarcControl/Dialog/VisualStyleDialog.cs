using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Vanara.PInvoke;

namespace LibraryStudio.Forms.MarcControlDialog
{
    /// <summary>
    /// 视觉风格对话框
    /// </summary>
    public partial class VisualStyleDialog : Form
    {
        ColorTheme _theme = null;
        // 用户订制的颜色主题。如果为 null，表示不使用任何定制的颜色主题，而是
        // 使用了 ColorThemeName 为名字的系统内置主题
        public ColorTheme CustomColorTheme
        {
            get
            {
                return _theme;
            }
            set
            {
                if (value is Metrics)
                {
                    throw new ArgumentException("set CustomColorTheme 之 value 不应为 Metrics 类型");
                }

                _theme = value;
                if (_theme != null)
                {
                    FillColors(_theme);
                }
            }
        }

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
                // 参考 RefControl 设置字体、空格突出显示和提示区宽度
                if (this.RefControl != null)
                {
                    this.marcControl_preview.Font = RefControl.Font;
                    this.marcControl_preview.FixedSizeFont = RefControl.FixedSizeFont;
                    this.marcControl_preview.CaptionFont = RefControl.CaptionFont;
                    this.marcControl_preview.HighlightBlankChar = RefControl.HighlightBlankChar;

                    this.marcControl_preview.CaptionPixelWidth = RefControl.CaptionPixelWidth;
                }

                this.marcControl_preview.GetStructure += (parent, name, level) =>
                {
                    if (RefControl != null)
                    {
                        return RefControl.GetStructure(parent, name, level);
                    }
                    else
                    {
                        /*
                        if (field.IsHeader)
                        {
                            return $"头标区";
                        }

                        return $"字段 '{field.FieldName}' 的名称";
                        */
                        return null;
                    }
                };

                /*
                var content = MarcRecord.BuildContent(@"012345678901234567890123
001000000000
2001 $aAAA$fFFFFF
300  $a选择文字没有被选择的文字
801  $aCN");
                */
                string content = @"01317nam0 2200277   450 00101201808143100520180814925326.0010  a978-7-02-014453-2dCNY42.00100  a20181221d2018    cemy0chiy50      ea1011 achiceng102  aCNb110000105  aa   z   000ay106  ar2001 a哈利·波特与魔法石Aha li· bo te yu mo fa shif(英)J.K.罗琳著g苏农译205  a2版210  a北京c人民文学出版社d2018.10215  a241页c图d24cm330  a本书讲述：哈利·波特的人生中没有魔法。他和一点都不友善的德思礼夫妇，还有他们令人厌恶的儿子达力住在一起。哈利的房间是一个窄小的储物间，就在楼梯下面，而且十一年来他从未有过生日派对。 但是有一天，猫头鹰信使突然送来一封神秘的信件，令人不敢相信的是，信里附着一张来自霍格沃茨魔法学校的录取哈利入学的通知书。 哈利于九月一日带着他的猫头鹰乘着特快列车来到魔法学校。在学校里，他遇到了他一生中两个最好的朋友，体验了骑着飞天扫帚打球的运动，从课堂上和生活中的所有事物里学到了魔法。不仅如此，他还得知自己将有一个伟大而不可思议的命运……333  a本书为儿童读物5101 aHarry Potter and the philosopher's stonezeng6060 a儿童小说x长篇小说y英国z现代690  aI561.84v5701 0c(英)a罗琳Aluo linc(Rowling, J. K)4著702 0a苏农Asu nong4译801 0aCNb百万庄c20181221";
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
                /*
                var start = content.IndexOf("选择文字");
                this.marcControl_preview.Select(start, start + 4, 0);
                */
                var start = content.IndexOf("哈利·波特与魔法石");
                this.marcControl_preview.Select(start, start + "哈利·波特与魔法石".Length, 0);

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
            ColorTheme theme = null;
            if (theme_name == MarcControl.CUSTOM_THEME_CAPTION)
            {
                if (this.CustomColorTheme == null)
                {
                    // 参考基于当前正在观察的一个 ColorTheme
                    var ref_theme = this.listView_colors.Tag as ColorTheme;
                    this.CustomColorTheme = ref_theme == null ? ColorTheme.ThemeDefault() : ref_theme.Clone();

                    // this.CustomColorTheme = ColorTheme.ThemeDefault();
                }
                theme = this.CustomColorTheme;
            }
            else
            {
                theme = ColorTheme.GetTheme(theme_name);
                if (theme == null)
                {
                    this.listView_colors.Items.Clear();
                    return;
                }
            }

            FillColors(theme);
#if REMOVED
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
#endif
        }

        void FillColors(ColorTheme theme)
        {
            this.listView_colors.BeginUpdate();
            try
            {
                this.listView_colors.Items.Clear();
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
            finally
            {
                this.listView_colors.EndUpdate();
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
                    // 参考基于当前正在观察的一个 ColorTheme
                    var ref_theme = this.listView_colors.Tag as ColorTheme;
                    this.CustomColorTheme = ref_theme == null ? ColorTheme.ThemeDefault() : ref_theme.Clone();
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
            if (theme == null)
            {
                e.DrawDefault = true;
                return;
            }
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
        bool ClearDelimeterColor()
        {
            if (this.CustomColorTheme == null)
                return false;
            this.CustomColorTheme.DelimeterBackColor = Color.Transparent;
            this.CustomColorTheme.DelimeterForeColor = this.CustomColorTheme.ForeColor;

            this.marcControl_preview.SetCustomColorTheme(this.CustomColorTheme);
            SampleSelect();
            this.listView_colors.Invalidate();
            return true;
        }

        bool CopyJson()
        {
            var theme = this.listView_colors.Tag as ColorTheme;
            if (theme == null)
                return false;
            if (theme is Metrics)
                throw new ArgumentException("this.listView_colors.Tag 不应为 Metrics 类型");
            MarcControl.ClipboardSetText(theme.ToJson());
            return true;
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
                    Enabled = IsCustom(),
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

        private void button_font_content_Click(object sender, EventArgs e)
        {
            var result = SettingFont(this.ContentFontString);
            if (result != null)
            {
                //ChangeTextBoxFont(this.textBox_font_content, result);
                this.ContentFontString = result;
                this.marcControl_preview.Font = MarcControl.GetFont(result);
                this.FontsChanged = true;
            }
        }

        private void button_font_fixed_Click(object sender, EventArgs e)
        {
            var result = SettingFont(this.FixedFontString);
            if (result != null)
            {
                //ChangeTextBoxFont(this.textBox_font_fixed, result);
                this.FixedFontString = result;
                this.marcControl_preview.FixedSizeFont = MarcControl.GetFont(result);
                this.FontsChanged = true;
            }
        }

        private void button_font_caption_Click(object sender, EventArgs e)
        {
            var result = SettingFont(this.CaptionFontString);
            if (result != null)
            {
                //ChangeTextBoxFont(this.textBox_font_caption, result);
                this.CaptionFontString = result;
                this.marcControl_preview.CaptionFont = MarcControl.GetFont(result);
                this.FontsChanged = true;
            }
        }

        // 改变 textBox 的字体。不改变字号。
        void ChangeTextBoxFont(TextBox textbox, string font_string)
        {
            var old_font = textbox.Font;
            using (var ref_font = MarcControl.GetFont(font_string))
            {
                if (ref_font != null)
                {
                    textbox.Font = new Font(ref_font.FontFamily,
                        old_font.SizeInPoints,
                        ref_font.Style,
                        GraphicsUnit.Point);
                }
            }
        }

        string SettingFont(string font_string)
        {
            using (FontDialog dlg = new FontDialog())
            {
                dlg.Font = MarcControl.GetFont(font_string);
                dlg.AllowVerticalFonts = false;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }

                return MarcControl.GetFontString(dlg.Font);
            }
        }

        public string ContentFontString
        {
            get
            {
                return this.textBox_font_content.Text;
            }
            set
            {
                this.textBox_font_content.Text = value;
            }
        }

        public string FixedFontString
        {
            get
            {
                return this.textBox_font_fixed.Text;
            }
            set
            {
                this.textBox_font_fixed.Text = value;
            }
        }

        public string CaptionFontString
        {
            get
            {
                return this.textBox_font_caption.Text;
            }
            set
            {
                this.textBox_font_caption.Text = value;
            }
        }

        // 将 MarcControl 中的字体装载到本对话框
        public void LoadAllFont(MarcControl control)
        {
            this.ContentFontString = MarcControl.GetFontString(control.Font);
            this.FixedFontString = MarcControl.GetFontString(control.FixedSizeFont);
            this.CaptionFontString = MarcControl.GetFontString(control.CaptionFont);
        }

        public bool FontsChanged { get; set; }

        // 将本对话框中的字体应用到指定的 MarcControl
        public bool ApplyAllFont(MarcControl control)
        {
            if (FontsChanged == false)
            {
                return false;
            }

            control.BeginUpdate();
            try
            {
                {
                    var font = MarcControl.GetFont(this.ContentFontString);
                    control.Font = font == null ? Control.DefaultFont : font;
                }

                {
                    var font = MarcControl.GetFont(this.FixedFontString);
                    control.FixedSizeFont = font == null ? Control.DefaultFont : font;
                }

                {
                    var font = MarcControl.GetFont(this.CaptionFontString);
                    control.CaptionFont = font == null ? Control.DefaultFont : font;
                }
                return true;
            }
            finally
            {
                control.EndUpdate();
            }
        }

        private void textBox_font_content_TextChanged(object sender, EventArgs e)
        {
            var textbox = sender as TextBox;
            if (textbox != null)
                ChangeTextBoxFont(textbox, textbox.Text);
        }

        // 恢复默认字体
        private void button_font_reset_Click(object sender, EventArgs e)
        {
            // TODO: 验证当前环境下这三个字体是否存在，如果不存在，用近似的字体替换

            this.ContentFontString = _default_content_font_string;
            this.marcControl_preview.Font = MarcControl.GetFont(_default_content_font_string);

            this.FixedFontString = _default_fixed_font_string;
            this.marcControl_preview.FixedSizeFont = MarcControl.GetFont(_default_fixed_font_string);

            this.CaptionFontString = _default_caption_font_string;
            this.marcControl_preview.CaptionFont = MarcControl.GetFont(_default_caption_font_string);

            this.FontsChanged = true;
        }

        const string _default_content_font_string = "宋体, 10.5pt";
        const string _default_fixed_font_string = "Courier New, 10.5pt, style=Bold";
        const string _default_caption_font_string = "楷体, 10.5pt";

        public int SelectedPageIndex
        {
            get
            {
                return this.tabControl_colorTheme.SelectedIndex;
            }
            set
            {
                this.tabControl_colorTheme.SelectedIndex = value;
            }
        }
    }
}
