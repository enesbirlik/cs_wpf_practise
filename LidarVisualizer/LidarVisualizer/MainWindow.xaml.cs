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
using System.IO.Ports;

namespace LidarVisualizer
{
    /// <summary>
    /// Interaction logic for LidarVisualizerApp.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LidarComm lidarComm;
        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            ListComPorts();

            // ComboBox seçim değişikliklerini dinle
            comPort_ComboBox.SelectionChanged += ComPort_ComboBox_SelectionChanged;
            baudRate_ComboBox.SelectionChanged += BaudRate_ComboBox_SelectionChanged;

            lidarComm = new LidarComm();
        }

        private void ListComPorts()
        {
            comPort_ComboBox.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comPort_ComboBox.Items.Add(port);
            }
            if (comPort_ComboBox.Items.Count > 0)
            {
                comPort_ComboBox.SelectedIndex = 0;
            }
        }

        private void connectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                if (string.IsNullOrEmpty(LidarComm.SelectedComPort) || LidarComm.SelectedBaudRate == 0)
                {
                    MessageBox.Show("Lütfen COM port ve Baud Rate seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (lidarComm.Connect())
                {
                    isConnected = true;
                    connectionButton.Content = "Disconnect";
                    connectionButton.Background = Brushes.Red;
                    comPort_ComboBox.IsEnabled = false;
                    baudRate_ComboBox.IsEnabled = false;
                }
                else
                {
                    MessageBox.Show("Bağlantı kurulamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                lidarComm.Disconnect();
                isConnected = false;
                connectionButton.Content = "Connect";
                connectionButton.Background = Brushes.LightGreen;
                comPort_ComboBox.IsEnabled = true;
                baudRate_ComboBox.IsEnabled = true;
            }
        }

        private void ComPort_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comPort_ComboBox.SelectedItem != null)
            {
                LidarComm.SelectedComPort = comPort_ComboBox.SelectedItem.ToString();
            }
        }

        private void BaudRate_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (baudRate_ComboBox.SelectedItem != null)
            {
                LidarComm.SelectedBaudRate = Convert.ToInt32(((ComboBoxItem)baudRate_ComboBox.SelectedItem).Content);
            }
        }

    }
}
