using System;
using System.Collections.Generic;
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

namespace RutrackerMole_v2._1.UserControls
{
    /// <summary>
    /// Interaction logic for Switch.xaml
    /// </summary>
    public partial class Switch : UserControl
    {
        public enum SwitchStatus
        {
            None = -1,
            LeftOff_RightOn,
            LeftOn_RightOff
        }

        private readonly Thickness thcLeftSide = new Thickness(1, 1, 0, 0);
        private readonly Thickness thcRightSide = new Thickness(200, 1, 0, 0);

        public SolidColorBrush RightOnColor { get; set; }
        public SolidColorBrush LeftOnColor { get; set; }
        public SolidColorBrush SwitchColor { get; set; }
        public TextBlock RightTextBlock { get => tbkRight; }
        public TextBlock LeftTextBlock { get => tbkLeft; }
        public SwitchStatus Status 
        { 
            get 
            {
                SwitchStatus ssRet = SwitchStatus.None;

                Dispatcher.Invoke(() =>
                {
                    if (rglSwitch.Margin == thcLeftSide)
                        ssRet = SwitchStatus.LeftOff_RightOn;
                    else if (rglSwitch.Margin == thcRightSide)
                        ssRet = SwitchStatus.LeftOn_RightOff;
                });

                return ssRet;
            }
        }

        public Switch()
        {
            InitializeComponent();

            rglSwitch.Fill = new SolidColorBrush(Color.FromRgb(166, 166, 166)); // Gray

            RightOnColor = new SolidColorBrush(Colors.White);
            LeftOnColor = new SolidColorBrush(Colors.White);
            SwitchColor = new SolidColorBrush(Color.FromRgb(166, 166, 166)); // Gray
        }

        public event EventHandler<SwitchEventArgs> Switched;

        protected virtual void OnSwitched()
        {
            if (Switched != null)
                Switched(this, new SwitchEventArgs() { Status = Status });
        }

        private void rglSwitch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (rglSwitch.Margin == thcLeftSide)
            {
                rglSwitch.Margin = thcRightSide;
                OnSwitched();
            }
            else if (rglSwitch.Margin == thcRightSide)
            {
                rglSwitch.Margin = thcLeftSide;
                OnSwitched();
            }
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                Background = new SolidColorBrush(Colors.Gray);
                Opacity = 0.4;
            }
            else
            {
                Background = new SolidColorBrush(Colors.Transparent);
                Opacity = 1;
            }
        }
    }

    public class SwitchEventArgs : EventArgs
    {
        public Switch.SwitchStatus Status { get; set; }
    }
}
