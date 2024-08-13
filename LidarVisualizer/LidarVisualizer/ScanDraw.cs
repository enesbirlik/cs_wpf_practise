using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace LidarVisualizer
{
    public class ScanDraw
    {
        private Canvas canvas;
        private List<LidarDataProcessor.LidarPoint> points;
        private const int MAX_POINTS = 100;
        private DispatcherTimer drawTimer;

        // Yeni eklenen bir liste, çizilen Ellipse nesnelerini tutar
        private List<Ellipse> pointEllipses;

        public ScanDraw(Canvas canvas)
        {
            this.canvas = canvas;
            this.points = new List<LidarDataProcessor.LidarPoint>(MAX_POINTS);
            this.pointEllipses = new List<Ellipse>(MAX_POINTS);

            drawTimer = new DispatcherTimer();
            drawTimer.Interval = TimeSpan.FromMilliseconds(1); // 1ms aralıklarla çizim yap
            drawTimer.Tick += DrawTimer_Tick;
            drawTimer.Start();
        }

        public void AddPoint(LidarDataProcessor.LidarPoint point)
        {
            try
            {
                if (!canvas.Dispatcher.CheckAccess())
                {
                    canvas.Dispatcher.BeginInvoke(new Action(() => AddPoint(point)));
                    return;
                }

                points.Add(point);

                // Eğer MAX_POINTS'i aşıyorsa, eski noktaları ve ilgili Ellipse'leri silelim
                if (points.Count > MAX_POINTS)
                {
                    points.RemoveAt(0);

                    // İlk eklenen Ellipse'i sil
                    if (pointEllipses.Count > 0)
                    {
                        Ellipse oldEllipse = pointEllipses[0];
                        canvas.Children.Remove(oldEllipse);
                        pointEllipses.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AddPoint'te bir hata oluştu: " + ex.Message);
            }
        }

        private void DrawTimer_Tick(object sender, EventArgs e)
        {
            DrawPoints();
        }

        private void DrawPoints()
        {
            try
            {
                if (points.Count == 0) return;

                foreach (var point in points)
                {
                    DrawPoint(point);
                }

                canvas.InvalidateVisual();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DrawPoints'te bir hata oluştu: " + ex.Message);
            }
        }

        private void DrawPoint(LidarDataProcessor.LidarPoint point)
        {
            try
            {
                double angle = point.Angle * Math.PI / 180;
                double scaledDistance = point.Distance * Math.Min(canvas.ActualWidth, canvas.ActualHeight) / 16000;

                double x = scaledDistance * Math.Cos(angle) + canvas.ActualWidth / 2;
                double y = canvas.ActualHeight / 2 - scaledDistance * Math.Sin(angle);

                Ellipse pointEllipse = new Ellipse
                {
                    Width = 2,
                    Height = 2,
                    Fill = new SolidColorBrush(Color.FromRgb(point.Intensity, (byte)(255 - point.Intensity), 0))
                };

                Canvas.SetLeft(pointEllipse, x - pointEllipse.Width / 2);
                Canvas.SetTop(pointEllipse, y - pointEllipse.Height / 2);

                canvas.Children.Add(pointEllipse);
                pointEllipses.Add(pointEllipse); // Çizilen Ellipse'i takip ediyoruz
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DrawPoint'te bir hata oluştu: " + ex.Message);
            }
        }
    }
}
