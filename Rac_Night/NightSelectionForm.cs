using System;
using System.Drawing;
using System.Windows.Forms;

namespace Rac_Night
{
    public class NightSelectionForm : Form
    {
        private Button[] nightButtons;
        private Button btnBonusNight;
        private Button btnBack;

        public int SelectedNight { get; private set; } = 0;

        public NightSelectionForm()
        {
            SetupForm();
        }

        private void SetupForm()
        {
            this.Text = "Выбор ночи";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            Label titleLabel = new Label();
            titleLabel.Text = "ВЫБЕРИТЕ НОЧЬ";
            titleLabel.Font = new Font("Arial", 32, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(this.Width / 2 - 150, 30);
            this.Controls.Add(titleLabel);

            // Кнопки ночей 1-5
            nightButtons = new Button[5];
            int startX = 100;
            int startY = 120;
            int buttonWidth = 100;
            int buttonHeight = 100;
            int spacing = 20;

            for (int i = 0; i < 5; i++)
            {
                int nightNumber = i + 1;
                nightButtons[i] = new Button();
                nightButtons[i].Text = $"НОЧЬ {nightNumber}";
                nightButtons[i].Size = new Size(buttonWidth, buttonHeight);
                nightButtons[i].Location = new Point(startX + i * (buttonWidth + spacing), startY);
                nightButtons[i].Tag = nightNumber;
                nightButtons[i].Click += NightButton_Click;

                // Проверяем доступность ночи
                nightButtons[i].Enabled = ProgressManager.IsNightAvailable(nightNumber);

                // Настраиваем внешний вид
                nightButtons[i].BackColor = nightButtons[i].Enabled ?
                    Color.FromArgb(80, 80, 120) : Color.FromArgb(40, 40, 60);
                nightButtons[i].ForeColor = Color.White;
                nightButtons[i].Font = new Font("Arial", 10, FontStyle.Bold);
                nightButtons[i].FlatStyle = FlatStyle.Flat;

                this.Controls.Add(nightButtons[i]);
            }

            // Кнопка бонусной ночи
            btnBonusNight = new Button();
            btnBonusNight.Text = "БОНУСНАЯ\nНОЧЬ";
            btnBonusNight.Size = new Size(150, 150);
            btnBonusNight.Location = new Point(this.Width / 2 - 75, 250);
            btnBonusNight.Tag = 6;
            btnBonusNight.Click += NightButton_Click;
            btnBonusNight.Enabled = ProgressManager.IsNightAvailable(6);
            btnBonusNight.BackColor = btnBonusNight.Enabled ?
                Color.FromArgb(120, 80, 160) : Color.FromArgb(60, 40, 80);
            btnBonusNight.ForeColor = Color.White;
            btnBonusNight.Font = new Font("Arial", 12, FontStyle.Bold);
            btnBonusNight.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnBonusNight);

            // Кнопка "Назад"
            btnBack = new Button();
            btnBack.Text = "НАЗАД";
            btnBack.Size = new Size(150, 50);
            btnBack.Location = new Point(this.Width / 2 - 75, 450);
            btnBack.Click += (s, e) => this.Close();
            btnBack.BackColor = Color.FromArgb(80, 80, 80);
            btnBack.ForeColor = Color.White;
            btnBack.Font = new Font("Arial", 12, FontStyle.Bold);
            btnBack.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnBack);
        }

        private void NightButton_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            int nightNumber = (int)button.Tag;

            if (ProgressManager.IsNightAvailable(nightNumber))
            {
                SelectedNight = nightNumber;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}