using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Rac_Night
{
    public partial class GameplayForm : Form
    {
        private PcTamagochiForm _tamagochiScreen;
        private Label _timeLabel;
        private Panel _blindOverlay;
        private PictureBox _currentGhost;
        private PictureBox _flashlightPicture; // PictureBox для фонарика

        // Таймеры
        private System.Windows.Forms.Timer _mainGameTimer;
        private System.Windows.Forms.Timer _ghostTimer;

        // Состояния
        private bool _isFlashlightActive = false;
        private bool _isBlinded = false;
        private bool _isGhostActive = false;
        private DateTime _flashlightStartTime;
        private int _ghostFlashlightCounter = 0;

        private Random _random = new Random();
        private GhostType _currentGhostType;

        private enum GhostType { Black, Brown, White }

        // Словари для соответствий
        private Dictionary<GhostType, string> _ghostResources;
        private Dictionary<GhostType, string> _ghostWarnings;

        public GameplayForm()
        {
            InitializeComponent();

            // Инициализируем словари
            InitializeDictionaries();

            // Показываем инструкции перед началом игры
            ShowInstructions();

            SetupFullscreenBorderless();
            InitializeGameUI();

            // Инициализируем форму тамагочи, но не показываем
            _tamagochiScreen = new PcTamagochiForm();
            _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;

            // Подписка на события GameManager
            GameManager.Instance.GameTimeUpdated += GameManager_GameTimeUpdated;

            // Запускаем таймеры
            StartMainGameTimer();
            StartGhostTimer();
        }

        private void InitializeDictionaries()
        {
            _ghostResources = new Dictionary<GhostType, string>
            {
                { GhostType.Black, "чёрный" },
                { GhostType.Brown, "коричневый" },
                { GhostType.White, "белый" }
            };

            _ghostWarnings = new Dictionary<GhostType, string>
            {
                { GhostType.Black, "ЧЁРНЫЙ ПРИЗРАК ПОЯВИЛСЯ!" },
                { GhostType.Brown, "КОРИЧНЕВЫЙ ПРИЗРАК ПОЯВИЛСЯ!" },
                { GhostType.White, "БЕЛЫЙ ПРИЗРАК ПОЯВИЛСЯ!" }
            };
        }

        private void ShowInstructions()
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
                "УДАЧИ!";

            MessageBox.Show(instructions, "ИНСТРУКЦИЯ",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetupFullscreenBorderless()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.Black;
            this.KeyPreview = true;
        }

        private void InitializeGameUI()
        {
            // Label для отображения времени
            _timeLabel = new Label();
            _timeLabel.Location = new Point(20, 20);
            _timeLabel.ForeColor = Color.Lime;
            _timeLabel.BackColor = Color.Transparent;
            _timeLabel.Font = new Font("Arial", 20, FontStyle.Bold);
            _timeLabel.AutoSize = true;
            this.Controls.Add(_timeLabel);

            // Оверлей для ослепления
            _blindOverlay = new Panel();
            _blindOverlay.Dock = DockStyle.Fill;
            _blindOverlay.BackColor = Color.Black;
            _blindOverlay.Visible = false;
            this.Controls.Add(_blindOverlay);
            _blindOverlay.BringToFront();

            // PictureBox для фонарика (изначально скрыт)
            _flashlightPicture = new PictureBox();
            _flashlightPicture.SizeMode = PictureBoxSizeMode.Zoom;
            _flashlightPicture.Size = new Size(400, 400);
            _flashlightPicture.BackColor = Color.Transparent;
            _flashlightPicture.Visible = false;
            this.Controls.Add(_flashlightPicture);
            _flashlightPicture.BringToFront();

            // Инструкция внизу экрана
            Label instructionLabel = new Label();
            instructionLabel.Text = "W - Монитор | F - Фонарик (зажать) | ESC - Выход";
            instructionLabel.Location = new Point(20, this.Height - 50);
            instructionLabel.ForeColor = Color.White;
            instructionLabel.Font = new Font("Arial", 12);
            instructionLabel.AutoSize = true;
            this.Controls.Add(instructionLabel);

            // Индикатор фонарика
            Label flashlightIndicator = new Label();
            flashlightIndicator.Name = "FlashlightIndicator";
            flashlightIndicator.Text = "ФОНАРИК: ВЫКЛ";
            flashlightIndicator.Location = new Point(this.Width - 250, 20);
            flashlightIndicator.ForeColor = Color.Gray;
            flashlightIndicator.Font = new Font("Arial", 14, FontStyle.Bold);
            flashlightIndicator.AutoSize = true;
            this.Controls.Add(flashlightIndicator);

            UpdateGameTimeDisplay();
        }

        private void StartMainGameTimer()
        {
            _mainGameTimer = new System.Windows.Forms.Timer();
            _mainGameTimer.Interval = 1000;
            _mainGameTimer.Tick += (s, e) => {
                CheckGameConditions();
                UpdateFlashlight();
            };
            _mainGameTimer.Start();
        }

        private void StartGhostTimer()
        {
            _ghostTimer = new System.Windows.Forms.Timer();
            _ghostTimer.Interval = 45000; // Призраки каждые 45 секунд
            _ghostTimer.Tick += (s, e) => {
                if (!_isGhostActive && _random.Next(0, 100) > 30) // 70% шанс появления
                {
                    SpawnGhost();
                }
            };
            _ghostTimer.Start();
        }

        private void GameManager_GameTimeUpdated(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateGameTimeDisplay));
            }
            else
            {
                UpdateGameTimeDisplay();
            }
        }

        private void UpdateGameTimeDisplay()
        {
            _timeLabel.Text = $"НОЧЬ: {GameManager.Instance.CurrentGameTime:hh\\:mm}";

            // Меняем цвет времени в зависимости от часа
            int hour = GameManager.Instance.CurrentGameTime.Hours;
            if (hour >= 0 && hour < 3)
                _timeLabel.ForeColor = Color.DarkRed;
            else if (hour >= 3 && hour < 6)
                _timeLabel.ForeColor = Color.Red;
            else
                _timeLabel.ForeColor = Color.Lime;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // W: Показать/Скрыть тамагочи
            if (e.KeyCode == Keys.W)
            {
                if (!_tamagochiScreen.Visible)
                {
                    ShowTamagochiScreen();
                }
                else
                {
                    HideTamagochiScreen();
                }
            }

            // F: Включить фонарик (зажать)
            if (e.KeyCode == Keys.F && !_isFlashlightActive)
            {
                ActivateFlashlight();
            }

            // Esc: Выход
            if (e.KeyCode == Keys.Escape)
            {
                var result = MessageBox.Show("Выйти из игры?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    this.Close();
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            // F: Выключить фонарик (отпустить)
            if (e.KeyCode == Keys.F && _isFlashlightActive)
            {
                DeactivateFlashlight();
            }
        }

        private void ActivateFlashlight()
        {
            _isFlashlightActive = true;
            _flashlightStartTime = DateTime.Now;

            // Загружаем изображение фонарика из ресурсов
            Image flashlightImage = LoadResourceImage("фонарик");
            if (flashlightImage != null)
            {
                _flashlightPicture.Image = flashlightImage;
                _flashlightPicture.Visible = true;

                // Позиционируем фонарик по центру экрана
                _flashlightPicture.Location = new Point(
                    (this.ClientSize.Width - _flashlightPicture.Width) / 2,
                    (this.ClientSize.Height - _flashlightPicture.Height) / 2
                );
            }

            // Обновляем индикатор
            UpdateFlashlightIndicator();

            // Если есть активный призрак, начинаем отсчет
            if (_isGhostActive && _currentGhost != null)
            {
                _ghostFlashlightCounter++;

                // Если светили 2 секунды на призрака
                if (_ghostFlashlightCounter >= 2)
                {
                    BanishGhost();
                }
            }
        }

        private void DeactivateFlashlight()
        {
            _isFlashlightActive = false;
            _flashlightPicture.Visible = false;
            _ghostFlashlightCounter = 0; // Сбрасываем счетчик

            // Очищаем изображение фонарика
            if (_flashlightPicture.Image != null)
            {
                _flashlightPicture.Image.Dispose();
                _flashlightPicture.Image = null;
            }

            // Обновляем индикатор
            UpdateFlashlightIndicator();
        }

        private void UpdateFlashlight()
        {
            if (!_isFlashlightActive) return;

            // Если есть активный призрак, проверяем время свечения
            if (_isGhostActive && _currentGhost != null)
            {
                TimeSpan flashlightTime = DateTime.Now - _flashlightStartTime;
                if (flashlightTime.TotalSeconds >= 2 && _ghostFlashlightCounter < 2)
                {
                    _ghostFlashlightCounter = 2;
                    BanishGhost();
                }
            }

            // Обновляем индикатор с временем работы
            TimeSpan elapsed = DateTime.Now - _flashlightStartTime;
            UpdateFlashlightIndicator(elapsed);
        }

        private void UpdateFlashlightIndicator(TimeSpan? time = null)
        {
            var indicator = this.Controls.Find("FlashlightIndicator", true);
            if (indicator.Length > 0 && indicator[0] is Label label)
            {
                if (_isFlashlightActive)
                {
                    string timeText = time.HasValue ? $" ({time.Value.Seconds}с)" : "";
                    label.Text = $"ФОНАРИК: ВКЛ{timeText}";
                    label.ForeColor = Color.Yellow;
                }
                else
                {
                    label.Text = "ФОНАРИК: ВЫКЛ";
                    label.ForeColor = Color.Gray;
                }
            }
        }

        private void SpawnGhost()
        {
            // Случайно выбираем тип призрака
            Array ghostTypes = Enum.GetValues(typeof(GhostType));
            _currentGhostType = (GhostType)ghostTypes.GetValue(_random.Next(ghostTypes.Length));

            _isGhostActive = true;
            _ghostFlashlightCounter = 0;

            // Загружаем изображение призрака из ресурсов
            string ghostColorName = GetGhostResourceName(_currentGhostType);
            Image ghostImage = LoadResourceImage("призрак_" + ghostColorName);

            _currentGhost = new PictureBox();
            _currentGhost.SizeMode = PictureBoxSizeMode.Zoom;
            _currentGhost.Size = new Size(300, 300);

            // Случайная позиция на экране (но не слишком близко к краям)
            int x = _random.Next(100, this.Width - 400);
            int y = _random.Next(100, this.Height - 400);
            _currentGhost.Location = new Point(x, y);

            _currentGhost.Image = ghostImage;
            _currentGhost.BackColor = Color.Transparent;
            this.Controls.Add(_currentGhost);
            _currentGhost.BringToFront();

            // Показываем предупреждение
            ShowGhostWarning(_currentGhostType);

            // Запускаем таймер для исчезновения призрака (если не прогнать)
            Timer ghostDespawnTimer = new Timer();
            ghostDespawnTimer.Interval = 10000; // 10 секунд
            ghostDespawnTimer.Tag = _currentGhostType;
            ghostDespawnTimer.Tick += (s, e) =>
            {
                if (_isGhostActive)
                {
                    ExecuteGhostAttack((GhostType)ghostDespawnTimer.Tag);
                    RemoveGhost();
                }
                ghostDespawnTimer.Stop();
                ghostDespawnTimer.Dispose();
            };
            ghostDespawnTimer.Start();
        }

        private string GetGhostResourceName(GhostType ghostType)
        {
            // Используем dictionary для получения имени ресурса
            if (_ghostResources.ContainsKey(ghostType))
            {
                return _ghostResources[ghostType];
            }
            return "чёрный"; // fallback
        }

        private Image LoadResourceImage(string resourceName)
        {
            // Пробуем загрузить из ресурсов
            try
            {
                object resource = Properties.Resources.ResourceManager.GetObject(resourceName);
                if (resource is Image image)
                {
                    return image;
                }

                // Пробуем разные варианты написания
                string[] variations = {
                    resourceName,
                    resourceName.ToLower(),
                    resourceName.ToUpper(),
                    resourceName.Replace("ё", "е"), // на случай "чёрный" vs "черный"
                    resourceName.Replace("_", ""),
                    resourceName.Replace(" ", "_")
                };

                foreach (var variation in variations)
                {
                    resource = Properties.Resources.ResourceManager.GetObject(variation);
                    if (resource is Image img)
                    {
                        return img;
                    }
                }
            }
            catch { }

            // Fallback: создаем цветное изображение
            return CreateFallbackImage(resourceName);
        }

        private Image CreateFallbackImage(string imageName)
        {
            Bitmap bmp = new Bitmap(300, 300);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                // Определяем цвет в зависимости от названия
                Color mainColor = Color.DarkGray;
                string displayText = imageName;

                if (imageName.Contains("фонарик") || imageName.Contains("flashlight"))
                {
                    mainColor = Color.Yellow;
                    displayText = "ФОНАРИК";

                    // Рисуем фонарик
                    g.FillRectangle(new SolidBrush(Color.DarkGray), 140, 100, 20, 150);
                    g.FillEllipse(new SolidBrush(Color.Yellow), 100, 70, 100, 100);
                    g.FillEllipse(new SolidBrush(Color.White), 120, 90, 60, 60);
                }
                else if (imageName.Contains("чёрный") || imageName.Contains("черный") || imageName.Contains("black"))
                {
                    mainColor = Color.Black;
                    displayText = "ПРИЗРАК";

                    // Рисуем призрака
                    g.FillEllipse(new SolidBrush(mainColor), 50, 50, 200, 150);
                    for (int i = 0; i < 5; i++)
                    {
                        g.FillEllipse(new SolidBrush(mainColor), 30 + i * 40, 180, 60, 40);
                    }
                    g.FillEllipse(Brushes.Red, 110, 100, 30, 40);
                    g.FillEllipse(Brushes.Red, 160, 100, 30, 40);
                }
                else if (imageName.Contains("коричневый") || imageName.Contains("brown"))
                {
                    mainColor = Color.SaddleBrown;
                    displayText = "ПРИЗРАК";

                    // Рисуем призрака
                    g.FillEllipse(new SolidBrush(mainColor), 50, 50, 200, 150);
                    for (int i = 0; i < 5; i++)
                    {
                        g.FillEllipse(new SolidBrush(mainColor), 30 + i * 40, 180, 60, 40);
                    }
                    g.FillEllipse(Brushes.Red, 110, 100, 30, 40);
                    g.FillEllipse(Brushes.Red, 160, 100, 30, 40);
                }
                else if (imageName.Contains("белый") || imageName.Contains("white"))
                {
                    mainColor = Color.WhiteSmoke;
                    displayText = "ПРИЗРАК";

                    // Рисуем призрака
                    g.FillEllipse(new SolidBrush(mainColor), 50, 50, 200, 150);
                    for (int i = 0; i < 5; i++)
                    {
                        g.FillEllipse(new SolidBrush(mainColor), 30 + i * 40, 180, 60, 40);
                    }
                    g.FillEllipse(Brushes.Red, 110, 100, 30, 40);
                    g.FillEllipse(Brushes.Red, 160, 100, 30, 40);
                }

                // Добавляем текст
                if (!imageName.Contains("фонарик"))
                {
                    g.DrawString(displayText, new Font("Arial", 16, FontStyle.Bold),
                        Brushes.White, 100, 230);
                }
            }
            return bmp;
        }

        private void ShowGhostWarning(GhostType ghostType)
        {
            // Используем dictionary для предупреждений
            string warning = "ПРИЗРАК ПОЯВИЛСЯ!"; // По умолчанию

            if (_ghostWarnings.ContainsKey(ghostType))
            {
                warning = _ghostWarnings[ghostType];
            }

            ShowTemporaryMessage(warning, Color.Red, 2000);
        }

        private void BanishGhost()
        {
            if (!_isGhostActive || _currentGhost == null) return;

            // Эффект изгнания
            ShowTemporaryMessage("ПРИЗРАК ИЗГНАН!", Color.Lime, 1500);

            // Анимация исчезновения
            Timer fadeTimer = new Timer();
            fadeTimer.Interval = 50;
            int alpha = 255;

            fadeTimer.Tick += (s, e) =>
            {
                if (_currentGhost != null && _currentGhost.Image != null)
                {
                    using (Bitmap bmp = new Bitmap(_currentGhost.Width, _currentGhost.Height))
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Transparent);

                        var matrix = new System.Drawing.Imaging.ColorMatrix();
                        matrix.Matrix33 = alpha / 255f;

                        var attributes = new System.Drawing.Imaging.ImageAttributes();
                        attributes.SetColorMatrix(matrix);

                        g.DrawImage(_currentGhost.Image,
                            new Rectangle(0, 0, _currentGhost.Width, _currentGhost.Height),
                            0, 0, _currentGhost.Image.Width, _currentGhost.Image.Height,
                            GraphicsUnit.Pixel, attributes);

                        _currentGhost.Image.Dispose();
                        _currentGhost.Image = (Image)bmp.Clone();
                    }
                }

                alpha -= 25;
                if (alpha <= 0)
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    RemoveGhost();
                }
            };
            fadeTimer.Start();
        }

        private void RemoveGhost()
        {
            if (_currentGhost != null)
            {
                this.Controls.Remove(_currentGhost);
                _currentGhost.Image?.Dispose();
                _currentGhost.Dispose();
                _currentGhost = null;
            }
            _isGhostActive = false;
            _ghostFlashlightCounter = 0;
        }

        private void ExecuteGhostAttack(GhostType ghostType)
        {
            // Используем классический switch statement
            switch (ghostType)
            {
                case GhostType.Black:
                    // Чёрный призрак: атакует монитор
                    GameManager.Instance.CurrentTamagotchi.DecreaseAllStatsByPercentage(20);
                    ShowTemporaryMessage("ЧЁРНЫЙ ПРИЗРАК АТАКОВАЛ ЕНОТА!", Color.DarkRed, 2000);
                    break;

                case GhostType.Brown:
                    // Коричневый призрак: ослепляет игрока
                    if (!_isBlinded)
                    {
                        _ = BlindPlayer(4000);
                    }
                    break;

                case GhostType.White:
                    // Белый призрак: в зависимости от состояния
                    if (_tamagochiScreen.Visible)
                    {
                        // В мониторе: перезагрузка
                        _tamagochiScreen.MonitorRestart(5000);
                        ShowTemporaryMessage("МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ!", Color.White, 2000);
                    }
                    else
                    {
                        // Не в мониторе: отключает фонарик
                        _ = DisableFlashlight(8000);
                    }
                    break;
            }
        }

        private void ShowTemporaryMessage(string message, Color color, int durationMs)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowTemporaryMessage(message, color, durationMs)));
                return;
            }

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.Font = new Font("Arial", 24, FontStyle.Bold);
            messageLabel.ForeColor = color;
            messageLabel.BackColor = Color.FromArgb(150, 0, 0, 0);
            messageLabel.AutoSize = false;
            messageLabel.Size = new Size(600, 80);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            messageLabel.Location = new Point(
                (this.Width - messageLabel.Width) / 2,
                100);

            this.Controls.Add(messageLabel);
            messageLabel.BringToFront();

            Timer removeTimer = new Timer();
            removeTimer.Interval = durationMs;
            removeTimer.Tick += (s, e) =>
            {
                this.Controls.Remove(messageLabel);
                messageLabel.Dispose();
                removeTimer.Stop();
                removeTimer.Dispose();
            };
            removeTimer.Start();
        }

        private void CheckGameConditions()
        {
            var tama = GameManager.Instance.CurrentTamagotchi;
            TimeSpan currentTime = GameManager.Instance.CurrentGameTime;

            // Проверка на победу (6:00 утра)
            if (currentTime.Hours >= 6 && currentTime.Hours < 12)
            {
                StopAllTimers();
                MessageBox.Show("ПЕРВАЯ НОЧЬ УСПЕШНО ПРОЙДЕНА!\nЕНОТ ЖИВ И ЗДОРОВ!", "ПОБЕДА",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                return;
            }

            // Проверка на проигрыш
            if (tama.Health <= 0)
            {
                StopAllTimers();
                MessageBox.Show("ВЫ ПРОИГРАЛИ, ЕНОТИК ПОГИБ.", "ПОРАЖЕНИЕ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            // Проверка на критическое состояние
            if (tama.IsSick && tama.Health < 15)
            {
                ShowTemporaryMessage("ЕНОТ В КРИТИЧЕСКОМ СОСТОЯНИИ!", Color.Red, 1000);
            }
        }

        private async Task BlindPlayer(int durationMs)
        {
            _isBlinded = true;
            _blindOverlay.Visible = true;
            ShowTemporaryMessage("ВЫ ОСЛЕПЛЕНЫ!", Color.White, 1000);

            await Task.Delay(durationMs);

            _blindOverlay.Visible = false;
            _isBlinded = false;
            ShowTemporaryMessage("ЗРЕНИЕ ВОССТАНОВЛЕНО", Color.Lime, 1000);
        }

        private async Task DisableFlashlight(int durationMs)
        {
            DeactivateFlashlight(); // Выключаем фонарик сразу

            ShowTemporaryMessage("ФОНАРИК ОТКЛЮЧЁН ПРИЗРАКОМ!", Color.Red, 2000);

            // Блокируем фонарик на время
            var originalInterval = _ghostTimer.Interval;
            _ghostTimer.Stop();

            await Task.Delay(durationMs);

            ShowTemporaryMessage("ФОНАРИК СНОВА ДОСТУПЕН", Color.Lime, 1500);

            // Возвращаем нормальный интервал появления призраков
            if (_ghostTimer != null && !_ghostTimer.Enabled)
            {
                _ghostTimer.Interval = originalInterval;
                _ghostTimer.Start();
            }
        }

        private void ShowTamagochiScreen()
        {
            if (_tamagochiScreen.IsDisposed)
            {
                _tamagochiScreen = new PcTamagochiForm();
                _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;
            }

            _tamagochiScreen.Show();
            _tamagochiScreen.Location = new Point(
                (this.Width - _tamagochiScreen.Width) / 2,
                (this.Height - _tamagochiScreen.Height) / 2);
            _tamagochiScreen.BringToFront();
        }

        private void HideTamagochiScreen()
        {
            _tamagochiScreen.Hide();
        }

        private void TamagochiScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            HideTamagochiScreen();
        }

        private void StopAllTimers()
        {
            if (_mainGameTimer != null)
            {
                _mainGameTimer.Stop();
                _mainGameTimer.Dispose();
            }

            if (_ghostTimer != null)
            {
                _ghostTimer.Stop();
                _ghostTimer.Dispose();
            }

            GameManager.Instance.StopGame();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Отписываемся от событий
            GameManager.Instance.GameTimeUpdated -= GameManager_GameTimeUpdated;

            StopAllTimers();

            // Очистка ресурсов
            if (_currentGhost != null)
            {
                _currentGhost.Image?.Dispose();
                _currentGhost.Dispose();
            }

            if (_flashlightPicture != null)
            {
                _flashlightPicture.Image?.Dispose();
                _flashlightPicture.Dispose();
            }

            if (_tamagochiScreen != null)
            {
                _tamagochiScreen.Close();
                _tamagochiScreen.Dispose();
            }
        }
    }
}