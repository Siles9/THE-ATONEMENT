using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Rac_Night
{
    public static class VolumeManager
    {
        private const string SettingsFile = "game_settings.ini";

        [DllImport("winmm.dll")]
        private static extern uint waveOutGetVolume(uint hwo, out uint pdwVolume);

        [DllImport("winmm.dll")]
        private static extern uint waveOutSetVolume(uint hwo, uint dwVolume);

        private static float _currentVolume = 0.5f;

        public static float CurrentVolume
        {
            get => _currentVolume;
            set
            {
                if (value < 0.0f) value = 0.0f;
                if (value > 1.0f) value = 1.0f;

                _currentVolume = value;
                SetSystemVolume(value);
                SaveSettings();
            }
        }

        static VolumeManager()
        {
            LoadSettings();
            SetSystemVolume(_currentVolume);
        }

        private static void SetSystemVolume(float volume)
        {
            uint newVolume = (uint)(volume * 65535);
            waveOutSetVolume(0, newVolume | (newVolume << 16));
        }

        private static void SaveSettings()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(SettingsFile))
                {
                    writer.WriteLine($"Volume={_currentVolume}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private static void LoadSettings()
        {
            if (!File.Exists(SettingsFile)) return;

            try
            {
                string line;
                using (StreamReader reader = new StreamReader(SettingsFile))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Volume="))
                        {
                            if (float.TryParse(line.Substring(7), out float volume))
                            {
                                _currentVolume = volume;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}");
            }
        }

        public static void ApplyVolume(System.Media.SoundPlayer sound)
        {
            // Громкость управляется через системную настройку
        }
    }
}