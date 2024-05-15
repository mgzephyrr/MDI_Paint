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
    public class SepiaPlugin : IPlugin
    {
        public string Name => "SepiaPlugin";

        public string ButtonName => "Эффект сепии";

        public string Author => "Кирилл Талан";

        public event EventHandler<ProgressEventArgs> ProgressChanged;

        async public Task<InkCanvas> Transform(InkCanvas app, CancellationTokenSource cts)
        {
            var bitmap = InkCanvasToBitmap(app);

            Bitmap sepiaBitmap = null;

            await Task.Run(() =>
            {
                sepiaBitmap = ConvertToSepia(bitmap);
            });

            var inkCanvas = ConvertBitmapToInkCanvas(sepiaBitmap);

            sepiaBitmap.Dispose();

            return inkCanvas;
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
                drawingContext.DrawRectangle(new System.Windows.Media.VisualBrush(inkCanvas), null, new System.Windows.Rect(0, 0, width, height));
                drawingContext.Close();

                // Рендеринг Visual на Graphics
                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
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

        Bitmap ConvertToSepia(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            // Матрица преобразования цветов для оттенков сепии
            float[][] sepiaMatrix =
            {
                new float[] {0.393f, 0.349f, 0.272f, 0f, 0f},
                new float[] {0.769f, 0.686f, 0.534f, 0f, 0f},
                new float[] {0.189f, 0.168f, 0.131f, 0f, 0f},
                new float[] {0f, 0f, 0f, 1f, 0f},
                new float[] {0f, 0f, 0f, 0f, 1f}
            };

            // Создание ImageAttributes и установка матрицы цветов
            using (Graphics graphics = Graphics.FromImage(newBitmap))
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    ColorMatrix colorMatrix = new ColorMatrix(sepiaMatrix);
                    attributes.SetColorMatrix(colorMatrix);

                    // Наложение матрицы цветов на изображение
                    graphics.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                        0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }

            return newBitmap;
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
