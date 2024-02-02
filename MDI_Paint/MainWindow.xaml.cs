using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MDI_Paint
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
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
            }  
        }

        private void eraser_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            }
        }

        private void colors_SelectedColorChanged(object sender, RoutedEventArgs e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.DefaultDrawingAttributes.Color = colors.SelectedColor != null ? (Color)colors.SelectedColor : Colors.Black;
            }
        }

        private void thickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkCanvas != null)
            {
                inkCanvas.DefaultDrawingAttributes.Width = thickness.Value;
                inkCanvas.DefaultDrawingAttributes.Height = thickness.Value;
            }
        }
    }
}
