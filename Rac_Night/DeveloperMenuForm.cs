using System;
using System.Drawing;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class DeveloperMenuForm : Form
    {
        private CheckBox[] nightCheckBoxes;
        private CheckBox bonusNightCheckBox;
        private Button btnSave;
        private Button btnCancel;
        private Button btnUnlockAll;
        private Button btnResetProgress;

        public DeveloperMenuForm()
        {
            InitializeComponents();
            LoadProgress();
        }

        private void InitializeComponents()
        {
            this.Text = "Меню разработчика";
            this.Size = new Size(600, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 40);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.KeyPreview = true;

            Label titleLabel = new Label();
            titleLabel.Text = "МЕНЮ РАЗРАБОТЧИКА";
            titleLabel.Font = new Font("Arial", 24, FontStyle.Bold);
            titleLabel.ForeColor = Color.Yellow;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(this.Width / 2 - titleLabel.Width / 2, 20);
            this.Controls.Add(titleLabel);

            // Чекбоксы для ночей 1-5
            nightCheckBoxes = new CheckBox[5];
            int startY = 80;
            for (int i = 0; i < 5; i++)
            {
                int nightNumber = i + 1;
                nightCheckBoxes[i] = new CheckBox();
                nightCheckBoxes[i].Text = $"Ночь {nightNumber} пройдена";
                nightCheckBoxes[i].Location = new Point(50, startY + i * 40);
                nightCheckBoxes[i].Size = new Size(200, 30);
                nightCheckBoxes[i].ForeColor = Color.White;
                nightCheckBoxes[i].Font = new Font("Arial", 12);
                this.Controls.Add(nightCheckBoxes[i]);
            }

            // Бонусная ночь
            bonusNightCheckBox = new CheckBox();
            bonusNightCheckBox.Text = "Бонусная ночь пройдена";
            bonusNightCheckBox.Location = new Point(50, startY + 5 * 40);
            bonusNightCheckBox.Size = new Size(250, 30);
            bonusNightCheckBox.ForeColor = Color.Cyan;
            bonusNightCheckBox.Font = new Font("Arial", 12, FontStyle.Bold);
            this.Controls.Add(bonusNightCheckBox);

            // Информация о лекарствах
            Label infoLabel = new Label();
            infoLabel.Text = "Лекарства по ночам:\n" +
                            "Ночь 1-2: 3 лекарства\n" +
                            "Ночь 3: 2 лекарства\n" +
                            "Ночь 4: 1 лекарство\n" +
                            "Ночь 5: 0 лекарств\n" +
                            "Бонусная: 3 лекарства";
            infoLabel.Location = new Point(300, 80);
            infoLabel.Size = new Size(180, 150);
            infoLabel.ForeColor = Color.LightGreen;
            infoLabel.Font = new Font("Arial", 10);
            this.Controls.Add(infoLabel);

            // Кнопка "Разблокировать всё"
            btnUnlockAll = new Button();
            btnUnlockAll.Text = "РАЗБЛОКИРОВАТЬ ВСЁ";
            btnUnlockAll.Size = new Size(200, 40);
            btnUnlockAll.Location = new Point(50, startY + 6 * 40 + 10);
            btnUnlockAll.Click += BtnUnlockAll_Click;
            btnUnlockAll.BackColor = Color.Gold;
            btnUnlockAll.ForeColor = Color.Black;
            btnUnlockAll.Font = new Font("Arial", 12, FontStyle.Bold);
            btnUnlockAll.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnUnlockAll);

            // Кнопка "Сбросить прогресс"
            btnResetProgress = new Button();
            btnResetProgress.Text = "СБРОСИТЬ ПРОГРЕСС";
            btnResetProgress.Size = new Size(200, 40);
            btnResetProgress.Location = new Point(260, startY + 6 * 40 + 10);
            btnResetProgress.Click += BtnResetProgress_Click;
            btnResetProgress.BackColor = Color.DarkRed;
            btnResetProgress.ForeColor = Color.White;
            btnResetProgress.Font = new Font("Arial", 12, FontStyle.Bold);
            btnResetProgress.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnResetProgress);

            // Кнопка "Сохранить"
            btnSave = new Button();
            btnSave.Text = "СОХРАНИТЬ";
            btnSave.Size = new Size(150, 50);
            btnSave.Location = new Point(100, this.Height - 80);
            btnSave.Click += BtnSave_Click;
            btnSave.BackColor = Color.Green;
            btnSave.ForeColor = Color.White;
            btnSave.Font = new Font("Arial", 14, FontStyle.Bold);
            btnSave.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnSave);

            // Кнопка "Отмена"
            btnCancel = new Button();
            btnCancel.Text = "ОТМЕНА";
            btnCancel.Size = new Size(150, 50);
            btnCancel.Location = new Point(260, this.Height - 80);
            btnCancel.Click += (s, e) => this.Close();
            btnCancel.BackColor = Color.Gray;
            btnCancel.ForeColor = Color.White;
            btnCancel.Font = new Font("Arial", 14, FontStyle.Bold);
            btnCancel.FlatStyle = FlatStyle.Flat;
            this.Controls.Add(btnCancel);
        }

        private void LoadProgress()
        {
            for (int i = 0; i < 5; i++)
            {
                nightCheckBoxes[i].Checked = (i + 1) <= ProgressManager.Progress.NightsCompleted;
            }
            bonusNightCheckBox.Checked = ProgressManager.Progress.BonusNightCompleted;
        }

        private void BtnUnlockAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                nightCheckBoxes[i].Checked = true;
            }
            bonusNightCheckBox.Checked = true;
        }

        private void BtnResetProgress_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите сбросить весь прогресс?\nВсе пройденные ночи будут удалены.",
                                        "Сброс прогресса",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                for (int i = 0; i < 5; i++)
                {
                    nightCheckBoxes[i].Checked = false;
                }
                bonusNightCheckBox.Checked = false;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Определяем максимальную пройденную ночь
            int nightsCompleted = 0;
            for (int i = 0; i < 5; i++)
            {
                if (nightCheckBoxes[i].Checked)
                {
                    nightsCompleted = i + 1;
                }
            }

            ProgressManager.Progress.NightsCompleted = nightsCompleted;
            ProgressManager.Progress.BonusNightCompleted = bonusNightCheckBox.Checked;

            ProgressManager.SaveProgress();

            MessageBox.Show($"Прогресс сохранен!\nПройдено ночей: {nightsCompleted}\nБонусная ночь: {(bonusNightCheckBox.Checked ? "Да" : "Нет")}",
                          "Успех",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information);

            this.Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}