using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace البطاريه
{
    public class ArcConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == null || values[1] == null)
            {
                return DependencyProperty.UnsetValue;
            }

            double value;
            double diameter;
            double thickness = 0;
            try
            {
                value = System.Convert.ToDouble(values[0], culture);
                diameter = System.Convert.ToDouble(values[1], culture);
                if (values.Length >= 3 && values[2] != null)
                {
                    thickness = System.Convert.ToDouble(values[2], culture);
                }
            }
            catch (Exception)
            {
                return DependencyProperty.UnsetValue;
            }

            if (double.IsNaN(value) || double.IsInfinity(value) ||
                double.IsNaN(diameter) || double.IsInfinity(diameter) ||
                double.IsNaN(thickness) || double.IsInfinity(thickness))
            {
                return DependencyProperty.UnsetValue;
            }

            if (diameter <= 0 || thickness < 0)
            {
                return DependencyProperty.UnsetValue;
            }

            if (value < 0) value = 0;
            if (value >= 100) value = 99.999; // Avoid full circle closing issue

            double radius = (diameter - thickness) / 2.0;
            if (radius <= 0)
            {
                return DependencyProperty.UnsetValue;
            }

            double angle = (value / 100) * 360;
            double angleRad = (angle - 90) * Math.PI / 180; // -90 to start from top

            double center = radius + (thickness / 2.0);
            double x = center + radius * Math.Cos(angleRad);
            double y = center + radius * Math.Sin(angleRad);

            Point startPoint = new Point(center, thickness / 2.0); // Top center (inside bounds)
            Point endPoint = new Point(x, y);

            bool isLargeArc = angle > 180;

            System.Windows.Media.PathGeometry geometry = new System.Windows.Media.PathGeometry();
            System.Windows.Media.PathFigure figure = new System.Windows.Media.PathFigure();
            figure.StartPoint = startPoint;

            System.Windows.Media.ArcSegment segment = new System.Windows.Media.ArcSegment();
            segment.Point = endPoint;
            segment.Size = new Size(radius, radius);
            segment.SweepDirection = System.Windows.Media.SweepDirection.Clockwise;
            segment.IsLargeArc = isLargeArc;

            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);

            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
