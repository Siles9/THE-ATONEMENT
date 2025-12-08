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

        // События
        public event EventHandler GameTimeUpdated;
        public event EventHandler MedicinesUpdated;

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
            _gameTimer.Interval = 1000; // Обновляем каждую секунду
            _gameTimer.Tick += (s, e) => UpdateGameTime();
            _gameTimer.Start();
        }

        public void ResetGame()
        {
            // Создаем нового енота
            _currentTamagotchi = new Tamagotchi();

            // Устанавливаем время на 12:00
            _currentGameTime = new TimeSpan(12, 0, 0);

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
            // Добавляем 1 минуту игрового времени каждую секунду реального времени
            CurrentGameTime = CurrentGameTime.Add(TimeSpan.FromMinutes(1));

            // Если прошли сутки, сбрасываем на 0:00
            if (CurrentGameTime.Days > 0)
            {
                CurrentGameTime = new TimeSpan(0, CurrentGameTime.Hours, CurrentGameTime.Minutes, CurrentGameTime.Seconds);
            }

            // Уменьшаем параметры енота каждую минуту игрового времени
            _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(1));
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
        public bool IsNightTime
        {
            get
            {
                int hour = CurrentGameTime.Hours;
                return hour >= 0 && hour < 6; // Ночь с 0:00 до 6:00
            }
        }

        public bool IsDangerTime
        {
            get
            {
                int hour = CurrentGameTime.Hours;
                return hour >= 2 && hour < 4; // Самые опасные часы
            }
        }
    }
}