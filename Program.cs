using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutrackerMole_v0_1
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Get Input
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Rutracker forum URL or ID: ");
            Console.ForegroundColor = ConsoleColor.White;

            string strInput = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("String to find: ");
            Console.ForegroundColor = ConsoleColor.White;

            string strStringToFind = Console.ReadLine();
            #endregion

            Console.CursorVisible = false;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nSearching....");
            Console.ForegroundColor = ConsoleColor.White;

            bool bIsUrl = !(new char[10] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }).Contains(strInput.First());
            string strUrl = bIsUrl ? (!strInput.Contains("&start=") ? strInput : strInput.Substring(0, strInput.IndexOf("&start="))) : $"https://rutracker.org/forum/viewtopic.php?t={strInput}&start=0";
            string strForumID = Helpers.GetForumIdFromURL(strUrl);
            List<int> lnPages = new List<int>();

            try
            {
                string strHTML = Helpers.GetHTML(strUrl);
                int nStartVal = 0;

                while (!strHTML.Contains("Ошибочный запрос: недопустимый start [") && !strHTML.Contains("В этой теме нет такой страницы"))
                {
                    if (Helpers.CheckHTML(strHTML, strStringToFind))
                        lnPages.Add(Helpers.GetPageNumberByURL(strUrl));

                    nStartVal += 30;
                    strUrl = $"https://rutracker.org/forum/viewtopic.php?t={strForumID}&start={nStartVal}";
                    strHTML = Helpers.GetHTML(strUrl);
                }

                if (lnPages.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\nFound {lnPages.Count} pages:");

                    foreach (int nPage in lnPages)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($"     |------> ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(nPage);
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Not found");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error has occurred!");
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine("\nExit - Press Esc");

            while (true)
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                    Environment.Exit(0);
        }
    }
}
