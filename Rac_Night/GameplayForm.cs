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
        private Timer _mainGameTimer;
        private Timer _ghostTimer;
        private bool _isDeathSequence = false;
        private float _originalVolume = 0.5f;
        private bool _isFlashlightActive = false;
        private bool _isBlinded = false;
        private bool _isGhostActive = false;
        private DateTime _flashlightStartTime;
        private int _ghostFlashlightCounter = 0;
        private Random _random = new Random();
        private GhostType _currentGhostType;
        private bool _isFlashlightDisabledByGhost = false;
        private bool _isControlsDisabled = false;
        private bool _wasMusicPlaying = false;
        private enum GhostType { Black, Brown, White }
        private Dictionary<GhostType, string> _ghostResources;
        private Dictionary<GhostType, string> _ghostWarnings;
        private bool _nightEnded = false;
        private bool _dangerTimeActive = false;
        private float _currentGhostSpawnChance = 60f;
        private DateTime _lastHourNotification = DateTime.MinValue;
        public event EventHandler GameEnded;
        private SoundPlayer _ambientPlayer;
        private bool _isAmbientPlaying = false;
        private float _ambientVolume = 0.3f;
        private int _activeGhostsCount = 0;

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
            InitializeAmbient();

            StopMenuMusicIfNeeded();

            _tamagochiScreen = new PcTamagochiForm();
            _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;

            GameManager.Instance.GameTimeUpdated += GameManager_GameTimeUpdated;
            GameManager.Instance.NightEnded += GameManager_NightEnded;

            StartMainGameTimer();
            StartGhostTimer();

            _mainGameTimer.Stop();
            _ghostTimer.Stop();

            ShowTemporaryMessage("ИГРА НАЧНЕТСЯ ПОСЛЕ ИНСТРУКЦИИ...", Color.Yellow, 3000);
        }

        private void InitializeAmbient()
        {
            try
            {
                object ambientResource = Properties.Resources.ResourceManager.GetObject("ночной_эмбиент");

                if (ambientResource == null)
                {
                    ambientResource = Properties.Resources.ResourceManager.GetObject("ambient_night");
                }
                if (ambientResource == null)
                {
                    ambientResource = Properties.Resources.ResourceManager.GetObject("ambient");
                }
                if (ambientResource == null)
                {
                    ambientResource = Properties.Resources.ResourceManager.GetObject("night_ambient");
                }

                if (ambientResource != null)
                {
                    if (ambientResource is byte[])
                    {
                        using (var ms = new System.IO.MemoryStream((byte[])ambientResource))
                        {
                            _ambientPlayer = new SoundPlayer(ms);
                        }
                    }
                    else if (ambientResource is System.IO.UnmanagedMemoryStream)
                    {
                        _ambientPlayer = new SoundPlayer((System.IO.UnmanagedMemoryStream)ambientResource);
                    }
                    else
                    {
                        _ambientPlayer = new SoundPlayer(Properties.Resources.ночной_эмбиент);
                    }
                }
                else
                {
                    CreateTestAmbient();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось загрузить эмбиент: {ex.Message}");
                CreateTestAmbient();
            }
        }

        private void CreateTestAmbient()
        {
            try
            {
                System.IO.MemoryStream testStream = new System.IO.MemoryStream();
                System.IO.BinaryWriter writer = new System.IO.BinaryWriter(testStream);

                int sampleRate = 44100;
                int duration = 10;
                int numSamples = sampleRate * duration;

                writer.Write(0x46464952);
                writer.Write(36 + numSamples * 2);
                writer.Write(0x45564157);
                writer.Write(0x20746D66);
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(0x61746164);
                writer.Write(numSamples * 2);

                double frequency = 50.0;
                for (int i = 0; i < numSamples; i++)
                {
                    double time = (double)i / sampleRate;
                    double amplitude = Math.Sin(2 * Math.PI * frequency * time) * 0.1;
                    short sample = (short)(amplitude * 32767);
                    writer.Write(sample);
                }

                testStream.Position = 0;
                _ambientPlayer = new SoundPlayer(testStream);
            }
            catch { }
        }

        private void StopMenuMusicIfNeeded()
        {
            try
            {
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
                if (!GameManager.Instance.IsGameActive) return;
                CheckGameConditions();
                UpdateFlashlight();
                UpdateTimeEffects();
            };
        }

        private void UpdateTimeEffects()
        {
            if (_nightEnded || !GameManager.Instance.IsGameActive) return;

            int hour = GameManager.Instance.CurrentGameTime.Hours;

            if (_ghostTimer != null)
            {
                float ghostChance = GameManager.Instance.GetGhostSpawnChance(hour);
                int ghostInterval = GameManager.Instance.GetGhostInterval(hour);

                _ghostTimer.Interval = Math.Max(8000, ghostInterval);
                _currentGhostSpawnChance = ghostChance;

                Debug.WriteLine($"Ночь {GameManager.Instance.CurrentNight}: Шанс призрака {ghostChance}%, Интервал {ghostInterval}мс");
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

            if (_tamagochiScreen != null && _tamagochiScreen.Visible)
            {
                _tamagochiScreen.UpdateUI();
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

            try
            {
                if (GameManager.Instance == null)
                {
                    MessageBox.Show("GameManager не инициализирован!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (GameManager.Instance.CurrentTamagotchi == null)
                {
                    MessageBox.Show("Tamagotchi не инициализирован!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!GameManager.Instance.IsGameActive)
                {
                    GameManager.Instance.StartGame();
                }

                if (_mainGameTimer == null)
                {
                    StartMainGameTimer();
                }

                if (_ghostTimer == null)
                {
                    StartGhostTimer();
                }

                StartAmbient();

                if (!_mainGameTimer.Enabled)
                {
                    _mainGameTimer.Start();
                }

                if (!_ghostTimer.Enabled)
                {
                    _ghostTimer.Start();
                }

                UpdateGameTimeDisplay();

                Debug.WriteLine("Таймеры игры успешно запущены!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске таймеров: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"Ошибка StartGameTimers: {ex.Message}");
            }
        }

        private void StartAmbient()
        {
            if (_ambientPlayer != null && !_isAmbientPlaying)
            {
                try
                {
                    float originalVolume = VolumeManager.CurrentVolume;
                    VolumeManager.CurrentVolume = _ambientVolume;
                    _ambientPlayer.PlayLooping();
                    _isAmbientPlaying = true;
                    VolumeManager.CurrentVolume = originalVolume;
                    Debug.WriteLine("Эмбиент запущен");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка запуска эмбиента: {ex.Message}");
                }
            }
        }

        private void StopAmbient()
        {
            if (_ambientPlayer != null && _isAmbientPlaying)
            {
                try
                {
                    _ambientPlayer.Stop();
                    _isAmbientPlaying = false;
                    Debug.WriteLine("Эмбиент остановлен");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка остановки эмбиента: {ex.Message}");
                }
            }
        }

        private void AdjustAmbientVolume(float volume)
        {
            _ambientVolume = Math.Max(0.0f, Math.Min(1.0f, volume));

            if (_isAmbientPlaying)
            {
                float originalVolume = VolumeManager.CurrentVolume;
                VolumeManager.CurrentVolume = _ambientVolume;
                _ambientPlayer.Stop();
                System.Threading.Thread.Sleep(100);
                _ambientPlayer.PlayLooping();
                VolumeManager.CurrentVolume = originalVolume;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!GameManager.Instance.IsGameActive)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    this.Close();
                }
                return;
            }

            if (_isControlsDisabled || _isBlinded) return;

            if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
            {
                AdjustAmbientVolume(_ambientVolume + 0.1f);
                ShowTemporaryMessage($"Громкость эмбиента: {(int)(_ambientVolume * 100)}%", Color.LightBlue, 1000);
                return;
            }

            if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            {
                AdjustAmbientVolume(_ambientVolume - 0.1f);
                ShowTemporaryMessage($"Громкость эмбиента: {(int)(_ambientVolume * 100)}%", Color.LightBlue, 1000);
                return;
            }

            if (e.KeyCode == Keys.W)
            {
                if (!_tamagochiScreen.Visible)
                {
                    ForceDeactivateFlashlight();
                    ShowTamagochiScreen();
                }
                else
                {
                    ForceDeactivateFlashlight();
                    HideTamagochiScreen();
                }
            }

            if (e.KeyCode == Keys.F && !_isFlashlightActive && !_isFlashlightDisabledByGhost)
            {
                ActivateFlashlight();
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
            if (_tamagochiScreen != null && _tamagochiScreen.Visible)
            {
                return;
            }

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
            if (_flashlightPicture != null)
            {
                _flashlightPicture.Visible = false;
                if (_flashlightPicture.Image != null)
                {
                    _flashlightPicture.Image.Dispose();
                    _flashlightPicture.Image = null;
                }
            }
            _ghostFlashlightCounter = 0;
        }

        private void UpdateFlashlight()
        {
            if (_tamagochiScreen != null && _tamagochiScreen.Visible)
            {
                ForceDeactivateFlashlight();
                return;
            }

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
                !GameManager.Instance.IsNightTime || _isDeathSequence ||
                _isGhostActive) return;

            Array ghostTypes = Enum.GetValues(typeof(GhostType));
            _currentGhostType = (GhostType)ghostTypes.GetValue(_random.Next(ghostTypes.Length));

            _isGhostActive = true;
            _activeGhostsCount++;
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
            float ghostPowerMultiplier = GameManager.Instance.IsDangerTime ? 1.5f : 1.0f;
            ghostPowerMultiplier *= GameManager.Instance.DifficultyMultiplier;

            Timer ghostDespawnTimer = new Timer();
            ghostDespawnTimer.Interval = baseDespawnTime;
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

            if (_ghostWarnings.ContainsKey(_currentGhostType))
            {
                ShowTemporaryMessage(_ghostWarnings[_currentGhostType], Color.Red, 2000);
            }

            Debug.WriteLine($"Призрак спавн! Тип: {_currentGhostType}, Активных: {_activeGhostsCount}");
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
            _activeGhostsCount = Math.Max(0, _activeGhostsCount - 1);
        }

        private void ExecuteGhostAttack(GhostType ghostType, float powerMultiplier = 1.0f)
        {
            float difficultyMultiplier = GameManager.Instance.DifficultyMultiplier;
            float finalMultiplier = powerMultiplier * difficultyMultiplier;

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
                        int blindDuration = (int)(4000 * finalMultiplier);
                        _ = BlindPlayer(blindDuration);
                    }
                    break;

                case GhostType.White:
                    if (_tamagochiScreen.Visible)
                    {
                        int restartDuration = (int)(5000 * finalMultiplier);
                        _tamagochiScreen.MonitorRestart(restartDuration);
                        string restartText = GameManager.Instance.IsDangerTime ?
                            "МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ ДОЛЬШЕ!" :
                            "МОНИТОР ПЕРЕЗАГРУЖАЕТСЯ...";
                        ShowTemporaryMessage(restartText, Color.White, 2000);
                    }
                    else
                    {
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
            _isControlsDisabled = true;
            _blindOverlay.Visible = true;
            ForceDeactivateFlashlight();
            HideTamagochiScreen();
            ShowTemporaryMessage("ВЫ ОСЛЕПЛЕНЫ!", Color.White, 1000);

            await Task.Delay(durationMs);

            _blindOverlay.Visible = false;
            _isBlinded = false;
            _isControlsDisabled = false;
            ShowTemporaryMessage("ЗРЕНИЕ ВОССТАНОВЛЕНО", Color.Lime, 1000);
        }

        private async Task DisableFlashlight(int durationMs)
        {
            DeactivateFlashlight();
            _isFlashlightDisabledByGhost = true;
            ShowTemporaryMessage("ФОНАРИК ОТКЛЮЧЁН ПРИЗРАКОМ!", Color.Red, 2000);

            var originalInterval = _ghostTimer.Interval;
            _ghostTimer.Stop();

            await Task.Delay(durationMs);

            _isFlashlightDisabledByGhost = false;
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

            ForceDeactivateFlashlight();
            _tamagochiScreen.Show();
            _tamagochiScreen.Location = new Point(
                (this.Width - _tamagochiScreen.Width) / 2,
                (this.Height - _tamagochiScreen.Height) / 2);
            _tamagochiScreen.BringToFront();
        }

        private void HideTamagochiScreen()
        {
            ForceDeactivateFlashlight();
            _tamagochiScreen.Hide();
        }

        private void TamagochiScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            HideTamagochiScreen();
        }

        private void StopAllTimers()
        {
            StopAmbient();

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
            _isDeathSequence = true;
            StopAllTimers();
            GameManager.Instance.StopGame();

            if (_isGhostActive)
            {
                RemoveGhost();
            }

            HideTamagochiScreen();
            _isControlsDisabled = true;
            _isFlashlightActive = false;
            _isBlinded = false;
            DeactivateFlashlight();

            try
            {
                _originalVolume = VolumeManager.CurrentVolume;
                VolumeManager.CurrentVolume = 1.0f;
            }
            catch { }

            _timeLabel.Visible = false;

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

            await Task.Delay(5000);

            try
            {
                VolumeManager.CurrentVolume = _originalVolume;
            }
            catch { }

            this.Controls.Remove(rickRollPicture);
            if (rickRollPicture.Image != null)
            {
                rickRollPicture.Image.Dispose();
            }
            rickRollPicture.Dispose();

            MessageBox.Show($"ВЫ ПРОИГРАЛИ, ЕНОТИК ПОГИБ.\n\n" +
                $"Время выживания: {GameManager.Instance.CurrentGameTime:hh\\:mm}",
                "ПОРАЖЕНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Error);

            CloseAllFormsAndReturnToMenu();
        }

        private void CloseAllFormsAndReturnToMenu()
        {
            StopAllTimers();
            GameManager.Instance.StopGame();

            if (_tamagochiScreen != null && !_tamagochiScreen.IsDisposed)
            {
                _tamagochiScreen.Close();
                _tamagochiScreen.Dispose();
                _tamagochiScreen = null;
            }

            this.Close();

            if (!this.IsDisposed && this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(() =>
                {
                    MenuForm menuForm = new MenuForm();
                    menuForm.Show();
                }));
            }
        }

        private void ForceDeactivateFlashlight()
        {
            _isFlashlightActive = false;
            _ghostFlashlightCounter = 0;
            if (_flashlightPicture != null && _flashlightPicture.Visible)
            {
                _flashlightPicture.Visible = false;
                if (_flashlightPicture.Image != null)
                {
                    _flashlightPicture.Image.Dispose();
                    _flashlightPicture.Image = null;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            StopAmbient();

            if (_ambientPlayer != null)
            {
                _ambientPlayer.Dispose();
                _ambientPlayer = null;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameTimeUpdated -= GameManager_GameTimeUpdated;
                GameManager.Instance.NightEnded -= GameManager_NightEnded;
            }

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

            GameEnded?.Invoke(this, EventArgs.Empty);
        }
    }
}