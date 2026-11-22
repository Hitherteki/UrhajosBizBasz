using System;
using System.Windows;
using System.Windows.Media;

namespace SpaceShooter
{
    public enum GameState
    {
        Boot,
        BootFadeOut,
        MenuAwakening,
        Menu,
        Hangar,
        HighScores,
        Bestiary,
        Graphics,
        Playing,
        Upgrade,
        Paused,
        GameOver
    }

    public enum UpgradeRarity
    {
        Common,
        Rare,
        Epic,
        Void
    }

    public enum UpgradeType
    {
        Damage,
        FireRate,
        Speed,
        Health,
        Shield,
        Multishot,
        BulletSpeed,
        HeatCapacity,
        Cooling,
        HeatEfficiency,
        ShieldRegeneration,
        KineticDash,
        NovaCondenser,
        Piercing,
        ChainArc,
        ThermalOverdrive,
        ComboSiphon,
        VoidOverclock,
        WeaponPlasma,
        WeaponRocket,
        WeaponLaser,
        WeaponShotgun,
        WeaponCharged
    }

    public enum BossKind
    {
        Sentinel,
        Warden,
        Overlord,
        Devourer,
        VoidTitan,
        AegisBastion,
        VortexWeaver,
        AbyssalLeviathan
    }

    public enum MenuAction
    {
        Start,
        Hangar,
        HighScores,
        Bestiary,
        Graphics,
        Back,
        PreviousSkin,
        NextSkin,
        ClearScores,
        Exit,
        Upgrade0,
        Upgrade1,
        Upgrade2,
        Reroll,
        Resume,
        QuitToMenu,
        QualityLow,
        QualityMedium,
        QualityHigh,
        QualityUltra
    }

    public enum GraphicsQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public enum BulletType
    {
        Plasma,
        Rocket,
        Laser,
        Shotgun,
        Charged
    }

    public enum WaveKind
    {
        Normal,
        Swarm,
        ArmoredConvoy,
        CrystalRush,
        UnstableField
    }

    public sealed class MenuButton
    {
        public MenuAction Action;
        public Rect Bounds;
        public string Text;
    }

    public sealed class HighScoreEntry
    {
        public int Score;
        public int Level;
        public int Bosses;
        public DateTime Date;
    }

    public sealed class FloatingText
    {
        public string Text;
        public double X;
        public double Y;
        public double Life;
        public double MaxLife;
        public Color Color;
    }

    public sealed class ShipSkin
    {
        public string Name;
        public Color Body;
        public Color Outline;
        public Color Cockpit;
        public Color Engine;
        public int Shape;
        public double DamageMultiplier;
        public double SpeedMultiplier;
        public double FireRateMultiplier;
        public double CoolingMultiplier;
        public int HealthBonus;
    }

    public sealed class Bullet
    {
        public BulletType Type;
        public double X;
        public double Y;
        public double PreviousX;
        public double PreviousY;
        public double Vx;
        public double Width;
        public double Height;
        public double Speed;
        public double Damage;
        public double ExplosionRadius;
        public bool Piercing;
        public int Hits;
        public int ChainCounter;
    }

    public sealed class EnemyBullet
    {
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Radius;
    }

    public sealed class Asteroid
    {
        public int Type;
        public double X;
        public double Y;
        public double Radius;
        public double Speed;
        public double Health;
        public double MaxHealth;
        public double Rotation;
        public double RotationSpeed;
        public double[] RadiusOffsets;
    }

    public sealed class Boss
    {
        public BossKind Kind;
        public double X;
        public double Y;
        public double Width;
        public double Height;
        public double Health;
        public double MaxHealth;
        public double Speed;
        public double ShootTimer;
        public double PatternTimer;
        public int Direction;
        public int AttackIndex;
        public BossPhaseSystem Phase = new BossPhaseSystem();
    }

    public sealed class PowerUp
    {
        public double X;
        public double Y;
        public int Type;
        public double Rotation;
    }

    public sealed class Particle
    {
        public double X;
        public double Y;
        public double Vx;
        public double Vy;
        public double Life;
        public double MaxLife;
        public double Size;
        public Color Color;
    }

    public sealed class UpgradeOption
    {
        public UpgradeType Type;
        public UpgradeRarity Rarity;
        public string Title;
        public string Description;
        public Color Color;
    }

    public static class BossData
    {
        public static readonly string[] Names =
        {
            "SENTINEL",
            "WARDEN",
            "OVERLORD",
            "DEVOURER",
            "VOID TITAN",
            "AEGIS BASTION",
            "VORTEX WEAVER",
            "ABYSSAL LEVIATHAN"
        };

        public static readonly Color[] Colors =
        {
            Color.FromRgb(0, 220, 255),
            Color.FromRgb(255, 190, 60),
            Color.FromRgb(180, 80, 255),
            Color.FromRgb(255, 70, 120),
            Color.FromRgb(120, 255, 170),
            Color.FromRgb(90, 170, 255),
            Color.FromRgb(190, 90, 255),
            Color.FromRgb(255, 60, 90)
        };

