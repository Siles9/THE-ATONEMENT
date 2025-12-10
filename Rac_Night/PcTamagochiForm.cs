using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class PcTamagochiForm : Form
    {
        private Tamagotchi _tama;

        // Элементы UI
        private PictureBox pictureBoxRaccoon;
        private ProgressBar progressBarHunger, progressBarPlay, progressBarHygiene, progressBarHealth;
        private Label lblHungerValue, lblPlayValue, lblHygieneValue, lblHealthValue;
        private Button btnFeed, btnPlay, btnWash, btnCure;
        private Label lblSicknessTimer;
        private Label lblMedicines;
        private Panel _restartOverlay;
        private Panel _backgroundPanel;

        private enum RaccoonActionState
        {
            None,
            Feeding,
            Playing,
            Washing,
            Healing
        }

        private RaccoonActionState _currentAction = RaccoonActionState.None;
        private Timer _actionAnimationTimer;
        private const int ActionAnimationDurationMs = 2000;

        public PcTamagochiForm()
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(PcTamagochiForm_KeyDown);

            _tama = GameManager.Instance.CurrentTamagotchi;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1600, 900);
            this.Text = "Монитор енота";
            this.BackColor = Color.FromArgb(20, 20, 20);

            _backgroundPanel = new Panel();
            _backgroundPanel.Dock = DockStyle.Fill;
            _backgroundPanel.BackColor = Color.FromArgb(15, 15, 25);
            this.Controls.Add(_backgroundPanel);

            InitializeTamagochiControls();

            _actionAnimationTimer = new Timer();
            _actionAnimationTimer.Interval = ActionAnimationDurationMs;
            _actionAnimationTimer.Tick += ActionAnimationTimer_Tick;

            _tama.StatsChanged += Tama_StatsChanged;
            _tama.SicknessStatusChanged += Tama_SicknessStatusChanged;
            GameManager.Instance.MedicinesUpdated += GameManager_MedicinesUpdated;

            _restartOverlay = new Panel();
            _restartOverlay.Dock = DockStyle.Fill;
            _restartOverlay.BackColor = Color.FromArgb(200, 0, 0, 0);
            _restartOverlay.Visible = false;
            _backgroundPanel.Controls.Add(_restartOverlay);
            _restartOverlay.BringToFront();

            UpdateUI();
            UpdateSicknessUI();
        }

        private void InitializeTamagochiControls()
        {
            int startX = 50;
            int startY = 100;
            int progressBarWidth = 300;
            int progressBarHeight = 30;
            int spacing = 40;

            // Кормление
            progressBarHunger = CreateProgressBarWithLabel("Желудок", new Point(startX, startY),
                progressBarWidth, progressBarHeight, Color.Orange);
            lblHungerValue = CreateValueLabel(new Point(startX + progressBarWidth + 10, startY + 5), "100%");

            // Игра
            progressBarPlay = CreateProgressBarWithLabel("Настроение", new Point(startX + progressBarWidth + spacing + 100, startY),
                progressBarWidth, progressBarHeight, Color.DodgerBlue);
            lblPlayValue = CreateValueLabel(new Point(startX + progressBarWidth + spacing + 100 + progressBarWidth + 10, startY + 5), "100%");

            // Чистота
            progressBarHygiene = CreateProgressBarWithLabel("Чистота", new Point(startX, startY + 100),
                progressBarWidth, progressBarHeight, Color.MediumAquamarine);
            lblHygieneValue = CreateValueLabel(new Point(startX + progressBarWidth + 10, startY + 100 + 5), "100%");

            // Здоровье
            progressBarHealth = CreateProgressBarWithLabel("Здоровье", new Point(startX + progressBarWidth + spacing + 100, startY + 100),
                progressBarWidth, progressBarHeight, Color.Crimson);
            lblHealthValue = CreateValueLabel(new Point(startX + progressBarWidth + spacing + 100 + progressBarWidth + 10, startY + 100 + 5), "100%");

            // Енот
            pictureBoxRaccoon = new PictureBox();
            pictureBoxRaccoon.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxRaccoon.Size = new Size(450, 450);
            pictureBoxRaccoon.Location = new Point(
                (_backgroundPanel.Width - pictureBoxRaccoon.Width) / 2,
                (_backgroundPanel.Height - pictureBoxRaccoon.Height) / 2
            );
            pictureBoxRaccoon.BackColor = Color.Transparent;
            _backgroundPanel.Controls.Add(pictureBoxRaccoon);

            // Кнопки
            int buttonWidth = 180;
            int buttonHeight = 55;
            int buttonSpacing = 25;
            int totalButtonsWidth = buttonWidth * 4 + buttonSpacing * 3;
            int buttonsStartX = (_backgroundPanel.Width - totalButtonsWidth) / 2;
            int buttonsStartY = _backgroundPanel.Height - buttonHeight - 100;

            btnFeed = CreateAndAddButton("ПОКОРМИТЬ", new Point(buttonsStartX, buttonsStartY),
                buttonWidth, buttonHeight, btnFeed_Click, Color.DarkOrange);
            btnPlay = CreateAndAddButton("ПОИГРАТЬ", new Point(buttonsStartX + buttonWidth + buttonSpacing, buttonsStartY),
                buttonWidth, buttonHeight, btnPlay_Click, Color.RoyalBlue);
            btnWash = CreateAndAddButton("ПОМЫТЬ", new Point(buttonsStartX + 2 * (buttonWidth + buttonSpacing), buttonsStartY),
                buttonWidth, buttonHeight, btnWash_Click, Color.Teal);
            btnCure = CreateAndAddButton("ЛЕЧИТЬ", new Point(buttonsStartX + 3 * (buttonWidth + buttonSpacing), buttonsStartY),
                buttonWidth, buttonHeight, BtnCure_Click, Color.DarkRed);
            btnCure.Enabled = false;

            // Лекарства
            lblMedicines = new Label();
            lblMedicines.Text = $"ЛЕКАРСТВА: {GameManager.Instance.MedicinesLeft}";
            lblMedicines.Location = new Point(_backgroundPanel.Width - 250, buttonsStartY - 50);
            lblMedicines.ForeColor = Color.Gold;
            lblMedicines.Font = new Font("Arial", 14, FontStyle.Bold);
            lblMedicines.BackColor = Color.Transparent;
            lblMedicines.AutoSize = true;
            _backgroundPanel.Controls.Add(lblMedicines);

            // Таймер болезни
            lblSicknessTimer = new Label();
            lblSicknessTimer.Text = "ЕНОТ БОЛЕН!";
            lblSicknessTimer.Location = new Point(_backgroundPanel.Width / 2 - 200, 50);
            lblSicknessTimer.Size = new Size(400, 50);
            lblSicknessTimer.ForeColor = Color.Red;
            lblSicknessTimer.Font = new Font("Arial", 24, FontStyle.Bold);
            lblSicknessTimer.TextAlign = ContentAlignment.MiddleCenter;
            lblSicknessTimer.BackColor = Color.Transparent;
            lblSicknessTimer.Visible = false;
            _backgroundPanel.Controls.Add(lblSicknessTimer);

            // Заголовок
            Label titleLabel = new Label();
            titleLabel.Text = "МОНИТОР ЕНОТА";
            titleLabel.Font = new Font("Arial", 32, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(_backgroundPanel.Width / 2 - 150, 20);
            _backgroundPanel.Controls.Add(titleLabel);
        }

        private ProgressBar CreateProgressBarWithLabel(string labelText, Point location, int width, int height, Color color)
        {
            Label lbl = new Label();
            lbl.Text = labelText;
            lbl.ForeColor = Color.White;
            lbl.Location = new Point(location.X, location.Y - 30);
            lbl.Font = new Font("Arial", 12, FontStyle.Bold);
            lbl.AutoSize = true;
            lbl.BackColor = Color.Transparent;
            _backgroundPanel.Controls.Add(lbl);

            ProgressBar pb = new ProgressBar();
            pb.Size = new Size(width, height);
            pb.Location = location;
            pb.Maximum = 100;
            pb.Minimum = 0;
            pb.Value = 100;
            pb.ForeColor = color;
            pb.BackColor = Color.FromArgb(50, 50, 50);
            pb.Style = ProgressBarStyle.Continuous;
            _backgroundPanel.Controls.Add(pb);

            return pb;
        }

        private Label CreateValueLabel(Point location, string initialText)
        {
            Label lbl = new Label();
            lbl.Text = initialText;
            lbl.ForeColor = Color.White;
            lbl.Location = location;
            lbl.Font = new Font("Arial", 14, FontStyle.Bold);
            lbl.AutoSize = true;
            lbl.BackColor = Color.Transparent;
            _backgroundPanel.Controls.Add(lbl);

            return lbl;
        }

        private Button CreateAndAddButton(string text, Point location, int width, int height, EventHandler clickHandler, Color baseColor)
        {
            Button button = new Button();
            button.Text = text;
            button.Size = new Size(width, height);
            button.Location = location;
            button.Click += clickHandler;
            button.BackColor = Color.FromArgb(baseColor.R / 3, baseColor.G / 3, baseColor.B / 3);
            button.ForeColor = Color.White;
            button.Font = new Font("Arial", 12, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseDownBackColor = baseColor;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(baseColor.R / 2, baseColor.G / 2, baseColor.B / 2);
            _backgroundPanel.Controls.Add(button);

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

        private void UpdateUI()
        {
            try
            {
                int hungerValue = (int)Math.Round(_tama.Hunger);
                int playValue = (int)Math.Round(_tama.Play);
                int hygieneValue = (int)Math.Round(_tama.Hygiene);
                int healthValue = (int)Math.Round(_tama.Health);

                if (progressBarHunger != null && !progressBarHunger.IsDisposed)
                    progressBarHunger.Value = hungerValue;
                if (lblHungerValue != null && !lblHungerValue.IsDisposed)
                    lblHungerValue.Text = $"{hungerValue}%";

                if (progressBarPlay != null && !progressBarPlay.IsDisposed)
                    progressBarPlay.Value = playValue;
                if (lblPlayValue != null && !lblPlayValue.IsDisposed)
                    lblPlayValue.Text = $"{playValue}%";

                if (progressBarHygiene != null && !progressBarHygiene.IsDisposed)
                    progressBarHygiene.Value = hygieneValue;
                if (lblHygieneValue != null && !lblHygieneValue.IsDisposed)
                    lblHygieneValue.Text = $"{hygieneValue}%";

                if (progressBarHealth != null && !progressBarHealth.IsDisposed)
                    progressBarHealth.Value = healthValue;
                if (lblHealthValue != null && !lblHealthValue.IsDisposed)
                    lblHealthValue.Text = $"{healthValue}%";

                UpdateProgressBarColors(hungerValue, playValue, hygieneValue, healthValue);
                UpdateSicknessUI(); // Важно: обновляем кнопку лечения при каждом изменении параметров
                UpdateRaccoonAnimation();
            }
            catch { }
        }

        private void UpdateProgressBarColors(int hunger, int play, int hygiene, int health)
        {
            progressBarHunger.ForeColor = GetColorForValue(hunger, Color.Orange);
            progressBarPlay.ForeColor = GetColorForValue(play, Color.DodgerBlue);
            progressBarHygiene.ForeColor = GetColorForValue(hygiene, Color.MediumAquamarine);
            progressBarHealth.ForeColor = GetColorForValue(health, Color.Crimson);

            lblHungerValue.ForeColor = GetColorForValue(hunger, Color.White);
            lblPlayValue.ForeColor = GetColorForValue(play, Color.White);
            lblHygieneValue.ForeColor = GetColorForValue(hygiene, Color.White);
            lblHealthValue.ForeColor = GetColorForValue(health, Color.White);
        }

        private Color GetColorForValue(int value, Color normalColor)
        {
            if (value <= 20) return Color.Red;
            if (value <= 40) return Color.OrangeRed;
            if (value <= 60) return Color.Yellow;
            if (value <= 80) return Color.YellowGreen;
            return normalColor;
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
                {
                    lblSicknessTimer.Visible = isSick;
                    if (isSick)
                    {
                        lblSicknessTimer.Text = "⚠ ЕНОТ БОЛЕН! ТРЕБУЕТСЯ ЛЕЧЕНИЕ ⚠";
                        lblSicknessTimer.ForeColor = Color.Red;
                    }
                }

                UpdateMedicinesUI();
            }
            catch { }
        }

        private void UpdateMedicinesUI()
        {
            try
            {
                if (lblMedicines != null && !lblMedicines.IsDisposed)
                {
                    lblMedicines.Text = $"ЛЕКАРСТВА: {GameManager.Instance.MedicinesLeft}";
                    lblMedicines.ForeColor = GameManager.Instance.MedicinesLeft > 0 ? Color.Gold : Color.Gray;
                }
            }
            catch { }
        }

        private void btnFeed_Click(object sender, EventArgs e)
        {
            _tama.Feed(50);
            _currentAction = RaccoonActionState.Feeding;
            StartActionAnimationTimer();
            UpdateUI();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            _tama.PlayWith(40);
            _currentAction = RaccoonActionState.Playing;
            StartActionAnimationTimer();
            UpdateUI();
        }

        private void btnWash_Click(object sender, EventArgs e)
        {
            _tama.Wash(55);
            _currentAction = RaccoonActionState.Washing;
            StartActionAnimationTimer();
            UpdateUI();
        }

        private void BtnCure_Click(object sender, EventArgs e)
        {
            if (GameManager.Instance.MedicinesLeft <= 0)
            {
                MessageBox.Show("НЕТ ЛЕКАРСТВ!", "ОШИБКА", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!_tama.IsSick)
            {
                MessageBox.Show("ЕНОТ НЕ БОЛЕН!", "ИНФОРМАЦИЯ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            GameManager.Instance.TryUseMedicine();
            _tama.Cure();
            _tama.Heal(50);
            _tama.Feed(50);
            _tama.PlayWith(50);
            _tama.Wash(50);

            _currentAction = RaccoonActionState.Healing;
            StartActionAnimationTimer();

            MessageBox.Show("ЕНОТ ВЫЛЕЧЕН! ПАРАМЕТРЫ ВОССТАНОВЛЕНЫ.", "УСПЕХ",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            UpdateSicknessUI();
            UpdateUI();
        }

        private void ShowHealingEffect()
        {
            Panel healEffect = new Panel();
            healEffect.Size = new Size(450, 450);
            healEffect.Location = pictureBoxRaccoon.Location;
            healEffect.BackColor = Color.Transparent;
            _backgroundPanel.Controls.Add(healEffect);
            healEffect.BringToFront();

            Timer effectTimer = new Timer();
            effectTimer.Interval = 50;
            int alpha = 255;

            effectTimer.Tick += (s, e) =>
            {
                using (Graphics g = healEffect.CreateGraphics())
                {
                    g.Clear(Color.Transparent);
                    using (Pen pen = new Pen(Color.FromArgb(alpha, Color.Lime), 5))
                    {
                        g.DrawEllipse(pen, 10, 10, healEffect.Width - 20, healEffect.Height - 20);
                    }
                }

                alpha -= 15;
                if (alpha <= 0)
                {
                    effectTimer.Stop();
                    effectTimer.Dispose();
                    _backgroundPanel.Controls.Remove(healEffect);
                    healEffect.Dispose();
                }
            };
            effectTimer.Start();
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

                Image image = null;

                switch (_currentAction)
                {
                    case RaccoonActionState.Feeding:
                        image = TryLoadResourceImage("raccoon_feeding_anim") ?? CreateFallbackImage("КУШАЕТ", Color.Green);
                        break;
                    case RaccoonActionState.Playing:
                        image = TryLoadResourceImage("raccoon_playing_anim") ?? CreateFallbackImage("ИГРАЕТ", Color.Blue);
                        break;
                    case RaccoonActionState.Washing:
                        image = TryLoadResourceImage("raccoon_washing_anim") ?? CreateFallbackImage("МОЕТСЯ", Color.Cyan);
                        break;
                    case RaccoonActionState.Healing:
                        image = TryLoadResourceImage("raccoon_healing_anim") ?? CreateFallbackImage("ЛЕЧИТСЯ", Color.Magenta);
                        break;
                    case RaccoonActionState.None:
                        if (_tama.IsSick || _tama.Health < 30)
                        {
                            image = TryLoadResourceImage("raccoon_sick") ?? CreateFallbackImage("БОЛЕН", Color.Red);
                        }
                        else if (_tama.Hunger < 30)
                        {
                            image = TryLoadResourceImage("raccoon_hungry") ?? CreateFallbackImage("ГОЛОДЕН", Color.Orange);
                        }
                        else if (_tama.Hygiene < 30)
                        {
                            image = TryLoadResourceImage("raccoon_dirty") ?? CreateFallbackImage("ГРЯЗНЫЙ", Color.Brown);
                        }
                        else if (_tama.Play < 40)
                        {
                            image = TryLoadResourceImage("raccoon_bored") ?? CreateFallbackImage("СКУЧАЕТ", Color.Gray);
                        }
                        else
                        {
                            image = TryLoadResourceImage("raccoon_happy") ?? CreateFallbackImage("СЧАСТЛИВ", Color.Yellow);
                        }
                        break;
                }

                if (image != null && (pictureBoxRaccoon.Image == null || !ReferenceEquals(pictureBoxRaccoon.Image, image)))
                {
                    var oldImg = pictureBoxRaccoon.Image;
                    pictureBoxRaccoon.Image = image;
                    if (oldImg != null)
                    {
                        oldImg.Dispose();
                    }
                }
            }
            catch { }
        }

        private Image TryLoadResourceImage(string resourceName)
        {
            try
            {
                object resource = Properties.Resources.ResourceManager.GetObject(resourceName);
                if (resource is Image)
                {
                    return (Image)resource;
                }

                resourceName = resourceName.ToLower();
                resource = Properties.Resources.ResourceManager.GetObject(resourceName);
                if (resource is Image)
                {
                    return (Image)resource;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private Image CreateFallbackImage(string text, Color color)
        {
            Bitmap bmp = new Bitmap(450, 450);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(30, 30, 40));

                g.FillEllipse(new SolidBrush(Color.FromArgb(60, 60, 70)), 50, 50, 350, 350);

                g.FillEllipse(new SolidBrush(Color.Gray), 100, 80, 80, 80);
                g.FillEllipse(new SolidBrush(Color.Gray), 270, 80, 80, 80);

                g.FillEllipse(Brushes.Black, 150, 180, 60, 60);
                g.FillEllipse(Brushes.Black, 240, 180, 60, 60);
                g.FillEllipse(Brushes.White, 160, 190, 20, 20);
                g.FillEllipse(Brushes.White, 250, 190, 20, 20);

                g.FillEllipse(Brushes.Black, 210, 280, 40, 30);

                g.DrawString(text, new Font("Arial", 24, FontStyle.Bold), new SolidBrush(color), 150, 350);
            }
            return bmp;
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
                var result = MessageBox.Show("Закрыть монитор енота?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    this.Close();
                }
                e.Handled = true;
            }
        }

        public async void MonitorRestart(int durationMs)
        {
            this.Enabled = false;
            _restartOverlay.Visible = true;

            Label restartLabel = new Label();
            restartLabel.Text = "ПЕРЕЗАГРУЗКА МОНИТОРА...";
            restartLabel.Font = new Font("Arial", 36, FontStyle.Bold);
            restartLabel.ForeColor = Color.Red;
            restartLabel.BackColor = Color.Transparent;
            restartLabel.AutoSize = true;
            restartLabel.Location = new Point(
                (_restartOverlay.Width - restartLabel.Width) / 2,
                (_restartOverlay.Height - restartLabel.Height) / 2);
            _restartOverlay.Controls.Add(restartLabel);

            for (int i = 0; i < 5; i++)
            {
                restartLabel.Visible = !restartLabel.Visible;
                await Task.Delay(200);
            }

            restartLabel.Visible = true;
            await Task.Delay(durationMs - 1000);

            _restartOverlay.Controls.Clear();
            _restartOverlay.Visible = false;
            this.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_tama != null)
            {
                _tama.StatsChanged -= Tama_StatsChanged;
                _tama.SicknessStatusChanged -= Tama_SicknessStatusChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.MedicinesUpdated -= GameManager_MedicinesUpdated;
            }

            if (_actionAnimationTimer != null)
            {
                _actionAnimationTimer.Stop();
                _actionAnimationTimer.Dispose();
            }

            if (pictureBoxRaccoon != null && pictureBoxRaccoon.Image != null)
            {
                pictureBoxRaccoon.Image.Dispose();
                pictureBoxRaccoon.Image = null;
            }

            base.OnFormClosing(e);
        }
    }
}