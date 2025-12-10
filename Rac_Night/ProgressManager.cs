using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Rac_Night
{
    public static class ProgressManager
    {
        private static string SavePath = "progress.json";
        private static GameProgress _progress;

        static ProgressManager()
        {
            LoadProgress();
        }

        public static GameProgress Progress => _progress;

        public static void SaveProgress()
        {
            try
            {
                string json = JsonSerializer.Serialize(_progress);
                File.WriteAllText(SavePath, json);

                Debug.WriteLine($"Прогресс сохранен в {SavePath}");
                Debug.WriteLine($"Содержимое: {json}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения прогресса: {ex.Message}");
            }
        }

        public static void LoadProgress()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    _progress = JsonSerializer.Deserialize<GameProgress>(json);
                }
                else
                {
                    _progress = new GameProgress();
                }
            }
            catch
            {
                _progress = new GameProgress();
            }
        }

        public static void CompleteNight(int nightNumber)
        {
            if (nightNumber < 1 || nightNumber > 6) return;

            if (nightNumber >= 1 && nightNumber <= 5)
            {
                if (nightNumber > _progress.NightsCompleted)
                {
                    _progress.NightsCompleted = nightNumber;
                }
            }
            else if (nightNumber == 6) // Бонусная ночь
            {
                _progress.BonusNightCompleted = true;
            }

            SaveProgress();
        }

        public static bool IsNightAvailable(int nightNumber)
        {
            if (nightNumber < 1 || nightNumber > 6) return false;

            if (nightNumber == 6)
            {
                return _progress.NightsCompleted >= 5;
            }

            return nightNumber <= _progress.NightsCompleted + 1;
        }
    }

    public class GameProgress
    {
        public int NightsCompleted { get; set; } = 0;
        public bool BonusNightCompleted { get; set; } = false;
        public bool IsFirstNightCompleted => NightsCompleted >= 1;
    }
}
