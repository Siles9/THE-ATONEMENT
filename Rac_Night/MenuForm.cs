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
    public partial class MenuForm : Form
    {
        private string[] buttonTexts = {
            "Новая игра",
            "Продолжить игру",
            "Настройки",
            "Авторы",
            "Выход"
        };

        public MenuForm()
        {
            InitializeComponent();
            InitializeMenuUI();

        }

        private void InitializeMenuUI()
        {
            // Настройка формы
            this.Text = "Меню игры: Искупление питомца";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(135, 206, 250);

            // Заголовок игры
            Label titleLabel = new Label();
            titleLabel.Text = "THE ATONEMENT";
            titleLabel.Font = new Font("Impact", 48, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(0, 0, 139);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(50, 50);
            this.Controls.Add(titleLabel);

            // Создание кнопок
            int buttonWidth = 200;
            int buttonHeight = 50;
            int startY = 150;

            for (int i = 0; i < buttonTexts.Length; i++)
            {
                Button button = new Button();
                button.Name = $"btn{buttonTexts[i].Replace(' ', ' ')}";
                button.Text = buttonTexts[i];
                button.Width = buttonWidth;
                button.Height = buttonHeight;
                button.BackColor = Color.White;
                button.ForeColor = Color.FromArgb(0, 0, 139);
                button.Font = new Font("Arial", 14, FontStyle.Bold);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(0, 0, 139);
                button.FlatAppearance.BorderSize = 1;

                button.Location = new Point(
                    50,
                    startY + i * (buttonHeight + 20)
                );

                button.Click += (sender, e) => HandleButtonClick((string)sender.GetType().GetProperty("Text").GetValue(sender));
                this.Controls.Add(button);
            }

            // Обработка рисования облаков
            this.Paint += (sender, e) =>
            {
                Random rnd = new Random();
                List<Point> cloudPositions = new List<Point>();

                // Рандомные позиции облаков
                for (int i = 0; i < 3; i++)
                {
                    int x = rnd.Next(300, this.Width - 300); // Случайная X-координата
                    int y = rnd.Next(200, this.Height - 200); // Случайная Y-координата

                    // Проверяем, не перекрываются ли облака
                    bool isValidPosition = true;
                    foreach (var pos in cloudPositions)
                    {
                        if (Math.Abs(pos.X - x) < 100 || Math.Abs(pos.Y - y) < 100)
                        {
                            isValidPosition = false;
                            break;
                        }
                    }

                    if (isValidPosition)
                    {
                        cloudPositions.Add(new Point(x, y));

                        // Рисуем облако
                        DrawCloud(e.Graphics, x, y, 150, 100);
                        DrawCloud(e.Graphics, x + 50, y - 50, 150, 100);
                        DrawCloud(e.Graphics, x + 100, y, 150, 100);

                        // Добавляем детали
                        DrawCloudDetail(e.Graphics, x + 20, y + 20, 50, 40);
                        DrawCloudDetail(e.Graphics, x + 100, y - 30, 40, 30);
                        DrawCloudDetail(e.Graphics, x + 180, y + 20, 50, 40);
                    }
                }
            };
        }
        private void DrawCloud(Graphics g, int x, int y, int width, int height)
        {
            g.FillEllipse(Brushes.White, x, y, width, height);
        }

        private void DrawCloudDetail(Graphics g, int x, int y, int width, int height)
        {
            g.FillEllipse(Brushes.White, x, y, width, height);
            g.DrawEllipse(new Pen(Color.FromArgb(220, 220, 220)), x, y, width, height);
        }

        private void HandleButtonClick(string buttonText)
        {
            switch (buttonText)
            {
                case "Новая игра":
                    this.Hide();
                    MainForm gameForm = new MainForm();
                    gameForm.ShowDialog();
                    this.Close();
                    break;
                case "Продолжить игру":
                    MessageBox.Show("Функция 'Продолжить игру' пока не реализована.", "В разработке", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "Настройки":
                    SettingsForm settings = new SettingsForm();
                    settings.ShowDialog();
                    break;
                case "Авторы":
                    AutorForm autorForm = new AutorForm();
                    autorForm.ShowDialog();
                    break;
                case "Выход":
                    Application.Exit();
                    break;
            }
        }
    }
}