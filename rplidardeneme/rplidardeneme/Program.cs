using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;

public class RPLidar
{
    private SerialPort serialPort;
    private const int MaxMotorPwm = 1023;
    private const int DefaultMotorPwm = 660;
    private const byte SetPwmByte = 0xF0;
    private const byte SyncByte = 0xA5;
    private const byte SyncByte2 = 0x5A;
    private const byte GetInfoByte = 0x50;
    private const byte GetHealthByte = 0x52;
    private const byte StartScanByte = 0x20;
    private const byte StopByte = 0x25;
    private const byte ResetByte = 0x40;

    private int motorSpeed;
    private bool motor_running;
    private bool isScanning;

    public RPLidar(string portName = "COM6", int baudRate = 115200, int timeout = 1000)
    {
        this.serialPort = new SerialPort(portName, baudRate)
        {
            ReadTimeout = timeout,
            WriteTimeout = timeout,
            DtrEnable = true
        };
        this.motorSpeed = DefaultMotorPwm;
        this.motor_running = false;
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
            this.serialPort = new SerialPort(this.serialPort.PortName, this.serialPort.BaudRate)
            {
                Parity = Parity.None,
                StopBits = StopBits.One,
                DataBits = 8,
                Handshake = Handshake.None,
                ReadTimeout = this.serialPort.ReadTimeout,
                WriteTimeout = this.serialPort.WriteTimeout,
                DtrEnable = true
            };
            this.serialPort.Open();
            Thread.Sleep(1000); // Give some time for the device to initialize
            Reset();
            Thread.Sleep(1000);
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
        if (pwm > MaxMotorPwm)
        {
            throw new ArgumentException($"PWM value cannot exceed {MaxMotorPwm}");
        }
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
            if (this.motor_running)
            {
                this.SetPWM((ushort)this.motorSpeed);
            }
        }
    }

    public void StartMotor()
    {
        Console.WriteLine("Starting motor");
        // For A1
        this.serialPort.DtrEnable = false;

        // For A2
        this.SetPWM((ushort)this.motorSpeed);
        this.motor_running = true;
        Thread.Sleep(1000); // Give some time for the motor to start
    }

    public void StopMotor()
    {
        Console.WriteLine("Stopping motor");
        // For A2
        this.SetPWM(0);
        Thread.Sleep(1);
        // For A1
        this.serialPort.DtrEnable = true;
        this.motor_running = false;
    }

    public void StartScan()
    {
        if (!this.motor_running)
        {
            this.StartMotor();
        }
        Console.WriteLine("Starting scan");
        this.SendCommand(StartScanByte);
        this.isScanning = true;
        Thread.Sleep(100); // Wait for the scan to start

        // Read and verify the descriptor
        byte[] descriptor = ReadResponse(7);
        if (descriptor[0] != SyncByte || descriptor[1] != SyncByte2 || descriptor[2] != 0x05 || descriptor[6] != 0x81)
        {
            throw new Exception($"Invalid scan descriptor: {BitConverter.ToString(descriptor)}");
        }
        Console.WriteLine("Scan started successfully");
    }

    public void StopScan()
    {
        this.isScanning = false;
        this.SendCommand(StopByte);
    }

    public void Reset()
    {
        Console.WriteLine("Resetting device");
        SendCommand(ResetByte);
        Thread.Sleep(2000);
        serialPort.DiscardInBuffer();
    }

    public (float angle, float distance) ReadMeasurement()
    {
        if (!this.isScanning)
        {
            throw new InvalidOperationException("Scanning is not started. Call StartScan() first.");
        }

        byte[] data = ReadResponse(5);
        bool startBit = (data[0] & 0x1) == 1;
        bool inversedStartBit = ((data[0] >> 1) & 0x1) == 1;

        if (startBit == inversedStartBit)
        {
            throw new Exception("Invalid start bit");
        }

        float angle = ((data[1] >> 1) | (data[2] << 7)) / 64.0f;
        float distance = (data[3] | (data[4] << 8)) / 4.0f;

        return (angle, distance);
    }

    private void SendCommand(byte cmd)
    {
        byte[] req = new byte[] { SyncByte, cmd };
        this.serialPort.Write(req, 0, req.Length);
    }

    private void SendPayloadCmd(byte cmd, byte[] payload)
    {
        List<byte> req = new List<byte> { SyncByte, cmd, (byte)payload.Length };
        req.AddRange(payload);
        byte checksum = 0;
        foreach (byte b in req)
        {
            checksum ^= b;
        }
        req.Add(checksum);
        this.serialPort.Write(req.ToArray(), 0, req.Count);
    }

    private byte[] ReadResponse(int length)
    {
        byte[] response = new byte[length];
        int bytesRead = 0;
        while (bytesRead < length)
        {
            bytesRead += this.serialPort.Read(response, bytesRead, length - bytesRead);
        }
        return response;
    }

    public string GetDeviceInfo()
    {
        Console.WriteLine("Getting device info");
        SendCommand(GetInfoByte);
        Thread.Sleep(100);
        byte[] descriptor = ReadResponse(7);
        if (descriptor[0] != SyncByte || descriptor[1] != SyncByte2)
        {
            throw new Exception($"Invalid info descriptor: {BitConverter.ToString(descriptor)}");
        }
        byte[] response = ReadResponse(20);
        return $"Model: {response[0]}, Firmware: {response[2]}.{response[1]}, Hardware: {response[3]}, Serial: {BitConverter.ToString(response, 4, 16).Replace("-", "")}";
    }

    public string GetHealthStatus()
    {
        Console.WriteLine("Getting health status");
        SendCommand(GetHealthByte);
        Thread.Sleep(100);
        byte[] descriptor = ReadResponse(7);
        if (descriptor[0] != SyncByte || descriptor[1] != SyncByte2)
        {
            throw new Exception($"Invalid health descriptor: {BitConverter.ToString(descriptor)}");
        }
        byte[] response = ReadResponse(3);
        string[] statuses = { "Good", "Warning", "Error" };
        return $"Status: {statuses[response[0]]}, Error Code: {response[1] | (response[2] << 8)}";
    }

    public void Run()
    {
        try
        {
            Connect();
            Console.WriteLine("Connected to RPLidar");

            string deviceInfo = GetDeviceInfo();
            Console.WriteLine($"Device Info: {deviceInfo}");

            string healthStatus = GetHealthStatus();
            Console.WriteLine($"Health Status: {healthStatus}");

            StartMotor();
            Thread.Sleep(2000); // Give the motor time to reach full speed

            StartScan();

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
                    var (angle, distance) = ReadMeasurement();
                    Console.WriteLine($"Angle: {angle:F2}°, Distance: {distance:F2}mm");

                    MotorSpeed = DefaultMotorPwm; // Maintain default motor speed
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout occurred while reading data. Retrying...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                Thread.Sleep(10);
            }

            StopScan();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally
        {
            Disconnect();
            Console.WriteLine("Disconnected from RPLidar");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        RPLidar lidar = new RPLidar();
        lidar.Run();

        Console.WriteLine("Application ended. Press any key to exit.");
        Console.ReadKey();
    }
}