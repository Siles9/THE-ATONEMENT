using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class SettingsForm : Form
    {
        private TrackBar volumeBar;
        private Button exitButton;
        private Label volumeLabel;

        public SettingsForm()
        {
            InitializeComponent();
            InitializeSettingsUI();

            // Устанавливаем начальное значение громкости из менеджера громкости
            volumeBar.Value = (int)(VolumeManager.CurrentVolume * 100);
        }

        private void InitializeSettingsUI()
        {
            this.Text = "Настройки";
            this.Size = new Size(300, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(135, 206, 250);

            volumeLabel = new Label();
            volumeLabel.Text = "Настройка громкости";
            volumeLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            volumeLabel.ForeColor = Color.FromArgb(0, 0, 139);
            volumeLabel.Location = new Point(50, 20);
            this.Controls.Add(volumeLabel);

            volumeBar = new TrackBar();
            volumeBar.Location = new Point(50, 50);
            volumeBar.Width = 200;
            volumeBar.Minimum = 0;
            volumeBar.Maximum = 100;
            volumeBar.TickFrequency = 10;
            volumeBar.Scroll += VolumeBar_Scroll;
            this.Controls.Add(volumeBar);

            exitButton = new Button();
            exitButton.Text = "Выход";
            exitButton.Location = new Point(155, 20);
            exitButton.Width = 50;
            exitButton.Click += (sender, e) => this.Close();
            this.Controls.Add(exitButton);
        }

        private void VolumeBar_Scroll(object sender, EventArgs e)
        {
            // Получаем значение с ползунка
            int currentValue = volumeBar.Value;

            // Конвертируем в значение от 0 до 1
            float volume = (float)currentValue / 100;

            // Устанавливаем громкость через менеджер громкости
            VolumeManager.CurrentVolume = volume;
        }
    }
}
