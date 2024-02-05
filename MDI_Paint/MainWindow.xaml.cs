using Fluent;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MDI_Paint
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
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
        }

        private void newFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TabItem tab = new TabItem();
            tab.Header = "Без имени " + filesCreated.ToString();
            InkCanvas ic = new InkCanvas();
            ic.Name = "ic" + filesCreated.ToString();
            filesCreated++;
            ic.MouseLeftButtonDown += inkCanvas_MouseLeftButtonDown;
            ic.MouseMove += inkCanvas_MouseMove;
            ic.MouseUp += inkCanvas_MouseUp;
            tab.Content = ic;
            tabsController.Items.Add(tab);
        }

        private void tabsController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (inkCanvas == null)
                return;

            pen_Click(new object(), new RoutedEventArgs());
            thickness.Value = inkCanvas.DefaultDrawingAttributes.Width;
        }

        private void closeTab_Click(object sender, RoutedEventArgs e)
        {
            //SAVE FILE
            tabsController.Items.Remove(tabsController.SelectedItem);
        }

        //private void OpenFile_Click(object sender, RoutedEventArgs e)
        //{
        //    OpenFileDialog openDialog = new OpenFileDialog
        //    {
        //        Filter = "Image Files (*.jpg)|*.jpg|Image Files (*.png)|*.png|Image Files (*.bmp)|*.bmp",
        //        Title = "Open Image File"
        //    };
        //    if (openDialog.ShowDialog() == true)
        //    {
        //        BitmapImage image = new BitmapImage();
        //        image.BeginInit();
        //        image.UriSource = new Uri(openDialog.FileName, UriKind.RelativeOrAbsolute);
        //        image.EndInit();
        //        imgMeasure.Source = image;
        //    }
        //}

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
            InkCanvas inkCanvas = (InkCanvas)sender;
            StartX = e.GetPosition(inkCanvas).X;
            StartY = e.GetPosition(inkCanvas).Y;

            if (ClickedShape != null)
            {
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
            InkCanvas inkCanvas = (InkCanvas)sender;
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

                    for (int i = 0; i < pointsCount; i++)
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

                    x = centerX + width / 2 * Math.Sin(Math.PI / angleCount.Value);
                    y = centerY + height / 2 * Math.Cos(Math.PI / angleCount.Value);

                    star.Points.Add(new Point(x, y));
                break;
            }
        }

        private void inkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            InkCanvas inkCanvas = (InkCanvas)sender;
            if (UnreleasedShape != null)
            {
                Stroke stroke;
                if (UnreleasedShape is Line line)
                {
                    stroke = ConvertLineToStroke(line);
                }
                else
                {  
                    stroke = ConvertEllipseToStroke(UnreleasedShape);
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

        private Stroke ConvertLineToStroke(Line line)
        {
            StylusPointCollection stylusPoints = new StylusPointCollection
            {
                new StylusPoint(line.X1, line.Y1),
                new StylusPoint(line.X2, line.Y2)
            };

            return new Stroke(stylusPoints);
        }

        private Stroke ConvertEllipseToStroke(Shape shape)
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
    }
}
