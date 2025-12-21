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
    public partial class PropertyDialog : Form
    {
        MarcControl _instance;
        public MarcControl Instance {
            get { return _instance; }
            set {
                _instance = value;

                if (propertyGrid1 != null)
                {
                    if (value != null)
                    {
                        var viewModel = ViewModel.DressUp(value);
                        propertyGrid1.SelectedObject = viewModel;
                    }
                    else
                        propertyGrid1.SelectedObject = null;
                }
            }
        }
        public PropertyDialog()
        {
            InitializeComponent();
        }
    }
}
