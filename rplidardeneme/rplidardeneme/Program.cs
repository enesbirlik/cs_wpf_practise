using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Diagnostics;

public class RPLidar
{
    private SerialPort serialPort;
    private const int MaxMotorPwm = 1023;
    private const int DefaultMotorPwm = 660;
    private const byte SetPwmByte = 0xF0;
    private const byte SyncByte = 0xA5;
    private const byte StartScanByte = 0x20;
    private const byte StopByte = 0x25;

    private int motorSpeed;
    private bool isMotorRunning;
    private bool isScanning;

    public RPLidar(string portName = "COM6", int baudRate = 115200, int timeout = 1000)
    {
        this.serialPort = new SerialPort(portName, baudRate);
        this.serialPort.ReadTimeout = timeout;
        this.motorSpeed = DefaultMotorPwm;
        this.isMotorRunning = false;
        this.isScanning = false;
    }

    public void Connect()
    {
        if (this.serialPort.IsOpen)
        {
            this.Disconnect();
        }
        try
        {
            this.serialPort.Open();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to the sensor due to: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        this.StopScan();
        this.StopMotor();
        if (this.serialPort.IsOpen)
        {
            this.serialPort.Close();
        }
    }

    private void SetPWM(ushort pwm)
    {
        byte[] payload = BitConverter.GetBytes(pwm);
        this.SendPayloadCmd(SetPwmByte, payload);
    }
    
    public int MotorSpeed
    {
        get { return this.motorSpeed; }
        set
        {
            if (value < 0 || value > MaxMotorPwm)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Motor speed must be between 0 and {MaxMotorPwm}");
            }
            this.motorSpeed = value;
            if (this.isMotorRunning)
            {
                this.SetPWM((ushort)this.motorSpeed);
            }
        }
    }

    public void StartMotor()
    {
        Console.WriteLine("Starting motor");
        this.SetPWM((ushort)this.motorSpeed);
        this.isMotorRunning = true;
    }

    public void StopMotor()
    {
        Console.WriteLine("Stopping motor");
        this.SetPWM(0);
        this.isMotorRunning = false;
    }

    public void StartScan()
    {
        if (!this.isMotorRunning)
        {
            this.StartMotor();
        }
        this.SendCommand(StartScanByte);
        this.isScanning = true;
    }

    public void StopScan()
    {
        this.isScanning = false;
        this.SendCommand(StopByte);
    }

    public byte[] ReadRawData()
    {
        if (!this.isScanning)
        {
            throw new InvalidOperationException("Scanning is not started. Call StartScan() first.");
        }

        List<byte> buffer = new List<byte>();
        int startFlagCount = 0;

        while (startFlagCount < 1)//2
        {
            int b = this.serialPort.ReadByte();
            if (b == SyncByte)
                startFlagCount++;
            else
                startFlagCount = 0;

            buffer.Add((byte)b);
        }

        for (int i = 0; i < 2; i++)//5
        {
            buffer.Add((byte)this.serialPort.ReadByte());
        }

        return buffer.ToArray();
    }

    private void SendCommand(byte cmd)
    {
        byte[] req = new byte[] { SyncByte, cmd };
        this.serialPort.Write(req, 0, req.Length);
    }

    private void SendPayloadCmd(byte cmd, byte[] payload)
    {
        byte size = (byte)payload.Length;
        byte[] req = new byte[4 + payload.Length];
        req[0] = SyncByte;
        req[1] = cmd;
        req[2] = size;
        Array.Copy(payload, 0, req, 3, payload.Length);

        byte checksum = 0;
        for (int i = 0; i < req.Length - 1; i++)
        {
            checksum ^= req[i];
        }
        req[req.Length - 1] = checksum;

        this.serialPort.Write(req, 0, req.Length);
        //Console.WriteLine($"Command sent: {BitConverter.ToString(req)}");
    }

    public (float angle, float distance) ParseScanData(byte[] rawData)
    {
        if (rawData.Length < 5)
        {
            throw new ArgumentException("Raw data is too short");
        }


        float angle = ((rawData[1] >> 1) + (rawData[2] << 7)) / 64.0f %360;
        float distance = (rawData[3] + rawData[4] << 8) / 4.0f;

        Console.WriteLine($"Angle: {angle:F2}°, Distance: {distance:F2}mm");

        return (angle, distance);
    }
}

class Program
{
    static void Main(string[] args)
    {
        RPLidar lidar = new RPLidar();

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
                    //Console.WriteLine($"Raw Data: {BitConverter.ToString(rawData)}");


                    var (angle, distance) = lidar.ParseScanData(rawData);

                    //Console.WriteLine($"Angle: {angle:F2}°, Distance: {distance:F2}mm");

                    lidar.MotorSpeed = 660; // DefaultMotorPwm değeri
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout occurred while reading data. Retrying...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

               // System.Threading.Thread.Sleep(10);
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