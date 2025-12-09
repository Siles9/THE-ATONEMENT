using System;

namespace Rac_Night
{
    public class Tamagotchi
    {
        // Параметры
        private double _hunger = 100;
        private double _play = 100;
        private double _hygiene = 100;
        private double _health = 100;

        public double Hunger
        {
            get => _hunger;
            private set => _hunger = Clamp(value);
        }

        public double Play
        {
            get => _play;
            private set => _play = Clamp(value);
        }

        public double Hygiene
        {
            get => _hygiene;
            private set => _hygiene = Clamp(value);
        }

        public double Health
        {
            get => _health;
            private set => _health = Clamp(value);
        }

        public bool IsSick { get; private set; } = false;
        public int ZeroStatCount { get; private set; } = 0;

        public event EventHandler StatsChanged;
        public event EventHandler SicknessStatusChanged;

        private const double HungerDecayPerMinute = 1.0;
        private const double PlayDecayPerMinute = 0.9;
        private const double HygieneDecayPerMinute = 0.8;
        private const double HealthDecayPerMinute = 0.00;

        private const double CriticalDecayMultiplier = 1.15; // +15% при критическом состоянии

        // Метод для сброса состояния
        public void Reset()
        {
            _hunger = 100;
            _play = 100;
            _hygiene = 100;
            _health = 100;
            IsSick = false;
            ZeroStatCount = 0;
            OnStatsChanged();
        }

        public void Feed(double amount = 50)
        {
            if (amount <= 0) return;
            Hunger += amount;
            OnStatsChanged();
        }

        public void PlayWith(double amount = 40)
        {
            if (amount <= 0) return;
            Play += amount;
            OnStatsChanged();
        }

        public void Wash(double amount = 55)
        {
            if (amount <= 0) return;
            Hygiene += amount;
            OnStatsChanged();
        }

        public void Heal(double amount = 60)
        {
            if (amount <= 0) return;
            Health += amount;
            OnStatsChanged();
        }

        public void Cure()
        {
            if (!IsSick) return;

            SetSickness(false);
            OnStatsChanged();
        }

        public void DecreaseAllStatsByPercentage(double percentage)
        {
            if (percentage <= 0) return;

            double amount = percentage / 100.0;
            Hunger -= Hunger * amount;
            Play -= Play * amount;
            Hygiene -= Hygiene * amount;
            Health -= Health * amount;

            UpdateCriticalState();
            OnStatsChanged();
        }

        public void DecreaseStats(TimeSpan elapsed)
        {
            double minutes = Math.Max(0, elapsed.TotalMinutes);
            if (minutes <= 0) return;

            // Проверяем, есть ли нулевые параметры для множителя
            bool hasZeroStat = Hunger <= 0 || Play <= 0 || Hygiene <= 0 || Health <= 0;
            double decayMultiplier = hasZeroStat ? CriticalDecayMultiplier : 1.0;

            // Падение параметров (несинхронно)
            Hunger -= HungerDecayPerMinute * minutes * decayMultiplier;
            Play -= PlayDecayPerMinute * minutes * decayMultiplier;
            Hygiene -= HygieneDecayPerMinute * minutes * decayMultiplier;

            // Здоровье падает медленно и только при очень низких других параметрах
            double extraHealthDecay = 0;
            if (Hunger < 5) extraHealthDecay += 0.3;
            if (Play < 5) extraHealthDecay += 0.15;
            if (Hygiene < 5) extraHealthDecay += 0.2;

            Health -= (HealthDecayPerMinute + extraHealthDecay) * minutes;

            // Обновляем состояние
            UpdateCriticalState();
            OnStatsChanged();
        }

        private void UpdateCriticalState()
        {
            // Считаем нулевые параметры
            int newZeroStatCount = 0;
            if (Hunger <= 0) newZeroStatCount++;
            if (Play <= 0) newZeroStatCount++;
            if (Hygiene <= 0) newZeroStatCount++;
            if (Health <= 0) newZeroStatCount++;

            ZeroStatCount = newZeroStatCount;

            // Логика болезни
            if (ZeroStatCount >= 2 && !IsSick)
            {
                SetSickness(true);
            }
        }

        private void SetSickness(bool isSick)
        {
            if (IsSick != isSick)
            {
                IsSick = isSick;
                SicknessStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private double Clamp(double value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return value;
        }

        protected void OnStatsChanged()
        {
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}