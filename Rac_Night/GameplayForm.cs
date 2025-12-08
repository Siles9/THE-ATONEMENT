using System.Windows.Forms;
using System.Drawing;
using System;
using System.Threading.Tasks;

namespace Rac_Night
{
    public partial class GameplayForm : Form
    {
        private PcTamagochiForm _tamagochiScreen;
        private Label _timeLabel; // Для отображения игрового времени
        private Panel _blindOverlay; // Для эффекта ослепления

        // Состояния игрока
        private bool _isFlashlightDisabled = false;
        private bool _isBlinded = false;

        public GameplayForm()
        {
            InitializeComponent();
            SetupFullscreenBorderless();
            InitializeGameUI();

            // Инициализируем форму тамагочи, но не показываем
            _tamagochiScreen = new PcTamagochiForm();
            _tamagochiScreen.FormClosing += TamagochiScreen_FormClosing;

            // Подписка на события GameManager
            GameManager.Instance.GameTimeUpdated += GameManager_GameTimeUpdated;
            GameManager.Instance.GhostSpawned += GameManager_GhostSpawned;
            GameManager.Instance.SicknessTimeout += GameManager_SicknessTimeout;
        }

        private void SetupFullscreenBorderless()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }

        private void InitializeGameUI()
        {
            // 1. Label для отображения времени
            _timeLabel = new Label();
            _timeLabel.Location = new Point(10, 10);
            _timeLabel.ForeColor = Color.White;
            _timeLabel.BackColor = Color.Black;
            _timeLabel.Font = new Font("Arial", 16, FontStyle.Bold);
            _timeLabel.AutoSize = true;
            this.Controls.Add(_timeLabel);

            // Инициализация оверлея для ослепления
            _blindOverlay = new Panel();
            _blindOverlay.Dock = DockStyle.Fill;
            _blindOverlay.BackColor = Color.Black;
            _blindOverlay.Visible = false;
            _blindOverlay.BringToFront();
            this.Controls.Add(_blindOverlay);

            // Обновляем время сразу
            UpdateGameTimeDisplay();
        }

        private void GameManager_GameTimeUpdated(object sender, EventArgs e)
        {
            // Обновление UI должно происходить в UI-потоке
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
            _timeLabel.Text = GameManager.Instance.CurrentGameTime.ToString("hh:mm tt");
        }

        // --- Обработка нажатий клавиш ---

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // W: Показать/Скрыть тамагочи
            if (e.KeyCode == Keys.W)
            {
                if (!_tamagochiScreen.Visible)
                {
                    // Показываем форму тамагочи
                    ShowTamagochiScreen();
                }
                else
                {
                    // Скрываем форму (если игрок не нажал S внутри формы)
                    HideTamagochiScreen();
                }
            }

            // F: Фонарик (пример)
            if (e.KeyCode == Keys.F)
            {
                if (_isFlashlightDisabled)
                {
                    // Сообщение, что фонарик не работает
                    MessageBox.Show("Фонарик не работает!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    // Логика фонарика
                }
            }
        }

        private void ShowTamagochiScreen()
        {
            // Показываем модально, чтобы игрок фокусировался на нем
            // Но чтобы он мог быть скрыт по S, лучше Show()
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
            // Если форма закрывается (например, по ESC), мы просто скрываем её
            e.Cancel = true;
            HideTamagochiScreen();
        }

        // --- Обработка призраков ---

        private void GameManager_GhostSpawned(object sender, GhostType ghost)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleGhostAttack(ghost)));
            }
            else
            {
                HandleGhostAttack(ghost);
            }
        }

        private async void HandleGhostAttack(GhostType ghost)
        {
            switch (ghost)
            {
                case GhostType.Black:
                    // Призрак чёрный: Атакует монитор, -15% всех показателей
                    GameManager.Instance.CurrentTamagotchi.DecreaseAllStatsByPercentage(15);
                    // Визуальный эффект (например, мигание экрана)
                    MessageBox.Show("Черный призрак атаковал монитор!", "Атака!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;

                case GhostType.Brown:
                    // Призрак коричневый: Ослепляет игрока на 3 секунды
                    if (!_isBlinded)
                    {
                        await BlindPlayer(3000);
                    }
                    break;

                case GhostType.White:
                    // Призрак белый: Зависит от состояния монитора
                    if (_tamagochiScreen.Visible)
                    {
                        // В мониторе: перезагрузка монитора (5 секунд)
                        _tamagochiScreen.MonitorRestart(5000);
                    }
                    else
                    {
                        // Не в мониторе: выключает фонарик на 6 секунд
                        await DisableFlashlight(6000);
                    }
                    break;
            }
        }

        private async Task BlindPlayer(int durationMs)
        {
            _isBlinded = true;
            _blindOverlay.Visible = true;
            // Можно добавить скриммер или звук

            await Task.Delay(durationMs);

            _blindOverlay.Visible = false;
            _isBlinded = false;
        }

        private async Task DisableFlashlight(int durationMs)
        {
            _isFlashlightDisabled = true;
            MessageBox.Show("Фонарик отключен!", "Белый призрак", MessageBoxButtons.OK, MessageBoxIcon.Error);

            await Task.Delay(durationMs);

            _isFlashlightDisabled = false;
            MessageBox.Show("Фонарик снова работает.", "Восстановлено", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- Поражение ---
        private void GameManager_SicknessTimeout(object sender, EventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HandleDefeat));
            }
            else
            {
                HandleDefeat();
            }
        }

        private void HandleDefeat()
        {
            // Главный призрак пугает игрока и завершает ночь поражением
            MessageBox.Show("ГЛАВНЫЙ ПРИЗРАК! Вы не успели вылечить енота. ПОРАЖЕНИЕ.", "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Stop);

            // Закрываем все формы и возвращаемся в меню или показываем экран поражения
            Application.Exit(); // В реальном проекте нужно более мягкое завершение
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            GameManager.Instance.StopGame();
        }
    }
}
