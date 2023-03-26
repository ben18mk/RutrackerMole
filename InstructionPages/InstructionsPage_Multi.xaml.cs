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
    /// Interaction logic for InstructionsPage_Multi.xaml
    /// </summary>
    public partial class InstructionsPage_Multi : Page
    {
        string strMainProgramFolderLocation = System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"..\..\"));

        public InstructionsPage_Multi()
        {
            InitializeComponent();

            BitmapImage bmpiImageTemp = new BitmapImage();
            bmpiImageTemp.BeginInit();
            bmpiImageTemp.UriSource = new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\Multi\PresetFile.gif");
            bmpiImageTemp.EndInit();
            ImageBehavior.SetAnimatedSource(gifPresetFile, bmpiImageTemp);

            imgFind.Source = new BitmapImage(new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\Find.png"));
            imgClearButton.Source = new BitmapImage(new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\ClearButton.png"));
            imgNewSearchButton.Source = new BitmapImage(new Uri(strMainProgramFolderLocation + @"Images\InstructionPages\NewSearchButton.png"));
        }
    }
}
