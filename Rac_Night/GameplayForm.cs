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
        private PictureBox _currentGhost;

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

        // Переменные для механики времени
        private bool _nightEnded = false;
        private bool _dangerTimeActive = false;
        private float _currentGhostSpawnChance = 60f;
        private DateTime _lastHourNotification = DateTime.MinValue;

        // Структура для хранения данных призрака в таймере
        private class GhostTimerData
        {
            public GhostType GhostType { get; set; }
            public float PowerMultiplier { get; set; }
        }

        public GameplayForm()
        {
            InitializeComponent();

            SetupFullscreenBorderless();
            InitializeGameUI();

            _tamagochiScreen = new PcTamagochiForm();
            _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;

            GameManager.Instance.GameTimeUpdated += GameManager_GameTimeUpdated;

            StartMainGameTimer();
            StartGhostTimer();
        }

        private void GameManager_NightEnded(object sender, EventArgs e)
        {
            _nightEnded = true;
            if (_ghostTimer != null)
            {
                _ghostTimer.Stop();
            }
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
            _timeLabel.Location = new Point(20, 20);
            _timeLabel.ForeColor = Color.Lime;
            _timeLabel.BackColor = Color.Transparent;
            _timeLabel.Font = new Font("Arial", 20, FontStyle.Bold);
            _timeLabel.AutoSize = true;

            _blindOverlay.Dock = DockStyle.Fill;
            _blindOverlay.BackColor = Color.Black;
            _blindOverlay.Visible = false;
            this.Controls.Add(_blindOverlay);
            _blindOverlay.BringToFront();

            _flashlightPicture.SizeMode = PictureBoxSizeMode.Zoom;
            _flashlightPicture.Size = new Size(400, 400);
            _flashlightPicture.BackColor = Color.Transparent;
            _flashlightPicture.Visible = false;
            this.Controls.Add(_flashlightPicture);
            _flashlightPicture.BringToFront();

            UpdateGameTimeDisplay();
        }

        private void StartMainGameTimer()
        {
            _mainGameTimer = new System.Windows.Forms.Timer();
            _mainGameTimer.Interval = 1000;
            _mainGameTimer.Tick += (s, e) => {
                CheckGameConditions();
                UpdateFlashlight();
                UpdateTimeEffects();
            };
            _mainGameTimer.Start();
        }

        private void UpdateTimeEffects()
        {
            if (_nightEnded) return;

            int hour = GameManager.Instance.CurrentGameTime.Hours;

            if (_ghostTimer != null && _ghostTimer.Enabled)
            {
                if (hour >= 0 && hour < 2)
                {
                    _ghostTimer.Interval = 60000;
                    _currentGhostSpawnChance = 60f;
                }
                else if (hour >= 2 && hour < 4)
                {
                    _ghostTimer.Interval = 45000;
                    _currentGhostSpawnChance = 75f;

                    if (!_dangerTimeActive)
                    {
                        _dangerTimeActive = true;
                        ShowTemporaryMessage("ОПАСНОЕ ВРЕМЯ! ПРИЗРАКИ СТАЛИ СИЛЬНЕЕ!", Color.DarkRed, 3000);
                    }
                }
                else if (hour >= 4 && hour < 6)
                {
                    _ghostTimer.Interval = 30000;
                    _currentGhostSpawnChance = 85f;
                    _dangerTimeActive = false;
                }
            }

            CheckTimeWarnings();
            UpdateGameTimeDisplay();
        }

        private void CheckTimeWarnings()
        {
            int remainingMinutes = GameManager.Instance.GetRemainingNightMinutes();

            if (remainingMinutes <= 30 && remainingMinutes > 20)
            {
                ShowTemporaryMessage($"ДО УТРА ОСТАЛОСЬ {remainingMinutes} МИНУТ!", Color.Yellow, 2000);
            }
            else if (remainingMinutes <= 20 && remainingMinutes > 10)
            {
                ShowTemporaryMessage($"ОСТАЛОСЬ {remainingMinutes} МИНУТ! ПРОДЕРЖИТЕСЬ!", Color.Orange, 2000);
            }
            else if (remainingMinutes <= 10 && remainingMinutes > 5)
            {
                ShowTemporaryMessage($"ВСЕГО {remainingMinutes} МИНУТ ДО РАССВЕТА!", Color.Red, 2000);
            }
            else if (remainingMinutes <= 5 && remainingMinutes > 0)
            {
                ShowTemporaryMessage($"ПОСЛЕДНИЕ {remainingMinutes} МИНУТ! ЕЩЁ НЕМНОГО!", Color.DarkRed, 2000);
            }

            if ((DateTime.Now - _lastHourNotification).TotalSeconds > 30)
            {
                int hour = GameManager.Instance.CurrentGameTime.Hours;
                int minute = GameManager.Instance.CurrentGameTime.Minutes;

                if (minute == 0 && (hour == 1 || hour == 3 || hour == 5))
                {
                    string message = "";
                    if (hour == 1)
                        message = "ПРОШЁЛ ЧАС НОЧИ. ВСЕГО 5 ЧАСОВ ОСТАЛОСЬ.";
                    else if (hour == 3)
                        message = "ПОЛНОЧЬ. САМОЕ ОПАСНОЕ ВРЕМЯ!";
                    else if (hour == 5)
                        message = "СКОРО РАССВЕТ! ПОСЛЕДНИЙ ЧАС!";

                    if (!string.IsNullOrEmpty(message))
                    {
                        ShowTemporaryMessage(message, Color.White, 2500);
                        _lastHourNotification = DateTime.Now;
                    }
                }
            }
        }

        private void StartGhostTimer()
        {
            _ghostTimer = new System.Windows.Forms.Timer();
            _ghostTimer.Interval = 60000;
            _ghostTimer.Tick += (s, e) => {
                if (!_isGhostActive && !_nightEnded && GameManager.Instance.IsNightTime)
                {
                    float chance = _random.Next(0, 100);
                    if (chance > (100 - _currentGhostSpawnChance))
                    {
                        SpawnGhost();
                    }
                }
            };
            _ghostTimer.Start();
        }

        private void UpdateGameTimeDisplay()
        {
            string timeDescription = GameManager.Instance.GetTimeDescription();
            Color timeColor = GameManager.Instance.GetTimeColor();
            int remainingMinutes = GameManager.Instance.GetRemainingNightMinutes();

            _timeLabel.Text = $"НОЧЬ: {GameManager.Instance.CurrentGameTime:hh\\:mm}\n" +
                             $"{timeDescription} | До утра: {remainingMinutes} мин";
            _timeLabel.ForeColor = timeColor;

            if (GameManager.Instance.IsDangerTime)
            {
                _timeLabel.Text += "\n⚠ ОПАСНОЕ ВРЕМЯ!";
                _timeLabel.ForeColor = Color.Red;
            }
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

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

            if (e.KeyCode == Keys.F && !_isFlashlightActive)
            {
                ActivateFlashlight();
            }

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

            if (e.KeyCode == Keys.F && _isFlashlightActive)
            {
                DeactivateFlashlight();
            }
        }

        private void ActivateFlashlight()
        {
            _isFlashlightActive = true;
            _flashlightStartTime = DateTime.Now;

            Image flashlightImage = LoadResourceImage("фонарик");
            if (flashlightImage != null)
            {
                _flashlightPicture.Image = flashlightImage;
                _flashlightPicture.Visible = true;
                _flashlightPicture.Location = new Point(
                    (this.ClientSize.Width - _flashlightPicture.Width) / 2,
                    (this.ClientSize.Height - _flashlightPicture.Height) / 2
                );
            }

            if (_isGhostActive && _currentGhost != null)
            {
                _ghostFlashlightCounter++;
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
            _ghostFlashlightCounter = 0;

            if (_flashlightPicture.Image != null)
            {
                _flashlightPicture.Image.Dispose();
                _flashlightPicture.Image = null;
            }
        }

        private void UpdateFlashlight()
        {
            if (!_isFlashlightActive) return;

            if (_isGhostActive && _currentGhost != null)
            {
                TimeSpan flashlightTime = DateTime.Now - _flashlightStartTime;
                if (flashlightTime.TotalSeconds >= 2 && _ghostFlashlightCounter < 2)
                {
                    _ghostFlashlightCounter = 2;
                    BanishGhost();
                }
            }
        }

        private void SpawnGhost()
        {
            if (_nightEnded || !GameManager.Instance.IsNightTime) return;

            Array ghostTypes = Enum.GetValues(typeof(GhostType));
            _currentGhostType = (GhostType)ghostTypes.GetValue(_random.Next(ghostTypes.Length));

            _isGhostActive = true;
            _ghostFlashlightCounter = 0;

            string ghostColorName = GetGhostResourceName(_currentGhostType);
            Image ghostImage = LoadResourceImage("призрак_" + ghostColorName);

            _currentGhost = new PictureBox();
            _currentGhost.SizeMode = PictureBoxSizeMode.Zoom;
            _currentGhost.Size = new Size(300, 300);

            int x = _random.Next(100, this.Width - 400);
            int y = _random.Next(100, this.Height - 400);
            _currentGhost.Location = new Point(x, y);

            _currentGhost.Image = ghostImage;
            _currentGhost.BackColor = Color.Transparent;
            this.Controls.Add(_currentGhost);
            _currentGhost.BringToFront();

            ShowGhostWarning(_currentGhostType);

            int despawnTime = GameManager.Instance.IsDangerTime ? 15000 : 10000;
            float ghostPowerMultiplier = GameManager.Instance.IsDangerTime ? 1.5f : 1.0f;

            Timer ghostDespawnTimer = new Timer();
            ghostDespawnTimer.Interval = despawnTime;

            // Используем класс вместо кортежа
            ghostDespawnTimer.Tag = new GhostTimerData
            {
                GhostType = _currentGhostType,
                PowerMultiplier = ghostPowerMultiplier
            };

            ghostDespawnTimer.Tick += (s, e) =>
            {
                if (_isGhostActive && ghostDespawnTimer.Tag is GhostTimerData data)
                {
                    ExecuteGhostAttack(data.GhostType, data.PowerMultiplier);
                    RemoveGhost();
                }
                ghostDespawnTimer.Stop();
                ghostDespawnTimer.Dispose();
            };
            ghostDespawnTimer.Start();
        }

        private string GetGhostResourceName(GhostType ghostType)
        {
            if (_ghostResources.ContainsKey(ghostType))
            {
                return _ghostResources[ghostType];
            }
            return "чёрный";
        }

        private Image LoadResourceImage(string resourceName)
        {
            try
            {
                object resource = Properties.Resources.ResourceManager.GetObject(resourceName);
                if (resource is Image image)
                {
                    return image;
                }

                string[] variations = {
                    resourceName,
                    resourceName.ToLower(),
                    resourceName.ToUpper(),
                    resourceName.Replace("ё", "е"),
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

            return CreateFallbackImage(resourceName);
        }

        private Image CreateFallbackImage(string imageName)
        {
            Bitmap bmp = new Bitmap(300, 300);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                Color mainColor = Color.DarkGray;
                string displayText = imageName;

                if (imageName.Contains("фонарик") || imageName.Contains("flashlight"))
                {
                    mainColor = Color.Yellow;
                    displayText = "ФОНАРИК";

                    g.FillRectangle(new SolidBrush(Color.DarkGray), 140, 100, 20, 150);
                    g.FillEllipse(new SolidBrush(Color.Yellow), 100, 70, 100, 100);
                    g.FillEllipse(new SolidBrush(Color.White), 120, 90, 60, 60);
                }
                else if (imageName.Contains("чёрный") || imageName.Contains("черный") || imageName.Contains("black"))
                {
                    mainColor = Color.Black;
                    displayText = "ПРИЗРАК";

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

                    g.FillEllipse(new SolidBrush(mainColor), 50, 50, 200, 150);
                    for (int i = 0; i < 5; i++)
                    {
                        g.FillEllipse(new SolidBrush(mainColor), 30 + i * 40, 180, 60, 40);
                    }
                    g.FillEllipse(Brushes.Red, 110, 100, 30, 40);
                    g.FillEllipse(Brushes.Red, 160, 100, 30, 40);
                }

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
            string warning = "ПРИЗРАК ПОЯВИЛСЯ!";

            if (_ghostWarnings.ContainsKey(ghostType))
            {
                warning = _ghostWarnings[ghostType];
            }

            ShowTemporaryMessage(warning, Color.Red, 2000);
        }

        private void BanishGhost()
        {
            if (!_isGhostActive || _currentGhost == null) return;

            ShowTemporaryMessage("ПРИЗРАК ИЗГНАН!", Color.Lime, 1500);

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

        private void ExecuteGhostAttack(GhostType ghostType, float powerMultiplier = 1.0f)
        {
            switch (ghostType)
            {
                case GhostType.Black:
                    float damagePercentage = 20 * powerMultiplier;
                    GameManager.Instance.CurrentTamagotchi.DecreaseAllStatsByPercentage((int)damagePercentage);
                    string damageText = GameManager.Instance.IsDangerTime ?
                        "ЧЁРНЫЙ ПРИЗРАК СИЛЬНО АТАКОВАЛ ЕНОТА!" :
                        "ЧЁРНЫЙ ПРИЗРАК АТАКОВАЛ ЕНОТА!";
                    ShowTemporaryMessage(damageText, Color.DarkRed, 2000);
                    break;

                case GhostType.Brown:
                    if (!_isBlinded)
                    {
                        int blindDuration = (int)(4000 * powerMultiplier);
                        _ = BlindPlayer(blindDuration);
                    }
                    break;

                case GhostType.White:
                    if (_tamagochiScreen.Visible)
                    {
                        int restartDuration = (int)(5000 * powerMultiplier);
                        _tamagochiScreen.MonitorRestart(restartDuration);
                        string restartText = GameManager.Instance.IsDangerTime ?
                            "МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ ДОЛЬШЕ!" :
                            "МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ...";
                        ShowTemporaryMessage(restartText, Color.White, 2000);
                    }
                    else
                    {
                        int disableDuration = (int)(8000 * powerMultiplier);
                        _ = DisableFlashlight(disableDuration);
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
            if (_nightEnded) return;

            var tama = GameManager.Instance.CurrentTamagotchi;

            if (GameManager.Instance.IsNightOver)
            {
                StopAllTimers();
                GameManager.Instance.StopGame();

                string healthStatus;
                if (tama.Health >= 70)
                    healthStatus = "ЕНОТ ЖИВ И ЗДОРОВ!";
                else if (tama.Health >= 40)
                    healthStatus = "ЕНОТ ВЫЖИЛ, НО ЕМУ НУЖЕН ОТДЫХ";
                else if (tama.Health >= 20)
                    healthStatus = "ЕНОТ ЕЛЕ ВЫЖИЛ, НУЖНА ПОМОЩЬ";
                else
                    healthStatus = "ЕНОТ В КРИТИЧЕСКОМ СОСТОЯНИИ!";

                MessageBox.Show($"НОЧЬ УСПЕШНО ПРОЙДЕНА!\n\n" +
                               $"Время выживания: {GameManager.Instance.CurrentGameTime:hh\\:mm}\n" +
                               $"{healthStatus}",
                    "ПОБЕДА", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                return;
            }

            if (tama.Health <= 0)
            {
                StopAllTimers();
                GameManager.Instance.StopGame();

                MessageBox.Show($"ВЫ ПРОИГРАЛИ, ЕНОТИК ПОГИБ.\n\n" +
                               $"Время выживания: {GameManager.Instance.CurrentGameTime:hh\\:mm}",
                    "ПОРАЖЕНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

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
            DeactivateFlashlight();
            ShowTemporaryMessage("ФОНАРИК ОТКЛЮЧЁН ПРИЗРАКОМ!", Color.Red, 2000);

            var originalInterval = _ghostTimer.Interval;
            _ghostTimer.Stop();

            await Task.Delay(durationMs);

            ShowTemporaryMessage("ФОНАРИК СНОВА ДОСТУПЕН", Color.Lime, 1500);

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

            GameManager.Instance.GameTimeUpdated -= GameManager_GameTimeUpdated;
            GameManager.Instance.NightEnded -= GameManager_NightEnded;

            StopAllTimers();

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