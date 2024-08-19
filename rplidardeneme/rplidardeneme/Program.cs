using System;
using System.IO.Ports;
using System.Threading;

class Program
{
    static SerialPort serialPort;
    static bool isScanning = false;

    static void Main()
    {
        // Seri port ayarlarını yapılandırın
        serialPort = new SerialPort("COM6", 115200);
        serialPort.DataBits = 8;
        serialPort.Parity = Parity.None;
        serialPort.StopBits = StopBits.One;

        // Seri portu açın
        serialPort.Open();

        // RPLIDAR'ı başlatın
        InitRPLIDAR();

        // Taramayı başlatın
        StartScan();

        // Veri okuma döngüsü
        while (isScanning)
        {
            try
            {
                // Tarama verilerini okuyun
                ReadScanData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                break;
            }
        }

        // Taramayı durdurun ve bağlantıyı kesin
        StopScan();
        DisconnectRPLIDAR();

        Console.WriteLine("Program sonlandı. Çıkmak için bir tuşa basın...");
        Console.ReadKey();
    }

    static void InitRPLIDAR()
    {
        // Stop komutu gönderin
        byte[] stopCommand = { 0xA5, 0x25 };
        serialPort.Write(stopCommand, 0, stopCommand.Length);
        Thread.Sleep(10);

        // Motoru başlatın
        byte[] startMotorCommand = { 0xA5, 0xF0 };
        serialPort.Write(startMotorCommand, 0, startMotorCommand.Length);
        Thread.Sleep(10);
    }

    static void DisconnectRPLIDAR()
    {
        // Stop komutu gönderin
        byte[] stopCommand = { 0xA5, 0x25 };
        serialPort.Write(stopCommand, 0, stopCommand.Length);
        Thread.Sleep(10);

        // Seri portu kapatın
        serialPort.Close();
    }

    static void StartScan()
    {
        isScanning = true;

        // Tarama başlatma komutu gönderin
        byte[] startScanCommand = { 0xA5, 0x20 };
        serialPort.Write(startScanCommand, 0, startScanCommand.Length);
        Thread.Sleep(10);
    }

    static void StopScan()
    {
        isScanning = false;

        // Tarama durdurma komutu gönderin
        byte[] stopScanCommand = { 0xA5, 0x25 };
        serialPort.Write(stopScanCommand, 0, stopScanCommand.Length);
        Thread.Sleep(10);
    }

    static void ReadScanData()
    {
        // Yanıt başlığını okuyun
        byte[] header = new byte[7];
        serialPort.Read(header, 0, header.Length);

        // Tarama verilerini okuyun
        byte[] scanData = new byte[5];
        serialPort.Read(scanData, 0, scanData.Length);

        // Tarama verilerini ayrıştırın
        float angle = (scanData[1] << 8 | scanData[0]) / 64f;
        int distance = (scanData[3] << 8 | scanData[2]);
        byte quality = scanData[4];

        // Tarama verilerini ekrana yazdırın
        Console.WriteLine($"Açı: {angle}, Mesafe: {distance}, Kalite: {quality}");
    }
}