using PluginInterface;
using System.Collections.Generic;
using System.Drawing;
using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Transforms
{
    [Version(1, 0)]
    public class ShufflePlugin : IPlugin
    {
        public string Name => "ShufflePlugin";

        public string ButtonName => "Перемешать";

        public string Author => "Кирилл Талан";

        public event EventHandler<ProgressEventArgs> ProgressChanged;
        public async Task<InkCanvas> Transform(InkCanvas app, CancellationTokenSource cts)
        {
            var bitmap = InkCanvasToBitmap(app);

            Bitmap shuffledBitmap = null;

            await Task.Run(() =>
            {
                List<Bitmap> imageParts = SplitImage(bitmap, 3, 3, cts);
                imageParts = ShuffleImageParts(imageParts, cts);
                shuffledBitmap = CombineImageParts(imageParts, bitmap.Width, bitmap.Height, cts);
            });

            var inkCanvas = BitmapToInkCanvas(shuffledBitmap, app.ActualWidth, app.ActualHeight);
            
            shuffledBitmap.Dispose();

            return inkCanvas;
        }
        protected virtual void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs(progress));
        }

        private Bitmap InkCanvasToBitmap(InkCanvas inkCanvas)
        {
            int width = (int)inkCanvas.ActualWidth;
            int height = (int)inkCanvas.ActualHeight;
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Transparent);

                var visual = new DrawingVisual();

                var drawingContext = visual.RenderOpen();
                drawingContext.DrawRectangle(new VisualBrush(inkCanvas), null, new System.Windows.Rect(0, 0, width, height));
                drawingContext.Close();

                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);

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

        private List<Bitmap> SplitImage(Bitmap image, int rows, int cols, CancellationTokenSource cts)
        {
            List<Bitmap> imageParts = new List<Bitmap>();

            int partWidth = image.Width / cols;
            int partHeight = image.Height / rows;
            int processedParts = 0;
            int totalParts = cols * rows;

            for (int y = 0; y < rows; y++)
            {
                cts.Token.ThrowIfCancellationRequested();
                for (int x = 0; x < cols; x++)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    Rectangle rect = new Rectangle(x * partWidth, y * partHeight, partWidth, partHeight);
                    Bitmap part = image.Clone(rect, image.PixelFormat);
                    imageParts.Add(part);

                    processedParts++;
                    int progress = (int)((double)processedParts / totalParts * 100);
                    OnProgressChanged(progress);
                }
            }

            cts.Token.ThrowIfCancellationRequested();
            return imageParts;
        }

        private List<Bitmap> ShuffleImageParts(List<Bitmap> imageParts, CancellationTokenSource cts)
        {
            Random rng = new Random();
            int n = imageParts.Count;
            while (n > 1)
            {
                cts.Token.ThrowIfCancellationRequested();
                n--;
                int k = rng.Next(n + 1);
                Bitmap value = imageParts[k];
                imageParts[k] = imageParts[n];
                imageParts[n] = value;
            }
            cts.Token.ThrowIfCancellationRequested();
            return imageParts;
        }

        private Bitmap CombineImageParts(List<Bitmap> imageParts, int width, int height, CancellationTokenSource cts)
        {
            Bitmap combinedBitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(combinedBitmap))
            {
                int index = 0;
                foreach (Bitmap part in imageParts)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    int x = (index % 3) * (width / 3);
                    int y = (index / 3) * (height / 3);
                    graphics.DrawImage(part, x, y);
                    index++;
                }
            }

            cts.Token.ThrowIfCancellationRequested();
            return combinedBitmap;
        }

        private InkCanvas BitmapToInkCanvas(Bitmap bitmap, double width, double height)
        {
            InkCanvas inkCanvas = new InkCanvas();

            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.Source = ToBitmapImage(bitmap);
            image.Width = width;
            image.Height = height;

            inkCanvas.Children.Add(image);

            return inkCanvas;
        }
        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
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
