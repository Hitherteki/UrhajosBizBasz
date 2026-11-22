using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SpaceShooter
{
    public sealed class VisualLayers
    {
        private readonly Random _random = new Random();
        private readonly List<BackgroundStar>[] _layers;
        private readonly List<NebulaCloud> _clouds = new List<NebulaCloud>();
        private readonly Dictionary<Color, SolidColorBrush> _brushCache = new Dictionary<Color, SolidColorBrush>();

        private double _planetX;
        private double _planetY;
        private double _planetRadius;

        public VisualLayers(int layerCount)
        {
            _layers = new List<BackgroundStar>[layerCount];

            for (int i = 0; i < layerCount; i++)
                _layers[i] = new List<BackgroundStar>();
        }

        public void Initialize(double width, double height, int starCount)
        {
            int perLayer = Math.Max(20, starCount / _layers.Length);

            for (int layer = 0; layer < _layers.Length; layer++)
            {
                _layers[layer].Clear();

                double speed = 18 + layer * 34;
                double size = 0.8 + layer * 0.45;

                for (int i = 0; i < perLayer; i++)
                {
                    BackgroundStar star = new BackgroundStar();

                    star.X = _random.NextDouble() * width;
                    star.Y = _random.NextDouble() * height;
                    star.Speed = speed + _random.NextDouble() * 22;
                    star.Size = size + _random.NextDouble() * 0.7;
                    star.Alpha = 0.25 + layer * 0.16;

                    _layers[layer].Add(star);
                }
            }

            _clouds.Clear();

            for (int i = 0; i < 3; i++)
            {
                NebulaCloud cloud = new NebulaCloud();

                cloud.X = _random.NextDouble() * width;
                cloud.Y = _random.NextDouble() * height;
                cloud.Radius = 180 + _random.NextDouble() * 320;
                cloud.Speed = 6 + _random.NextDouble() * 14;
                cloud.Hue = _random.NextDouble();

                _clouds.Add(cloud);
            }

            _planetRadius = Math.Min(width, height) * 0.22;
            _planetX = width * 0.78;
            _planetY = height * 0.22;
        }

        public void Update(double dt, double width, double height, double playerOffset)
        {
            for (int layer = 0; layer < _layers.Length; layer++)
            {
                List<BackgroundStar> stars = _layers[layer];
                double parallax = 1 + layer * 0.35;

                for (int i = 0; i < stars.Count; i++)
                {
                    BackgroundStar star = stars[i];

                    star.Y += star.Speed * dt;
                    star.X -= playerOffset * parallax * dt * 8;

                    if (star.Y > height + 4)
                    {
                        star.Y = -4;
                        star.X = _random.NextDouble() * width;
                    }

                    if (star.X < -20)
                        star.X += width + 40;

                    if (star.X > width + 20)
                        star.X -= width + 40;
                }
            }

            for (int i = 0; i < _clouds.Count; i++)
            {
                NebulaCloud cloud = _clouds[i];

                cloud.Y += cloud.Speed * dt;

                if (cloud.Y - cloud.Radius > height)
                {
                    cloud.Y = -cloud.Radius;
                    cloud.X = _random.NextDouble() * width;
                }
            }

            _planetY += 4 * dt;

            if (_planetY - _planetRadius > height)
                _planetY = -_planetRadius * 2;
        }

        public void Render(DrawingContext dc, double width, double height, bool bloom)
        {
            for (int i = 0; i < _clouds.Count; i++)
            {
                NebulaCloud cloud = _clouds[i];

                dc.DrawEllipse(
                    GetCloudBrush(cloud.Hue),
                    null,
                    new Point(cloud.X, cloud.Y),
                    cloud.Radius,
                    cloud.Radius * 0.72);
            }

            DrawPlanet(dc, bloom);

            for (int layer = 0; layer < _layers.Length; layer++)
            {
                List<BackgroundStar> stars = _layers[layer];

                for (int i = 0; i < stars.Count; i++)
                {
                    BackgroundStar star = stars[i];

                    dc.DrawEllipse(
                        GetStarBrush(star.Alpha),
                        null,
                        new Point(star.X, star.Y),
                        star.Size,
                        star.Size);
                }
            }
        }

        private void DrawPlanet(DrawingContext dc, bool bloom)
        {
            if (bloom)
            {
                dc.DrawEllipse(
                    GetBrush(Color.FromArgb(70, 120, 70, 255)),
                    null,
                    new Point(_planetX, _planetY),
                    _planetRadius * 1.5,
                    _planetRadius * 1.5);
            }

            dc.DrawEllipse(
                GetBrush(Color.FromRgb(60, 40, 120)),
                null,
                new Point(_planetX, _planetY),
                _planetRadius,
                _planetRadius);

            dc.DrawEllipse(
                null,
                GetPen(Color.FromArgb(70, 140, 190, 255), 2),
                new Point(_planetX, _planetY),
                _planetRadius * 1.45,
                _planetRadius * 0.35);
        }

        private Brush GetCloudBrush(double hue)
        {
            Color color;

            if (hue < 0.33)
                color = Color.FromArgb(34, 70, 40, 170);
            else if (hue < 0.66)
                color = Color.FromArgb(30, 30, 90, 180);
            else
                color = Color.FromArgb(28, 160, 40, 130);

            RadialGradientBrush brush = new RadialGradientBrush();

            brush.GradientStops.Add(new GradientStop(color, 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));

            if (brush.CanFreeze)
                brush.Freeze();

            return brush;
        }

        private Brush GetStarBrush(double alpha)
        {
            byte a = (byte)(alpha * 255);

            return GetBrush(Color.FromArgb(a, 205, 225, 255));
        }

        private Brush GetBrush(Color color)
        {
            SolidColorBrush brush;

            if (_brushCache.TryGetValue(color, out brush))
                return brush;

            brush = new SolidColorBrush(color);

            if (brush.CanFreeze)
                brush.Freeze();

            _brushCache[color] = brush;
            return brush;
        }

        private Pen GetPen(Color color, double thickness)
        {
            Pen pen = new Pen(GetBrush(color), thickness);

            if (pen.CanFreeze)
                pen.Freeze();

            return pen;
        }
    }

    public sealed class BackgroundStar
    {
        public double X;
        public double Y;
        public double Speed;
        public double Size;
        public double Alpha;
    }

    public sealed class NebulaCloud
    {
        public double X;
        public double Y;
        public double Radius;
        public double Speed;
        public double Hue;
    }
}