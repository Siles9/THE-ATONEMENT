using System.Windows.Forms;
using System.Drawing;

namespace Rac_Night
{
    public partial class GameplayForm : Form
    {
        public GameplayForm()
        {
            InitializeComponent();
            SetupFullscreenBorderless();
        }

        private void SetupFullscreenBorderless()
        {
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}