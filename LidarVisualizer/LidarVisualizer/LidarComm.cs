using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;

namespace LidarVisualizer
{
    public class LidarComm
    {
        public static string SelectedComPort { get; set; }
        public static int SelectedBaudRate { get; set; }

        private SerialPort serialPort;
        private const int BUFFER_SIZE = 1013;
        private byte[] buffer = new byte[BUFFER_SIZE];
        private int bufferIndex = 0;

        private LidarDataProcessor dataProcessor;
        public bool IsRunning { get; private set; } = false;

        public event EventHandler<byte[]> DataReceived;


        public LidarComm()
        {
            dataProcessor = new LidarDataProcessor(this);
        }

        public bool Connect()
        {
            try
            {
                serialPort = new SerialPort(SelectedComPort, SelectedBaudRate);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
                IsRunning = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LiDAR başlatılamadı: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            IsRunning = false;
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = serialPort.BytesToRead;
            if (bufferIndex + bytesToRead > BUFFER_SIZE)
            {
                bytesToRead = BUFFER_SIZE - bufferIndex;
            }
            serialPort.Read(buffer, bufferIndex, bytesToRead);
            bufferIndex += bytesToRead;

            // Yeni veri geldiğinde
            OnDataReceived();
        }

        private void OnDataReceived()
        {
            byte[] data = new byte[bufferIndex];
            Array.Copy(buffer, data, bufferIndex);
            Debug.WriteLine($"LidarComm: Veri alındı, boyut: {data.Length} byte");
            Debug.WriteLine($"Ham Veri {BitConverter.ToString(data)}");
    
            DataReceived?.Invoke(this, data);//bu abi ne yapiyo araştır

            dataProcessor.ProcessData(data);
            // Buffer'ı temizle
            bufferIndex = 0;
        }

        public int ReadBuffer(byte[] targetBuffer, int offset, int count)
        {
            int bytesToCopy = Math.Min(count, bufferIndex);
            Array.Copy(buffer, 0, targetBuffer, offset, bytesToCopy);

            // Kopyalanan verileri buffer'dan kaldıran kısım
            Array.Copy(buffer, bytesToCopy, buffer, 0, bufferIndex - bytesToCopy);
            bufferIndex -= bytesToCopy;

            return bytesToCopy;
        }
    }
}