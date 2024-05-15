using PluginInterface;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Transforms
{
    [Version(1, 0)]
    public class ContrastPlugin : IPlugin
    {
        public string Name => "ContrastPlugin";

        public string ButtonName => "Повысить контрастность";

        public string Author => "Кирилл Талан";

        public event EventHandler<ProgressEventArgs> ProgressChanged;

        async public Task<InkCanvas> Transform(InkCanvas app, CancellationTokenSource cts)
        {
            var bitmap = InkCanvasToBitmap(app);
            Bitmap increasedContrastBitmap = null;

            await Task.Run(() =>
            {
                // Проверяем, не было ли запроса на отмену
                cts.Token.ThrowIfCancellationRequested();
                increasedContrastBitmap = IncreaseContrast(bitmap, cts);
            });

            var inkCanvas = ConvertBitmapToInkCanvas(increasedContrastBitmap);

            bitmap.Dispose();
            increasedContrastBitmap.Dispose();

            return inkCanvas;
        }

        public Bitmap IncreaseContrast(Bitmap originalBitmap, CancellationTokenSource cts)
        {
            Bitmap newBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
            double contrastFactor = 1.5;
            int totalPixels = originalBitmap.Height;
            int processedPixels = 0;

            for (int y = 0; y < originalBitmap.Height; y++)
            {
                cts.Token.ThrowIfCancellationRequested();
                for (int x = 0; x < originalBitmap.Width; x++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    System.Drawing.Color originalColor = originalBitmap.GetPixel(x, y);

                    int r = (int)((originalColor.R - 128) * contrastFactor + 128);
                    int g = (int)((originalColor.G - 128) * contrastFactor + 128);
                    int b = (int)((originalColor.B - 128) * contrastFactor + 128);

                    r = Math.Max(0, Math.Min(255, r));
                    g = Math.Max(0, Math.Min(255, g));
                    b = Math.Max(0, Math.Min(255, b));

                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(originalColor.A, r, g, b);

                    newBitmap.SetPixel(x, y, newColor);             
                }
                processedPixels++;
                int progress = (int)((double)processedPixels / totalPixels * 100);
                OnProgressChanged(progress);
            }

            cts.Token.ThrowIfCancellationRequested();
            return newBitmap;
        }

        protected virtual void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs(progress));
        }

        Bitmap InkCanvasToBitmap(InkCanvas inkCanvas)
        {
            // Определение размеров InkCanvas
            int width = (int)inkCanvas.ActualWidth;
            int height = (int)inkCanvas.ActualHeight;

            // Создание нового Bitmap с заданными размерами
            Bitmap bitmap = new Bitmap(width, height);

            // Создание Graphics для рисования на Bitmap
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Создание прозрачного фона
                graphics.Clear(System.Drawing.Color.Transparent);

                // Создание нового Visual для отрисовки InkCanvas
                var visual = new DrawingVisual();

                // Рендеринг InkCanvas на Visual
                var drawingContext = visual.RenderOpen();
                drawingContext.DrawRectangle(new VisualBrush(inkCanvas), null, new System.Windows.Rect(0, 0, width, height));
                drawingContext.Close();

                // Рендеринг Visual на Graphics
                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);

                // Конвертация RenderTargetBitmap в Bitmap
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var stream = new System.IO.MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    bitmap = (Bitmap)System.Drawing.Image.FromStream(stream);
                }
            }

            return bitmap;
        }

        InkCanvas ConvertBitmapToInkCanvas(Bitmap bitmap)
        {
            // Создание объекта Image для отображения Bitmap на InkCanvas
            var image = new System.Windows.Controls.Image
            {
                Source = ConvertBitmapToBitmapImage(bitmap)
            };

            // Создание нового InkCanvas
            InkCanvas inkCanvas = new InkCanvas();

            // Установка размеров InkCanvas
            inkCanvas.Width = bitmap.Width;
            inkCanvas.Height = bitmap.Height;

            // Добавление Image на InkCanvas
            inkCanvas.Children.Add(image);

            return inkCanvas;
        }

        BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            // Конвертация Bitmap в BitmapImage
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
