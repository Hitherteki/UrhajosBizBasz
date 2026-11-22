using System;
using System.Globalization;
using System.IO;

namespace SpaceShooter
{
    public sealed class GameSettings
    {
        public GraphicsQuality Quality = GraphicsQuality.High;
        public bool ScreenShake = true;
        public bool ScreenFlash = true;
        public bool BulletTrails = true;
        public bool BackgroundParallax = true;
        public bool NebulaClouds = true;
        public bool GlowEffects = true;
        public int ParticleLimit = 1200;

        public static GameSettings Load(string path)
        {
            GameSettings settings = new GameSettings();

            try
            {
                if (!File.Exists(path))
                    return settings;

                string[] lines = File.ReadAllLines(path);

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split('=');

                    if (parts.Length != 2)
                        continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (key)
                    {
                        case "QUALITY":
                            settings.Quality = ParseQuality(value);
                            break;

                        case "SCREEN_SHAKE":
                            settings.ScreenShake = ParseBool(value);
                            break;

                        case "SCREEN_FLASH":
                            settings.ScreenFlash = ParseBool(value);
                            break;

                        case "BULLET_TRAILS":
                            settings.BulletTrails = ParseBool(value);
                            break;

                        case "BACKGROUND_PARALLAX":
                            settings.BackgroundParallax = ParseBool(value);
                            break;

                        case "NEBULA_CLOUDS":
                            settings.NebulaClouds = ParseBool(value);
                            break;

                        case "GLOW_EFFECTS":
                            settings.GlowEffects = ParseBool(value);
                            break;

                        case "PARTICLE_LIMIT":
                            settings.ParticleLimit = ParseInt(value, 1200);
                            break;
                    }
                }
            }
            catch
            {
            }

            settings.ApplyLimits();
            return settings;
        }

        public void Save(string path)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                string[] lines =
                {
                    "QUALITY=" + Quality.ToString().ToUpperInvariant(),
                    "SCREEN_SHAKE=" + BoolText(ScreenShake),
                    "SCREEN_FLASH=" + BoolText(ScreenFlash),
                    "BULLET_TRAILS=" + BoolText(BulletTrails),
                    "BACKGROUND_PARALLAX=" + BoolText(BackgroundParallax),
                    "NEBULA_CLOUDS=" + BoolText(NebulaClouds),
                    "GLOW_EFFECTS=" + BoolText(GlowEffects),
                    "PARTICLE_LIMIT=" + ParticleLimit
                };

                File.WriteAllLines(path, lines);
            }
            catch
            {
            }
        }

        public void ApplyLimits()
        {
            switch (Quality)
            {
                case GraphicsQuality.Low:
                    ParticleLimit = 300;
                    break;

                case GraphicsQuality.Medium:
                    ParticleLimit = 600;
                    break;

                case GraphicsQuality.High:
                    ParticleLimit = 1200;
                    break;

                default:
                    ParticleLimit = 2500;
                    break;
            }
        }

        private static GraphicsQuality ParseQuality(string value)
        {
            switch (value)
            {
                case "LOW":
                    return GraphicsQuality.Low;

                case "MEDIUM":
                    return GraphicsQuality.Medium;

                case "ULTRA":
                    return GraphicsQuality.Ultra;

                default:
                    return GraphicsQuality.High;
            }
        }

        private static bool ParseBool(string value)
        {
            return value == "1" || value == "TRUE" || value == "true";
        }

        private static string BoolText(bool value)
        {
            return value ? "1" : "0";
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                return result;

            return fallback;
        }
    }
}