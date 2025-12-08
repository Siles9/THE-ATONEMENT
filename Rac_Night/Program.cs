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

            var gm = GameManager.Instance;
            //Application.Run(new MenuForm());
            Application.Run(new GameplayForm());
        }
    }
}
