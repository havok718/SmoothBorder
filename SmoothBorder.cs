using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MyProject.Controls
{
    internal class SmoothBorder : Border
    {
        private readonly struct Radii
        {
            internal readonly double LeftTop;
            internal readonly double TopLeft;
            internal readonly double TopRight;
            internal readonly double RightTop;
            internal readonly double RightBottom;
            internal readonly double BottomRight;
            internal readonly double BottomLeft;
            internal readonly double LeftBottom;

            internal Radii(CornerRadius radii, Thickness borders, bool outer)
            {
                var num1 = 0.5 * borders.Left;
                var num2 = 0.5 * borders.Top;
                var num3 = 0.5 * borders.Right;
                var num4 = 0.5 * borders.Bottom;

                if (outer)
                {
                    if (radii.TopLeft.CompareTo(0) == 0)
                    {
                        LeftTop = TopLeft = 0.0;
                    }
                    else
                    {
                        LeftTop = radii.TopLeft + num1;
                        TopLeft = radii.TopLeft + num2;
                    }
                    if (radii.TopRight.CompareTo(0) == 0)
                    {
                        TopRight = RightTop = 0.0;
                    }
                    else
                    {
                        TopRight = radii.TopRight + num2;
                        RightTop = radii.TopRight + num3;
                    }
                    if (radii.BottomRight.CompareTo(0) == 0)
                    {
                        RightBottom = BottomRight = 0.0;
                    }
                    else
                    {
                        RightBottom = radii.BottomRight + num3;
                        BottomRight = radii.BottomRight + num4;
                    }
                    if (radii.BottomLeft.CompareTo(0) == 0)
                    {
                        BottomLeft = this.LeftBottom = 0.0;
                    }
                    else
                    {
                        BottomLeft = radii.BottomLeft + num4;
                        LeftBottom = radii.BottomLeft + num1;
                    }
                }
                else
                {
                    LeftTop = Math.Max(0.0, radii.TopLeft - num1);
                    TopLeft = Math.Max(0.0, radii.TopLeft - num2);
                    TopRight = Math.Max(0.0, radii.TopRight - num2);
                    RightTop = Math.Max(0.0, radii.TopRight - num3);
                    RightBottom = Math.Max(0.0, radii.BottomRight - num3);
                    BottomRight = Math.Max(0.0, radii.BottomRight - num4);
                    BottomLeft = Math.Max(0.0, radii.BottomLeft - num4);
                    LeftBottom = Math.Max(0.0, radii.BottomLeft - num1);
                }
            }
        }

        private StreamGeometry _backgroundGeometryCache = new StreamGeometry();
        private StreamGeometry _borderGeometryCache = new StreamGeometry();

        public static readonly DependencyProperty SmoothingProperty =
            DependencyProperty.Register(nameof(SmoothingProperty), typeof(double), typeof(SmoothBorder),
                new PropertyMetadata(1.0));

        public double Smoothing
        {
            get => (double)GetValue(SmoothingProperty);
            set => SetValue(SmoothingProperty, value);
        }

        private const double BezierControlPointCoef = 1.10;
        private const double BezierCoef = 0.95;

        protected override Size ArrangeOverride(Size finalSize)
        {
            var rect1 = new Rect(finalSize);
            var rect2 = HelperDeflateRect(rect1, BorderThickness);

            Child?.Arrange(HelperDeflateRect(rect2, Padding));

            if (rect2.Width > 0 && rect2.Height > 0)
            {
                var geometry = new StreamGeometry();

                using (var ctx = geometry.Open())
                    GenerateGeometry(ctx, rect2, new Radii(CornerRadius, BorderThickness, false), Smoothing);

                geometry.Freeze();

                _backgroundGeometryCache = _borderGeometryCache = geometry;
            }
            else
            {
                _backgroundGeometryCache = null;
            }

            if (rect1.Width.CompareTo(0) != 0 && rect1.Height.CompareTo(0) != 0)
            {
                var geometry = new StreamGeometry();

                using (var ctx = geometry.Open())
                    GenerateGeometry(ctx, rect1, new Radii(CornerRadius, BorderThickness, true), Smoothing);

                geometry.Freeze();

                _borderGeometryCache = geometry;
            }
            else
            {
                _borderGeometryCache = null;
            }

            return finalSize;
        }

        private static void GenerateGeometry(StreamGeometryContext ctx, Rect rect, Radii radii, double s)
        {
            //Calculate inner bezier control points coefficient
            var bc = radii.LeftTop * (1 - s) * Math.PI / 2 * BezierControlPointCoef;

            //Calculate points positions
            var points = new[]
            {
                new Point(radii.LeftTop / s * Math.PI / 2 * BezierCoef, 0.0),
                new Point(rect.Width - radii.RightTop / s * Math.PI / 2 * BezierCoef, 0.0),
                new Point(rect.Width, radii.TopRight / s * Math.PI / 2 * BezierCoef),
                new Point(rect.Width, rect.Height - radii.BottomRight / s * Math.PI / 2 * BezierCoef),
                new Point(rect.Width - radii.RightBottom / s * Math.PI / 2 * BezierCoef, rect.Height),
                new Point(radii.LeftBottom / s * Math.PI / 2 * BezierCoef, rect.Height),
                new Point(0.0, rect.Height - radii.BottomLeft / s * Math.PI / 2 * BezierCoef),
                new Point(0.0, radii.TopLeft / s * Math.PI / 2 * BezierCoef)
            };

            //Calculate bezier control points positions
            var bPoints = new[]
            {
                new Point(rect.Width - bc, 0),
                new Point(rect.Width, bc),
                new Point(rect.Width, rect.Height - bc),
                new Point(rect.Width - bc, rect.Height),
                new Point(bc, rect.Height),
                new Point(0, rect.Height - bc),
                new Point(0, bc),
                new Point(bc, 0)
            };

            if (points[0].X > points[1].X)
            {
                points[0].X = points[1].X = radii.LeftTop / (radii.LeftTop + radii.RightTop) * rect.Width;
            }
            if (points[2].Y > points[3].Y)
            {
                points[2].Y = points[3].Y = radii.TopRight / (radii.TopRight + radii.BottomRight) * rect.Height;
            }
            if (points[4].X < points[5].X)
            {
                points[4].X = points[5].X = radii.LeftBottom / (radii.LeftBottom + radii.RightBottom) * rect.Width;
            }
            if (points[6].Y < points[7].Y)
            {
                points[6].Y = points[7].Y = radii.TopLeft / (radii.TopLeft + radii.BottomLeft) * rect.Height;
            }

            var v = new Vector(rect.TopLeft.X, rect.TopLeft.Y);

            for (var i = 0; i < 8; i++)
            {
                points[i] += v;
                bPoints[i] += v;
            }

            //Draw top straight line
            ctx.BeginFigure(points[0], true, true);
            ctx.LineTo(points[1], true, false);
            ctx.BezierTo(bPoints[0], bPoints[1], points[2], false, false);
            ctx.LineTo(points[3], true, false);
            ctx.BezierTo(bPoints[2], bPoints[3], points[4], false, false);
            ctx.LineTo(points[5], true, false);
            ctx.BezierTo(bPoints[4], bPoints[5], points[6], false, false);
            ctx.LineTo(points[7], true, false);
            ctx.BezierTo(bPoints[6], bPoints[7], points[0], false, false);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_borderGeometryCache != null && BorderBrush != null)
                dc.DrawGeometry(BorderBrush, null, _borderGeometryCache);

            if (_backgroundGeometryCache != null && Background != null)
                dc.DrawGeometry(Background, null, _backgroundGeometryCache);
        }

        private static Rect HelperDeflateRect(Rect rt, Thickness thick)
        {
            return new Rect(rt.Left + thick.Left, rt.Top + thick.Top,
                Math.Max(0.0, rt.Width - thick.Left - thick.Right),
                Math.Max(0.0, rt.Height - thick.Top - thick.Bottom));
        }
    }
}
