// Version: 0.0.0.1
// Thmd.Utilities/DragAdorner.cs
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Thmd.Utilities;

public class DragAdorner : Adorner
{
    private readonly UIElement _child;
    private double _left;
    private double _top;

    public DragAdorner(UIElement owner, UIElement child) : base(owner)
    {
        _child = child;
        IsHitTestVisible = false;
    }

    // METODA SETPOSITION – TERAZ JEST PUBLICZNA
    public void SetPosition(double left, double top)
    {
        _left = left;
        _top = top;
        if (Parent is AdornerLayer layer)
            layer.Update();
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _child;

    protected override Size MeasureOverride(Size constraint)
    {
        _child.Measure(constraint);
        return _child.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _child.Arrange(new Rect(finalSize));
        return finalSize;
    }

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        var result = new GeneralTransformGroup();
        result.Children.Add(new TranslateTransform(_left, _top));
        result.Children.Add(transform);
        return result;
    }
}
