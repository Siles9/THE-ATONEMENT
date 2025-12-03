using System.Windows.Forms;
using System.Drawing;

namespace Rac_Night
{
    public partial class GameplayForm : Form
    {
        public GameplayForm()
        {
            InitializeComponent();
            this.Text = "Gameplay Form";
            this.BackColor = Color.DarkGreen;
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            Label welcomeLabel = new Label();
            welcomeLabel.Text = "Добро пожаловать в игру!";
            welcomeLabel.Font = new Font("Arial", 48, FontStyle.Bold);
            welcomeLabel.ForeColor = Color.White;
            welcomeLabel.Dock = DockStyle.Fill;
            welcomeLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(welcomeLabel);

            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Application.Exit(); };
        }
    }
}

