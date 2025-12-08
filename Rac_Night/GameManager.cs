using System;
using System.IO;
using System.Text.Json;
using System.Timers;
using System.Reflection;

namespace Rac_Night
{
    public enum GhostType { Black, Brown, White }

    public class GameManager
    {
        private static readonly Lazy<GameManager> _lazy = new Lazy<GameManager>(() => new GameManager());
        public static GameManager Instance => _lazy.Value;

        public Tamagotchi CurrentTamagotchi { get; private set; }

        // Таймеры
        private Timer _decayTimer;
        private const int DecayIntervalMs = 3 * 1000; // Каждые 3 секунды
        private DateTime _lastTick;

        // Игровое время
        public DateTime GameStartTime { get; private set; }
        public DateTime CurrentGameTime { get; private set; }
        private Timer _gameClockTimer;
        private Timer _ghostSpawnTimer;
        private const int RealTimePerGameHour = 100 * 1000; // 100 секунд на игровой час

        // Болезнь
        private Timer _sicknessTimer;
        private const int SicknessTimeoutMs = 20 * 1000; // 20 секунд на лечение
        public bool IsSicknessTimerActive { get; private set; } = false;
        public int MedicinesLeft { get; private set; } = 2;

        // События
        public event EventHandler GameTimeUpdated;
        public event EventHandler<GhostType> GhostSpawned;
        public event EventHandler SicknessTimeout;
        public event EventHandler MedicinesUpdated;

        private string SaveFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RacNight", "tamagotchi.json");

        private GameManager()
        {
            Load();
            StartDecayTimer();
            StartGameClock();
            CurrentTamagotchi.SicknessStatusChanged += CurrentTamagotchi_SicknessStatusChanged;
        }

        private void StartDecayTimer()
        {
            _lastTick = DateTime.UtcNow;
            _decayTimer = new Timer(DecayIntervalMs);
            _decayTimer.Elapsed += DecayTimer_Elapsed;
            _decayTimer.AutoReset = true;
            _decayTimer.Start();
        }

        private void DecayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan delta = now - _lastTick;
            _lastTick = now;

            CurrentTamagotchi.DecreaseStats(delta);
            Save();
        }

        private void StartGameClock()
        {
            GameStartTime = DateTime.Today.AddHours(0); // 00:00 (12:00 AM)
            CurrentGameTime = GameStartTime;

            _gameClockTimer = new Timer(1000); // 1 секунда реального времени
            _gameClockTimer.Elapsed += GameClockTimer_Elapsed;
            _gameClockTimer.AutoReset = true;
            _gameClockTimer.Start();

            StartGhostSpawnTimer();
        }

        private void GameClockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // 1 секунда реального = 0.6 минуты игрового (100 сек/час = 1.667 сек/мин)
            CurrentGameTime = CurrentGameTime.AddMinutes(0.6);
            GameTimeUpdated?.Invoke(this, EventArgs.Empty);

