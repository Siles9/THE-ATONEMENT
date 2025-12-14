using System;
using System.Windows.Forms;

namespace Rac_Night
{
    public class GameManager
    {
        private static GameManager _instance;
        private static readonly object _lock = new object();

        private int _currentNight = 1;
        private Tamagotchi _currentTamagotchi;
        private TimeSpan _currentGameTime;
        private int _medicinesLeft;
        private Timer _gameTimer;
        private bool _isGameActive = false;

        private const int NIGHT_START_HOUR = 0;
        private const int NIGHT_END_HOUR = 6;
        private const int GAME_MINUTES_PER_SECOND = 2;

        private float[] _decayMultipliers = { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f, 2.3f };
        private float[] _ghostSpawnMultipliers = { 1.0f, 1.3f, 1.6f, 1.9f, 2.2f, 2.5f };
        private float[] _ghostIntervalMultipliers = { 1.0f, 0.85f, 0.7f, 0.6f, 0.5f, 0.4f };

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
                if (_currentNight >= 1 && _currentNight <= 6)
                    return _decayMultipliers[_currentNight - 1];
                return 1.0f;
            }
        }

        public float GhostSpawnFrequencyMultiplier
        {
            get
            {
                if (_currentNight >= 1 && _currentNight <= 6)
                    return _ghostSpawnMultipliers[_currentNight - 1];
                return 1.0f;
            }
        }

        public float GhostIntervalMultiplier
        {
            get
            {
                if (_currentNight >= 1 && _currentNight <= 6)
                    return _ghostIntervalMultipliers[_currentNight - 1];
                return 1.0f;
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

        public bool IsNightTime => CurrentGameTime.Hours >= NIGHT_START_HOUR &&
                                  CurrentGameTime.Hours < NIGHT_END_HOUR;
        public bool IsDangerTime => CurrentGameTime.Hours >= 2 && CurrentGameTime.Hours < 4;
        public bool IsNightOver => CurrentGameTime.Hours >= NIGHT_END_HOUR;

        public event EventHandler GameTimeUpdated;
        public event EventHandler MedicinesUpdated;
        public event EventHandler NightEnded;

        private GameManager()
        {
            _currentTamagotchi = new Tamagotchi();
            CurrentGameTime = new TimeSpan(NIGHT_START_HOUR, 0, 0);
            MedicinesLeft = 3;
            SetupGameTimer();
        }

        private void SetupGameTimer()
        {
            _gameTimer = new Timer();
            _gameTimer.Interval = 1000;
            _gameTimer.Tick += GameTimer_Tick;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (!_isGameActive || _currentTamagotchi == null) return;

            CurrentGameTime = CurrentGameTime.Add(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND));

            float timeMultiplier = DifficultyMultiplier;
            _currentTamagotchi.DecreaseStats(TimeSpan.FromMinutes(GAME_MINUTES_PER_SECOND * timeMultiplier));

            if (IsNightOver)
            {
                StopGame();
                NightEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        public void StartGame()
        {
            _isGameActive = true;

            if (_gameTimer != null && !_gameTimer.Enabled)
            {
                _gameTimer.Start();
            }
        }

        public void ResetGame(int nightNumber = 1)
        {
            _currentNight = nightNumber;

            if (_currentTamagotchi != null)
            {
                _currentTamagotchi.Reset();
            }
            else
            {
                _currentTamagotchi = new Tamagotchi();
            }

            CurrentGameTime = new TimeSpan(NIGHT_START_HOUR, 0, 0);

            switch (nightNumber)
            {
                case 1: MedicinesLeft = 3; break;
                case 2: MedicinesLeft = 3; break;
                case 3: MedicinesLeft = 2; break;
                case 4: MedicinesLeft = 1; break;
                case 5: MedicinesLeft = 0; break;
                case 6: MedicinesLeft = 3; break;
                default: MedicinesLeft = 3; break;
            }

            _isGameActive = false;

            if (_gameTimer != null && _gameTimer.Enabled)
            {
                _gameTimer.Stop();
            }
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
            }
        }

        public int GetRemainingNightMinutes()
        {
            TimeSpan morning = TimeSpan.FromHours(NIGHT_END_HOUR);
            TimeSpan remaining = morning - CurrentGameTime;
            return (int)Math.Max(0, remaining.TotalMinutes);
        }

        public System.Drawing.Color GetTimeColor()
        {
            int hour = CurrentGameTime.Hours;
            if (hour >= 0 && hour < 2) return System.Drawing.Color.LimeGreen;
            else if (hour >= 2 && hour < 4) return System.Drawing.Color.Orange;
            else if (hour >= 4 && hour < 6) return System.Drawing.Color.Red;
            else return System.Drawing.Color.Gold;
        }

        public float GetGhostSpawnChance(int hour)
        {
            float baseChance;

            if (hour >= 0 && hour < 2)
                baseChance = 60f;
            else if (hour >= 2 && hour < 4)
                baseChance = 75f;
            else if (hour >= 4 && hour < 6)
                baseChance = 90f;
            else
                baseChance = 0f;

            return Math.Min(100f, baseChance * GhostSpawnFrequencyMultiplier);
        }

        public int GetGhostInterval(int hour)
        {
            int baseInterval;

            if (hour >= 0 && hour < 2)
                baseInterval = 30000;
            else if (hour >= 2 && hour < 4)
                baseInterval = 25500;
            else if (hour >= 4 && hour < 6)
                baseInterval = 20000;
            else
                baseInterval = 60000;

            return (int)(baseInterval * GhostIntervalMultiplier);
        }
    }
}