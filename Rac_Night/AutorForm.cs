using System;
using System.Drawing;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class AutorForm : Form
    {
        private Button exitButton;
        private PictureBox authorImage;

        public AutorForm()
        {
            InitializeComponent();
            InitializeAutorUI();
        }

        private void InitializeAutorUI()
        {
            this.Text = "Авторы";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(135, 206, 250);

            // Добавление изображения
            authorImage = new PictureBox();
            authorImage.Dock = DockStyle.Fill;
            authorImage.Image = Properties.Resources.authors_image_meme;
            authorImage.SizeMode = PictureBoxSizeMode.StretchImage;
            this.Controls.Add(authorImage);

            // Создание кнопки выхода
            exitButton = new Button();
            exitButton.Text = "Выход";
            exitButton.Location = new Point(50, this.Height - 70);
            exitButton.Click += (sender, e) => this.Close();
            this.Controls.Add(exitButton);

            // Добавляем обработку клавиши ESC
            this.KeyPreview = true;
            this.KeyDown += AutorForm_KeyDown;
        }

        private void AutorForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша ESC
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
