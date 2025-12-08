using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace Rac_Night
{
    public partial class PcTamagochiForm : Form
    {
        private Tamagotchi _tama;
        private PictureBox pictureBoxRaccoon;
        private ProgressBar progressBarHunger, progressBarPlay, progressBarHygiene, progressBarHealth;
        private Button btnFeed, btnPlay, btnWash, btnHeal;

        private Button btnCure;
        private Label lblSicknessTimer;
        private Label lblMedicines;
        private Panel _restartOverlay;

        private enum RaccoonActionState
        {
            None,
            Feeding,
            Playing,
            Washing,
            Healing
        }
        private RaccoonActionState _currentAction = RaccoonActionState.None;

        private System.Windows.Forms.Timer _actionAnimationTimer;
        private const int ActionAnimationDurationMs = 2000;

        public PcTamagochiForm()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(PcTamagochiForm_KeyDown);

            // Старый интерфейс
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1200, 800);
            this.Text = "Монитор енота";

            // Получаем экземпляр тамагочи
            _tama = GameManager.Instance.CurrentTamagotchi;

            // Инициализация элементов
            InitializeTamagochiControls();

            // Таймер для анимаций
            _actionAnimationTimer = new System.Windows.Forms.Timer();
            _actionAnimationTimer.Interval = ActionAnimationDurationMs;
            _actionAnimationTimer.Tick += ActionAnimationTimer_Tick;

            // Подписка на события
            _tama.StatsChanged += Tama_StatsChanged;
            _tama.SicknessStatusChanged += Tama_SicknessStatusChanged;
            GameManager.Instance.MedicinesUpdated += GameManager_MedicinesUpdated;
            GameManager.Instance.GameTimeUpdated += GameManager_GameTimeUpdated;

            // Оверлей для перезагрузки
            _restartOverlay = new Panel();
            _restartOverlay.Dock = DockStyle.Fill;
            _restartOverlay.BackColor = Color.FromArgb(180, 0, 0, 0);
            _restartOverlay.Visible = false;
            this.Controls.Add(_restartOverlay);
            _restartOverlay.BringToFront();

            // Первоначальное обновление UI
            UpdateUI();
            UpdateSicknessUI();
        }

        private void InitializeTamagochiControls()
        {
            this.SuspendLayout();

            // Прогресс-бары вверху
            int startX = 80;
            int startY = 50;
            int progressBarWidth = 250;
            int progressBarHeight = 25;
            int spacing = 30;

            progressBarHunger = CreateAndAddProgressBar("Кормление", new Point(startX, startY), progressBarWidth, progressBarHeight);
            progressBarPlay = CreateAndAddProgressBar("Игра", new Point(startX + progressBarWidth + spacing, startY), progressBarWidth, progressBarHeight);
            progressBarHygiene = CreateAndAddProgressBar("Чистота", new Point(startX + 2 * (progressBarWidth + spacing), startY), progressBarWidth, progressBarHeight);
            progressBarHealth = CreateAndAddProgressBar("Здоровье", new Point(startX + 3 * (progressBarWidth + spacing), startY), progressBarWidth, progressBarHeight);

            // Енот по центру
            pictureBoxRaccoon = new PictureBox();
            pictureBoxRaccoon.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxRaccoon.Size = new Size(400, 400);
            pictureBoxRaccoon.Location = new Point(
                (this.ClientSize.Width - pictureBoxRaccoon.Width) / 2,
                (this.ClientSize.Height - pictureBoxRaccoon.Height) / 2 - 30
            );
            pictureBoxRaccoon.BackColor = Color.Transparent;
            this.Controls.Add(pictureBoxRaccoon);

            // Кнопки действий внизу
            int buttonWidth = 150;
            int buttonHeight = 50;
            int buttonSpacing = 25;
            int totalButtonsWidth = buttonWidth * 4 + buttonSpacing * 3;
            int buttonsStartX = (this.ClientSize.Width - totalButtonsWidth) / 2;
            int buttonsStartY = this.ClientSize.Height - buttonHeight - 100;

            btnFeed = CreateAndAddButton("Покормить", new Point(buttonsStartX, buttonsStartY),
                buttonWidth, buttonHeight, btnFeed_Click);
            btnPlay = CreateAndAddButton("Поиграть", new Point(buttonsStartX + buttonWidth + buttonSpacing, buttonsStartY),
                buttonWidth, buttonHeight, btnPlay_Click);
            btnWash = CreateAndAddButton("Помыть", new Point(buttonsStartX + 2 * (buttonWidth + buttonSpacing), buttonsStartY),
                buttonWidth, buttonHeight, btnWash_Click);
            btnHeal = CreateAndAddButton("Подлечить", new Point(buttonsStartX + 3 * (buttonWidth + buttonSpacing), buttonsStartY),
                buttonWidth, buttonHeight, btnHeal_Click);

            // Кнопка лечения (всегда видна)
            btnCure = CreateAndAddButton("Лечить", new Point(this.ClientSize.Width - buttonWidth - 20, buttonsStartY),
                buttonWidth, buttonHeight, BtnCure_Click);
            btnCure.Enabled = false;

            // Лейбл лекарств
            lblMedicines = new Label();
            lblMedicines.Text = $"Лекарства: {GameManager.Instance.MedicinesLeft}";
            lblMedicines.Location = new Point(this.ClientSize.Width - 220, buttonsStartY - 40);
            lblMedicines.ForeColor = Color.Yellow;
            lblMedicines.Font = new Font("Arial", 12, FontStyle.Bold);
            lblMedicines.AutoSize = true;
            this.Controls.Add(lblMedicines);

            // Таймер болезни
            lblSicknessTimer = new Label();
            lblSicknessTimer.Text = "";
            lblSicknessTimer.Location = new Point(this.ClientSize.Width / 2 - 150, 100);
            lblSicknessTimer.Size = new Size(300, 40);
            lblSicknessTimer.ForeColor = Color.Red;
            lblSicknessTimer.Font = new Font("Arial", 24, FontStyle.Bold);
            lblSicknessTimer.TextAlign = ContentAlignment.MiddleCenter;
            lblSicknessTimer.Visible = false;
            this.Controls.Add(lblSicknessTimer);
            lblSicknessTimer.BringToFront();

            this.ResumeLayout(false);
        }

        private ProgressBar CreateAndAddProgressBar(string labelText, Point location, int width, int height)
        {
            // Метка
            Label lbl = new Label();
            lbl.Text = labelText;
            lbl.ForeColor = Color.Fuchsia;
            lbl.Location = new Point(location.X, location.Y);
            lbl.Font = new Font("Arial", 12, FontStyle.Bold);
            lbl.AutoSize = true;
            lbl.BackColor = Color.Transparent;
            this.Controls.Add(lbl);

            // Прогресс-бар
            ProgressBar pb = new ProgressBar();
            pb.Size = new Size(width, height);
            pb.Location = new Point(location.X, location.Y + lbl.Height + 8);
            pb.Maximum = 100;
            pb.Minimum = 0;
            pb.Value = 100;
            this.Controls.Add(pb);

            return pb;
        }

        private Button CreateAndAddButton(string text, Point location, int width, int height, EventHandler clickHandler)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(width, height);
            button.Location = location;
            button.Click += clickHandler;
            button.BackColor = Color.FromArgb(80, 80, 80);
            button.ForeColor = Color.White;
            button.Font = new Font("Arial", 12, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
            this.Controls.Add(button);
            return button;
        }

        private void Tama_StatsChanged(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(UpdateUI));
                else
                    UpdateUI();
            }
        }

        private void Tama_SicknessStatusChanged(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(UpdateSicknessUI));
                else
                    UpdateSicknessUI();
            }
        }

        private void GameManager_MedicinesUpdated(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(UpdateMedicinesUI));
                else
                    UpdateMedicinesUI();
            }
        }

        private void GameManager_GameTimeUpdated(object sender, EventArgs e)
        {
            if (this.IsHandleCreated && !this.IsDisposed && GameManager.Instance.IsSicknessTimerActive)
            {
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(UpdateSicknessTimerDisplay));
                else
                    UpdateSicknessTimerDisplay();
            }
        }

        private void UpdateUI()
        {
            try
            {
                if (progressBarHunger != null && !progressBarHunger.IsDisposed)
                    progressBarHunger.Value = (int)Math.Round(_tama.Hunger);

                if (progressBarPlay != null && !progressBarPlay.IsDisposed)
                    progressBarPlay.Value = (int)Math.Round(_tama.Play);

                if (progressBarHygiene != null && !progressBarHygiene.IsDisposed)
                    progressBarHygiene.Value = (int)Math.Round(_tama.Hygiene);

                if (progressBarHealth != null && !progressBarHealth.IsDisposed)
                    progressBarHealth.Value = (int)Math.Round(_tama.Health);

                UpdateRaccoonAnimation();
            }
            catch { }
        }

        private void UpdateSicknessUI()
        {
            try
            {
                if (btnCure == null || btnCure.IsDisposed) return;

                bool isSick = _tama.IsSick;
                bool hasMedicine = GameManager.Instance.MedicinesLeft > 0;

                btnCure.Enabled = isSick && hasMedicine;

                if (lblSicknessTimer != null && !lblSicknessTimer.IsDisposed)
                    lblSicknessTimer.Visible = isSick;

                UpdateMedicinesUI();
                UpdateSicknessTimerDisplay();
            }
            catch { }
        }

        private void UpdateMedicinesUI()
        {
            try
            {
                if (lblMedicines != null && !lblMedicines.IsDisposed)
                    lblMedicines.Text = $"Лекарства: {GameManager.Instance.MedicinesLeft}";
            }
            catch { }
        }

        private void UpdateSicknessTimerDisplay()
        {
            try
            {
                if (lblSicknessTimer != null && !lblSicknessTimer.IsDisposed)
                {
                    if (GameManager.Instance.IsSicknessTimerActive)
                    {
                        lblSicknessTimer.Text = "СРОЧНО ЛЕЧИТЬ ЕНОТА!";
                    }
                    else
                    {
                        lblSicknessTimer.Text = "";
                    }
                }
            }
            catch { }
        }

        // Обработчики кнопок
        private void btnFeed_Click(object sender, EventArgs e)
        {
            _tama.Feed(50);
            _currentAction = RaccoonActionState.Feeding;
            StartActionAnimationTimer();
            UpdateUI();
            GameManager.Instance.Save();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            _tama.PlayWith(40);
            _currentAction = RaccoonActionState.Playing;
            StartActionAnimationTimer();
            UpdateUI();
            GameManager.Instance.Save();
        }

        private void btnWash_Click(object sender, EventArgs e)
        {
            _tama.Wash(55);
            _currentAction = RaccoonActionState.Washing;
            StartActionAnimationTimer();
            UpdateUI();
            GameManager.Instance.Save();
        }

        private void btnHeal_Click(object sender, EventArgs e)
        {
            _tama.Heal(60);
            _currentAction = RaccoonActionState.Healing;
            StartActionAnimationTimer();
            UpdateUI();
            GameManager.Instance.Save();
        }

        private void BtnCure_Click(object sender, EventArgs e)
        {
            if (GameManager.Instance.MedicinesLeft <= 0)
            {
                MessageBox.Show("Нет лекарств!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!_tama.IsSick)
            {
                MessageBox.Show("Енот не болен!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (GameManager.Instance.TryUseMedicine())
            {
                MessageBox.Show("Енот вылечен! Параметры восстановлены до 30%.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateSicknessUI();
                UpdateUI();
            }
        }

        private void StartActionAnimationTimer()
        {
            _actionAnimationTimer.Stop();
            _actionAnimationTimer.Interval = ActionAnimationDurationMs;
            _actionAnimationTimer.Start();
        }

        private void ActionAnimationTimer_Tick(object sender, EventArgs e)
        {
            _actionAnimationTimer.Stop();
            _currentAction = RaccoonActionState.None;
            UpdateUI();
        }

        private void UpdateRaccoonAnimation()
        {
            try
            {
                if (pictureBoxRaccoon == null || pictureBoxRaccoon.IsDisposed) return;

                Image gif = null;

                switch (_currentAction)
                {
                    case RaccoonActionState.Feeding:
                        gif = TryLoadResourceImage("raccoon_feeding_anim");
                        break;
                    case RaccoonActionState.Playing:
                        gif = TryLoadResourceImage("raccoon_playing_anim");
                        break;
                    case RaccoonActionState.Washing:
                        gif = TryLoadResourceImage("raccoon_washing_anim");
                        break;
                    case RaccoonActionState.Healing:
                        gif = TryLoadResourceImage("raccoon_healing_anim");
                        break;
                    case RaccoonActionState.None:
                        if (_tama.Health < 30)
                        {
                            gif = TryLoadResourceImage("raccoon_sick");
                        }
                        else if (_tama.Hunger < 30)
                        {
                            gif = TryLoadResourceImage("raccoon_hungry");
                        }
                        else if (_tama.Hygiene < 30)
                        {
                            gif = TryLoadResourceImage("raccoon_dirty");
                        }
                        else if (_tama.Play < 40)
                        {
                            gif = TryLoadResourceImage("raccoon_bored");
                        }
                        else
                        {
                            gif = TryLoadResourceImage("raccoon_happy");
                        }
                        break;
                }

                if (gif != null && (pictureBoxRaccoon.Image == null || !pictureBoxRaccoon.Image.Equals(gif)))
                {
                    var oldImg = pictureBoxRaccoon.Image;
                    pictureBoxRaccoon.Image = gif;
                    oldImg?.Dispose();
                }
            }
            catch { }
        }

        private Image TryLoadResourceImage(string resourceName)
        {
            try
            {
                var resources = Properties.Resources.ResourceManager;
                return (Image)resources.GetObject(resourceName);
            }
            catch
            {
                return null;
            }
        }

        private void PcTamagochiForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S)
            {
                this.Hide();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }

        public async void MonitorRestart(int durationMs)
        {
            this.Enabled = false;
            _restartOverlay.Visible = true;

            Label restartLabel = new Label();
            restartLabel.Text = "ПЕРЕЗАГРУЗКА...";
            restartLabel.Font = new Font("Arial", 48, FontStyle.Bold);
            restartLabel.ForeColor = Color.Red;
            restartLabel.BackColor = Color.Transparent;
            restartLabel.AutoSize = true;
            restartLabel.Location = new Point(
                (_restartOverlay.Width - restartLabel.Width) / 2,
                (_restartOverlay.Height - restartLabel.Height) / 2);
            _restartOverlay.Controls.Add(restartLabel);

            await Task.Delay(durationMs);

            _restartOverlay.Controls.Clear();
            _restartOverlay.Visible = false;
            this.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Отписываемся от событий
            if (_tama != null)
            {
                _tama.StatsChanged -= Tama_StatsChanged;
                _tama.SicknessStatusChanged -= Tama_SicknessStatusChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MedicinesUpdated -= GameManager_MedicinesUpdated;
                GameManager.Instance.GameTimeUpdated -= GameManager_GameTimeUpdated;
            }

            // Останавливаем и освобождаем таймер
            if (_actionAnimationTimer != null)
            {
                _actionAnimationTimer.Stop();
                _actionAnimationTimer.Dispose();
            }

            // Освобождаем изображение
            if (pictureBoxRaccoon != null && pictureBoxRaccoon.Image != null)
            {
                pictureBoxRaccoon.Image.Dispose();
                pictureBoxRaccoon.Image = null;
            }

            base.OnFormClosing(e);
        }
    }
}