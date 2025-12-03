using System;
using System.Drawing;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace Rac_Night
{
    public partial class MainForm : Form
    {
        // --- Перечисление для отслеживания текущего этапа ---
        private enum CutsceneStage
        {
            Disclaimer,
            AuthorIntro,
            NarrativeIntro,
            Finished
        }
        private CutsceneStage currentStage;

        // --- Поля для управления элементами и состоянием ---
        private Panel disclaimerPanel;
        private Label disclaimerLabel;
        private CheckBox disclaimerCheckBox;
        private Button continueButton;

        private PictureBox gifPictureBox;
        private Label typingLabel;

        private System.Media.SoundPlayer disclaimerSoundPlayer;
        private System.Media.SoundPlayer textSoundPlayer;

        private Timer typingTimer;
        private string fullIntroText; // Текст для текущего этапа или под-этапа
        private int currentTextCharIndex;

        // Отдельные строки для каждого этапа
        private string authorIntroSentence1 = "Не буду затягивать с предисторией,\n" +
                                              "Звук текста не очень приятный,\n" +
                                              "Но так оно и задумано)\n";
        private string authorIntroSentence2 = "Это история о Человеке и то,\n" +
                                              "куда привели его грехи";

        // Текст для катсцены, теперь с новой строкой для каждой фразы
        private string narrativeIntroText = "Последнее что я помню это как я решил прибраться в квартире.\n" +
                                            "Когда я выкидывал мусор.\n" +
                                            "Я нашёл этого миленького енота на помойке.\n" +
                                            "И решил забрать его домой.\n" +
                                            "Больше ничего не могу вспомнить....";

        // --- Поля для управления GIF-анимацией (воспроизведение 1 раз) ---
        private int _gifTotalFrames = 0;
        private int _gifCurrentFrameIndex = 0;
        private bool _gifPlaying = false;
        private bool _gifPlayedOnce = false; // Флаг, что GIF проигрался один раз
        private Timer _gifFrameTimer; // Новый таймер для ручного управления GIF кадрами

        // --- TaskCompletionSource для ожидания завершения печати текста ---
        private TaskCompletionSource<bool> _typingCompletionSource;

        // --- Константы для пауз (увеличены по запросу) ---
        private const int ShortPauseMs = 2500; // Пауза после первой строки автора
        private const int LongPauseMs = 6000;  // Пауза между этапами
        private const int LineDelayMs = 1000;  // Пауза между строками в narrativeIntro (1 секунда)


        // --- Конструктор формы ---
        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
            // this.KeyDown += MainForm_KeyDown; // Обработчик клавиш Esc больше не нужен для этой формы

            // Инициализация звуковых проигрывателей
            disclaimerSoundPlayer = new System.Media.SoundPlayer(Properties.Resources.Disclamer);
            textSoundPlayer = new System.Media.SoundPlayer(Properties.Resources.TextSound);

            // Инициализация таймера для печати текста
            typingTimer = new Timer();
            typingTimer.Interval = 90; // Скорость печати
            typingTimer.Tick += TypingTimer_Tick;

            // Инициализация таймера для GIF анимации
            _gifFrameTimer = new Timer();
            _gifFrameTimer.Interval = 100; // Интервал для смены кадров GIF
            _gifFrameTimer.Tick += GifFrameTimer_Tick;

            // --- НАСТРОЙКИ ФОРМЫ ДЛЯ ПОЛНОЭКРАННОГО РЕЖИМА БЕЗ РАМОК ---
            this.Text = "Rac Night";
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            // Инициализация элементов интро (изначально скрыты)
            SetupIntroElements();

            // Устанавливаем начальный этап
            currentStage = CutsceneStage.Disclaimer;
        }

        // --- Обработчик события загрузки формы ---
        private void MainForm_Load(object sender, EventArgs e)
        {
            SetupDisclaimerScreen(); // Показать дисклеймер при загрузке формы
        }

        // --- Метод для настройки и отображения экрана дисклеймера ---
        private void SetupDisclaimerScreen()
        {
            disclaimerPanel = new Panel();
            disclaimerPanel.Size = new Size(this.ClientSize.Width * 3 / 4, this.ClientSize.Height * 3 / 4);
            disclaimerPanel.Location = new Point((this.ClientSize.Width - disclaimerPanel.Width) / 2, (this.ClientSize.Height - disclaimerPanel.Height) / 2);
            disclaimerPanel.BackColor = Color.FromArgb(20, 20, 20);
            disclaimerPanel.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(disclaimerPanel);

            continueButton = new Button();
            continueButton.Text = "Продолжить";
            continueButton.Font = new Font("Arial", 12, FontStyle.Bold);
            continueButton.Size = new Size(200, 50);
            continueButton.Location = new Point((disclaimerPanel.Width - continueButton.Width) / 2, disclaimerPanel.Height - continueButton.Height - 20);
            continueButton.Enabled = false;
            continueButton.Click += ContinueButton_Click;
            continueButton.BackColor = Color.FromArgb(80, 80, 80);
            continueButton.ForeColor = Color.White;
            continueButton.FlatStyle = FlatStyle.Flat;
            continueButton.FlatAppearance.BorderSize = 1;
            continueButton.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
            disclaimerPanel.Controls.Add(continueButton);

            disclaimerCheckBox = new CheckBox();
            disclaimerCheckBox.Text = "Я понимаю и принимаю эти условия";
            disclaimerCheckBox.Font = new Font("Arial", 10, FontStyle.Regular);
            disclaimerCheckBox.ForeColor = Color.Red;
            disclaimerCheckBox.AutoSize = true;
            disclaimerCheckBox.BackColor = Color.Transparent;
            disclaimerPanel.Controls.Add(disclaimerCheckBox);

            disclaimerCheckBox.Location = new Point(
                (disclaimerPanel.Width - disclaimerCheckBox.PreferredSize.Width) / 2,
                continueButton.Top - disclaimerCheckBox.Height - 15);
            disclaimerCheckBox.CheckedChanged += DisclaimerCheckBox_CheckedChanged;

            disclaimerLabel = new Label();
            disclaimerLabel.Text = "ВНИМАНИЕ:\n\n" +
                                   "Эта игра содержит интенсивные  визуальные эффекты, громкие звуки\n" +
                                   "и скриммеры которые могут вызвать дискомфорт\n" +
                                   "или быть неприемлемыми для некоторых игроков.\n" +
                                   "Рекомендуется воздержаться от игры, если вы чувствительны к подобному контенту,\n" +
                                   "(страдаете эпилепсией, сердечными заболеваниями или другими похожими заболеваниями).\n\n" +
                                   "Продолжая, вы подтверждаете, что понимаете потенциальные риски и " +
                                   "играете на свой страх и риск.\n Разработчики не несут ответственности " +
                                   "за любые негативные последствия, вызванные игрой.\n\n";
            disclaimerLabel.Font = new Font("Arial", 20, FontStyle.Regular);
            disclaimerLabel.ForeColor = Color.Red;
            disclaimerLabel.TextAlign = ContentAlignment.MiddleCenter;
            disclaimerLabel.Dock = DockStyle.Fill;
            disclaimerLabel.Padding = new Padding(20, 20, 20, 120);
            disclaimerPanel.Controls.Add(disclaimerLabel);
            disclaimerLabel.SendToBack();

            disclaimerSoundPlayer.Play();
        }

        // --- Обработчик изменения состояния CheckBox ---
        private void DisclaimerCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            continueButton.Enabled = disclaimerCheckBox.Checked;
        }

        // --- Обработчик клика по кнопке "Продолжить" (теперь async) ---
        private async void ContinueButton_Click(object sender, EventArgs e)
        {
            disclaimerSoundPlayer.Stop();
            disclaimerPanel.Hide();
            disclaimerPanel.Dispose();

            currentStage = CutsceneStage.AuthorIntro; // Переходим к первому этапу кастсцены
            await ProcessCutsceneStage(); // Запускаем асинхронный процесс кастсцены
        }

        // --- Инициализация элементов интро (GIF и текст) ---
        private void SetupIntroElements()
        {
            // Настройка PictureBox для GIF
            gifPictureBox = new PictureBox();
            gifPictureBox.BackColor = Color.Transparent;
            gifPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            gifPictureBox.Visible = false; // Изначально скрыт
            this.Controls.Add(gifPictureBox);

            // Настройка Label для печатающегося текста
            typingLabel = new Label();
            typingLabel.BackColor = Color.Transparent;
            typingLabel.ForeColor = Color.LightGray;
            typingLabel.Text = "";
            typingLabel.Visible = false; // Изначально скрыт
            this.Controls.Add(typingLabel);
        }

        // --- Метод для запуска текущего этапа кастсцены (теперь async) ---
        private async Task ProcessCutsceneStage()
        {
            typingLabel.Text = ""; // Очищаем текст перед новым этапом
            currentTextCharIndex = 0;
            typingTimer.Stop(); // Останавливаем таймер на всякий случай

            switch (currentStage)
            {
                case CutsceneStage.AuthorIntro:
                    typingLabel.Visible = true;
                    gifPictureBox.Visible = false; // Убеждаемся, что GIF скрыт

                    // Настройка typingLabel для центрированного текста без гифки
                    typingLabel.Size = new Size((int)(this.ClientSize.Width * 0.8f), (int)(this.ClientSize.Height * 0.6f));
                    typingLabel.Location = new Point((this.ClientSize.Width - typingLabel.Width) / 2, (this.ClientSize.Height - typingLabel.Height) / 2);
                    typingLabel.TextAlign = ContentAlignment.MiddleCenter; // Центрируем текст для этого этапа
                    typingLabel.Font = new Font("Consolas", 20, FontStyle.Regular); // Увеличенный шрифт для автора

                    // Первая строка автора
                    fullIntroText = authorIntroSentence1;
                    await WaitForTypingCompletion(); // Ждем завершения печати первой строки

                    await Task.Delay(ShortPauseMs); // Пауза после первой строки
                    typingLabel.Text = ""; // Очищаем текст
                    currentTextCharIndex = 0; // Сбрасываем индекс для новой строки

                    // Вторая строка автора
                    fullIntroText = authorIntroSentence2;
                    await WaitForTypingCompletion(); // Ждем завершения печати второй строки

                    await Task.Delay(LongPauseMs); // Длинная пауза перед переходом к следующему этапу

                    currentStage = CutsceneStage.NarrativeIntro;
                    await ProcessCutsceneStage(); // Переходим к следующему этапу
                    break;

                case CutsceneStage.NarrativeIntro:
                    typingLabel.Visible = true;
                    gifPictureBox.Visible = true; // Показываем GIF

                    // --- РАСПОЛОЖЕНИЕ GIF И ТЕКСТА ---
                    // Размер GIF (примерно 50% ширины, 45% высоты)
                    gifPictureBox.Width = (int)(this.ClientSize.Width * 0.5f);
                    gifPictureBox.Height = (int)(this.ClientSize.Height * 0.45f);
                    // Расположение GIF: по центру горизонтально, чуть выше центра вертикально
                    gifPictureBox.Location = new Point(
                        (this.ClientSize.Width - gifPictureBox.Width) / 2,
                        (int)(this.ClientSize.Height * 0.15f)); // 15% от верха экрана

                    // Расположение текста: по центру горизонтально, под GIF
                    int textPaddingFromGif = 30; // Отступ текста от GIF
                    typingLabel.Width = (int)(this.ClientSize.Width * 0.7f); // Ширина текста 70%
                    typingLabel.Height = (int)(this.ClientSize.Height - gifPictureBox.Bottom - textPaddingFromGif - 50); // Оставшаяся высота с небольшим отступом снизу
                    typingLabel.Location = new Point(
                        (this.ClientSize.Width - typingLabel.Width) / 2,
                        gifPictureBox.Bottom + textPaddingFromGif);

                    typingLabel.TextAlign = ContentAlignment.TopCenter; // Выравнивание текста по центру и сверху
                    typingLabel.Font = new Font("Consolas", 18, FontStyle.Regular); // Немного увеличенный шрифт для лучшей читаемости

                    // Убеждаемся, что GIF будет загружен и проигран
                    StartGifAnimation();

                    // --- ИЗМЕНЕНИЯ ЗДЕСЬ: ОБРАБОТКА ТЕКСТА ПО СТРОКАМ ---
                    // Разбиваем весь текст на отдельные строки
                    string[] narrativeLines = narrativeIntroText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in narrativeLines)
                    {
                        typingLabel.Text = ""; // Очищаем Label перед печатанием новой строки
                        currentTextCharIndex = 0; // Сбрасываем индекс для новой строки

                        fullIntroText = line.Trim(); // Устанавливаем текущую строку и удаляем лишние пробелы/переносы
                        await WaitForTypingCompletion(); // Ждем завершения печати текущей строки

                        await Task.Delay(LineDelayMs); // Пауза в 1 секунду после каждой строки
                    }
                    // --- КОНЕЦ ИЗМЕНЕНИЙ ---

                    await Task.Delay(LongPauseMs); // Пауза после всей пред истории

                    currentStage = CutsceneStage.Finished;
                    await ProcessCutsceneStage(); // Переходим к завершению
                    break;

                case CutsceneStage.Finished:
                    // --- ПЕРЕХОД НА НОВУЮ ФОРМУ ---
                    gifPictureBox.Hide();
                    typingLabel.Hide();

                    this.Hide(); // Скрываем текущую форму катсцены
                    GameplayForm gameplayForm = new GameplayForm(); // Создаем экземпляр новой формы
                    gameplayForm.Show(); // Показываем новую форму
                    this.Close(); // Закрываем форму катсцены, она больше не нужна
                    break;
            }
        }

        // --- Хелпер метод для ожидания завершения печати текста ---
        private async Task WaitForTypingCompletion()
        {
            _typingCompletionSource = new TaskCompletionSource<bool>();
            typingTimer.Start();
            await _typingCompletionSource.Task;
            typingTimer.Stop(); // Гарантируем остановку таймера после завершения
        }

        // --- Обработчик события Tick таймера для печати текста ---
        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            if (currentTextCharIndex < fullIntroText.Length)
            {
                typingLabel.Text += fullIntroText[currentTextCharIndex];
                textSoundPlayer.Play(); // Воспроизвести звук буквы

                currentTextCharIndex++;
            }
            else // Текущий фрагмент текста полностью напечатан
            {
                // Сигнализируем, что печать завершена для текущей части текста
                _typingCompletionSource?.TrySetResult(true);
            }
        }

        // --- Методы для управления GIF-анимацией (ручное переключение кадров) ---
        private void StartGifAnimation()
        {
            _gifFrameTimer.Stop(); // Останавливаем предыдущий таймер, если он был активен

            if (gifPictureBox.Image != null)
            {
                gifPictureBox.Image.Dispose(); // Освобождаем ресурсы предыдущего изображения
            }

            gifPictureBox.Image = Properties.Resources.intro_gif; // Загружаем GIF из ресурсов
            _gifTotalFrames = gifPictureBox.Image.GetFrameCount(FrameDimension.Time);
            _gifCurrentFrameIndex = 0;
            _gifPlayedOnce = false; // Сбрасываем флаг проигрывания перед стартом
            _gifPlaying = true;

            // Запускаем наш собственный таймер для покадровой анимации GIF
            if (_gifTotalFrames > 0)
            {
                _gifFrameTimer.Start();
            }
        }

        private void GifFrameTimer_Tick(object sender, EventArgs e)
        {
            if (_gifPlaying && !_gifPlayedOnce)
            {
                // Переходим к следующему кадру
                _gifCurrentFrameIndex++;

                if (_gifCurrentFrameIndex < _gifTotalFrames)
                {
                    // Выбираем активный кадр
                    gifPictureBox.Image.SelectActiveFrame(FrameDimension.Time, _gifCurrentFrameIndex);
                }
                else // GIF проигрался до конца
                {
                    _gifPlayedOnce = true; // Отмечаем, что проиграли один раз
                    StopGifAnimation();    // Останавливаем таймер
                    // Убеждаемся, что GIF остается на последнем кадре
                    if (_gifTotalFrames > 0)
                    {
                        gifPictureBox.Image.SelectActiveFrame(FrameDimension.Time, _gifTotalFrames - 1);
                    }
                }
                gifPictureBox.Invalidate(); // Перерисовываем PictureBox для отображения нового кадра
            }
        }

        private void StopGifAnimation()
        {
            if (_gifPlaying)
            {
                _gifFrameTimer.Stop(); // Останавливаем наш таймер
                _gifPlaying = false;
                // Убеждаемся, что изображение остается на последнем кадре, если оно было
                if (_gifTotalFrames > 0 && _gifCurrentFrameIndex > 0)
                {
                    gifPictureBox.Image.SelectActiveFrame(FrameDimension.Time, _gifTotalFrames - 1);
                }
                gifPictureBox.Invalidate(); // Перерисовать, чтобы показать последний кадр
            }
        }

        // --- Переопределение OnFormClosing для освобождения ресурсов ---
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

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
            // Останавливаем и освобождаем ресурсы GIF-таймера
            if (_gifFrameTimer != null)
            {
                _gifFrameTimer.Stop();
                _gifFrameTimer.Dispose();
            }
            // Освобождаем Image из PictureBox
            if (gifPictureBox != null && gifPictureBox.Image != null)
            {
                gifPictureBox.Image.Dispose();
            }
        }
    }
}
