using System;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace Rac_Night
{
    public class GameManager
    {
        private static GameManager _instance;
        private static readonly object _lock = new object();

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
        private DateTime _lastUpdateTime;
        private System.Windows.Forms.Timer _gameTimer;

        // Константы для настройки времени
        private const int NIGHT_START_HOUR = 0;   // 00:00
        private const int NIGHT_END_HOUR = 6;     // 06:00
        private const int GAME_MINUTES_PER_SECOND = 2; // 2 минуты игры за 1 секунду реального времени

        // Свойства
        public Tamagotchi CurrentTamagotchi => _currentTamagotchi;

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

        // Новые свойства для времени
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
                return hour >= 2 && hour < 4; // Самые опасные часы с 2 до 4 утра
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

        public bool IsNightOver
        {
            get
            {
                return CurrentGameTime.Hours >= NIGHT_END_HOUR;
            }
        }

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
            // Сбрасываем игру
            ResetGame();

            // Запускаем таймер времени игры
            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = 1000; // Обновляем каждую секунду реального времени
            _gameTimer.Tick += (s, e) => UpdateGameTime();
            _gameTimer.Start();
        }

        public void ResetGame()
        {
            // Создаем нового енота
            _currentTamagotchi = new Tamagotchi();

            // Устанавливаем время на 00:00 (начало ночи)
            _currentGameTime = new TimeSpan(NIGHT_START_HOUR, 0, 0);

            // Устанавливаем лекарства
            _medicinesLeft = 3;

            // Сбрасываем флаги
            _isSicknessTimerActive = false;

            // Запоминаем время старта
            _lastUpdateTime = DateTime.Now;

            // Подписываемся на события енота
            _currentTamagotchi.SicknessStatusChanged += OnTamagotchiSicknessChanged;
        }

        private void OnTamagotchiSicknessChanged(object sender, EventArgs e)
        {
            _isSicknessTimerActive = _currentTamagotchi.IsSick;
            GameTimeUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateGameTime()
        {
            // Добавляем игровое время
            CurrentGameTime = CurrentGameTime.Add(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND));

            // Проверяем, не закончилась ли ночь
            if (IsNightOver)
            {
                OnNightEnded();
                return;
            }

            // Уменьшаем параметры енота каждую минуту игрового времени
            _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND));

            // Если енот болен, параметры ухудшаются быстрее
            if (_currentTamagotchi.IsSick)
            {
                _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND * 2));
            }

            // В опасное время параметры ухудшаются еще быстрее
            if (IsDangerTime)
            {
                _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND));
            }
        }

        private void OnNightEnded()
        {
            // Останавливаем игровой таймер
            if (_gameTimer != null && _gameTimer.Enabled)
            {
                _gameTimer.Stop();
            }

            // Вызываем событие окончания ночи
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
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer.Dispose();
                _gameTimer = null;
            }
        }

        // Методы для расчета оставшегося времени
        public int GetRemainingNightMinutes()
        {
            return (int)TimeUntilMorning.TotalMinutes;
        }

        public bool IsWarningTime(int minutesBefore)
        {
            int remainingMinutes = GetRemainingNightMinutes();
            return remainingMinutes <= minutesBefore && remainingMinutes > 0;
        }

        // Метод для получения текстового описания времени
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

        // Метод для получения цвета времени
        public System.Drawing.Color GetTimeColor()
        {
            int hour = CurrentGameTime.Hours;

            if (hour >= 0 && hour < 2)
                return System.Drawing.Color.LimeGreen; // Ранняя ночь
            else if (hour >= 2 && hour < 4)
                return System.Drawing.Color.Orange;    // Полночь
            else if (hour >= 4 && hour < 6)
                return System.Drawing.Color.Red;       // Предрассветное время
            else
                return System.Drawing.Color.Gold;      // Утро
        }

        // Метод для ручного добавления времени (для тестирования)
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