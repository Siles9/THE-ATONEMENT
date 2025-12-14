using System;

namespace Rac_Night
{
    public class Tamagotchi
    {
        private double _hunger = 100;
        private double _play = 100;
        private double _hygiene = 100;
        private double _health = 100;

        public double Hunger => _hunger;
        public double Play => _play;
        public double Hygiene => _hygiene;
        public double Health => _health;

        public bool IsSick { get; private set; } = false;
        public int ZeroStatCount { get; private set; } = 0;

        public event EventHandler StatsChanged;
        public event EventHandler SicknessStatusChanged;

        private const double BaseHungerDecayPerMinute = 1.5;
        private const double BasePlayDecayPerMinute = 1.3;
        private const double BaseHygieneDecayPerMinute = 1.2;

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
            _hunger = Clamp(_hunger + amount);
            OnStatsChanged();
        }

        public void PlayWith(double amount = 40)
        {
            if (amount <= 0) return;
            _play = Clamp(_play + amount);
            OnStatsChanged();
        }

        public void Wash(double amount = 55)
        {
            if (amount <= 0) return;
            _hygiene = Clamp(_hygiene + amount);
            OnStatsChanged();
        }

        public void Heal(double amount = 60)
        {
            if (amount <= 0) return;
            _health = Clamp(_health + amount);
            OnStatsChanged();
        }

        public void Cure()
        {
            if (!IsSick) return;
            IsSick = false;
            SicknessStatusChanged?.Invoke(this, EventArgs.Empty);
            OnStatsChanged();
        }

        public void DecreaseAllStatsByPercentage(double percentage)
        {
            if (percentage <= 0) return;
            double amount = percentage / 100.0;
            _hunger = Clamp(_hunger - (_hunger * amount));
            _play = Clamp(_play - (_play * amount));
            _hygiene = Clamp(_hygiene - (_hygiene * amount));
            _health = Clamp(_health - (_health * amount));
            UpdateCriticalState();
            OnStatsChanged();
        }

        public void DecreaseStats(TimeSpan elapsed)
        {
            double minutes = Math.Max(0, elapsed.TotalMinutes);
            if (minutes <= 0) return;

            _hunger = Clamp(_hunger - (BaseHungerDecayPerMinute * minutes));
            _play = Clamp(_play - (BasePlayDecayPerMinute * minutes));
            _hygiene = Clamp(_hygiene - (BaseHygieneDecayPerMinute * minutes));

            double healthDecay = 0;
            if (_hunger < 30) healthDecay += 0.3;
            if (_play < 30) healthDecay += 0.2;
            if (_hygiene < 30) healthDecay += 0.25;

            _health = Clamp(_health - (healthDecay * minutes));

            UpdateCriticalState();
            OnStatsChanged();
        }

        private void UpdateCriticalState()
        {
            int newZeroStatCount = 0;
            if (_hunger <= 15) newZeroStatCount++;
            if (_play <= 15) newZeroStatCount++;
            if (_hygiene <= 15) newZeroStatCount++;
            if (_health <= 15) newZeroStatCount++;

            ZeroStatCount = newZeroStatCount;

            if (ZeroStatCount >= 2 && !IsSick)
            {
                IsSick = true;
                SicknessStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private double Clamp(double value)
        {
            return Math.Max(0, Math.Min(100, value));
        }

        protected void OnStatsChanged()
        {
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}