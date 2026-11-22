using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SpaceShooter
{
    public sealed class MeteoriteSystem
    {
        private readonly Random _random = new Random();

        public WaveKind CurrentWave = WaveKind.Normal;
        public double WaveTimer = 20;
        public int WaveNumber = 1;

        public void Update(double dt)
        {
            WaveTimer -= dt;

            if (WaveTimer > 0)
                return;

            WaveNumber++;
            WaveTimer = 20;
            CurrentWave = GetWave(WaveNumber);
        }

        public void Reset()
        {
            WaveNumber = 1;
            WaveTimer = 20;
            CurrentWave = WaveKind.Normal;
        }

        private WaveKind GetWave(int wave)
        {
            int value = wave % 5;

            switch (value)
            {
                case 1:
                    return WaveKind.Swarm;

                case 2:
                    return WaveKind.ArmoredConvoy;

                case 3:
                    return WaveKind.CrystalRush;

                case 4:
                    return WaveKind.UnstableField;

                default:
                    return WaveKind.Normal;
            }
        }

        public Asteroid Create(double width, double difficulty)
        {
            Asteroid asteroid = new Asteroid();

            asteroid.Type = ChooseType();
            asteroid.X = 30 + _random.NextDouble() * Math.Max(1, width - 60);
            asteroid.Y = -60;
            asteroid.Rotation = _random.NextDouble() * Math.PI * 2;
            asteroid.RotationSpeed = (_random.NextDouble() - 0.5) * 3.4;
            asteroid.RadiusOffsets = CreateOffsets();

            switch (asteroid.Type)
            {
                case 1:
                    asteroid.Radius = 26 + _random.NextDouble() * 14;
                    asteroid.Speed = (70 + difficulty * 8 + _random.NextDouble() * 25) * 0.78;
                    asteroid.MaxHealth = 4 + difficulty * 0.8;
                    break;

                case 2:
                    asteroid.Radius = 14 + _random.NextDouble() * 8;
                    asteroid.Speed = 150 + difficulty * 12 + _random.NextDouble() * 60;
                    asteroid.MaxHealth = 1 + difficulty * 0.2;
                    break;

                case 3:
                    asteroid.Radius = 21 + _random.NextDouble() * 11;
                    asteroid.Speed = 100 + difficulty * 9 + _random.NextDouble() * 35;
                    asteroid.MaxHealth = 3 + difficulty * 0.45;
                    break;

                case 4:
                    asteroid.Radius = 9 + _random.NextDouble() * 5;
                    asteroid.Speed = 170 + difficulty * 14 + _random.NextDouble() * 70;
                    asteroid.MaxHealth = 1;
                    break;

                default:
                    asteroid.Radius = 17 + _random.NextDouble() * 12;
                    asteroid.Speed = 110 + difficulty * 10 + _random.NextDouble() * 40;
                    asteroid.MaxHealth = 2 + difficulty * 0.35;
                    break;
            }

            asteroid.Health = asteroid.MaxHealth;
            return asteroid;
        }

        public List<Asteroid> CreateFragments(Asteroid source)
        {
            List<Asteroid> fragments = new List<Asteroid>();

            if (source.Type != 3)
                return fragments;

            int count = 2 + _random.Next(2);

            for (int i = 0; i < count; i++)
            {
                Asteroid fragment = new Asteroid();

                fragment.Type = 4;
                fragment.X = source.X;
                fragment.Y = source.Y;
                fragment.Radius = Math.Max(8, source.Radius * 0.38);
                fragment.Speed = source.Speed * 1.45;
                fragment.MaxHealth = 1;
                fragment.Health = 1;
                fragment.Rotation = _random.NextDouble() * Math.PI * 2;
                fragment.RotationSpeed = (_random.NextDouble() - 0.5) * 6;
                fragment.RadiusOffsets = CreateOffsets();

                double angle = (Math.PI * 2 / count) * i + _random.NextDouble() * 0.6;

                fragments.Add(fragment);
            }

            return fragments;
        }

        public int GetScore(Asteroid asteroid)
        {
            switch (asteroid.Type)
            {
                case 1:
                    return 120;

                case 2:
                    return 90;

                case 3:
                    return 100;

                case 4:
                    return 30;

                default:
                    return 50;
            }
        }

        public int GetExperience(Asteroid asteroid)
        {
            switch (asteroid.Type)
            {
                case 1:
                    return 22;

                case 2:
                    return 20;

                case 3:
                    return 16;

                case 4:
                    return 4;

                default:
                    return 12;
            }
        }

        public double GetPowerUpChance(Asteroid asteroid)
        {
            switch (asteroid.Type)
            {
                case 1:
                    return 0.16;

                case 2:
                    return 0.1;

                case 3:
                    return 0.12;

                case 4:
                    return 0.04;

                default:
                    return 0.08;
            }
        }

        public Color GetColor(Asteroid asteroid)
        {
            switch (asteroid.Type)
            {
                case 1:
                    return Color.FromRgb(120, 140, 170);

                case 2:
                    return Color.FromRgb(90, 255, 220);

                case 3:
                    return Color.FromRgb(255, 110, 50);

                case 4:
                    return Color.FromRgb(190, 190, 210);

                default:
                    return Color.FromRgb(140, 120, 110);
            }
        }

        private int ChooseType()
        {
            double value = _random.NextDouble();

            switch (CurrentWave)
            {
                case WaveKind.Swarm:
                    if (value < 0.65)
                        return 0;

                    if (value < 0.9)
                        return 4;

                    return 2;

                case WaveKind.ArmoredConvoy:
                    if (value < 0.45)
                        return 1;

                    if (value < 0.85)
                        return 0;

                    return 3;

                case WaveKind.CrystalRush:
                    if (value < 0.55)
                        return 2;

                    if (value < 0.85)
                        return 4;

                    return 0;

                case WaveKind.UnstableField:
                    if (value < 0.45)
                        return 3;

                    if (value < 0.75)
                        return 0;

                    return 4;

                default:
                    if (value < 0.62)
                        return 0;

                    if (value < 0.78)
                        return 1;

                    if (value < 0.9)
                        return 2;

                    return 3;
            }
        }

        private double[] CreateOffsets()
        {
            double[] offsets = new double[9];

            for (int i = 0; i < offsets.Length; i++)
                offsets[i] = 0.8 + _random.NextDouble() * 0.35;

            return offsets;
        }
    }
}