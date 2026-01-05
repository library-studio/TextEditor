using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryStudio.Forms.MarcControlDialog
{
    /// <summary>
    /// 视觉风格对话框
    /// </summary>
    public partial class VisualStyleDialog : Form
    {
        public MarcControl RefControl { get; set; }

        public VisualStyleDialog()
        {
            InitializeComponent();
        }

        private void VisualStyleDialog_Load(object sender, EventArgs e)
        {
            foreach(var name in Metrics.ColorThemeNames)
            {
                this.comboBox_colorTheme.Items.Add(name);
            }

            // 参考 RefControl 设置字体
            if (this.RefControl != null)
            {
                this.marcControl_preview.Font = RefControl.Font;
                this.marcControl_preview.FixedSizeFont = RefControl.FixedSizeFont;
                this.marcControl_preview.CaptionFont = RefControl.CaptionFont;
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
            var start = content.IndexOf("选择文字");
            this.marcControl_preview.Select(start, start+4, 0);
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
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
            var name = this.comboBox_colorTheme.Text;
            if (Metrics.ColorThemeNames.Contains(name) == false)
                name = "default";
            this.marcControl_preview.ColorThemeName = name;
        }
    }
}
