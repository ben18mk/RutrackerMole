using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RutrackerMole_v2._1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        struct multi_ThreadData
        {
            public string strForumId;
            public int nNumOfPages;
            public int nStartVal;
            public List<int> lnStartValsList;
        }

        static volatile int nPagesChecked;
        static int nNumOfPages;
        static object oLock;
        ObservableCollection<int> ocnPages;
        ObservableCollection<Helpers.ForumIdAndPage_Display> ochfiapdPages;
        List<Thread> ltThreads;
        Thread tWaiting;
        Thread tError;
        string strPresetFilePath;
        bool bAborted = false;

        public MainWindow()
        {
            InitializeComponent();

            nPagesChecked = 0;
            nNumOfPages = 0;
            oLock = new Object();
            ocnPages = new ObservableCollection<int>();
            ochfiapdPages = new ObservableCollection<Helpers.ForumIdAndPage_Display>();
            ltThreads = new List<Thread>();
            tWaiting = null;
            tError = null;
            strPresetFilePath = "";

            lbxResultData.IsEnabled = false;
            ucsSwitchSingleMulti.RightTextBlock.Text = "Single";
            ucsSwitchSingleMulti.RightTextBlock.FontSize = 22;
            ucsSwitchSingleMulti.RightTextBlock.FontFamily = new FontFamily("Roboto Medium");
            ucsSwitchSingleMulti.LeftTextBlock.Text = "Multi";
            ucsSwitchSingleMulti.LeftTextBlock.FontSize = 22;
            ucsSwitchSingleMulti.LeftTextBlock.FontFamily = new FontFamily("Roboto Medium");

            pageChecked += (sender, e) =>
            {
                lock (oLock)
                {
                    if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn)
                    {
                        ocnPages.OrderBy(x => x);

                        Dispatcher.Invoke(() =>
                        {
                            tbxResultInfo.Text = $"Searching... {++nPagesChecked}/{nNumOfPages}";
                            tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(255, 195, 18)); // Yellow
                        });
                    }
                    else if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
                        Dispatcher.Invoke(() => pbrSearch.Value++);

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

                    if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn)
                    {
                        ocnPages.Distinct();

                        lbxResultData.ItemsSource = null;
                        lbxResultData.Items.Clear();
                        lbxResultData.ItemsSource = new BindingList<int>(ocnPages.OrderBy(x => x).ToList());
                        lbxResultData.Items.Refresh();

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
                    }
                    else if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
                    {
                        ochfiapdPages.Distinct();

                        pbrSearch.Visibility = Visibility.Hidden;
                        pbrSearch.IsEnabled = false;

                        tbxResultInfo.Visibility = Visibility.Visible;
                        tbxResultInfo.IsEnabled = true;

                        lbxResultData.ItemsSource = null;
                        lbxResultData.Items.Clear();
                        lbxResultData.ItemsSource = new BindingList<Helpers.ForumIdAndPage_Display>(ochfiapdPages.Sort().ToList());
                        lbxResultData.Items.Refresh();

                        if (ochfiapdPages.Count > 0)
                        {
                            tbxResultInfo.Text = $"Found {ochfiapdPages.Count} pages";
                            tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(0, 148, 50)); // Green
                        }
                        else
                        {
                            tbxResultInfo.Text = "Not found";
                            tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(234, 32, 39)); // Red
                        }
                    }
                });
            };

            ocnPages.CollectionChanged += (sender, e)
                => Dispatcher.Invoke(()
                    => lbxResultData.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending)));

            ochfiapdPages.CollectionChanged += (sender, e)
                => Dispatcher.Invoke(()
                    => lbxResultData.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("ForumID", System.ComponentModel.ListSortDirection.Ascending)));

            ucsSwitchSingleMulti.Switched += (sender, e) =>
            {
                if (e.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn)
                {
                    tbkFirstInput.Text = "URL or ID:";

                    btnImportPresetFile.Visibility = Visibility.Hidden;
                    btnImportPresetFile.IsEnabled = false;

                    tbxUrlOrId.Visibility = Visibility.Visible;
                    tbxUrlOrId.IsEnabled = true;

                    pbrSearch.Visibility = Visibility.Hidden;
                    pbrSearch.IsEnabled = false;

                    tbxResultInfo.Visibility = Visibility.Visible;
                    tbxResultInfo.IsEnabled = true;

                    lbxResultData.ItemsSource = null;
                    lbxResultData.ItemsSource = ocnPages;
                }
                else if (e.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
                {
                    tbkFirstInput.Text = "Preset file:";

                    tbxUrlOrId.Visibility = Visibility.Hidden;
                    tbxUrlOrId.IsEnabled = false;

                    btnImportPresetFile.Visibility = Visibility.Visible;
                    btnImportPresetFile.IsEnabled = true;

                    tbxResultInfo.Visibility = Visibility.Hidden;
                    tbxResultInfo.IsEnabled = false;

                    pbrSearch.Visibility = Visibility.Visible;
                    pbrSearch.IsEnabled = true;

                    lbxResultData.ItemsSource = null;
                    lbxResultData.ItemsSource = ochfiapdPages;
                }
            };

            Helpers.Abort += (sender, e) =>
            {
                if (!bAborted)
                {
                    Dispatcher.Invoke(() => btnClear_Click(this, new RoutedEventArgs()));
                    MessageBox.Show("Failed reaching rutracker.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    bAborted = true;
                }
            };
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
            ochfiapdPages.Clear();
            bAborted = false;

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
            tbxFind.IsReadOnly = false;
            tbxFind.ForceCursor = true;
            lbxResultData.IsEnabled = false;
            lbxResultData.ItemsSource = null;
            lbxResultData.Items.Clear();
            tbxUrlOrId.IsReadOnly = false;
            tbxUrlOrId.ForceCursor = true;
            btnSearch.IsEnabled = false;
            btnImportPresetFile.IsEnabled = true;
            ucsSwitchSingleMulti.IsEnabled = true;
            btnNewSearch.IsEnabled = true;
            btnClear.IsEnabled = true;
            pbrSearch.Maximum = 100;
            pbrSearch.Value = 0;

            if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
            {
                tbxResultInfo.Visibility = Visibility.Hidden;
                tbxResultInfo.IsEnabled = false;

                pbrSearch.Visibility = Visibility.Visible;
                pbrSearch.IsEnabled = true;
            }
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

            if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn &&
               tbxUrlOrId.Text != "" &&
               tbxFind.Text != "")
                if (e?.Key == Key.Enter)
                    btnSearch_Click(this, new RoutedEventArgs());
                else
                    btnSearch.IsEnabled = true;
            else if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff &&
                     strPresetFilePath != "" &&
                    (string)btnImportPresetFile.Content != "Import Preset File" &&
                     tbxFind.Text != "")
                if (e?.Key == Key.Enter)
                    btnSearch_Click(this, new RoutedEventArgs());
                else
                    btnSearch.IsEnabled = true;
            else
                btnSearch.IsEnabled = false;
        }

        private void textBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxKeyDown(sender, null);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbxUrlOrId.Text = "";

            strPresetFilePath = "";
            btnImportPresetFile.Content = "Import Preset File";

            reinit();
        }

        private void btnNewSearch_Click(object sender, RoutedEventArgs e)
        {
            reinit();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            tbxUrlOrId.IsReadOnly = true;
            tbxUrlOrId.ForceCursor = false;
            tbxResultInfo.IsReadOnly = true;
            tbxFind.IsReadOnly = true;
            tbxFind.ForceCursor = false;
            btnNewSearch.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnSearch.IsEnabled = false;
            btnImportPresetFile.IsEnabled = false;
            ucsSwitchSingleMulti.IsEnabled = false;

            tbxResultInfo.Text = "Searching...";
            tbxResultInfo.Foreground = new SolidColorBrush(Color.FromRgb(255, 195, 18)); // Yellow

            if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn)
                try
                {
                    bool bIsUrl = Helpers.IsUrl(tbxUrlOrId.Text);
                    string strUrl = bIsUrl ? (!tbxUrlOrId.Text.Contains("&start=") ? tbxUrlOrId.Text : tbxUrlOrId.Text.Substring(0, tbxUrlOrId.Text.IndexOf("&start="))) : $"https://rutracker.org/forum/viewtopic.php?t={tbxUrlOrId.Text}&start=0";
                    string strForumID = bIsUrl ? Helpers.GetForumIdFromURL(strUrl) : tbxUrlOrId.Text;
                    string strFind = tbxFind.Text;

                    lbxResultData.ItemsSource = ocnPages;

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
            else if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
                try
                {
                    List<string> lsForumIds = Helpers.GetForumIdsFromFile(strPresetFilePath);
                    List<int> lnForumsPages = Helpers.GetPagesByForumIds(lsForumIds);
                    string strFind = tbxFind.Text;

                    pbrSearch.Maximum = lnForumsPages.Sum();
                    lbxResultData.ItemsSource = ochfiapdPages;

                    for (int i = 0; i < lsForumIds.Count; i++)
                    {
                        string strForumId = lsForumIds[i];
                        int nForumPages = lnForumsPages[i];

                        //Thread tThread = new Thread((multi_threadData) =>
                        //{
                        bool bThreadsDoOnly1Req = nForumPages <= 30;
                        int nNumOfThreads_ = bThreadsDoOnly1Req ? nForumPages : (int)Math.Ceiling((double)nForumPages / 5);

                        if (bThreadsDoOnly1Req)
                        {
                            int nStartVal = 0;

                            for (int j = 0; j < nNumOfThreads_; j++)
                            {
                                Thread tThreadInner = new Thread((multi_ThreadData_Inner) =>
                                {
                                    string strUrl_ = $"https://rutracker.org/forum/viewtopic.php?t={((multi_ThreadData)multi_ThreadData_Inner).strForumId}&start={((multi_ThreadData)multi_ThreadData_Inner).nStartVal}";
                                    string strHTML_ = Helpers.GetHTML(strUrl_);
                                    int nPageNumber_ = Helpers.GetPageNumberByURL(strUrl_);

                                    if (Helpers.CheckHTML(strHTML_, strFind))
                                        //Dispatcher.Invoke(() => ocsPages.Add($"{((multi_ThreadData)multi_ThreadData_Inner).strForumId} => {nPageNumber_}"));
                                        Dispatcher.Invoke(() => ochfiapdPages.Add(new Helpers.ForumIdAndPage_Display() { ForumID = ((multi_ThreadData)multi_ThreadData_Inner).strForumId, PageNumber = nPageNumber_ }));

                                    onPageChecked();
                                });

                                lock (oLock)
                                {
                                    multi_ThreadData mtdInner;
                                    mtdInner.strForumId = strForumId;
                                    mtdInner.nNumOfPages = -1;
                                    mtdInner.nStartVal = nStartVal;
                                    mtdInner.lnStartValsList = null;


                                    tThreadInner.Start(mtdInner);
                                    ltThreads.Add(tThreadInner);

                                    nStartVal += 30;
                                }
                            }
                        }
                        else
                        {
                            List<int>[] lnStartVals = new List<int>[nNumOfThreads_];
                            lnStartVals[0] = new List<int>();

                            for (int j = 0, iset = 0; j < nForumPages; j++)
                            {
                                if (j % 5 == 0 && j != 0)
                                    lnStartVals[++iset] = new List<int>();

                                lnStartVals[iset].Add(j);
                            }

                            for (int j = 0; j < nNumOfThreads_; j++)
                            {
                                Thread tThreadInner = new Thread((multi_threadData_Inner) =>
                                {
                                    foreach (var pageIndx in ((multi_ThreadData)multi_threadData_Inner).lnStartValsList)
                                    {
                                        string strUrl_ = $"https://rutracker.org/forum/viewtopic.php?t={((multi_ThreadData)multi_threadData_Inner).strForumId}&start={pageIndx * 30}";
                                        string strHTML_ = Helpers.GetHTML(strUrl_);
                                        int nPageNumber_ = Helpers.GetPageNumberByURL(strUrl_);

                                        if (Helpers.CheckHTML(strHTML_, strFind))
                                            //Dispatcher.Invoke(() => ocsPages.Add($"{((multi_ThreadData)multi_threadData_Inner).strForumId} => {nPageNumber_}"));
                                            Dispatcher.Invoke(() => ochfiapdPages.Add(new Helpers.ForumIdAndPage_Display() { ForumID = ((multi_ThreadData)multi_threadData_Inner).strForumId, PageNumber = nPageNumber_ }));

                                        onPageChecked();
                                    }
                                });

                                lock (oLock)
                                {
                                    multi_ThreadData mtdInner;
                                    mtdInner.strForumId = strForumId;
                                    mtdInner.nNumOfPages = -1;
                                    mtdInner.nStartVal = -1;
                                    mtdInner.lnStartValsList = lnStartVals[j];

                                    tThreadInner.Start(mtdInner);
                                    ltThreads.Add(tThreadInner);
                                }
                            }
                        }
                        //});

                        //lock (oLock)
                        //{
                        //    multi_ThreadData mtd;
                        //    mtd.nNumOfPages = nForumPages;
                        //    mtd.strForumId = strForumId;
                        //    mtd.nStartVal = -1;
                        //    mtd.lnStartValsList = null;

                        //    tThread.Start(mtd);
                        //    ltThreads.Add(tThread);
                        //}
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
                    pbrSearch.Visibility = Visibility.Hidden;
                    pbrSearch.IsEnabled = false;

                    tbxResultInfo.Visibility = Visibility.Visible;
                    tbxResultInfo.IsEnabled = true;

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
            if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn)
            {
                int nPage = (int)lbxResultData.SelectedItem;
                bool bIsUrl = Helpers.IsUrl(tbxUrlOrId.Text);
                string strUrl = bIsUrl ? (!tbxUrlOrId.Text.Contains("&start=") ? tbxUrlOrId.Text : tbxUrlOrId.Text.Substring(0, tbxUrlOrId.Text.IndexOf("&start="))) : $"https://rutracker.org/forum/viewtopic.php?t={tbxUrlOrId.Text}&start=0";
                string strForumID = bIsUrl ? Helpers.GetForumIdFromURL(strUrl) : tbxUrlOrId.Text;

                strUrl = $"https://rutracker.org/forum/viewtopic.php?t={strForumID}&start={(nPage - 1) * 30}";
                Process.Start(strUrl);
            }
            else if (ucsSwitchSingleMulti.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
            {
                Helpers.ForumIdAndPage_Display hfiapdSelected = (Helpers.ForumIdAndPage_Display)lbxResultData.SelectedItem;
                string strUrl = $"https://rutracker.org/forum/viewtopic.php?t={hfiapdSelected.ForumID}&start={(hfiapdSelected.PageNumber - 1) * 30}";

                Process.Start(strUrl);
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            new Info().ShowDialog();
        }

        private void btnImportPresetFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdFileDialog = new OpenFileDialog();
            ofdFileDialog.Filter = "Text files (*.txt)|*.txt";

            if (ofdFileDialog.ShowDialog().Value)
            {
                strPresetFilePath = ofdFileDialog.FileName;
                btnImportPresetFile.Content = strPresetFilePath.Split('\\').Last();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
                new Instructions_Main().ShowDialog();
        }
    }
}
