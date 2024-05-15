using Fluent;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using PluginInterface;
using System.Windows.Data;
using Transforms;
using System.Threading;

namespace MDI_Paint
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private double ZoomMax = 3;
        private double ZoomMin = 1;
        private double ZoomSpeed = 0.001;
        private double Zoom = 1;
        private List<string> paths = new List<string>();
        private List<bool> edited = new List<bool>();
        private List<IPlugin> pluginsList = new List<IPlugin>();
        private InkCanvas inkCanvas 
        {
            get 
            {
                if (tabsController == null || tabsController.SelectedIndex == -1)
                {
                    return null;
                }
                return (InkCanvas)((TabItem)tabsController?.SelectedItem).Content;
            }
            set
            {
                ((TabItem)tabsController?.SelectedItem).Content = value;
            }
        }
        private GalleryItem ClickedShape { get; set; } = null;
        private Shape UnreleasedShape { get; set; } = null;
        private double StartX { get; set; }
        private double StartY { get; set; }
        private double EndX { get; set; }
        private double EndY { get; set; }
        private int filesCreated { get; set; } = 1;

        public MainWindow()
        {
            InitializeComponent();
            FindPlugins();
        }

        private void newFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CreateNewTab("Без имени " + filesCreated.ToString());
            filesCreated++;
            paths.Add("");
            edited.Add(false);
        }

        private void CreateNewTab(string name)
        {
            TabItem tab = new TabItem();
            tab.Header = name;
            InkCanvas ic = InitiateCanvasEvents(new InkCanvas());
            tab.Content = ic;
            tabsController.Items.Add(tab);
            tab.IsSelected = true;
        }

        private void inkCanvas_StrokeErased(object sender, RoutedEventArgs e)
        {
            edited[tabsController.SelectedIndex] = true;
        }

        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            edited[tabsController.SelectedIndex] = true;
        }

        private void tabsController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inkCanvas == null)
                return;

            pen_Click(new object(), new RoutedEventArgs());
            thickness.Value = inkCanvas.DefaultDrawingAttributes.Width;
            Zoom = 1;
            ZoomCanvas(0, new Point(0, 0));
        }

        private void closeTab_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentTab();
        }

        private void CloseCurrentTab()
        {
            if (edited[tabsController.SelectedIndex])
            {
                MessageBoxResult result = MessageBox.Show("Файл не сохранен, хотите сохранить?\nДа - сохранить и закрыть\nНет - не сохранять и закрыть\nОтмена - не сохранять и не закрывать", "Предупреждение", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        SaveFile();
                    break;

                    case MessageBoxResult.Cancel:
                        return;
                }
            }

            paths.RemoveAt(tabsController.SelectedIndex);
            edited.RemoveAt(tabsController.SelectedIndex);
            tabsController.Items.Remove(tabsController.SelectedItem);
        }

        private void open_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Открыть файл"
            };

            if (openDialog.ShowDialog() != true)
            {
                return; 
            }

            if (paths.Contains(openDialog.FileName))
            {
                MessageBox.Show("Файл занят другой вкладкой", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var stream = File.OpenRead(openDialog.FileName);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            stream.Close();

            var image = new System.Windows.Controls.Image
            {
                Source = bitmapImage
            };

            CreateNewTab(openDialog.SafeFileName);
            inkCanvas.Children.Add(image);
            paths.Add(openDialog.FileName);
            edited.Add(false);
        }

        private void save_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SaveFile();
        }

        private void SaveFile()
        {
            edited[tabsController.SelectedIndex] = false;
            if (!File.Exists(paths[tabsController.SelectedIndex]))
            {
                SaveFileWithDialog();
            }
            else
            {
                SaveFileByPath(paths[tabsController.SelectedIndex]);
            }
        }

        private void saveAs_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SaveFileWithDialog();
        }

        private void SaveFileByPath(string path)
        {
            try
            {
                Rect rect = new Rect(inkCanvas.RenderSize);
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)rect.Right,
                  (int)rect.Bottom, 96d, 96d, PixelFormats.Default);
                rtb.Render(inkCanvas);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    File.WriteAllBytes(path, ms.ToArray());
                }
            }
            catch (Exception)
            {
                Trace.WriteLine("Не удалось сохранить файл");
            }
        }

        private void SaveFileWithDialog()
        {
            var dialog = new SaveFileDialog 
            {
                DefaultExt = ".png",
                Filter = "(.png)|*.png|(.jpg)|*.jpg|(.jpeg)|*.jpeg|(.bmp)|*.bmp"
            };

            bool? result = dialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            if (paths.Contains(dialog.FileName) && paths.FindIndex(x => x == dialog.FileName) != tabsController.SelectedIndex)
            {
                MessageBox.Show("Файл занят другой вкладкой", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileByPath(dialog.FileName);
            paths[tabsController.SelectedIndex] = dialog.FileName;
            edited[tabsController.SelectedIndex] = false;
            ((TabItem)tabsController.SelectedItem).Header = dialog.SafeFileName;
        }

        private void pen_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                if (ClickedShape != null)
                {
                    ClickedShape.IsSelected = false;
                    ClickedShape = null;
                }
            }  
        }

        private void eraser_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas == null)
                return;

            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            
            if (ClickedShape != null)
            {
                ClickedShape.IsSelected = false;
                ClickedShape = null;
            }
        }

        private void colors_SelectedColorChanged(object sender, RoutedEventArgs e)
        {
            if (inkCanvas == null)
                return;

            inkCanvas.DefaultDrawingAttributes.Color = colors.SelectedColor != null ? (Color)colors.SelectedColor : Colors.Black;
        }

        private void thickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkCanvas == null)
                return;

            inkCanvas.DefaultDrawingAttributes.Width = thickness.Value;
            inkCanvas.DefaultDrawingAttributes.Height = thickness.Value;
        }

        private void line_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas == null)
                return;

            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            ClickedShape = (GalleryItem)sender;
        }

        private void ellipse_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas == null)
                return;
            
            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            ClickedShape = (GalleryItem)sender;
        }

        private void star_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas == null)
                return;

            inkCanvas.EditingMode = InkCanvasEditingMode.None;
            ClickedShape = (GalleryItem)sender;
        }

        private void inkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartX = e.GetPosition(inkCanvas).X;
            StartY = e.GetPosition(inkCanvas).Y;

            if (ClickedShape != null)
            {
                edited[tabsController.SelectedIndex] = true;

                switch (ClickedShape.Name)
                {
                    case "line":
                        UnreleasedShape = new Line();
                        ((Line)UnreleasedShape).X1 = StartX;
                        ((Line)UnreleasedShape).Y1 = StartY;
                        break;

                    case "ellipse":
                        UnreleasedShape = new Ellipse();
                        InkCanvas.SetLeft(UnreleasedShape, e.GetPosition(inkCanvas).X);
                        InkCanvas.SetTop(UnreleasedShape, e.GetPosition(inkCanvas).Y);
                        break;

                    case "star":
                        UnreleasedShape = new Polyline();
                        break;
                }

                UnreleasedShape.Stroke = new SolidColorBrush(inkCanvas.DefaultDrawingAttributes.Color);
                UnreleasedShape.StrokeThickness = thickness.Value;
                inkCanvas.Children.Add(UnreleasedShape);
            }
        }

        private void inkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            EndX = e.GetPosition(inkCanvas).X;
            EndY = e.GetPosition(inkCanvas).Y;

            if (UnreleasedShape == null)
            {
                return;
            }

            switch (ClickedShape.Name)
            {
                case "line":
                    Line line = (Line)UnreleasedShape;
                    line.X2 = EndX;
                    line.Y2 = EndY;
                    break;

                case "ellipse":
                    UnreleasedShape.Width = Math.Abs(StartX - EndX);
                    UnreleasedShape.Height = Math.Abs(StartY - EndY);
                    InkCanvas.SetTop(UnreleasedShape, Math.Min(StartY, EndY));
                    InkCanvas.SetLeft(UnreleasedShape, Math.Min(StartX, EndX));
                    break;

                case "star":
                    Polyline star = (Polyline)UnreleasedShape;
                    star.Points.Clear();
                    int pointsCount = (int)angleCount.Value * 2;
                    double width = Math.Abs(StartX - EndX);
                    double height = Math.Abs(StartY - EndY);
                    double centerX = Math.Min(StartX, EndX) + width / 2;
                    double centerY = Math.Min(StartY, EndY) + height / 2;
                    double x, y;

                    for (int i = 0; i < pointsCount + 1; i++)
                    {
                        double radiusX, radiusY;
                        if (i % 2 == 0)
                        {
                            radiusX = width / 2;
                            radiusY = height / 2;
                        }
                        else
                        {
                            radiusX = width / 2 * radiusRatio.Value;
                            radiusY = height / 2 * radiusRatio.Value;
                        }

                        x = centerX + radiusX * Math.Sin(i * 2 * Math.PI / pointsCount + Math.PI / angleCount.Value);
                        y = centerY + radiusY * Math.Cos(i * 2 * Math.PI / pointsCount + Math.PI / angleCount.Value);

                        star.Points.Add(new Point(x, y));
                    }
                    break;
            }
        }

        private void inkCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (UnreleasedShape != null)
            {
                Stroke stroke;
                if (UnreleasedShape is Line line)
                {
                    stroke = ConvertLineToStroke(line);
                }
                else
                {
                    stroke = ConvertShapeToStroke(UnreleasedShape);
                }
                DrawingAttributes drawingAttributes = new DrawingAttributes
                {
                    Width = inkCanvas.DefaultDrawingAttributes.Width,
                    Height = inkCanvas.DefaultDrawingAttributes.Height,
                    Color = inkCanvas.DefaultDrawingAttributes.Color
                };

                stroke.DrawingAttributes = drawingAttributes;
                inkCanvas.Children.Remove(UnreleasedShape);
                inkCanvas.Strokes.Add(stroke);
                UnreleasedShape = null;
            }
        }

        private void inkCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            inkCanvas_MouseLeftButtonUp(sender, new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left));
        }

        private void inkCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ZoomCanvas(e.Delta, e.GetPosition(inkCanvas));
        }

        private void ZoomCanvas(int delta, Point mousePos)
        {
            Zoom += ZoomSpeed * delta;
            if (Zoom < ZoomMin) { Zoom = ZoomMin; return; }
            if (Zoom > ZoomMax) { Zoom = ZoomMax; return; }

            zoom.Text = Zoom * 100 + "%";

            if (Zoom > 1)
            {
                inkCanvas.RenderTransform = new ScaleTransform(Zoom, Zoom, mousePos.X, mousePos.Y);
                Debug.WriteLine(tabsController.Width);
            }
            else
            {
                inkCanvas.RenderTransform = new ScaleTransform(Zoom, Zoom);
                Debug.WriteLine(tabsController.Width);
            }
        }

        private Stroke ConvertLineToStroke(Line line)
        {
            StylusPointCollection stylusPoints = new StylusPointCollection
            {
                new StylusPoint(line.X1, line.Y1),
                new StylusPoint(line.X2, line.Y2)
            };

            return new Stroke(stylusPoints);
        }

        private Stroke ConvertShapeToStroke(Shape shape)
        {
            Geometry shapeGeometry = shape.RenderedGeometry;
            PathGeometry pathGeometry = shapeGeometry.GetFlattenedPathGeometry();
            StylusPointCollection stylusPoints = new StylusPointCollection();

            AddPointsToConvert(ref stylusPoints, pathGeometry);

            Shape symmetricShape = shape;
            ScaleTransform symmetricTransform = new ScaleTransform(-1, 1);
            symmetricShape.RenderTransform = symmetricTransform;

            shapeGeometry = symmetricShape.RenderedGeometry;
            pathGeometry = shapeGeometry.GetFlattenedPathGeometry();

            AddPointsToConvert(ref stylusPoints, pathGeometry);

            if (stylusPoints.Count == 0)
            {
                stylusPoints.Add(new StylusPoint(StartX, StartY));
            }

            return new Stroke(stylusPoints);
        }

        private void AddPointsToConvert(ref StylusPointCollection stylusPoints, PathGeometry pathGeometry)
        {
            foreach (PathFigure pathFigure in pathGeometry.Figures)
            {
                foreach (PathSegment pathSegment in pathFigure.Segments)
                {
                    if (pathSegment is PolyLineSegment polyLineSegment)
                    {
                        foreach (Point point in polyLineSegment.Points)
                        {
                            double left = InkCanvas.GetLeft(UnreleasedShape);
                            double top = InkCanvas.GetTop(UnreleasedShape);
                            stylusPoints.Add(new StylusPoint(point.X + (left is Double.NaN ? 0 : left), point.Y + (top is Double.NaN ? 0 : top)));
                        }
                    }
                }
            }
        }

        private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (tabsController.Items.Count == 0) { return; }

            tabsController.SelectedIndex = 0;
            for (; tabsController.Items.Count > 0; ) CloseCurrentTab();
        }

        private void about_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Название: MDI_Paint\nВерсия: 1.0.0.0\nСоздатель: Талан Кирилл\n\nCopyright©️ 2024", "О программе MDI_Paint", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void FindPlugins()
        {
            // папка с плагинами
            string folder = AppDomain.CurrentDomain.BaseDirectory;

            // Создаем или загружаем конфигурационный файл
            string configFilePath = System.IO.Path.Combine(folder, "plugins.config");
            if (!File.Exists(configFilePath))
            {
                // Создаем новый конфигурационный файл и записываем в него имена плагинов
                File.WriteAllLines(configFilePath, new[] { "SepiaPlugin", "ShufflePlugin", "ContrastPlugin" });
            }

            // Читаем имена плагинов из конфигурационного файла
            string[] requiredPluginNames = File.ReadAllLines(configFilePath);

            // dll-файлы в этой папке
            string[] files = Directory.GetFiles(folder, "*.dll");

            foreach (string file in files)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFile(file);

                    foreach (Type type in assembly.GetTypes())
                    {
                        Type iface = type.GetInterface("PluginInterface.IPlugin");

                        if (iface != null)
                        {
                            IPlugin plugin = (IPlugin)Activator.CreateInstance(type);

                            // Проверяем, есть ли имя плагина в списке требуемых плагинов
                            if (Array.Exists(requiredPluginNames, name => name == plugin.Name))
                            {
                                pluginsList.Add(plugin);

                                var item = new Fluent.Button();
                                if (PluginsTab.Visibility == Visibility.Hidden)
                                {
                                    item.Margin = new Thickness(0, 5, 0, 0);
                                    PluginsTab.Visibility = Visibility.Visible;
                                }

                                item.Header = plugin.ButtonName;

                                async void ClickHandler(object sender, RoutedEventArgs e)
                                {
                                    this.IsEnabled = false;
                                    ProgressBarWindow progressBarWindow = new ProgressBarWindow();
                                    progressBarWindow.Owner = this; // Устанавливаем владельца для модального окна
                                    progressBarWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Позиционируем окно по центру владельца
                                    progressBarWindow.Show(); // Открываем окно как немодальное

                                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                                    progressBarWindow.CancelRequested += (sen, args) =>
                                    {
                                        cancellationTokenSource.Cancel();
                                    };

                                    // Подписка на событие ProgressChanged
                                    plugin.ProgressChanged += (send, args) =>
                                    {
                                        // Обновление прогресс бара в UI-потоке
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            progressBarWindow.UpdateProgress(args.Progress);
                                        });
                                    };

                                    try
                                    {
                                        // Выполнение операции с асинхронным методом TransformAsync
                                        edited[tabsController.SelectedIndex] = true;
                                        var newCanvas = await plugin.Transform(inkCanvas, cancellationTokenSource);
                                        inkCanvas = InitiateCanvasEvents(newCanvas);
                                        progressBarWindow.Close();
                                    }
                                    catch (OperationCanceledException)
                                    {

                                    }
                                    finally
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            // Разблокируем главное окно
                                            this.IsEnabled = true;
                                        });
                                    }
                                }

                                item.Click += ClickHandler;
                                item.SetBinding(IsEnabledProperty, new Binding
                                {
                                    ElementName = "tabsController",
                                    Path = new PropertyPath("Items.Count")
                                });

                                item.VerticalAlignment = VerticalAlignment.Center;
                                item.Size = RibbonControlSize.Middle;

                                PluginsTab.Items.Add(item);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка загрузки плагина\n" + ex.Message);
                }
            }
        }

        InkCanvas InitiateCanvasEvents(InkCanvas ic)
        {
            ic.MouseLeftButtonDown += inkCanvas_MouseLeftButtonDown;
            ic.MouseMove += inkCanvas_MouseMove;
            ic.MouseLeftButtonUp += inkCanvas_MouseLeftButtonUp;
            ic.MouseLeave += inkCanvas_MouseLeave;
            ic.MouseWheel += inkCanvas_MouseWheel;
            ic.StrokeCollected += inkCanvas_StrokeCollected;
            ic.StrokeErased += inkCanvas_StrokeErased;

            return ic;
        }

        private void plugins_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string pluginsInfo = "";

            foreach (var plugin in pluginsList) 
            {
                var versionAttribute = Attribute.GetCustomAttribute(plugin.GetType(), typeof(VersionAttribute)) as VersionAttribute;

                pluginsInfo += $"{plugin.Name} " + $"by {plugin.Author} " + $"(v.{versionAttribute.Major}.{versionAttribute.Minor})\n";
            }

            if (pluginsInfo.Length == 0 ) 
            {
                pluginsInfo = "У вас нет подключенных плагинов!";
            }
            MessageBox.Show(pluginsInfo, "Подключенные плагины", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
