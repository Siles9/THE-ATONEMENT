using System;
using System.Windows.Forms;

namespace Rac_Night
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Создаем и показываем MenuForm.
            //MenuForm menuForm = new MenuForm();
            //menuForm.Show();

            GameplayForm gameplayForm = new GameplayForm();
            gameplayForm.Show();

            Application.Run();
        }
    }
}
