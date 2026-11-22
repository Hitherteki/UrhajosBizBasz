using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SpaceShooter
{
    public sealed class WeaponSystem
    {
        private readonly Random _random = new Random();

        private BulletType _weapon = BulletType.Plasma;
        private double _heat;
        private double _heatMax = 100;
        private double _cooldown;
        private double _chargeTime;

        public double DamageMultiplier = 1;
        public double FireRateMultiplier = 1;
        public double BulletSpeedMultiplier = 1;
        public double HeatCapacityMultiplier = 1;
        public double CoolingMultiplier = 1;
        public double HeatGenerationMultiplier = 1;
        public double NovaCooldownMultiplier = 1;

        public int BulletCount = 1;
        public int ShieldRegenerationLevel;
        public int KineticDashLevel;
        public int PiercingLevel;
        public int ChainArcLevel;
        public int ThermalOverdriveLevel;
        public int ComboSiphonLevel;

        private int _plasmaLevel;
        private int _rocketLevel;
        private int _laserLevel;
        private int _shotgunLevel;
        private int _chargedLevel;

        public BulletType Weapon
        {
            get { return _weapon; }
        }

        public double Heat
        {
            get { return _heat; }
        }

        public double HeatMax
        {
            get { return _heatMax; }
        }

        public bool IsOverheated
        {
            get { return _heat >= _heatMax; }
        }

        public double ChargeRatio
        {
            get
            {
                if (_weapon != BulletType.Charged)
                    return 0;

                return Math.Min(1, _chargeTime / GetChargeTime());
            }
        }

        public double GetHeatRatio()
        {
            double ratio = _heat / _heatMax;

            if (ratio < 0)
                ratio = 0;

            if (ratio > 1)
                ratio = 1;

            return ratio;
        }

        public void ApplySkin(ShipSkin skin)
        {
            DamageMultiplier = skin.DamageMultiplier;
            FireRateMultiplier = skin.FireRateMultiplier;
            CoolingMultiplier = skin.CoolingMultiplier;
        }

        public void ApplyWeaponUpgrade(BulletType weapon)
        {
            switch (weapon)
            {
                case BulletType.Plasma:
                    _plasmaLevel++;
                    break;

                case BulletType.Rocket:
                    _rocketLevel++;
                    break;

                case BulletType.Laser:
                    _laserLevel++;
                    break;

                case BulletType.Shotgun:
                    _shotgunLevel++;
                    break;

                case BulletType.Charged:
                    _chargedLevel++;
                    break;
            }
        }

        public void SetWeapon(BulletType weapon)
        {
            _weapon = weapon;
            _chargeTime = 0;
        }

        public void CycleWeapon()
        {
            int next = (int)_weapon + 1;

            if (next >= 5)
                next = 0;

            _weapon = (BulletType)next;
            _chargeTime = 0;
        }

        public void Update(double dt)
        {
            if (_cooldown > 0)
                _cooldown -= dt;

            double cooling;

            if (IsOverheated)
                cooling = 44 * CoolingMultiplier;
            else
                cooling = 26 * CoolingMultiplier;

            _heat = Math.Max(0, _heat - cooling * dt);

            if (IsOverheated && _heat <= _heatMax * 0.32)
                _heat = _heatMax * 0.32;

            if (_weapon == BulletType.Charged)
                _chargeTime = Math.Min(GetChargeTime(), _chargeTime + dt);
        }

        public bool TryShoot(List<Bullet> bullets, double playerX, double playerY, double playerWidth)
        {
            if (_cooldown > 0 || IsOverheated)
                return false;

            double centerX = playerX + playerWidth / 2;
            double muzzleY = playerY - 12;

            switch (_weapon)
            {
                case BulletType.Plasma:
                    AddPlasma(bullets, centerX, muzzleY);
                    AddHeat(8);
                    _cooldown = 0.18 / FireRateMultiplier;
                    break;

                case BulletType.Rocket:
                    AddRocket(bullets, centerX, muzzleY);
                    AddHeat(22);
                    _cooldown = Math.Max(0.28, (0.65 - _rocketLevel * 0.05) / FireRateMultiplier);
                    break;

                case BulletType.Laser:
                    AddLaser(bullets, centerX, muzzleY);
                    AddHeat(14);
                    _cooldown = 0.09 / FireRateMultiplier;
                    break;

                case BulletType.Shotgun:
                    AddShotgun(bullets, centerX, muzzleY);
                    AddHeat(26);
                    _cooldown = 0.55 / FireRateMultiplier;
                    break;

                case BulletType.Charged:
                    AddCharged(bullets, centerX, muzzleY);
                    AddHeat(30);
                    _cooldown = 0.75 / FireRateMultiplier;
                    _chargeTime = 0;
                    break;
            }

            return true;
        }

        public double GetOverdriveDamageMultiplier()
        {
            if (ThermalOverdriveLevel <= 0)
                return 1;

            double ratio = GetHeatRatio();
            double bonus = ratio * 0.4 * ThermalOverdriveLevel;

            return 1 + bonus;
        }

        public bool ShouldChainArc()
        {
            if (ChainArcLevel <= 0)
                return false;

            int required = 4 - ChainArcLevel;

            if (required < 3)
                required = 3;

            return _random.Next(required) == 0;
        }

        public bool ShouldComboSiphon()
        {
            return ComboSiphonLevel > 0 && _random.Next(8 - Math.Min(4, ComboSiphonLevel)) == 0;
        }

        private double GetChargeTime()
        {
            return Math.Max(0.45, 1.2 - _chargedLevel * 0.15);
        }

        private void AddPlasma(List<Bullet> bullets, double x, double y)
        {
            int count = Math.Max(1, BulletCount + _plasmaLevel / 2);
            double spread = Math.Max(7, 14 - _plasmaLevel * 1.5);
            double size = 4 + _plasmaLevel * 0.7;

            for (int i = 0; i < count; i++)
            {
                double offset = (i - (count - 1) / 2.0) * spread;

                bullets.Add(CreateBullet(
                    BulletType.Plasma,
                    x - 2 + offset,
                    y,
                    offset * 0.55,
                    size,
                    18 + _plasmaLevel * 2,
                    820 * BulletSpeedMultiplier,
                    DamageMultiplier * GetOverdriveDamageMultiplier(),
                    0));
            }
        }

        private void AddRocket(List<Bullet> bullets, double x, double y)
        {
            bullets.Add(CreateBullet(
                BulletType.Rocket,
                x - 4,
                y,
                0,
                8 + _rocketLevel,
                24 + _rocketLevel * 2,
                820 * BulletSpeedMultiplier * 0.72,
                DamageMultiplier * GetOverdriveDamageMultiplier() * 2.4,
                85 + _rocketLevel * 16));
        }

        private void AddLaser(List<Bullet> bullets, double x, double y)
        {
            bullets.Add(CreateBullet(
                BulletType.Laser,
                x - 1.5,
                y,
                0,
                3 + _laserLevel * 0.4,
                34 + _laserLevel * 4,
                820 * BulletSpeedMultiplier * (1.45 + _laserLevel * 0.06),
                DamageMultiplier * GetOverdriveDamageMultiplier() * (0.65 + _laserLevel * 0.09),
                0));
        }

        private void AddShotgun(List<Bullet> bullets, double x, double y)
        {
            int pellets = 6 + _shotgunLevel;
            double spread = Math.Max(0.018, 0.035 - _shotgunLevel * 0.004);

            for (int i = 0; i < pellets; i++)
            {
                double offset = (i - (pellets - 1) / 2.0) * 9;
                double angle = offset * spread;

                bullets.Add(CreateBullet(
                    BulletType.Shotgun,
                    x - 2 + offset,
                    y,
                    Math.Sin(angle) * 820 * BulletSpeedMultiplier,
                    4,
                    12,
                    820 * BulletSpeedMultiplier * 0.85,
                    DamageMultiplier * GetOverdriveDamageMultiplier() * 0.55,
                    0));
            }
        }

        private void AddCharged(List<Bullet> bullets, double x, double y)
        {
            double ratio = ChargeRatio;

            bullets.Add(CreateBullet(
                BulletType.Charged,
                x - 5,
                y,
                0,
                10 + _chargedLevel,
                26 + _chargedLevel * 2,
                820 * BulletSpeedMultiplier * (0.85 + ratio * 0.35),
                DamageMultiplier * GetOverdriveDamageMultiplier() * (1.6 + ratio * (2.4 + _chargedLevel * 0.35)),
                35 + ratio * (55 + _chargedLevel * 12)));
        }

        private void AddHeat(double amount)
        {
            _heat = Math.Min(_heatMax, _heat + amount * HeatGenerationMultiplier);
        }

        private static Bullet CreateBullet(
            BulletType type,
            double x,
            double y,
            double vx,
            double width,
            double height,
            double speed,
            double damage,
            double explosionRadius)
        {
            Bullet bullet = new Bullet();

            bullet.Type = type;
            bullet.X = x;
            bullet.Y = y;
            bullet.PreviousX = x;
            bullet.PreviousY = y;
            bullet.Vx = vx;
            bullet.Width = width;
            bullet.Height = height;
            bullet.Speed = speed;
            bullet.Damage = damage;
            bullet.ExplosionRadius = explosionRadius;

            return bullet;
        }

        public static Color GetBulletColor(Bullet bullet)
        {
            switch (bullet.Type)
            {
                case BulletType.Rocket:
                    return Color.FromRgb(255, 140, 60);

                case BulletType.Laser:
                    return Color.FromRgb(180, 255, 120);

                case BulletType.Shotgun:
                    return Color.FromRgb(255, 220, 120);

                case BulletType.Charged:
                    return Color.FromRgb(190, 120, 255);

                default:
                    return Color.FromRgb(90, 240, 255);
            }
        }
    }
}