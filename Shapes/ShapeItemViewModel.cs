using System.Windows.Input;

namespace Shapes
{
    public class ShapeItemViewModel
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
        public ICommand Command { get; set; }
    }
}
