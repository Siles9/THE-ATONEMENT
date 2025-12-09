using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class MenuForm : Form
    {
        private Random rnd = new Random();
        private List<Point> cloudPositions = new List<Point>();

        public MenuForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            this.Paint += MenuForm_Paint;
        }

        private void SetupEventHandlers()
        {
            btnNewGame.Click += (sender, e) => HandleButtonClick("Новая игра");
            btnContinue.Click += (sender, e) => HandleButtonClick("Продолжить игру");
            btnSettings.Click += (sender, e) => HandleButtonClick("Настройки");
            btnAuthors.Click += (sender, e) => HandleButtonClick("Авторы");
            btnExit.Click += (sender, e) => HandleButtonClick("Выход");
        }

        private void MenuForm_Paint(object sender, PaintEventArgs e)
        {
            cloudPositions.Clear();

            // Рандомные позиции облаков
            for (int i = 0; i < 3; i++)
            {
                int x = rnd.Next(300, this.Width - 300);
                int y = rnd.Next(200, this.Height - 200);

                // Проверка на перекрытие облаков
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

                    // Рисуем облачка для меню
                    DrawCloud(e.Graphics, x, y, 150, 100);
                    DrawCloud(e.Graphics, x + 50, y - 50, 150, 100);
                    DrawCloud(e.Graphics, x + 100, y, 150, 100);
                    DrawCloudDetail(e.Graphics, x + 20, y + 20, 50, 40);
                    DrawCloudDetail(e.Graphics, x + 100, y - 30, 40, 30);
                    DrawCloudDetail(e.Graphics, x + 180, y + 20, 50, 40);
                }
            }
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
                    StartNewGame();
                    break;

                case "Продолжить игру":
                    MessageBox.Show("Функция 'Продолжить игру' пока не реализована.",
                        "В разработке", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private async void StartNewGame()
        {
            // Скрываем меню
            this.Hide();

            // 1. Показываем катсцену (MainForm)
            MainForm cutsceneForm = new MainForm();
            cutsceneForm.ShowDialog();

            // 2. После завершения катсцены показываем инструкцию
            ShowGameInstructions();

            // 3. Запускаем основную игру
            GameplayForm gameplayForm = new GameplayForm();
            gameplayForm.ShowDialog();

            // 4. Закрываем игру
            this.Close();
        }

        private void ShowGameInstructions()
        {
            string instructions =
                "УПРАВЛЕНИЕ И ПРАВИЛА:\n\n" +
                "W - Открыть/Закрыть монитор енота\n" +
                "F - Зажать для включения фонарика\n" +
                "ESC - Выход из игры\n\n" +
                "ПРАВИЛА:\n" +
                "1. Следите за параметрами енота в мониторе\n" +
                "2. Призраки появляются ночью\n" +
                "3. Чтобы прогнать призрака - светите на него фонариком 2 секунды\n" +
                "4. Енот может заболеть если 2+ параметра упадут до 0\n" +
                "5. Цель: пережить ночь до 6 утра\n\n" +
                "6. Опастное время с 02:00 до 04:00" +
                "В это время призраки куда активнее)" +
                "УДАЧИ!";

            MessageBox.Show(instructions, "ИНСТРУКЦИЯ",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}