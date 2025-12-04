using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rac_Night
{
    public static class VolumeManager
    {
        // Константы для работы с громкостью
        private const uint AUDIO_DEVICE_ROLE_CONSOLE = 0x00000001;
        private const uint AUDCLNT_SHAREMODE_SHARED = 0x00000001;
        private const uint AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00000004;
        private const uint AUDCLNT_STREAMFLAGS_AUTOCONVERT = 0x00000008;

        // Импорт функций Windows API
        [DllImport("winmm.dll")]
        private static extern uint waveOutGetVolume(uint hwo, out uint pdwVolume);

        [DllImport("winmm.dll")]
        private static extern uint waveOutSetVolume(uint hwo, uint dwVolume);

        // Путь к файлу настроек
        private const string SettingsFile = "game_settings.ini";

        // Минимальное и максимальное значение громкости
        private const float MinVolume = 0.0f;
        private const float MaxVolume = 1.0f;

        // Текущее значение громкости
        private static float _currentVolume = 0.5f;

        // Свойство для получения и установки текущей громкости
        public static float CurrentVolume
        {
            get => _currentVolume;
            set
            {
                if (value < MinVolume) value = MinVolume;
                if (value > MaxVolume) value = MaxVolume;

                _currentVolume = value;
                SetSystemVolume(value);
                SaveSettings();
            }
        }

        // Конструктор
        static VolumeManager()
        {
            // Загружаем сохранённые настройки при запуске
            LoadSettings();
            // Устанавливаем системную громкость при старте
            SetSystemVolume(_currentVolume);
        }

        // Метод установки системной громкости
        private static void SetSystemVolume(float volume)
        {
            // Конвертируем значение от 0 до 1 в формат Windows (0-65535)
            uint newVolume = (uint)(volume * 65535);
            // Устанавливаем громкость для левого и правого канала
            waveOutSetVolume(0, newVolume | (newVolume << 16));
        }

        // Сохранение настроек в файл
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

        // Загрузка настроек из файла
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

        // Метод для применения громкости к звуковым эффектам
        public static void ApplyVolume(SoundPlayer sound)
        {
            // Теперь громкость будет управляться через системную настройку
        }
    }
}

