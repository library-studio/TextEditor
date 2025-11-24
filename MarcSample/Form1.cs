using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarcSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.marcControl1.GetFieldCaption += (field) => {
                if (field.IsHeader)
                    return $"获得 '{field.FieldName}' 头标区的值";
                return $"获得 '{field.FieldName}' 的值";
            };

            this.marcControl1.Content = "012345678901234567890123abc12ABC";
            //this.marcControl1.Content = new string((char)31, 1) + "1";
        }
    }
}
