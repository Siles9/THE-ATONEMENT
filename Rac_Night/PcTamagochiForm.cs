using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class PcTamagochiForm : Form
    {
        public PcTamagochiForm()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(PcTamagochiForm_KeyDown);
        }

        private void PcTamagochiForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
