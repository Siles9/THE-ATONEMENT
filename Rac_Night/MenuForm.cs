using System;
using System.Collections.Generic;
using System.Drawing;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class MenuForm : Form
    {
        private Random rnd = new Random();
        private List<Point> cloudPositions = new List<Point>();
        private SoundPlayer menuMusicPlayer;
        private PictureBox starPicture;
        private PictureBox patrickStarPicture; // Вторая звездочка "Патрик"

        public MenuForm()
        {
            InitializeComponent();

            // ВАЖНО: Разрешаем форме перехватывать события клавиатуры
            this.KeyPreview = true;

            SetupEventHandlers();
            this.Paint += MenuForm_Paint;

            // Загружаем прогресс
            ProgressManager.LoadProgress();

            // Инициализируем звездочки
            InitializeStars();

            // Обновляем доступность кнопки "Продолжить игру"
            UpdateContinueButton();
        }

        private void SetupEventHandlers()
        {
            btnNewGame.Click += (sender, e) => HandleButtonClick("Новая игра");
            btnContinue.Click += (sender, e) => HandleButtonClick("Продолжить игру");
            btnSettings.Click += (sender, e) => HandleButtonClick("Настройки");
            btnAuthors.Click += (sender, e) => HandleButtonClick("Авторы");
            btnExit.Click += (sender, e) => HandleButtonClick("Выход");

            // Также подписываемся на KeyDown у самой формы
            this.KeyDown += MenuForm_KeyDown;
        }

        private void MenuForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем обе комбинации
            if (e.Control && e.Shift && e.KeyCode == Keys.Z)
            {
                OpenDeveloperMenu();
                e.Handled = true; // Помечаем как обработанное
                e.SuppressKeyPress = true; // Подавляем дальнейшую обработку
            }

            // Русская буква "ъ" обычно Key.Oem6 (клавиша ]) с Shift
            if (e.KeyCode == Keys.Oem6 && e.Shift)
            {
                OpenDeveloperMenu();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }

            // Альтернативная клавиша для "ъ" - Oemtilde (клавиша `/~)
            if (e.KeyCode == Keys.Oemtilde && e.Shift)
            {
                OpenDeveloperMenu();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void MenuForm_Paint(object sender, PaintEventArgs e)
        {
            cloudPositions.Clear();
            for (int i = 0; i < 3; i++)
            {
                int x = rnd.Next(300, this.Width - 300);
                int y = rnd.Next(200, this.Height - 200);
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

        private void InitializeStars()
        {
            // Первая звездочка (за 1-ю ночь)
            InitializeFirstStar();

            // Вторая звездочка "Патрик" (за 6-ю ночь)
            InitializePatrickStar();
        }

        private void InitializeFirstStar()
        {
            starPicture = new PictureBox();
            starPicture.Size = new Size(50, 50);
            starPicture.Location = new Point(this.Width - 70, 20);
            starPicture.SizeMode = PictureBoxSizeMode.Zoom;
            starPicture.Visible = ProgressManager.Progress.IsFirstNightCompleted;

            try
            {
                object starResource = Properties.Resources.ResourceManager.GetObject("звезда");
                if (starResource is Image)
                {
                    starPicture.Image = (Image)starResource;
                }
                else
                {
                    CreateFallbackStar(starPicture, Color.Gold);
                }
            }
            catch
            {
                CreateFallbackStar(starPicture, Color.Gold);
            }

            this.Controls.Add(starPicture);
            starPicture.BringToFront();
        }

        private void InitializePatrickStar()
        {
            patrickStarPicture = new PictureBox();
            patrickStarPicture.Size = new Size(60, 60); // Немного больше
            patrickStarPicture.Location = new Point(this.Width - 70, 80); // Под первой звездой
            patrickStarPicture.SizeMode = PictureBoxSizeMode.Zoom;
            patrickStarPicture.Visible = ProgressManager.Progress.BonusNightCompleted;

            try
            {
                // Пробуем загрузить специальную звезду "Патрик"
                object patrickResource = Properties.Resources.ResourceManager.GetObject("патрик_звезда");
                if (patrickResource is Image)
                {
                    patrickStarPicture.Image = (Image)patrickResource;
                }
                else
                {
                    // Если нет специальной, создаем звезду Патрика
                    CreatePatrickStar();
                }
            }
            catch
            {
                CreatePatrickStar();
            }

            this.Controls.Add(patrickStarPicture);
            patrickStarPicture.BringToFront();

            // Добавляем подсказку при наведении
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(patrickStarPicture, "Патрик - за прохождение бонусной ночи!");
        }

        private void CreateFallbackStar(PictureBox pictureBox, Color color)
        {
            Bitmap star = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (Graphics g = Graphics.FromImage(star))
            {
                g.Clear(Color.Transparent);
                PointF[] starPoints = {
                    new PointF(pictureBox.Width/2, 5),
                    new PointF(pictureBox.Width/2 + 5, 20),
                    new PointF(pictureBox.Width - 5, 20),
                    new PointF(pictureBox.Width/2 + 8, 30),
                    new PointF(pictureBox.Width/2 + 13, pictureBox.Height - 5),
                    new PointF(pictureBox.Width/2, pictureBox.Height - 15),
                    new PointF(pictureBox.Width/2 - 13, pictureBox.Height - 5),
                    new PointF(pictureBox.Width/2 - 8, 30),
                    new PointF(5, 20),
                    new PointF(pictureBox.Width/2 - 5, 20)
                };
                g.FillPolygon(new SolidBrush(color), starPoints);
            }
            pictureBox.Image = star;
        }

        private void CreatePatrickStar()
        {
            Bitmap patrick = new Bitmap(patrickStarPicture.Width, patrickStarPicture.Height);
            using (Graphics g = Graphics.FromImage(patrick))
            {
                g.Clear(Color.Transparent);

                // Рисуем звезду Патрика (розового цвета)
                PointF[] starPoints = {
                    new PointF(30, 5),
                    new PointF(35, 20),
                    new PointF(50, 20),
                    new PointF(38, 30),
                    new PointF(43, 45),
                    new PointF(30, 35),
                    new PointF(17, 45),
                    new PointF(22, 30),
                    new PointF(10, 20),
                    new PointF(25, 20)
                };
                g.FillPolygon(new SolidBrush(Color.HotPink), starPoints);

                // Рисуем лицо Патрика
                g.FillEllipse(Brushes.Pink, 25, 25, 10, 10); // Глаз
                g.FillEllipse(Brushes.Pink, 35, 25, 10, 10); // Глаз
                g.DrawArc(new Pen(Color.Black, 2), 25, 35, 20, 10, 0, 180); // Улыбка
            }
            patrickStarPicture.Image = patrick;
        }

        private void StartMenuMusic()
        {
            try
            {
                // Останавливаем предыдущую музыку если играет
                if (menuMusicPlayer != null)
                {
                    menuMusicPlayer.Stop();
                    menuMusicPlayer.Dispose();
                }

                // Пробуем загрузить музыку
                object musicResource = Properties.Resources.ResourceManager.GetObject("Меню_музыка");
                if (musicResource != null)
                {
                    if (musicResource is byte[])
                    {
                        using (var ms = new System.IO.MemoryStream((byte[])musicResource))
                        {
                            menuMusicPlayer = new SoundPlayer(ms);
                            menuMusicPlayer.PlayLooping();
                        }
                    }
                    else
                    {
                        menuMusicPlayer = new SoundPlayer(Properties.Resources.Меню_музыка);
                        menuMusicPlayer.PlayLooping();
                    }
                }
            }
            catch
            {
                // Если музыка не найдена, ничего не делаем
            }
        }

        private void UpdateContinueButton()
        {
            btnContinue.Enabled = ProgressManager.Progress.IsFirstNightCompleted;
        }

        private void HandleButtonClick(string buttonText)
        {
            switch (buttonText)
            {
                case "Новая игра":
                    StartNewGame(1, true);
                    break;
                case "Продолжить игру":
                    OpenNightSelection();
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

        private void OpenNightSelection()
        {
            using (NightSelectionForm nightForm = new NightSelectionForm())
            {
                if (nightForm.ShowDialog() == DialogResult.OK && nightForm.SelectedNight > 0)
                {
                    StartNewGame(nightForm.SelectedNight, false);
                }
            }
        }

        private async void StartNewGame(int nightNumber, bool showCutscene)
        {
            try
            {
                this.Hide();

                // Останавливаем музыку меню
                StopMenuMusic();

                if (showCutscene)
                {
                    using (MainForm cutsceneForm = new MainForm())
                    {
                        var cutsceneTask = Task.Run(() => cutsceneForm.ShowDialog());
                        await cutsceneTask;
                    }

                    await Task.Delay(2000);
                    ShowGameInstructions();
                }
                else
                {
                    int medicines = GetMedicinesForNight(nightNumber);
                    MessageBox.Show($"Начинается Ночь #{nightNumber}!\n\n" +
                                  $"Лекарств: {medicines}\n" +
                                  $"Удачи!",
                                  $"Ночь {nightNumber}",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                using (GameplayForm gameplayForm = new GameplayForm())
                {
                    var gameplayTask = Task.Run(() => gameplayForm.ShowDialog());
                    await Task.Delay(100);

                    GameManager.Instance.ResetGame(nightNumber);
                    gameplayForm.StartGameTimers();
                    await gameplayTask;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске игры: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // После закрытия GameplayForm показываем меню
                this.Show();

                // Обновляем звездочки и кнопку прогресс
                ProgressManager.LoadProgress();
                UpdateStarsVisibility();
                UpdateContinueButton();

                // ВКЛЮЧАЕМ МУЗЫКУ МЕНЮ
                StartMenuMusic();
            }
        }

        private void UpdateStarsVisibility()
        {
            // Обновляем видимость обеих звездочек
            if (starPicture != null)
            {
                starPicture.Visible = ProgressManager.Progress.IsFirstNightCompleted;
            }

            if (patrickStarPicture != null)
            {
                patrickStarPicture.Visible = ProgressManager.Progress.BonusNightCompleted;
            }
        }

        private int GetMedicinesForNight(int nightNumber)
        {
            switch (nightNumber)
            {
                case 1: return 3;
                case 2: return 3;
                case 3: return 2;
                case 4: return 1;
                case 5: return 0;
                case 6: return 3;
                default: return 3;
            }
        }

        private void ShowGameInstructions()
        {
            string instructions =
                "УПРАВЛЕНИЕ И ПРАВИЛА:\n\n" +
                "W|S - Открыть/Закрыть монитор енота\n" +
                "F - Зажать для включения фонарика\n" +
                "ESC - Выход из игры\n\n" +
                "ПРАВИЛА:\n" +
                "1. Следите за параметрами енота в мониторе\n" +
                "2. Призраки имеют особенные механики\n" +
                "3. Чтобы прогнать призрака - светите на него фонариком\n" +
                "4. Енот может заболеть если 2+ параметра упадут до 0\n" +
                "5. Цель:Выжить до 6 утра\n\n" +
                "УДАЧИ!";

            MessageBox.Show(instructions, "ИНСТРУКЦИЯ",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Запускаем музыку при показе формы
            StartMenuMusic();
        }

        public void StopMenuMusic()
        {
            if (menuMusicPlayer != null)
            {
                menuMusicPlayer.Stop();
                menuMusicPlayer.Dispose();
                menuMusicPlayer = null;
            }
        }

        private void OpenDeveloperMenu()
        {
            try
            {
                // Создаем и показываем меню разработчика
                DeveloperMenuForm devMenu = new DeveloperMenuForm();
                devMenu.ShowDialog();

                // После закрытия меню разработчика обновляем прогресс
                ProgressManager.LoadProgress();
                UpdateStarsVisibility();
                UpdateContinueButton();

                // Показываем сообщение об успешном обновлении
                MessageBox.Show("Прогресс обновлен!\n" +
                              $"Пройдено ночей: {ProgressManager.Progress.NightsCompleted}\n" +
                              $"Бонусная ночь: {(ProgressManager.Progress.BonusNightCompleted ? "пройдена" : "не пройдена")}",
                              "Информация",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии меню разработчика: {ex.Message}",
                              "Ошибка",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            StopMenuMusic();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Обновляем позицию звездочек при изменении размера
            if (starPicture != null)
            {
                starPicture.Location = new Point(this.Width - 70, 20);
            }
            if (patrickStarPicture != null)
            {
                patrickStarPicture.Location = new Point(this.Width - 70, 80);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            StopMenuMusic();
            this.Close();
        }
    }
}