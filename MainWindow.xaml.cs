using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RutrackerMole_v2._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static volatile int nPagesChecked;
        static int nNumOfPages;
        static object oLock;
        ObservableCollection<int> ocnPages;
        List<Thread> ltThreads;
        Thread tWaiting;
        Thread tError;

        public MainWindow()
        {
            InitializeComponent();

            nPagesChecked = 0;
            nNumOfPages = 0;
            oLock = new Object();
            ocnPages = new ObservableCollection<int>();
            ltThreads = new List<Thread>();
            tWaiting = null;

            lbxResultData.ItemsSource = ocnPages;
            lbxResultData.IsEnabled = false;

            pageChecked += (sender, e) =>
            {
                lock (oLock)
                {
                    ocnPages.OrderBy(x => x);

                    Dispatcher.Invoke(() =>
                    {
                        tbxResultInfo.Text = $"Searching... {++nPagesChecked}/{nNumOfPages}";
                        tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(255, 195, 18)); // Yellow
                    });

                    Thread.Sleep(5);
                }
            };

            finished += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    btnNewSearch.IsEnabled = true;
                    btnClear.IsEnabled = true;
                    lbxResultData.IsEnabled = true;
                });

                ocnPages.OrderBy(x => x);

                Dispatcher.Invoke(() =>
                {
                    if (ocnPages.Count > 0)
                    {
                        tbxResultInfo.Text = $"Found {ocnPages.Count} / {nNumOfPages} pages";
                        tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(0, 148, 50)); // Green
                    }
                    else
                    {
                        tbxResultInfo.Text = "Not found";
                        tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(234, 32, 39)); // Red
                    }
                });
            };

            ocnPages.CollectionChanged += (sender, e)
                => Dispatcher.Invoke(()
                    => lbxResultData.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending)));
        }

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

        private void reinit()
        {
            nPagesChecked = 0;
            nNumOfPages = 0;
            oLock = new Object();
            ocnPages.Clear();

            lock (oLock)
            {
                try
                {
                    if (tWaiting.IsAlive)
                        tWaiting.Abort();
                }
                catch (NullReferenceException) { }

                try
                {
                    if (tError.IsAlive)
                        tError.Abort();
                }
                catch (NullReferenceException) { }

                foreach (var thread in ltThreads)
                    try
                    {
                        if (thread.IsAlive)
                            thread.Abort();
                    }
                    catch (NullReferenceException) { }
            }

            ltThreads = new List<Thread>();
            tWaiting = null;
            tError = null;
            tbxFind.Text = "";
            tbxResultInfo.Text = "";
            tbxResultInfo.Foreground = new SolidColorBrush(Colors.Black);
            tbxResultInfo.Background = new SolidColorBrush(Colors.White);
            btnSearch.IsEnabled = true;
            tbxFind.IsReadOnly = false;
            lbxResultData.IsEnabled = false;
            tbxUrlOrId.IsReadOnly = false;
        }

        private void textBoxKeyDown(object sender, KeyEventArgs e)
        {
            TextBox tbxSender = (TextBox)sender;

            if (tbxSender.Text.Length <= 31)
                tbxSender.FontSize = 18;
            else if (tbxSender.Text.Length > 31 && tbxSender.Text.Length <= 36)
                tbxSender.FontSize = 16;
            else if (tbxSender.Text.Length > 36 && tbxSender.Text.Length <= 40)
                tbxSender.FontSize = 14;
            else if (tbxSender.Text.Length > 40 && tbxSender.Text.Length <= 47)
                tbxSender.FontSize = 12;
            else if (tbxSender.Text.Length > 47 && tbxSender.Text.Length <= 52)
                tbxSender.FontSize = 11;
            else if (tbxSender.Text.Length > 52 && tbxSender.Text.Length <= 56)
                tbxSender.FontSize = 10;
            else if (tbxSender.Text.Length > 56 && tbxSender.Text.Length <= 62)
                tbxSender.FontSize = 9;
            else if (tbxSender.Text.Length > 62 && tbxSender.Text.Length <= 70)
                tbxSender.FontSize = 8;
        }

        private void textBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxKeyDown(sender, null);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbxUrlOrId.Text = "";
            reinit();
        }

        private void btnNewSearch_Click(object sender, RoutedEventArgs e)
        {
            reinit();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            tbxUrlOrId.IsReadOnly = true;
            tbxResultInfo.IsReadOnly = true;
            tbxFind.IsReadOnly = true;
            btnNewSearch.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnSearch.IsEnabled = false;

            tbxResultInfo.Text = "Searching...";
            tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(255, 195, 18)); // Yellow

            try
            {
                bool bIsUrl = Helpers.IsUrl(tbxUrlOrId.Text);
                string strUrl = bIsUrl ? (!tbxUrlOrId.Text.Contains("&start=") ? tbxUrlOrId.Text : tbxUrlOrId.Text.Substring(0, tbxUrlOrId.Text.IndexOf("&start="))) : $"https://rutracker.org/forum/viewtopic.php?t={tbxUrlOrId.Text}&start=0";
                string strForumID = bIsUrl ? Helpers.GetForumIdFromURL(strUrl) : tbxUrlOrId.Text;
                string strFind = tbxFind.Text;

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

                            if (Helpers.CheckHTML(strHTML_, strFind))
                                Dispatcher.Invoke(() => ocnPages.Add(Helpers.GetPageNumberByURL(strUrl_)));

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

                                if (Helpers.CheckHTML(strHTML_, strFind))
                                    Dispatcher.Invoke(() => ocnPages.Add(Helpers.GetPageNumberByURL(strUrl_)));

                                onPageChecked();
                            }
                        });
                        tThread.Start(lnStartVals[i]);
                        ltThreads.Add(tThread);
                    }
                }

                tWaiting = new Thread(() =>
                {
                    foreach (var thread in ltThreads)
                        thread.Join();

                    onFinished();
                });

                tWaiting.Start();
            }
            catch (Exception)
            {
                tbxResultInfo.Text = "An error has occurred!";

                tError = new Thread(() =>
                {
                    bool bRedBackground = false;

                    while (true)
                    {
                        if (bRedBackground)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(234, 32, 39)); // Red
                                tbxResultInfo.Background = new SolidColorBrush(Colors.White);
                            });
                            bRedBackground = false;
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                tbxResultInfo.Foreground = new SolidColorBrush(Colors.White);
                                tbxResultInfo.Background = new SolidColorBrush(Color.FromRgb(234, 32, 39)); // Red
                            });
                            bRedBackground = true;
                        }

                        Thread.Sleep(1000);
                    }
                });
                tError.Start();

                btnNewSearch.IsEnabled = true;
                btnClear.IsEnabled = true;
            }
        }

        private void lbxResultData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int nPage = (int)lbxResultData.SelectedItem;
            bool bIsUrl = Helpers.IsUrl(tbxUrlOrId.Text);
            string strUrl = bIsUrl ? (!tbxUrlOrId.Text.Contains("&start=") ? tbxUrlOrId.Text : tbxUrlOrId.Text.Substring(0, tbxUrlOrId.Text.IndexOf("&start="))) : $"https://rutracker.org/forum/viewtopic.php?t={tbxUrlOrId.Text}&start=0";
            string strForumID = bIsUrl ? Helpers.GetForumIdFromURL(strUrl) : tbxUrlOrId.Text;

            strUrl = $"https://rutracker.org/forum/viewtopic.php?t={strForumID}&start={(nPage - 1) * 30}";
            Process.Start(strUrl);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            new Info().ShowDialog();
        }
    }
}
