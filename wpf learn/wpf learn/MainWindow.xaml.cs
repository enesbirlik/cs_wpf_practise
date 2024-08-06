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

namespace wpf_learn
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {   
            double myFontSize = myLabel.FontSize;
            myLabel.FontSize = myFontSize + 2;
        }

        private void cb123_Checked(object sender, RoutedEventArgs e)
        {
            lb123.FontWeight = FontWeights.Bold;
        }

        private void cb123_Unchecked(object sender, RoutedEventArgs e)
        {
            lb123.FontWeight = FontWeights.Normal;
        }

        private void cb456_Checked(object sender, RoutedEventArgs e)
        {
            lb456.FontWeight = FontWeights.Bold;
        }

        private void cb456_Unchecked(object sender, RoutedEventArgs e)
        {
            lb456.FontWeight = FontWeights.Normal;
        }

        private void cb789_Checked(object sender, RoutedEventArgs e)
        {
            lb789.FontWeight = FontWeights.Bold;
        }

        private void cb789_Unchecked(object sender, RoutedEventArgs e)
        {
            lb789.FontWeight = FontWeights.Normal;
        }
    }
}
