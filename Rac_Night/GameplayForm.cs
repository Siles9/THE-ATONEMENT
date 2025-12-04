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
            this.Text = "Rac Night - Gameplay"; // Название окна
            this.BackColor = Color.Black; // Фоновый цвет (по желанию)
            this.FormBorderStyle = FormBorderStyle.None; // Без рамки
            this.WindowState = FormWindowState.Maximized; // Максимальный размер (полноэкранный)

            Label gameplayLabel = new Label();
            gameplayLabel.Text = "Это форма GameplayForm!\nДобро пожаловать в игру!";
            gameplayLabel.Font = new Font("Arial", 36, FontStyle.Bold);
            gameplayLabel.ForeColor = Color.White;
            gameplayLabel.TextAlign = ContentAlignment.MiddleCenter;
            gameplayLabel.Dock = DockStyle.Fill;
            this.Controls.Add(gameplayLabel);
        }

        // Чтобы выйти из игры, когда GameplayForm активна, можно добавить обработчик клавиш, например, для Esc:
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