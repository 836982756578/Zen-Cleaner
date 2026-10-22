// Zen Cleaner - Taşınabilir Sistem ve Disk Temizleyici
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Threading;
using System.Windows;

namespace ZenCleaner
{
    public class App : Application
    {
        private static Mutex _mutex;

        [STAThread]
        public static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, "ZenCleaner_SingleInstance_SerkanSenturk", out createdNew);

            if (!createdNew)
            {
                // Already running
                return;
            }

            var app = new App();
            app.Run(new MainWindow());
        }
    }
}
