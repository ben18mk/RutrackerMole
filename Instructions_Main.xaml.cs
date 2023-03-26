using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Shapes;

namespace RutrackerMole_v2._1
{
    /// <summary>
    /// Interaction logic for Instructions_Main.xaml
    /// </summary>
    public partial class Instructions_Main : Window
    {
        public Instructions_Main()
        {
            InitializeComponent();

            Icon = SystemIcons.Information.ToImageSource();

            ucsSwitchSingleMulti.RightTextBlock.Text = "Single";
            ucsSwitchSingleMulti.RightTextBlock.FontSize = 22;
            ucsSwitchSingleMulti.RightTextBlock.FontFamily = new System.Windows.Media.FontFamily("Roboto Medium");
            ucsSwitchSingleMulti.LeftTextBlock.Text = "Multi";
            ucsSwitchSingleMulti.LeftTextBlock.FontSize = 22;
            ucsSwitchSingleMulti.LeftTextBlock.FontFamily = new System.Windows.Media.FontFamily("Roboto Medium");

            fFrame.Content = new InstructionPages.InstructionsPage_Single();
        }

        private void ucsSwitchSingleMulti_Switched(object sender, UserControls.SwitchEventArgs e)
        {
            if (e.Status == UserControls.Switch.SwitchStatus.LeftOff_RightOn)
                fFrame.Content = new InstructionPages.InstructionsPage_Single();
            else if (e.Status == UserControls.Switch.SwitchStatus.LeftOn_RightOff)
                fFrame.Content = new InstructionPages.InstructionsPage_Multi();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
