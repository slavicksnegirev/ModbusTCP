using Gtk;
using System;
using System.Text;

namespace Server
{
    class MainClass : MainWindow
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Application.Init();
            MainWindow window = new MainWindow();
            window.Show();
            Application.Run();
        }
    }
}