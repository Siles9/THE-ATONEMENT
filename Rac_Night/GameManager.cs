using System;
using System.Windows.Forms;

namespace Rac_Night
{
    public class GameManager
    {
        private static GameManager _instance;
        private static readonly object _lock = new object();

        private int _currentNight = 1;

        public int CurrentNight
        {
            get => _currentNight;
            set => _currentNight = value;
        }

        public static GameManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new GameManager();
                    return _instance;
                }
            }
        }

        // Поля
        private Tamagotchi _currentTamagotchi;
        private TimeSpan _currentGameTime;
        private int _medicinesLeft;
        private bool _isSicknessTimerActive;
        private Timer _gameTimer;
        private bool _isGameActive = false;


        // Константы для настройки времени
        private const int NIGHT_START_HOUR = 0;
        private const int NIGHT_END_HOUR = 6;
        private const int GAME_MINUTES_PER_SECOND = 2;

        // Свойства
        public Tamagotchi CurrentTamagotchi => _currentTamagotchi;
        public bool IsGameActive => _isGameActive;

        public TimeSpan CurrentGameTime
        {
            get => _currentGameTime;
            private set
            {
                if (_currentGameTime != value)
                {
                    _currentGameTime = value;
                    GameTimeUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public float DifficultyMultiplier
        {
            get
            {
                switch (CurrentNight)
                {
                    case 1: return 1.0f;  // Базовая сложность
                    case 2: return 1.2f;  // На 20% сложнее
                    case 3: return 1.5f;  // На 50% сложнее
                    case 4: return 1.8f;  // На 80% сложнее
                    case 5: return 2.0f;  // В 2 раза сложнее
                    case 6: return 2.0f;  // Как 5-я ночь
                    default: return 1.0f;
                }
            }
        }
        public int MedicinesLeft
        {
            get => _medicinesLeft;
            private set
            {
                if (_medicinesLeft != value)
                {
                    _medicinesLeft = value;
                    MedicinesUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsSicknessTimerActive => _isSicknessTimerActive;

        public bool IsNightTime
        {
            get
            {
                int hour = CurrentGameTime.Hours;
                return hour >= NIGHT_START_HOUR && hour < NIGHT_END_HOUR;
            }
        }

        public bool IsDangerTime
        {
            get
            {
                int hour = CurrentGameTime.Hours;
                return hour >= 2 && hour < 4;
            }
        }

        public TimeSpan TimeUntilMorning
        {
            get
            {
                TimeSpan current = CurrentGameTime;
                TimeSpan morning = TimeSpan.FromHours(NIGHT_END_HOUR);
                if (current < morning)
                    return morning - current;
                else
                    return TimeSpan.Zero;
            }
        }

        public bool IsNightOver => CurrentGameTime.Hours >= NIGHT_END_HOUR;

        // События
        public event EventHandler GameTimeUpdated;
        public event EventHandler MedicinesUpdated;
        public event EventHandler NightEnded;

        private GameManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            ResetGame();
            SetupGameTimer();
        }

        private void SetupGameTimer()
        {
            _gameTimer = new Timer();
            _gameTimer.Interval = 1000;
            _gameTimer.Tick += (s, e) => UpdateGameTime();
            _gameTimer.Stop(); // Не запускаем сразу
        }

        public void StartGame()
        {
            _isGameActive = true;
            _gameTimer.Start();
        }

        public void ResetGame(int nightNumber = 1)
        {
            CurrentNight = nightNumber;

            if (_currentTamagotchi != null)
            {
                _currentTamagotchi.SicknessStatusChanged -= OnTamagotchiSicknessChanged;
            }

            _currentTamagotchi = new Tamagotchi();
            CurrentGameTime = new TimeSpan(NIGHT_START_HOUR, 0, 0);

            // Количество лекарств в зависимости от ночи
            switch (nightNumber)
            {
                case 1:
                    MedicinesLeft = 3;
                    break;
                case 2:
                    MedicinesLeft = 3;
                    break;
                case 3:
                    MedicinesLeft = 2;
                    break;
                case 4:
                    MedicinesLeft = 1;
                    break;
                case 5:
                    MedicinesLeft = 0;
                    break;
                case 6:
                    MedicinesLeft = 3;
                    break;
                default:
                    MedicinesLeft = 3;
                    break;
            }

            _isSicknessTimerActive = false;
            _isGameActive = false;
            _currentTamagotchi.SicknessStatusChanged += OnTamagotchiSicknessChanged;
        }

        private void OnTamagotchiSicknessChanged(object sender, EventArgs e)
        {
            _isSicknessTimerActive = _currentTamagotchi.IsSick;
            GameTimeUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateGameTime()
        {
            if (!_isGameActive) return;
            if (IsNightOver)
            {
                OnNightEnded();
                return;
            }

            CurrentGameTime = CurrentGameTime.Add(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND));

            // Передаем множитель сложности
            float difficultyMultiplier = DifficultyMultiplier;

            _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND), difficultyMultiplier);

            if (_currentTamagotchi.IsSick)
            {
                _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND * 2), difficultyMultiplier);
            }

            if (IsDangerTime)
            {
                _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND), difficultyMultiplier);
            }
        }

        private void OnNightEnded()
        {
            if (_gameTimer != null && _gameTimer.Enabled)
            {
                _gameTimer.Stop();
            }

            NightEnded?.Invoke(this, EventArgs.Empty);
        }

        public bool TryUseMedicine()
        {
            if (MedicinesLeft <= 0)
                return false;

            MedicinesLeft--;
            return true;
        }

        public void StopGame()
        {
            _isGameActive = false;
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
                _gameTimer = null;
            }
        }

        public int GetRemainingNightMinutes()
        {
            return (int)TimeUntilMorning.TotalMinutes;
        }

        public bool IsWarningTime(int minutesBefore)
        {
            int remainingMinutes = GetRemainingNightMinutes();
            return remainingMinutes <= minutesBefore && remainingMinutes > 0;
        }

        public string GetTimeDescription()
        {
            int hour = CurrentGameTime.Hours;
            if (hour >= 0 && hour < 2)
                return "Ранняя ночь";
            else if (hour >= 2 && hour < 4)
                return "Полночь";
            else if (hour >= 4 && hour < 6)
                return "Предрассветное время";
            else
                return "Утро";
        }

        public System.Drawing.Color GetTimeColor()
        {
            int hour = CurrentGameTime.Hours;
            if (hour >= 0 && hour < 2)
                return System.Drawing.Color.LimeGreen;
            else if (hour >= 2 && hour < 4)
                return System.Drawing.Color.Orange;
            else if (hour >= 4 && hour < 6)
                return System.Drawing.Color.Red;
            else
                return System.Drawing.Color.Gold;
        }

        public void AddGameTime(TimeSpan timeToAdd)
        {
            CurrentGameTime = CurrentGameTime.Add(timeToAdd);
            if (IsNightOver)
            {
                OnNightEnded();
            }
        }
    }
}