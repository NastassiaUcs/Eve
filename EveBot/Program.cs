using System;

namespace EveBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainBot mainBot = new MainBot();
            mainBot.StartBot();

            string exit = Console.ReadLine();
            if (exit == "exit")
            {
                mainBot.EndBot();
                Environment.Exit(0);
            }
        }
    }
}