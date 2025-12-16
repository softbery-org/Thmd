// Version: 0.1.0.24
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Thmd.Images
{
    public static class GifExporter
    {
        public static void ExportToGif(FrameworkElement element, string outputPath, int durationMs = 3000, int fps = 30)
        {
            int frameCount = (int)(durationMs / 1000.0 * fps);
            double frameTime = 1.0 / fps;
            var encoder = new GifBitmapEncoder();

            // Wymuszenie układu kontrolki
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));
            element.UpdateLayout();

            for (int i = 0; i < frameCount; i++)
            {
                // Zaktualizuj efekt ręcznie (jeśli masz metodę aktualizacji)
                if (element is Thmd.Controls.Effects.BeatsEffect beats)
                {
                    beats.ForceUpdate(frameTime); // Dodamy tę metodę poniżej
                }

                // Renderuj do bitmapy
                var bmp = new RenderTargetBitmap(
                    (int)element.ActualWidth,
                    (int)element.ActualHeight,
                    96, 96, PixelFormats.Pbgra32);
                bmp.Render(element);

                encoder.Frames.Add(BitmapFrame.Create(bmp));
            }

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
    }
}
