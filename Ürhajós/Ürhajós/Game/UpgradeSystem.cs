using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SpaceShooter
{
    public sealed class UpgradeSystem
    {
        private readonly Random _random = new Random();
        private readonly Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();

        public int Rerolls { get; private set; } = 1;

        public int GetLevel(UpgradeType type)
        {
            int level;

            if (_levels.TryGetValue(type, out level))
                return level;

            return 0;
        }

        public bool IsMaxed(UpgradeType type)
        {
            return GetLevel(type) >= GetMaxLevel(type);
        }

        public int GetMaxLevel(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Multishot:
                case UpgradeType.ShieldRegeneration:
                case UpgradeType.KineticDash:
                case UpgradeType.NovaCondenser:
                case UpgradeType.Piercing:
                case UpgradeType.ChainArc:
                case UpgradeType.ThermalOverdrive:
                case UpgradeType.ComboSiphon:
                case UpgradeType.VoidOverclock:
                    return 3;

                case UpgradeType.WeaponPlasma:
                case UpgradeType.WeaponRocket:
                case UpgradeType.WeaponLaser:
                case UpgradeType.WeaponShotgun:
                case UpgradeType.WeaponCharged:
                    return 4;

                default:
                    return 8;
            }
        }

        public void AddReroll()
        {
            Rerolls++;
        }

        public bool CanReroll()
        {
            return Rerolls > 0;
        }

        public void UseReroll()
        {
            if (Rerolls > 0)
                Rerolls--;
        }

        public void Apply(UpgradeType type, WeaponSystem weapons)
        {
            if (IsMaxed(type))
                return;

            _levels[type] = GetLevel(type) + 1;

            switch (type)
            {
                case UpgradeType.Damage:
                    weapons.DamageMultiplier *= 1.15;
                    break;

                case UpgradeType.FireRate:
                    weapons.FireRateMultiplier *= 1.12;
                    break;

                case UpgradeType.BulletSpeed:
                    weapons.BulletSpeedMultiplier *= 1.1;
                    break;

                case UpgradeType.Multishot:
                    weapons.BulletCount++;
                    break;

                case UpgradeType.HeatCapacity:
                    weapons.HeatCapacityMultiplier *= 1.15;
                    break;

                case UpgradeType.Cooling:
                    weapons.CoolingMultiplier *= 1.15;
                    break;

                case UpgradeType.HeatEfficiency:
                    weapons.CoolingMultiplier *= 1.08;
                    break;

                case UpgradeType.ShieldRegeneration:
                    weapons.ShieldRegenerationLevel++;
                    break;

                case UpgradeType.KineticDash:
                    weapons.KineticDashLevel++;
                    break;

                case UpgradeType.NovaCondenser:
                    weapons.NovaCooldownMultiplier *= 0.8;
                    break;

                case UpgradeType.Piercing:
                    weapons.PiercingLevel++;
                    break;

                case UpgradeType.ChainArc:
                    weapons.ChainArcLevel++;
                    break;

                case UpgradeType.ThermalOverdrive:
                    weapons.ThermalOverdriveLevel++;
                    break;

                case UpgradeType.ComboSiphon:
                    weapons.ComboSiphonLevel++;
                    break;

                case UpgradeType.VoidOverclock:
                    weapons.DamageMultiplier *= 1.55;
                    weapons.HeatGenerationMultiplier *= 1.2;
                    break;

                case UpgradeType.WeaponPlasma:
                    weapons.ApplyWeaponUpgrade(BulletType.Plasma);
                    break;

                case UpgradeType.WeaponRocket:
                    weapons.ApplyWeaponUpgrade(BulletType.Rocket);
                    break;

                case UpgradeType.WeaponLaser:
                    weapons.ApplyWeaponUpgrade(BulletType.Laser);
                    break;

                case UpgradeType.WeaponShotgun:
                    weapons.ApplyWeaponUpgrade(BulletType.Shotgun);
                    break;

                case UpgradeType.WeaponCharged:
                    weapons.ApplyWeaponUpgrade(BulletType.Charged);
                    break;
            }
        }

        public List<UpgradeOption> Generate(BulletType weapon)
        {
            List<UpgradeOption> options = new List<UpgradeOption>();
            List<UpgradeType> pool = BuildPool(weapon);

            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int index = _random.Next(pool.Count);
                UpgradeType type = pool[index];
                pool.RemoveAt(index);

                options.Add(CreateOption(type));
            }

            return options;
        }

        private List<UpgradeType> BuildPool(BulletType weapon)
        {
            List<UpgradeType> pool = new List<UpgradeType>();

            AddIfAvailable(pool, UpgradeType.Damage, 6);
            AddIfAvailable(pool, UpgradeType.FireRate, 6);
            AddIfAvailable(pool, UpgradeType.Speed, 4);
            AddIfAvailable(pool, UpgradeType.Health, 5);
            AddIfAvailable(pool, UpgradeType.Shield, 5);
            AddIfAvailable(pool, UpgradeType.Multishot, 3);
            AddIfAvailable(pool, UpgradeType.BulletSpeed, 4);
            AddIfAvailable(pool, UpgradeType.HeatCapacity, 4);
            AddIfAvailable(pool, UpgradeType.Cooling, 4);
            AddIfAvailable(pool, UpgradeType.HeatEfficiency, 4);

            AddIfAvailable(pool, UpgradeType.ShieldRegeneration, 3);
            AddIfAvailable(pool, UpgradeType.KineticDash, 2);
            AddIfAvailable(pool, UpgradeType.NovaCondenser, 3);
            AddIfAvailable(pool, UpgradeType.Piercing, 3);

            AddIfAvailable(pool, UpgradeType.ChainArc, 2);
            AddIfAvailable(pool, UpgradeType.ThermalOverdrive, 2);
            AddIfAvailable(pool, UpgradeType.ComboSiphon, 2);
            AddIfAvailable(pool, UpgradeType.VoidOverclock, 1);

            switch (weapon)
            {
                case BulletType.Plasma:
                    AddIfAvailable(pool, UpgradeType.WeaponPlasma, 4);
                    break;

                case BulletType.Rocket:
                    AddIfAvailable(pool, UpgradeType.WeaponRocket, 4);
                    break;

                case BulletType.Laser:
                    AddIfAvailable(pool, UpgradeType.WeaponLaser, 4);
                    break;

                case BulletType.Shotgun:
                    AddIfAvailable(pool, UpgradeType.WeaponShotgun, 4);
                    break;

                case BulletType.Charged:
                    AddIfAvailable(pool, UpgradeType.WeaponCharged, 4);
                    break;
            }

            return pool;
        }

        private void AddIfAvailable(List<UpgradeType> pool, UpgradeType type, int weight)
        {
            if (IsMaxed(type))
                return;

            for (int i = 0; i < weight; i++)
                pool.Add(type);
        }

        private UpgradeOption CreateOption(UpgradeType type)
        {
            UpgradeRarity rarity = GetRarity(type);

            return new UpgradeOption
            {
                Type = type,
                Rarity = rarity,
                Title = GetTitle(type),
                Description = GetDescription(type),
                Color = GetColor(rarity)
            };
        }

        private UpgradeRarity GetRarity(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.ShieldRegeneration:
                case UpgradeType.KineticDash:
                case UpgradeType.NovaCondenser:
                case UpgradeType.Piercing:
                    return UpgradeRarity.Rare;

                case UpgradeType.ChainArc:
                case UpgradeType.ThermalOverdrive:
                case UpgradeType.ComboSiphon:
                    return UpgradeRarity.Epic;

                case UpgradeType.VoidOverclock:
                    return UpgradeRarity.Void;

                default:
                    return UpgradeRarity.Common;
            }
        }

        private Color GetColor(UpgradeRarity rarity)
        {
            switch (rarity)
            {
                case UpgradeRarity.Rare:
                    return Color.FromRgb(110, 130, 255);

                case UpgradeRarity.Epic:
                    return Color.FromRgb(255, 200, 70);

                case UpgradeRarity.Void:
                    return Color.FromRgb(255, 70, 220);

                default:
                    return Color.FromRgb(80, 240, 180);
            }
        }

        private string GetTitle(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage: return "SEBZÉS";
                case UpgradeType.FireRate: return "TŰZGYORSASÁG";
                case UpgradeType.Speed: return "HAJTÓMŰ";
                case UpgradeType.Health: return "PÁNCÉLZAT";
                case UpgradeType.Shield: return "PAJZS";
                case UpgradeType.Multishot: return "TÖBBSZÖRÖS LÖVÉS";
                case UpgradeType.BulletSpeed: return "LÖVEDÉKSEBESSÉG";
                case UpgradeType.HeatCapacity: return "HŐKAPACITÁS";
                case UpgradeType.Cooling: return "HŰTÉS";
                case UpgradeType.HeatEfficiency: return "HATÉKONYSÁG";
                case UpgradeType.ShieldRegeneration: return "PAJZS-VISSZATÖLTÉS";
                case UpgradeType.KineticDash: return "KINETIKUS DASH";
                case UpgradeType.NovaCondenser: return "NOVA KONDENZÁTOR";
                case UpgradeType.Piercing: return "ÁTÜTŐ LÖVEDÉK";
                case UpgradeType.ChainArc: return "LÁNC-KISÜLÉS";
                case UpgradeType.ThermalOverdrive: return "TÚLFŰTÖTT TÖLTET";
                case UpgradeType.ComboSiphon: return "KOMBÓ-SZÍVÁS";
                case UpgradeType.VoidOverclock: return "VOID ALKU";
                case UpgradeType.WeaponPlasma: return "PLAZMA TUNING";
                case UpgradeType.WeaponRocket: return "RAKÉTA TUNING";
                case UpgradeType.WeaponLaser: return "LÉZER TUNING";
                case UpgradeType.WeaponShotgun: return "SÖRÉT TUNING";
                default: return "TÖLTŐ TUNING";
            }
        }

        private string GetDescription(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage: return "+15% sebzés";
                case UpgradeType.FireRate: return "+12% tűzgyorsaság";
                case UpgradeType.Speed: return "+8% mozgási sebesség";
                case UpgradeType.Health: return "+20 max életerő";
                case UpgradeType.Shield: return "+15 max pajzs";
                case UpgradeType.Multishot: return "+1 lövedék";
                case UpgradeType.BulletSpeed: return "+10% lövedéksebesség";
                case UpgradeType.HeatCapacity: return "+15% hőkapacitás";
                case UpgradeType.Cooling: return "+15% hűtés";
                case UpgradeType.HeatEfficiency: return "+8% hűtés";
                case UpgradeType.ShieldRegeneration: return "Pajzs lassan újratöltődik";
                case UpgradeType.KineticDash: return "A dash átüti a lövedékeket";
                case UpgradeType.NovaCondenser: return "-20% Nova cooldown";
                case UpgradeType.Piercing: return "Átüti a gyenge célpontokat";
                case UpgradeType.ChainArc: return "Időnként elektromos ív ugrik át";
                case UpgradeType.ThermalOverdrive: return "Magas hő mellett több sebzés";
                case UpgradeType.ComboSiphon: return "Kombó alatt energia-visszatöltés";
                case UpgradeType.VoidOverclock: return "+55% sebzés, +20% hőképződés";
                case UpgradeType.WeaponPlasma: return "Gyorsabb ikerlövés, nagyobb lövedék";
                case UpgradeType.WeaponRocket: return "Nagyobb robbanás, gyorsabb újratöltés";
                case UpgradeType.WeaponLaser: return "Erősebb és gyorsabb lézer";
                case UpgradeType.WeaponShotgun: return "Több és fókuszáltabb sörét";
                default: return "Gyorsabb töltés, erősebb robbanás";
            }
        }
    }
}