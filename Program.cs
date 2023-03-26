using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RutrackerMole_v1_0
{
    class Program
    {
        static volatile int nPagesChecked = 0;
        static int nNumOfPages = 0;
        static object oLock = new Object();

        static event EventHandler<EventArgs> pageChecked;
        static event EventHandler<EventArgs> finished;

        static void onPageChecked()
        {
            if (pageChecked != null)
                pageChecked(null, new EventArgs());
        }

        static void onFinished()
        {
            if (pageChecked != null)
                finished(null, new EventArgs());
        }

        static void init()
        {
            Console.Clear();
            Console.CursorVisible = true;
            nPagesChecked = 0;
            nNumOfPages = 0;
            oLock = new Object();
            pageChecked = null;
            finished = null;
        }

        static void Main(string[] args)
        {
            Start:
            init();

            #region Get Input
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Rutracker forum URL or ID: ");
            string strCyan1 = "Rutracker forum URL or ID: ";
            Console.ForegroundColor = ConsoleColor.White;

            string strInput = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("String to find: ");
            string strCyan2 = "String to find: ";
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
            List<Thread> ltThreads = new List<Thread>();

            pageChecked += (sender, e) =>
            {
                lock (oLock)
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(strCyan1);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(strInput);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(strCyan2);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(strStringToFind);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\nPages checked: {++nPagesChecked}/{nNumOfPages}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Thread.Sleep(5);
                }
            };

            finished += (sender, e) =>
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(strCyan1);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(strInput);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(strCyan2);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(strStringToFind);
            };

            try
            {
                nNumOfPages = Helpers.CountPages(strUrl);
                bool bThreadsDoOnly1Req = nNumOfPages <= 30;
                int nNumOfThreads = bThreadsDoOnly1Req ? nNumOfPages : (int)Math.Ceiling((double)nNumOfPages / 5);

                if (bThreadsDoOnly1Req)
                {
                    int nStartVal = 0;

                    for (int i = 0; i < nNumOfThreads; i++)
                    {
                        Thread tThread = new Thread((val) =>
                        {
                            string strUrl_ = $"https://rutracker.org/forum/viewtopic.php?t={strForumID}&start={val}";
                            string strHTML_ = Helpers.GetHTML(strUrl_);

                            if (Helpers.CheckHTML(strHTML_, strStringToFind))
                                lnPages.Add(Helpers.GetPageNumberByURL(strUrl_));

                            onPageChecked();
                        });
                        tThread.Start(nStartVal);
                        ltThreads.Add(tThread);

                        nStartVal += 30;
                    }
                }
                else
                {
                    List<int>[] lnStartVals = new List<int>[nNumOfThreads];
                    lnStartVals[0] = new List<int>();

                    for (int i = 0, iset = 0; i < nNumOfPages; i++)
                    {
                        if (i % 5 == 0 && i != 0)
                            lnStartVals[++iset] = new List<int>();

                        lnStartVals[iset].Add(i);
                    }

                    for (int i = 0; i < nNumOfThreads; i++)
                    {
                        Thread tThread = new Thread((list) =>
                        {
                            foreach (var pageIndx in (List<int>)list)
                            {
                                string strUrl_ = $"https://rutracker.org/forum/viewtopic.php?t={strForumID}&start={pageIndx * 30}";
                                string strHTML_ = Helpers.GetHTML(strUrl_);

                                if (Helpers.CheckHTML(strHTML_, strStringToFind))
                                    lnPages.Add(Helpers.GetPageNumberByURL(strUrl_));

                                onPageChecked();
                            }
                        });
                        tThread.Start(lnStartVals[i]);
                        ltThreads.Add(tThread);
                    }
                }

                foreach (var thread in ltThreads)
                    thread.Join();

                lnPages.Sort();

                onFinished();

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
                    Console.WriteLine("\nNot found");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error has occurred!");
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine();
            Console.WriteLine("New search - Press Enter\n" +
                              "Exit - Press Esc");

            while (true)
            {
                ConsoleKey ckKey = Console.ReadKey(true).Key;

                if (ckKey == ConsoleKey.Escape)
                    Environment.Exit(0);
                else if (ckKey == ConsoleKey.Enter)
                    goto Start;
            }
        }
    }
}
