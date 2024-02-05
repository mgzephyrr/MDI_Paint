using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shapes
{
    public class ShapesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ShapeItemViewModel> ShapeItems { get; } = new ObservableCollection<ShapeItemViewModel>
        {
            new ShapeItemViewModel { Name = "Line", IconPath = "Images/Line.png" },
            new ShapeItemViewModel { Name = "Ellipse", IconPath = "Images/Ellipse.png" },
            new ShapeItemViewModel { Name = "Star", IconPath = "Images/Star.png" }
        };

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
