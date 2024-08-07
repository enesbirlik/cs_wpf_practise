using System;
using System.Diagnostics;

namespace LidarVisualizer
{
    public class LidarDataProcessor
    {
        public class LidarPoint
        {
            public double Angle { get; set; }
            public ushort Distance { get; set; }
            public byte Intensity { get; set; }
        }

        private LidarComm lidarComm;
        private byte[] buffer;
        private int bufferIndex;
        private const int packetSize = 47;

        public LidarDataProcessor(LidarComm lidarComm)
        {
            this.lidarComm = lidarComm;
            this.buffer = new byte[1024]; // Başlangıç buffer boyutu, gerekirse artırılabilir
            this.bufferIndex = 0;
            Init();
        }

        private void Init()
        {
            lidarComm.DataReceived += OnDataReceived;
        }

        private void OnDataReceived(object sender, byte[] data)
        {
            Debug.WriteLine($"OnDataReceived: Received {data.Length} bytes of data.");
            ProcessData(data);
        }

        public void ProcessData(byte[] data)
        {
            // Yeni gelen veriyi buffer'a ekle
            if (bufferIndex + data.Length > buffer.Length)
            {
                // Buffer boyutunu artır
                Array.Resize(ref buffer, buffer.Length + data.Length);
            }
            Array.Copy(data, 0, buffer, bufferIndex, data.Length);
            bufferIndex += data.Length;

            // Tam paketleri işle
            while (bufferIndex >= packetSize)
            {
                if (buffer[0] == 0x54) // Paket başlangıç byte'ı
                {
                    byte[] packet = new byte[packetSize];
                    Array.Copy(buffer, packet, packetSize);
                    ProcessPacket(packet);
                    Array.Copy(buffer, packetSize, buffer, 0, bufferIndex - packetSize);
                    bufferIndex -= packetSize;
                }
                else
                {
                    // Geçersiz başlangıç byte'ı, bir byte kaydır
                    Array.Copy(buffer, 1, buffer, 0, bufferIndex - 1);
                    bufferIndex--;
                }
            }
        }

        private void ProcessPacket(byte[] packet)
        {
            ushort startAngle = BitConverter.ToUInt16(packet, 4);
            ushort endAngle = BitConverter.ToUInt16(packet, 42);

            double startAngleDegrees = (startAngle / 100.0 ) % 360;
            double endAngleDegrees = (endAngle / 100.0) % 360;

            double angleDifference = endAngleDegrees - startAngleDegrees;
            if (angleDifference < 0) angleDifference += 360;

            double angleIncrement = angleDifference / 11;
            Debug.WriteLine($"Start Angle: {startAngleDegrees:F2}°, End Angle: {endAngleDegrees:F2}°, Angle Increment: {angleIncrement:F2}°");

            for (int i = 0; i < 12; i++)
            {
                ushort distance = BitConverter.ToUInt16(packet, 6 + i * 3);
                byte intensity = packet[8 + i * 3];
                double angle = startAngleDegrees + (i * angleIncrement);
                if (angle >= 360) angle -= 360;

                LidarPoint point = new LidarPoint
                {
                    Angle = angle,
                    Distance = distance,
                    Intensity = intensity
                };

                PrintProcessedPoint(point);
            }
        }

        private void PrintProcessedPoint(LidarPoint point)
        {
            Debug.WriteLine($"Angle: {point.Angle:F2}°, Distance: {point.Distance}mm, Intensity: {point.Intensity}");
        }
    }
}