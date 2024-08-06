using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LidarVisualizer
{
    public class LidarDataProcessor
    {
        // LiDAR nokta verisi için iç sınıf
        public class LidarPoint
        {
            public double Angle { get; set; }  // Derece cinsinden açı
            public double Distance { get; set; }  // Milimetre cinsinden mesafe
            public int Quality { get; set; }  // Sinyal kalitesi (opsiyonel)
        }

        // İşlenmiş veriyi tutacak liste
        public List<LidarPoint> ProcessedData { get; private set; }

        // Veri işleme için gerekli değişkenler
        private const byte PACKET_SYNC_BYTE = 0xA5;
        private const int PACKET_SIZE = 5;  // Paket boyutu (sizin LiDAR'ınıza göre değişebilir)

        public LidarDataProcessor()
        {
            ProcessedData = new List<LidarPoint>();
        }

        public void ProcessData(byte[] data)
        {
            ProcessedData.Clear();  // Önceki veriyi temizle

            for (int i = 0; i < data.Length - PACKET_SIZE; i++)
            {
                if (data[i] == PACKET_SYNC_BYTE)
                {
                    // Paket başlangıcı bulundu, veriyi işle
                    ProcessPacket(data, i);
                    i += PACKET_SIZE - 1;  // Bir sonraki olası pakete atla
                }
            }

            // İşlenmiş veriyi konsola yazdır
            PrintProcessedData();
        }

        private void ProcessPacket(byte[] data, int startIndex)
        {
            // Bu örnek, basit bir paket yapısı varsayar
            // Gerçek uygulamada, LiDAR'ınızın protokolüne göre uyarlamanız gerekebilir

            byte angle_lsb = data[startIndex + 1];
            byte angle_msb = data[startIndex + 2];
            byte distance_lsb = data[startIndex + 3];
            byte distance_msb = data[startIndex + 4];

            double angle = ((angle_msb << 8) | angle_lsb) / 64.0;  // Açıyı dereceye çevir
            double distance = ((distance_msb << 8) | distance_lsb) / 4.0;  // Mesafeyi mm'ye çevir

            LidarPoint point = new LidarPoint
            {
                Angle = angle,
                Distance = distance,
                Quality = 0  // Bu örnekte kalite bilgisi yok, gerekirse ekleyebilirsiniz
            };

            ProcessedData.Add(point);
        }

        private void PrintProcessedData()
        {
            Console.WriteLine("İşlenmiş LiDAR Verileri:");
            foreach (var point in ProcessedData)
            {
                Console.WriteLine($"Açı: {point.Angle:F2}°, Mesafe: {point.Distance:F2}mm");
            }
            Console.WriteLine("--------------------");
        }
    }
}
