using System;
using System.IO.Ports;
using System.Threading.Tasks;

class Program
{
    static SerialPort? serialPort;
    static bool ctrl_c_pressed = false;

    static async Task Main()
    {
        Console.WriteLine("SLAMTEC LIDAR Data Grabber");
        Console.WriteLine("Version: 1.0.0");

        string portName = "COM6";
        int baudRate = 115200;

        try
        {
            ConnectSerial(portName, baudRate);

            if (!await CheckLidarHealth())
            {
                Console.WriteLine("Error, SLAMTEC Lidar health check failed.");
                return;
            }

            await SetMotorSpeed(true); // Motoru başlat

            Console.CancelKeyPress += (sender, e) =>
            {
                ctrl_c_pressed = true;
                e.Cancel = true;
            };

            await GrabAndDisplayScanData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            await SetMotorSpeed(false); // Motoru durdur
            Disconnect();
        }
    }

    static void ConnectSerial(string portName, int baudRate)
    {
        serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        serialPort.ReadTimeout = 2000;
        serialPort.WriteTimeout = 2000;
        serialPort.Open();
        Console.WriteLine($"Connected to {portName} at {baudRate} baud");
    }

    static async Task<bool> CheckLidarHealth()
    {
        Console.WriteLine("Checking LIDAR health...");

        byte[] healthCommand = new byte[] { 0xA5, 0x52 };
        await serialPort!.BaseStream.WriteAsync(healthCommand, 0, healthCommand.Length);

        byte[] response = new byte[7];
        await serialPort.BaseStream.ReadAsync(response, 0, 7);

        if (response[0] == 0xA5 && response[1] == 0x5A && response[2] == 0x03 && response[6] == 0)
        {
            Console.WriteLine("LIDAR health status: Good");
            return true;
        }
        else
        {
            Console.WriteLine("LIDAR health status: Bad");
            return false;
        }
    }

    static async Task SetMotorSpeed(bool start)
    {
        byte[] command = start ? new byte[] { 0xA5, 0xF0, 0x02, 0x94, 0x01 } : new byte[] { 0xA5, 0xF0, 0x02, 0x00, 0x00 };
        await serialPort!.BaseStream.WriteAsync(command, 0, command.Length);
        Console.WriteLine(start ? "Motor started" : "Motor stopped");
    }

    static async Task GrabAndDisplayScanData()
    {
        Console.WriteLine("Starting scan. Press Ctrl+C to stop.");

        byte[] startScanCommand = new byte[] { 0xA5, 0x20 };
        await serialPort!.BaseStream.WriteAsync(startScanCommand, 0, startScanCommand.Length);

        byte[] buffer = new byte[5];
        while (!ctrl_c_pressed)
        {
            try
            {
                if (serialPort.BytesToRead >= 5)
                {
                    await serialPort.BaseStream.ReadAsync(buffer, 0, 5);

                    if (buffer[0] == 0xA5 && buffer[1] == 0x5A)
                    {
                        ushort distance = (ushort)((buffer[3] << 8) | buffer[2]);
                        byte quality = (byte)(buffer[4] >> 2);

                        Console.WriteLine($"Distance: {distance} mm, Quality: {quality}");
                    }
                }
            }
            catch (TimeoutException)
            {
                // Zaman aşımı durumunda devam et
            }

            await Task.Delay(10);
        }

        byte[] stopScanCommand = new byte[] { 0xA5, 0x25 };
        await serialPort.BaseStream.WriteAsync(stopScanCommand, 0, stopScanCommand.Length);
    }

    static void Disconnect()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Console.WriteLine("Disconnected from LIDAR");
        }
    }
}