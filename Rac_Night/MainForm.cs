using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public partial class MainForm : Form
    {
        private enum CutsceneStage
        {
            Disclaimer,
            AuthorIntro,
            NarrativeIntro,
            Finished
        }

        private CutsceneStage currentStage;

        // Элементы UI
        private Panel disclaimerPanel;
        private Label disclaimerLabel;
        private CheckBox disclaimerCheckBox;
        private Button continueButton;
        private PictureBox gifPictureBox;
        private Label typingLabel;

        // Звук
        private System.Media.SoundPlayer disclaimerSoundPlayer;
        private System.Media.SoundPlayer textSoundPlayer;

        // Таймеры
        private Timer typingTimer;
        private Timer gifFrameTimer;

        // Текст
        private string fullIntroText;
        private int currentTextCharIndex;

        private string authorIntroSentence1 = "Авторы: \n" +
                                            "Разработчик: Метелицин Захар\n" +
                                            "Дизайнер: Калуцкая Ульяна\n" +
                                            "Для скипа касцены Alt + F4 ))";

        private string authorIntroSentence2 = "Это простая история о Человеке,\n" +
                                            "который попал в неизвестность.";

        private string narrativeIntroText = "Последнее что я помню это ,то как я решил прибраться в квартире.\n" +
                                          "Когда я выкидывал мусор.\n" +
                                          "Я нашёл этого енота на помойке.\n" +
                                          "Я решил забрать его домой.\n" +
                                          "Больше ничего не могу вспомнить...";

        // GIF анимация
        private int gifTotalFrames = 0;
        private int gifCurrentFrameIndex = 0;
        private bool gifPlaying = false;
        private bool gifPlayedOnce = false;

        // Для ожидания печати текста
        private TaskCompletionSource<bool> typingCompletionSource;

        // Константы пауз
        private const int ShortPauseMs = 2500;
        private const int LongPauseMs = 5500;
        private const int LineDelayMs = 1000;

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;

            disclaimerSoundPlayer = new System.Media.SoundPlayer(Properties.Resources.Disclamer);
            textSoundPlayer = new System.Media.SoundPlayer(Properties.Resources.TextSound);

            typingTimer = new Timer();
            typingTimer.Interval = 70;
            typingTimer.Tick += TypingTimer_Tick;

            gifFrameTimer = new Timer();
            gifFrameTimer.Interval = 100;
            gifFrameTimer.Tick += GifFrameTimer_Tick;

            this.Text = "Rac Night";
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            SetupIntroElements();
            currentStage = CutsceneStage.Disclaimer;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetupDisclaimerScreen();
        }

        private void SetupDisclaimerScreen()
        {
            disclaimerPanel = new Panel();
            disclaimerPanel.Size = new Size(this.ClientSize.Width * 3 / 4, this.ClientSize.Height * 3 / 4);
            disclaimerPanel.Location = new Point((this.ClientSize.Width - disclaimerPanel.Width) / 2,
                                                (this.ClientSize.Height - disclaimerPanel.Height) / 2);
            disclaimerPanel.BackColor = Color.FromArgb(20, 20, 20);
            disclaimerPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(disclaimerPanel);

            continueButton = new Button();
            continueButton.Text = "Продолжить";
            continueButton.Font = new Font("Arial", 12, FontStyle.Bold);
            continueButton.Size = new Size(200, 50);
            continueButton.Location = new Point((disclaimerPanel.Width - continueButton.Width) / 2,
                                               disclaimerPanel.Height - continueButton.Height - 20);
            continueButton.Enabled = false;
            continueButton.Click += ContinueButton_Click;
            continueButton.BackColor = Color.FromArgb(80, 80, 80);
            continueButton.ForeColor = Color.White;
            continueButton.FlatStyle = FlatStyle.Flat;
            continueButton.FlatAppearance.BorderSize = 1;
            continueButton.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
            disclaimerPanel.Controls.Add(continueButton);

            disclaimerCheckBox = new CheckBox();
            disclaimerCheckBox.Text = "Я осознаю и принимаю все риски";
            disclaimerCheckBox.Font = new Font("Arial", 10, FontStyle.Regular);
            disclaimerCheckBox.ForeColor = Color.White;
            disclaimerCheckBox.AutoSize = true;
            disclaimerCheckBox.BackColor = Color.Transparent;
            disclaimerCheckBox.Location = new Point(
                (disclaimerPanel.Width - disclaimerCheckBox.PreferredSize.Width) / 2,
                continueButton.Top - disclaimerCheckBox.Height - 15);
            disclaimerCheckBox.CheckedChanged += DisclaimerCheckBox_CheckedChanged;
            disclaimerPanel.Controls.Add(disclaimerCheckBox);

            disclaimerLabel = new Label();
            disclaimerLabel.Text = "ВНИМАНИЕ:\n\n" +
                                 "Эта игра содержит резкие визуальные эффекты, громкие звуки\n" +
                                 "и скриммеры ,а так же мемы которые могут вызвать дискомфорт\n" +
                                 "или быть неприемлемыми для некоторых игроков.\n" +
                                 "Рекомендуется воздержаться от игры, если вы чувствительны к подобному контенту,\n" +
                                 "(страдаете эпилепсией, сердечными заболеваниями или другими похожими недугами).\n\n" +
                                 "Нажимая кнопку 'Продолжить', вы подтверждаете, что понимаете потенциальные риски" +
                                 "\n Разработчики не несут ответственности " +
                                 "за любые негативные последствия, вызванные игрой.\n\n";
            disclaimerLabel.Font = new Font("Arial", 20, FontStyle.Regular);
            disclaimerLabel.ForeColor = Color.White;
            disclaimerLabel.TextAlign = ContentAlignment.MiddleCenter;
            disclaimerLabel.Dock = DockStyle.Fill;
            disclaimerLabel.Padding = new Padding(20, 20, 20, 120);
            disclaimerPanel.Controls.Add(disclaimerLabel);
            disclaimerLabel.SendToBack();

            disclaimerSoundPlayer.Play();
        }

        private void DisclaimerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            continueButton.Enabled = disclaimerCheckBox.Checked;
        }

        private async void ContinueButton_Click(object sender, EventArgs e)
        {
            disclaimerSoundPlayer.Stop();
            disclaimerPanel.Hide();
            disclaimerPanel.Dispose();

            currentStage = CutsceneStage.AuthorIntro;
            await ProcessCutsceneStage();
        }

        private void SetupIntroElements()
        {
            gifPictureBox = new PictureBox();
            gifPictureBox.BackColor = Color.Transparent;
            gifPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            gifPictureBox.Visible = false;
            this.Controls.Add(gifPictureBox);

            typingLabel = new Label();
            typingLabel.BackColor = Color.Transparent;
            typingLabel.ForeColor = Color.LightGray;
            typingLabel.Text = "";
            typingLabel.Visible = false;
            this.Controls.Add(typingLabel);
        }

        private async Task ProcessCutsceneStage()
        {
            typingLabel.Text = "";
            currentTextCharIndex = 0;
            typingTimer.Stop();

            switch (currentStage)
            {
                case CutsceneStage.AuthorIntro:
                    typingLabel.Visible = true;
                    gifPictureBox.Visible = false;

                    typingLabel.Size = new Size((int)(this.ClientSize.Width * 0.8f),
                                               (int)(this.ClientSize.Height * 0.6f));
                    typingLabel.Location = new Point((this.ClientSize.Width - typingLabel.Width) / 2,
                                                    (this.ClientSize.Height - typingLabel.Height) / 2);
                    typingLabel.TextAlign = ContentAlignment.MiddleCenter;
                    typingLabel.Font = new Font("Consolas", 20, FontStyle.Regular);

                    fullIntroText = authorIntroSentence1;
                    await WaitForTypingCompletion();
                    await Task.Delay(ShortPauseMs);

                    typingLabel.Text = "";
                    currentTextCharIndex = 0;

                    fullIntroText = authorIntroSentence2;
                    await WaitForTypingCompletion();
                    await Task.Delay(LongPauseMs);

                    currentStage = CutsceneStage.NarrativeIntro;
                    await ProcessCutsceneStage();
                    break;

                case CutsceneStage.NarrativeIntro:
                    typingLabel.Visible = true;
                    gifPictureBox.Visible = true;

                    gifPictureBox.Width = (int)(this.ClientSize.Width * 0.5f);
                    gifPictureBox.Height = (int)(this.ClientSize.Height * 0.45f);
                    gifPictureBox.Location = new Point(
                        (this.ClientSize.Width - gifPictureBox.Width) / 2,
                        (int)(this.ClientSize.Height * 0.15f));

                    int textPaddingFromGif = 30;
                    typingLabel.Width = (int)(this.ClientSize.Width * 0.7f);
                    typingLabel.Height = (int)(this.ClientSize.Height - gifPictureBox.Bottom - textPaddingFromGif - 50);
                    typingLabel.Location = new Point(
                        (this.ClientSize.Width - typingLabel.Width) / 2,
                        gifPictureBox.Bottom + textPaddingFromGif);
                    typingLabel.TextAlign = ContentAlignment.TopCenter;
                    typingLabel.Font = new Font("Consolas", 18, FontStyle.Regular);

                    StartGifAnimation();

                    string[] narrativeLines = narrativeIntroText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in narrativeLines)
                    {
                        typingLabel.Text = "";
                        currentTextCharIndex = 0;
                        fullIntroText = line.Trim();
                        await WaitForTypingCompletion();
                        await Task.Delay(LineDelayMs);
                    }

                    await Task.Delay(LongPauseMs);
                    currentStage = CutsceneStage.Finished;
                    await ProcessCutsceneStage();
                    break;

                case CutsceneStage.Finished:
                    gifPictureBox.Hide();
                    typingLabel.Hide();
                    GameplayForm gameplayForm = new GameplayForm();
                    gameplayForm.Show();
                    StopAllResources();
                    this.Close();
                    break;
            }
        }

        private async Task WaitForTypingCompletion()
        {
            typingCompletionSource = new TaskCompletionSource<bool>();
            typingTimer.Start();
            await typingCompletionSource.Task;
            typingTimer.Stop();
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            if (currentTextCharIndex < fullIntroText.Length)
            {
                typingLabel.Text += fullIntroText[currentTextCharIndex];
                textSoundPlayer.Play();
                currentTextCharIndex++;
            }
            else
            {
                typingCompletionSource?.TrySetResult(true);
            }
        }

        private void StartGifAnimation()
        {
            gifFrameTimer.Stop();
            if (gifPictureBox.Image != null)
            {
                gifPictureBox.Image.Dispose();
            }

            gifPictureBox.Image = Properties.Resources.intro_gif;
            gifTotalFrames = gifPictureBox.Image.GetFrameCount(FrameDimension.Time);
            gifCurrentFrameIndex = 0;
            gifPlayedOnce = false;
            gifPlaying = true;

            if (gifTotalFrames > 0)
            {
                gifFrameTimer.Start();
            }
        }

        private void GifFrameTimer_Tick(object sender, EventArgs e)
        {
            if (gifPlaying && !gifPlayedOnce)
            {
                gifCurrentFrameIndex++;

                if (gifCurrentFrameIndex < gifTotalFrames)
                {
                    gifPictureBox.Image.SelectActiveFrame(FrameDimension.Time, gifCurrentFrameIndex);
                }
                else
                {
                    gifPlayedOnce = true;
                    StopGifAnimation();

                    if (gifTotalFrames > 0)
                    {
                        gifPictureBox.Image.SelectActiveFrame(FrameDimension.Time, gifTotalFrames - 1);
                    }
                }

                gifPictureBox.Invalidate();
            }
        }

        private void StopGifAnimation()
        {
            if (gifPlaying)
            {
                gifFrameTimer.Stop();
                gifPlaying = false;

                if (gifTotalFrames > 0 && gifCurrentFrameIndex > 0)
                {
                    gifPictureBox.Image.SelectActiveFrame(FrameDimension.Time, gifTotalFrames - 1);
                }

                gifPictureBox.Invalidate();
            }
        }

        private void StopAllResources()
        {
            if (disclaimerSoundPlayer != null)
            {
                disclaimerSoundPlayer.Stop();
                disclaimerSoundPlayer.Dispose();
            }

            if (textSoundPlayer != null)
            {
                textSoundPlayer.Stop();
                textSoundPlayer.Dispose();
            }

            if (typingTimer != null)
            {
                typingTimer.Stop();
                typingTimer.Dispose();
            }

            if (gifFrameTimer != null)
            {
                gifFrameTimer.Stop();
                gifFrameTimer.Dispose();
            }

            if (gifPictureBox != null && gifPictureBox.Image != null)
            {
                gifPictureBox.Image.Dispose();
                gifPictureBox.Image = null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            StopAllResources();
        }
    }
}