        public static double GetHealth(int bossNumber)
        {
            return 70 + bossNumber * 42;
        }

        public static double GetSpeed(int bossNumber)
        {
            return 100 + bossNumber * 14;
        }

        public static double GetFireInterval(int bossNumber)
        {
            return Math.Max(0.28, 1.25 - bossNumber * 0.07);
        }
    }

    public static class SkinData
    {
        public static readonly ShipSkin[] Skins =
        {
            new ShipSkin
            {
                Name = "AZÚR FANTOM",
                Body = Color.FromRgb(18, 30, 54),
                Outline = Color.FromRgb(0, 235, 255),
                Cockpit = Color.FromRgb(0, 240, 255),
                Engine = Color.FromRgb(0, 190, 255),
                Shape = 0,
                DamageMultiplier = 1.0,
                SpeedMultiplier = 1.0,
                FireRateMultiplier = 1.0,
                CoolingMultiplier = 1.0,
                HealthBonus = 0
            },
            new ShipSkin
            {
                Name = "KRIMZON DÚLÓ",
                Body = Color.FromRgb(54, 16, 30),
                Outline = Color.FromRgb(255, 80, 120),
                Cockpit = Color.FromRgb(255, 190, 200),
                Engine = Color.FromRgb(255, 90, 140),
                Shape = 1,
                DamageMultiplier = 1.15,
                SpeedMultiplier = 0.95,
                FireRateMultiplier = 1.0,
                CoolingMultiplier = 1.0,
                HealthBonus = 0
            },
            new ShipSkin
            {
                Name = "SMARAGD FANTOM",
                Body = Color.FromRgb(14, 44, 34),
                Outline = Color.FromRgb(70, 255, 170),
                Cockpit = Color.FromRgb(180, 255, 220),
                Engine = Color.FromRgb(80, 255, 180),
                Shape = 2,
                DamageMultiplier = 1.0,
                SpeedMultiplier = 1.1,
                FireRateMultiplier = 1.0,
                CoolingMultiplier = 1.0,
                HealthBonus = 10
            },
            new ShipSkin
            {
                Name = "NAPWRAITH",
                Body = Color.FromRgb(56, 40, 12),
                Outline = Color.FromRgb(255, 205, 70),
                Cockpit = Color.FromRgb(255, 235, 160),
                Engine = Color.FromRgb(255, 170, 40),
                Shape = 3,
                DamageMultiplier = 1.05,
                SpeedMultiplier = 1.0,
                FireRateMultiplier = 1.15,
                CoolingMultiplier = 0.9,
                HealthBonus = 0
            },
            new ShipSkin
            {
                Name = "ÁRNYÉK SZURKÁLÓ",
                Body = Color.FromRgb(24, 22, 38),
                Outline = Color.FromRgb(170, 110, 255),
                Cockpit = Color.FromRgb(220, 180, 255),
                Engine = Color.FromRgb(180, 110, 255),
                Shape = 4,
                DamageMultiplier = 1.1,
                SpeedMultiplier = 1.05,
                FireRateMultiplier = 1.0,
                CoolingMultiplier = 1.0,
                HealthBonus = -10
            },
            new ShipSkin
            {
                Name = "JÉGKORONA",
                Body = Color.FromRgb(22, 38, 52),
                Outline = Color.FromRgb(160, 230, 255),
                Cockpit = Color.FromRgb(220, 250, 255),
                Engine = Color.FromRgb(140, 220, 255),
                Shape = 5,
                DamageMultiplier = 1.0,
                SpeedMultiplier = 1.0,
                FireRateMultiplier = 1.0,
                CoolingMultiplier = 1.2,
                HealthBonus = 20
            },
            new ShipSkin
            {
                Name = "AEGIS PÁNCÉL",
                Body = Color.FromRgb(20, 34, 58),
                Outline = Color.FromRgb(90, 170, 255),
                Cockpit = Color.FromRgb(190, 225, 255),
                Engine = Color.FromRgb(70, 150, 255),
                Shape = 6,
                DamageMultiplier = 0.95,
                SpeedMultiplier = 0.95,
                FireRateMultiplier = 1.0,
                CoolingMultiplier = 1.1,
                HealthBonus = 30
            },
            new ShipSkin
            {
                Name = "VORTEX SZÖVŐ",
                Body = Color.FromRgb(38, 18, 56),
                Outline = Color.FromRgb(190, 90, 255),
                Cockpit = Color.FromRgb(230, 180, 255),
                Engine = Color.FromRgb(170, 80, 255),
                Shape = 7,
                DamageMultiplier = 1.1,
                SpeedMultiplier = 1.1,
                FireRateMultiplier = 1.05,
                CoolingMultiplier = 0.95,
                HealthBonus = -5
            }
        };
    }
}