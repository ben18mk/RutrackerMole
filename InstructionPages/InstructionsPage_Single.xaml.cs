using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using WpfAnimatedGif;

namespace RutrackerMole_v2._1.InstructionPages
{
    /// <summary>
    /// Interaction logic for InstructionsPage_Single.xaml
    /// </summary>
    public partial class InstructionsPage_Single : Page
    {
        string strMainProgramFolderLocation = System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"..\..\"));

        public InstructionsPage_Single()
        {
            InitializeComponent();

            BitmapImage bmpiImageTemp = new BitmapImage();
            bmpiImageTemp.BeginInit();
            bmpiImageTemp.UriSource = new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\Single\URL_or_ID.gif");
            bmpiImageTemp.EndInit();
            ImageBehavior.SetAnimatedSource(gifURLorID, bmpiImageTemp);

            imgFind.Source = new BitmapImage(new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\Find.png"));
            imgClearButton.Source = new BitmapImage(new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\ClearButton.png"));
            imgNewSearchButton.Source = new BitmapImage(new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\NewSearchButton.png"));
        }
    }
}
