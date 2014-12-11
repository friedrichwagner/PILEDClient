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
using PILEDClient.ViewModel;
using Lumitech.Interfaces;

namespace PILEDClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((DataContext as PILEDClientViewModel).ExpertModeEnabled == false)
                this.Width = 200; // only Brightness and CCT sliders
            else
                this.Width = 500; // Brightness, CCT, R,G,B sliders

            this.Top = SystemParameters.WorkArea.Height - this.Height - 10;
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width;
        }

        private void sldBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext!= null)
                (DataContext as PILEDClientViewModel).SendValue((sender as Slider).Value/100*255, NeoLinkMode.NL_BRIGHTNESS);
        }

        private void sldCCT_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext != null)
                (DataContext as PILEDClientViewModel).SendValue((sender as Slider).Value, NeoLinkMode.NL_CCT);
        }
    }
}
