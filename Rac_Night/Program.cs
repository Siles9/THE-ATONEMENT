using System;
using System.Windows.Forms;

namespace Rac_Night
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Создаем главное меню
            MenuForm menuForm = new MenuForm();

            // Запускаем цикл сообщений
            Application.Run(menuForm);
        }
    }
}