using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

public class RPLidar
{
    private SerialPort _serial;
    private const int MAX_MOTOR_PWM = 1023;
    const int DEFAULT_MOTOR_PWM = 660;
    private const byte SET_PWM_BYTE = 0xF0;
    private const byte SYNC_BYTE = 0xA5;
    private const byte START_SCAN_BYTE = 0x20;
    private const byte STOP_BYTE = 0x25;

    private int _motorSpeed;
    private bool _motorRunning;
    private bool _scanning;

    public RPLidar(string portName = "COM6", int baudRate = 115200, int timeout = 1000)
    {
        _serial = new SerialPort(portName, baudRate);
        _serial.ReadTimeout = timeout;
        _motorSpeed = DEFAULT_MOTOR_PWM;
        _motorRunning = false;
        _scanning = false;
    }

    public void Connect()
    {
        if (_serial.IsOpen)
        {
            Disconnect();
        }
        try
        {
            _serial.Open();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to the sensor due to: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        StopScan();
        StopMotor();
        if (_serial.IsOpen)
        {
            _serial.Close();
        }
    }

    private void SetPWM(ushort pwm)
    {
        byte[] payload = BitConverter.GetBytes(pwm);
        SendPayloadCmd(SET_PWM_BYTE, payload);
    }

    public int MotorSpeed
    {
        get { return _motorSpeed; }
        set
        {
            if (value < 0 || value > MAX_MOTOR_PWM)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Motor speed must be between 0 and {MAX_MOTOR_PWM}");
            }
            _motorSpeed = value;
            if (_motorRunning)
            {
                SetPWM((ushort)_motorSpeed);
            }
        }
    }

    public void StartMotor()
    {
        Console.WriteLine("Starting motor");
        SetPWM((ushort)_motorSpeed);
        _motorRunning = true;
    }

    public void StopMotor()
    {
        Console.WriteLine("Stopping motor");
        SetPWM(0);
        Thread.Sleep(1);
        _motorRunning = false;
    }

    public void StartScan()
    {
        if (!_motorRunning)
        {
            StartMotor();
        }
        SendCommand(START_SCAN_BYTE);
        _scanning = true;
    }

    public void StopScan()
    {
        _scanning = false;
        SendCommand(STOP_BYTE);
    }

    public byte[] ReadRawData()
    {
        if (!_scanning)
        {
            throw new InvalidOperationException("Scanning is not started. Call StartScan() first.");
        }

        List<byte> buffer = new List<byte>();
        int startFlagCount = 0;

        // Senkronizasyon baytlarını ara
        while (startFlagCount < 2)
        {
            int b = _serial.ReadByte();
            if (b == SYNC_BYTE)
                startFlagCount++;
            else
                startFlagCount = 0;

            buffer.Add((byte)b);
        }

        // Veri paketinin geri kalanını oku (5 bayt)
        for (int i = 0; i < 5; i++)
        {
            buffer.Add((byte)_serial.ReadByte());
        }

        return buffer.ToArray();
    }

    private void SendCommand(byte cmd)
    {
        byte[] req = new byte[] { SYNC_BYTE, cmd };
        _serial.Write(req, 0, req.Length);
    }

    private void SendPayloadCmd(byte cmd, byte[] payload)
    {
        byte size = (byte)payload.Length;
        byte[] req = new byte[4 + payload.Length];
        req[0] = SYNC_BYTE;
        req[1] = cmd;
        req[2] = size;
        Array.Copy(payload, 0, req, 3, payload.Length);

        byte checksum = 0;
        for (int i = 0; i < req.Length - 1; i++)
        {
            checksum ^= req[i];
        }
        req[req.Length - 1] = checksum;

        _serial.Write(req, 0, req.Length);
        Console.WriteLine($"Command sent: {BitConverter.ToString(req)}");
    }
}

class Program
{
    static void Main(string[] args)
    {
        RPLidar lidar = new RPLidar(); // Varsayılan olarak COM6'yı kullanacak

        try
        {
            lidar.Connect();
            Console.WriteLine("Connected to RPLidar on COM6");

            lidar.StartScan();
            Console.WriteLine("Scanning started. Press 'q' to stop...");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q')
                        break;
                }

                try
                {
                    byte[] rawData = lidar.ReadRawData();
                    Console.WriteLine($"Raw Data: {BitConverter.ToString(rawData)}");

                    // Motor hızını güncelle
                    lidar.MotorSpeed = 660;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout occurred while reading data. Retrying...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                Thread.Sleep(10); // CPU kullanımını azaltmak için küçük bir gecikme
            }

            lidar.StopScan();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            lidar.Disconnect();
            Console.WriteLine("Disconnected from RPLidar");
        }
    }
}