            // Проверка на окончание ночи (6:00 AM)
            if (CurrentGameTime.Hour >= 6)
            {
                StopGame();
                // TODO: Вызвать событие победы
            }
        }

        private void StartGhostSpawnTimer()
        {
            if (_ghostSpawnTimer == null)
            {
                _ghostSpawnTimer = new Timer();
                _ghostSpawnTimer.Elapsed += GhostSpawnTimer_Elapsed;
                _ghostSpawnTimer.AutoReset = true;
            }

            UpdateGhostSpawnInterval();
            _ghostSpawnTimer.Start();
        }

        private void UpdateGhostSpawnInterval()
        {
            int hour = CurrentGameTime.Hour;
            int intervalMs;

            if (hour >= 0 && hour < 2) // 12 AM - 2 AM
            {
                intervalMs = 20 * 1000;
            }
            else if (hour >= 2 && hour < 4) // 2 AM - 4 AM
            {
                intervalMs = 15 * 1000;
            }
            else if (hour >= 4 && hour < 6) // 4 AM - 6 AM
            {
                intervalMs = 10 * 1000;
            }
            else
            {
                intervalMs = 20 * 1000;
            }

            if (_ghostSpawnTimer.Interval != intervalMs)
            {
                _ghostSpawnTimer.Interval = intervalMs;
            }
        }

        private void GhostSpawnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UpdateGhostSpawnInterval();

            Random rnd = new Random();
            int ghostIndex = rnd.Next(0, 3);
            GhostType type = (GhostType)ghostIndex;

            GhostSpawned?.Invoke(this, type);
        }

        private void CurrentTamagotchi_SicknessStatusChanged(object sender, EventArgs e)
        {
            if (CurrentTamagotchi.IsSick)
            {
                StartSicknessTimer();
            }
            else
            {
                StopSicknessTimer();
            }
        }

        private void StartSicknessTimer()
        {
            if (IsSicknessTimerActive) return;

            IsSicknessTimerActive = true;
            if (_sicknessTimer == null)
            {
                _sicknessTimer = new Timer(SicknessTimeoutMs);
                _sicknessTimer.AutoReset = false;
                _sicknessTimer.Elapsed += SicknessTimer_Elapsed;
            }
            else
            {
                _sicknessTimer.Interval = SicknessTimeoutMs;
            }
            _sicknessTimer.Start();
        }

        private void StopSicknessTimer()
        {
            if (_sicknessTimer != null)
            {
                _sicknessTimer.Stop();
            }
            IsSicknessTimerActive = false;
        }

        private void SicknessTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StopSicknessTimer();
            SicknessTimeout?.Invoke(this, EventArgs.Empty);
            StopGame();
        }

        public bool TryUseMedicine()
        {
            if (MedicinesLeft > 0 && CurrentTamagotchi.IsSick)
            {
                MedicinesLeft--;
                CurrentTamagotchi.Cure();
                StopSicknessTimer(); // Останавливаем таймер после лечения
                MedicinesUpdated?.Invoke(this, EventArgs.Empty);
                Save();
                return true;
            }
            return false;
        }

        public void StopGame()
        {
            _decayTimer?.Stop();
            _decayTimer?.Dispose();

            _gameClockTimer?.Stop();
            _gameClockTimer?.Dispose();

            _ghostSpawnTimer?.Stop();
            _ghostSpawnTimer?.Dispose();

            StopSicknessTimer();
            _sicknessTimer?.Dispose();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SaveFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var model = new SaveModel
                {
                    Hunger = CurrentTamagotchi.Hunger,
                    Play = CurrentTamagotchi.Play,
                    Hygiene = CurrentTamagotchi.Hygiene,
                    Health = CurrentTamagotchi.Health,
                    MedicinesLeft = this.MedicinesLeft,
                    LastSavedUtc = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
            }
            catch { }
        }

        private void Load()
        {
            CurrentTamagotchi = new Tamagotchi();
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    var json = File.ReadAllText(SaveFilePath);
                    var model = JsonSerializer.Deserialize<SaveModel>(json);
                    if (model != null)
                    {
                        var tType = typeof(Tamagotchi);
                        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                        tType.GetProperty("Hunger", bindingFlags)?.SetValue(CurrentTamagotchi, model.Hunger);
                        tType.GetProperty("Play", bindingFlags)?.SetValue(CurrentTamagotchi, model.Play);
                        tType.GetProperty("Hygiene", bindingFlags)?.SetValue(CurrentTamagotchi, model.Hygiene);
                        tType.GetProperty("Health", bindingFlags)?.SetValue(CurrentTamagotchi, model.Health);

                        MedicinesLeft = model.MedicinesLeft;

                        // Учитываем время простоя
                        if (model.LastSavedUtc != default)
                        {
                            var elapsed = DateTime.UtcNow - model.LastSavedUtc;
                            if (elapsed.TotalSeconds > 1)
                            {
                                CurrentTamagotchi.DecreaseStats(elapsed);
                            }
                        }
                    }
                }
            }
            catch
            {
                CurrentTamagotchi = new Tamagotchi();
                MedicinesLeft = 2;
            }
        }

        private class SaveModel
        {
            public double Hunger { get; set; }
            public double Play { get; set; }
            public double Hygiene { get; set; }
            public double Health { get; set; }
            public int MedicinesLeft { get; set; }
            public DateTime LastSavedUtc { get; set; }
        }
    }
}