using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class GameplayForm : Form
    {
        private PcTamagochiForm _tamagochiScreen;
        private PictureBox _currentGhost;

        // Таймеры
        private Timer _mainGameTimer;
        private Timer _ghostTimer;

        // Состояния
        private bool _isDeathSequence = false;
        private float _originalVolume = 0.5f;
        private bool _isFlashlightActive = false;
        private bool _isBlinded = false;
        private bool _isGhostActive = false;
        private DateTime _flashlightStartTime;
        private int _ghostFlashlightCounter = 0;
        private Random _random = new Random();
        private GhostType _currentGhostType;

        // Новые флаги для блокировок
        private bool _isFlashlightDisabledByGhost = false;
        private bool _isControlsDisabled = false;
        private bool _wasMusicPlaying = false;

        private enum GhostType { Black, Brown, White }

        // Словари для соответствий
        private Dictionary<GhostType, string> _ghostResources;
        private Dictionary<GhostType, string> _ghostWarnings;

        // Переменные для механики времени
        private bool _nightEnded = false;
        private bool _dangerTimeActive = false;
        private float _currentGhostSpawnChance = 60f;
        private DateTime _lastHourNotification = DateTime.MinValue;
        public event EventHandler GameEnded;

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

            InitializeGhostDictionaries();

            _tamagochiScreen = new PcTamagochiForm();
            _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;
            GameManager.Instance.GameTimeUpdated += GameManager_GameTimeUpdated;
            GameManager.Instance.NightEnded += GameManager_NightEnded;

            StartMainGameTimer();
            StartGhostTimer();

            // Останавливаем таймеры до начала игры
            _mainGameTimer.Stop();
            _ghostTimer.Stop();


            // Показываем сообщение об ожидании начала
            ShowTemporaryMessage("ИГРА НАЧНЕТСЯ ПОСЛЕ ИНСТРУКЦИИ...", Color.Yellow, 3000);
        }

        private void StopMenuMusicIfNeeded()
        {
            // Останавливаем меню музыку, если она играет
            try
            {
                // Ищем MenuForm и останавливаем его музыку
                Form menuForm = Application.OpenForms["MenuForm"];
                if (menuForm is MenuForm)
                {
                    ((MenuForm)menuForm).StopMenuMusic();
                }
            }
            catch { }
        }
        private void InitializeGhostDictionaries()
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
            _mainGameTimer = new Timer();
            _mainGameTimer.Interval = 1000;
            _mainGameTimer.Tick += (s, e) =>
            {
                if (!GameManager.Instance.IsGameActive || _isDeathSequence) return;
                CheckGameConditions();
                UpdateFlashlight();
                UpdateTimeEffects();
            };
        }

        private void UpdateTimeEffects()
        {
            if (_nightEnded || !GameManager.Instance.IsGameActive) return;

            int hour = GameManager.Instance.CurrentGameTime.Hours;

            if (_ghostTimer != null && _ghostTimer.Enabled)
            {
                // Базовые интервалы для 1-й ночи
                int baseInterval;

                if (hour >= 0 && hour < 2)
                {
                    baseInterval = 30000;  // 30 секунд
                    _currentGhostSpawnChance = 60f;  // 60% шанс
                }
                else if (hour >= 2 && hour < 4)
                {
                    baseInterval = 25500;  // 25.5 секунд
                    _currentGhostSpawnChance = 75f;  // 75% шанс
                    if (!_dangerTimeActive)
                    {
                        _dangerTimeActive = true;
                    }
                }
                else if (hour >= 4 && hour < 6)
                {
                    baseInterval = 20000;  // 20 секунд
                    _currentGhostSpawnChance = 90f;  // 90% шанс
                    _dangerTimeActive = false;
                }
                else
                {
                    baseInterval = 60000;  // 1 минута (утро)
                    _currentGhostSpawnChance = 0f;  // 0% шанс
                }

                // Применяем множитель сложности для интервала (чем выше сложность, тем быстрее призраки)
                float difficultyMultiplier = GameManager.Instance.DifficultyMultiplier;
                int modifiedInterval = (int)(baseInterval / difficultyMultiplier);

                // Ограничиваем минимальный интервал 10 секундами
                _ghostTimer.Interval = Math.Max(10000, modifiedInterval);

                // Увеличиваем шанс появления с ростом сложности
                _currentGhostSpawnChance = Math.Min(100f, _currentGhostSpawnChance * difficultyMultiplier);
            }

            UpdateGameTimeDisplay();
        }

        private void StartGhostTimer()
        {
            _ghostTimer = new Timer();
            _ghostTimer.Interval = 60000;
            _ghostTimer.Tick += (s, e) =>
            {
                if (!_isGhostActive && !_nightEnded &&
                    GameManager.Instance.IsGameActive && !_isDeathSequence &&
                    GameManager.Instance.IsNightTime)
                {
                    float chance = _random.Next(0, 100);
                    if (chance > (100 - _currentGhostSpawnChance))
                    {
                        SpawnGhost();
                    }
                }
            };
        }

        private void UpdateGameTimeDisplay()
        {
            Color timeColor = GameManager.Instance.GetTimeColor();
            int remainingMinutes = GameManager.Instance.GetRemainingNightMinutes();

            _timeLabel.Text = $"НОЧЬ: {GameManager.Instance.CurrentGameTime:hh\\:mm}\n";

            _timeLabel.ForeColor = timeColor;

            if (GameManager.Instance.IsDangerTime)
            {
                _timeLabel.ForeColor = Color.Red;
            }

            if (!GameManager.Instance.IsGameActive)
            {
                _timeLabel.Text = "ОЖИДАНИЕ НАЧАЛА ИГРЫ...";
                _timeLabel.ForeColor = Color.Gray;
            }

            if (_tamagochiScreen != null && _tamagochiScreen.Visible)
            {
                if (_tamagochiScreen.IsHandleCreated && !_tamagochiScreen.IsDisposed)
                {
                    _tamagochiScreen.BeginInvoke(new Action(() =>
                    {
                        if (!_tamagochiScreen.IsDisposed)
                        {
                            _tamagochiScreen.Update();
                        }
                    }));
                }
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

        public void StartGameTimers()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(StartGameTimers));
                return;
            }

            // Запоминаем, что музыка меню играла
            _wasMusicPlaying = VolumeManager.CurrentVolume > 0;

            GameManager.Instance.StartGame();
            _mainGameTimer.Start();
            _ghostTimer.Start();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Блокируем управление, если игра не активна
            if (!GameManager.Instance.IsGameActive)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
                return;
            }

            // Блокируем управление если ослеплены или управление отключено
            if (_isControlsDisabled || _isBlinded) return;

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

            if (e.KeyCode == Keys.F && !_isFlashlightActive && !_isFlashlightDisabledByGhost)
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

            if (!GameManager.Instance.IsGameActive) return;

            if (e.KeyCode == Keys.F && _isFlashlightActive)
            {
                DeactivateFlashlight();
            }
        }

        private void ActivateFlashlight()
        {
            if (_isFlashlightDisabledByGhost) return;

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
            if (_nightEnded || !GameManager.Instance.IsGameActive ||
        !GameManager.Instance.IsNightTime || _isDeathSequence) return;

            Array ghostTypes = Enum.GetValues(typeof(GhostType));
            _currentGhostType = (GhostType)ghostTypes.GetValue(_random.Next(ghostTypes.Length));

            _isGhostActive = true;
            _ghostFlashlightCounter = 0;

            string ghostColorName = GetGhostResourceName(_currentGhostType);
            Image ghostImage = LoadResourceImage("призрак_" + ghostColorName);
            if (ghostImage == null)
            {
                ghostImage = CreateFallbackImage("призрак_" + ghostColorName);
            }

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

            int baseDespawnTime = GameManager.Instance.IsDangerTime ? 15000 : 10000;

            // Уменьшаем время до деспавна с ростом сложности
            float difficultyMultiplier = GameManager.Instance.DifficultyMultiplier;
            int despawnTime = (int)(baseDespawnTime / difficultyMultiplier);

            // Ограничиваем минимальное время деспавна 5 секундами
            despawnTime = Math.Max(5000, despawnTime);

            // Увеличиваем силу призрака с ростом сложности
            float ghostPowerMultiplier = GameManager.Instance.IsDangerTime ? 1.5f : 1.0f;
            ghostPowerMultiplier *= difficultyMultiplier;   

            Timer ghostDespawnTimer = new Timer();
            ghostDespawnTimer.Interval = despawnTime;
            ghostDespawnTimer.Tag = new GhostTimerData
            {
                GhostType = _currentGhostType,
                PowerMultiplier = ghostPowerMultiplier
            };

            ghostDespawnTimer.Tick += (s, e) =>
            {
                if (_isGhostActive && ghostDespawnTimer.Tag is GhostTimerData)
                {
                    GhostTimerData data = (GhostTimerData)ghostDespawnTimer.Tag;
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
            if (_ghostResources != null && _ghostResources.ContainsKey(ghostType))
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
                if (resource is Image)
                {
                    return (Image)resource;
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
                    if (resource is Image)
                    {
                        return (Image)resource;
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

        private void BanishGhost()
        {
            if (!_isGhostActive || _currentGhost == null) return;

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
                if (_currentGhost.Image != null)
                {
                    _currentGhost.Image.Dispose();
                }
                _currentGhost.Dispose();
                _currentGhost = null;
            }
            _isGhostActive = false;
            _ghostFlashlightCounter = 0;
        }

        private void ExecuteGhostAttack(GhostType ghostType, float powerMultiplier = 1.0f)
        {
            // Добавляем множитель сложности
            float difficultyMultiplier = GameManager.Instance.DifficultyMultiplier;
            float finalMultiplier = powerMultiplier * difficultyMultiplier;

            // Воспроизведение звука атаки призрака
            try
            {
                var sound = Properties.Resources.атака_призрака;
                if (sound != null)
                {
                    SoundPlayer player = new SoundPlayer(sound);
                    player.Play();
                }
            }
            catch { }

            switch (ghostType)
            {
                case GhostType.Black:
                    // Урон увеличивается с ростом сложности
                    float damagePercentage = 20 * finalMultiplier;
                    GameManager.Instance.CurrentTamagotchi.DecreaseAllStatsByPercentage((int)damagePercentage);

                    string damageText = GameManager.Instance.IsDangerTime ?
                        "ЧЁРНЫЙ ПРИЗРАК СИЛЬНО УДАРИЛ ЕНОТА!" :
                        "ЧЁРНЫЙ ПРИЗРАК УДАРИЛ ЕНОТА!";

                    ShowTemporaryMessage(damageText, Color.DarkRed, 2000);
                    break;

                case GhostType.Brown:
                    if (!_isBlinded)
                    {
                        // Длительность ослепления увеличивается с ростом сложности
                        int blindDuration = (int)(4000 * finalMultiplier);
                        _ = BlindPlayer(blindDuration);
                    }
                    break;

                case GhostType.White:
                    if (_tamagochiScreen.Visible)
                    {
                        // Длительность перезагрузки увеличивается с ростом сложности
                        int restartDuration = (int)(5000 * finalMultiplier);
                        _tamagochiScreen.MonitorRestart(restartDuration);
                        string restartText = GameManager.Instance.IsDangerTime ?
                            "МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ ДОЛЬШЕ!" :
                            "МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ...";
                        ShowTemporaryMessage(restartText, Color.White, 2000);
                    }
                    else
                    {
                        // Длительность отключения фонарика увеличивается с ростом сложности
                        int disableDuration = (int)(6000 * finalMultiplier);
                        _ = DisableFlashlight(disableDuration);
                    }
                    break;
            }
        }

        private void ShowTemporaryMessage(string message, Color color, int durationMs)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action(() => ShowTemporaryMessage(message, color, durationMs)));
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
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
                if (this.IsDisposed || !this.IsHandleCreated) return;

                this.Controls.Remove(messageLabel);
                messageLabel.Dispose();
                removeTimer.Stop();
                removeTimer.Dispose();
            };
            removeTimer.Start();
        }

        private void CheckGameConditions()
        {
            if (_nightEnded || !GameManager.Instance.IsGameActive || _isDeathSequence) return;

            var tama = GameManager.Instance.CurrentTamagotchi;

            if (GameManager.Instance.IsNightOver)
            {
                StopAllTimers();
                GameManager.Instance.StopGame();
                string healthStatus;
                if (tama.Health >= 70)
                    healthStatus = "ЕНОТ ЖИВ И ЗДОРОВ!";
                else if (tama.Health >= 40)
                    healthStatus = "ЕНОТ ВЫЖИЛ";
                else if (tama.Health >= 20)
                    healthStatus = "ЕНОТ ЕЛЕ ВЫЖИЛ";
                else
                    healthStatus = "ЕНОТ В КРИТИЧЕСКОМ СОСТОЯНИИ, НО ХОТЯ БЫ ЖИВ";

                MessageBox.Show($"НОЧЬ УСПЕШНО ПРОЙДЕНА!\n\n" +
                                $"Время выживания: {GameManager.Instance.CurrentGameTime:hh\\:mm}\n" +
                                $"{healthStatus}",
                                "ПОБЕДА", MessageBoxButtons.OK, MessageBoxIcon.Information);

                SaveGameProgress();

                CloseAllFormsAndReturnToMenu();
                return;
            }

            if (tama.Health <= 0 && !_isDeathSequence)
            {
                _isDeathSequence = true;
                StopAllTimers();
                GameManager.Instance.StopGame();

                // Запускаем последовательность рик-ролла
                ShowRickRollDeathSequence();
                return;
            }

            if (tama.IsSick && tama.Health < 15)
            {
                ShowTemporaryMessage("ЕНОТ В КРИТИЧЕСКОМ СОСТОЯНИИ!", Color.Red, 1000);
            }
        }
        private void SaveGameProgress()
        {
            try
            {
                int nightNumber = GameManager.Instance.CurrentNight;

                ProgressManager.CompleteNight(nightNumber);
                Debug.WriteLine($"Прогресс сохранен: ночь {nightNumber} пройдена");
                if (nightNumber == 6)
                {
                    Debug.WriteLine("Бонусная ночь пройдена! Звездочка 'Патрик' разблокирована!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения прогресса: {ex.Message}");
            }
        }
        private async Task BlindPlayer(int durationMs)
        {
            _isBlinded = true;
            _isControlsDisabled = true; // Блокируем все управление
            _blindOverlay.Visible = true;
            HideTamagochiScreen(); // Закрываем монитор если открыт
            DeactivateFlashlight(); // Выключаем фонарик если включен

            ShowTemporaryMessage("ВЫ ОСЛЕПЛЕНЫ!", Color.White, 1000);

            await Task.Delay(durationMs);

            _blindOverlay.Visible = false;
            _isBlinded = false;
            _isControlsDisabled = false; // Разблокируем управление
            ShowTemporaryMessage("ЗРЕНИЕ ВОССТАНОВЛЕНО", Color.Lime, 1000);
        }

        private async Task DisableFlashlight(int durationMs)
        {
            DeactivateFlashlight();
            _isFlashlightDisabledByGhost = true; // Блокируем фонарик
            ShowTemporaryMessage("ФОНАРИК ОТКЛЮЧЁН ПРИЗРАКОМ!", Color.Red, 2000);

            var originalInterval = _ghostTimer.Interval;
            _ghostTimer.Stop();

            await Task.Delay(durationMs);

            _isFlashlightDisabledByGhost = false; // Разблокируем фонарик
            ShowTemporaryMessage("ФОНАРИК СНОВА ДОСТУПЕН", Color.Lime, 1500);

            if (_ghostTimer != null && !_ghostTimer.Enabled)
            {
                _ghostTimer.Interval = originalInterval;
                _ghostTimer.Start();
            }
        }

        private void ShowTamagochiScreen()
        {
            if (_tamagochiScreen != null)
            {
                _tamagochiScreen.FormClosing -= TamagochiScreen_FormClosing;
                _tamagochiScreen.Close();
                _tamagochiScreen.Dispose();
                _tamagochiScreen = null;
            }

            _tamagochiScreen = new PcTamagochiForm();
            _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;
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
                _mainGameTimer = null;
            }

            if (_ghostTimer != null)
            {
                _ghostTimer.Stop();
                _ghostTimer.Dispose();
                _ghostTimer = null;
            }

            GameManager.Instance.StopGame();
        }
        private async void ShowRickRollDeathSequence()
        {
            // Устанавливаем флаг смерти
            _isDeathSequence = true;

            // Останавливаем ВСЕ таймеры сразу
            StopAllTimers();
            GameManager.Instance.StopGame();

            // Сбрасываем все состояния призраков
            if (_isGhostActive)
            {
                RemoveGhost();
            }

            // Закрываем форму монитора енота, если она открыта
            HideTamagochiScreen();

            // Блокируем все управление
            _isControlsDisabled = true;
            _isFlashlightActive = false;
            _isBlinded = false;

            // Выключаем фонарик если был включен
            DeactivateFlashlight();

            // Сохраняем текущую громкость и устанавливаем максимальную
            try
            {
                _originalVolume = VolumeManager.CurrentVolume;
                VolumeManager.CurrentVolume = 1.0f; // Максимальная громкость
            }
            catch { }

            // Скрываем все элементы UI
            _timeLabel.Visible = false;

            // Показываем картинку Рик-Ролл
            PictureBox rickRollPicture = new PictureBox();
            rickRollPicture.Dock = DockStyle.Fill;
            rickRollPicture.SizeMode = PictureBoxSizeMode.Zoom;
            rickRollPicture.BackColor = Color.Black;

            try
            {
                Image rickRollImage = Properties.Resources.Рик_Ролл;
                if (rickRollImage != null)
                {
                    rickRollPicture.Image = rickRollImage;
                }
                else
                {
                    throw new Exception("Image not found");
                }
            }
            catch
            {
                Bitmap fallback = new Bitmap(this.Width, this.Height);
                using (Graphics g = Graphics.FromImage(fallback))
                {
                    g.Clear(Color.Black);
                    g.DrawString("RICK ROLL",
                        new Font("Arial", 72, FontStyle.Bold),
                        Brushes.White,
                        new PointF(this.Width / 2 - 200, this.Height / 2 - 50));
                }
                rickRollPicture.Image = fallback;
            }

            this.Controls.Add(rickRollPicture);
            rickRollPicture.BringToFront();

            // Воспроизводим звук Рик-Ролл (4 секунды)
            try
            {
                var sound = Properties.Resources.Рик_Ролл_звук;
                if (sound != null)
                {
                    SoundPlayer rickRollSound = new SoundPlayer(sound);
                    rickRollSound.Play();
                }
            }
            catch { }

            // Ждем 5 секунд (звук 4 секунды + 1 секунда тишины)
            await Task.Delay(5000);

            // Восстанавливаем громкость
            try
            {
                VolumeManager.CurrentVolume = _originalVolume;
            }
            catch { }

            // Убираем картинку
            this.Controls.Remove(rickRollPicture);
            if (rickRollPicture.Image != null)
            {
                rickRollPicture.Image.Dispose();
            }
            rickRollPicture.Dispose();

            MessageBox.Show($"ВЫ ПРОИГРАЛИ, ЕНОТИК ПОГИБ.\n\n" +
                    $"Время выживания: {GameManager.Instance.CurrentGameTime:hh\\:mm}",
                    "ПОРАЖЕНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Закрываем все формы и возвращаемся в меню
            CloseAllFormsAndReturnToMenu();
        }

        private void CloseAllFormsAndReturnToMenu()
        {
            // Убеждаемся, что все таймеры остановлены
            StopAllTimers();
            GameManager.Instance.StopGame();

            // Закрываем форму монитора енота, если она открыта
            if (_tamagochiScreen != null && !_tamagochiScreen.IsDisposed)
            {
                _tamagochiScreen.Close();
                _tamagochiScreen.Dispose();
                _tamagochiScreen = null;
            }

            // Закрываем текущую форму (GameplayForm)
            this.Close();

            // Создаем и показываем новое главное меню
            // Важно использовать BeginInvoke, так как мы находимся в процессе закрытия формы
            if (!this.IsDisposed && this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() =>
                {
                    MenuForm menuForm = new MenuForm();
                    menuForm.Show();
                }));
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Отписываемся от событий GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameTimeUpdated -= GameManager_GameTimeUpdated;
                GameManager.Instance.NightEnded -= GameManager_NightEnded;
            }

            // Очищаем ресурсы
            if (_currentGhost != null)
            {
                if (_currentGhost.Image != null)
                {
                    _currentGhost.Image.Dispose();
                }
                _currentGhost.Dispose();
                _currentGhost = null;
            }

            if (_flashlightPicture != null)
            {
                if (_flashlightPicture.Image != null)
                {
                    _flashlightPicture.Image.Dispose();
                }
                _flashlightPicture.Dispose();
                _flashlightPicture = null;
            }

            // Вызываем событие окончания игры
            GameEnded?.Invoke(this, EventArgs.Empty);
        }
    }
}