using System;
using System.Collections.Generic;

namespace SpaceShooter
{
    public sealed class BossSystem
    {
        private readonly Random _random = new Random();

        public Boss Create(int bossNumber, double width)
        {
            Boss boss = new Boss();

            boss.Kind = (BossKind)(bossNumber % 8);
            boss.MaxHealth = BossData.GetHealth(bossNumber);
            boss.Health = boss.MaxHealth;
            boss.Width = Math.Min(280, width * 0.32);
            boss.Height = boss.Width * 0.55;
            boss.X = width / 2 - boss.Width / 2;
            boss.Y = 60;
            boss.Speed = BossData.GetSpeed(bossNumber);
            boss.ShootTimer = 1;
            boss.PatternTimer = 0;
            boss.Direction = 1;

            return boss;
        }

        public void Update(Boss boss, double width, double height, double dt, List<EnemyBullet> bullets)
        {
            boss.Phase.Update(boss, dt);
            boss.PatternTimer += dt;

            UpdateMovement(boss, width, dt);

            boss.ShootTimer -= dt;

            if (boss.ShootTimer > 0)
                return;

            boss.ShootTimer = boss.Phase.FireInterval(BossData.GetFireInterval((int)boss.Kind));

            if (boss.Phase.Telegraph > 0)
                return;

            boss.Phase.Telegraph = 0.55;

            switch (boss.Kind)
            {
                case BossKind.AegisBastion:
                    Fan(boss, bullets, 5 + boss.Phase.ProjectileMultiplier, 0.22);
                    break;

                case BossKind.VortexWeaver:
                    Spiral(boss, bullets, 8 + boss.Phase.ProjectileMultiplier * 2);
                    break;

                case BossKind.AbyssalLeviathan:
                    if (boss.Phase.Phase >= 2)
                    {
                        Fan(boss, bullets, 7, 0.16);
                        Spiral(boss, bullets, 6);
                    }
                    else
                    {
                        AimedBurst(boss, bullets, 3 + boss.Phase.ProjectileMultiplier);
                    }
                    break;

                default:
                    if (boss.Phase.Phase >= 2)
                        Fan(boss, bullets, 5 + boss.Phase.ProjectileMultiplier, 0.24);
                    else
                        AimedBurst(boss, bullets, 2 + boss.Phase.ProjectileMultiplier);
                    break;
            }
        }

        private void UpdateMovement(Boss boss, double width, double dt)
        {
            double left = 30;
            double right = width - boss.Width - 30;

            switch (boss.Kind)
            {
                case BossKind.AegisBastion:
                    boss.X += boss.Direction * boss.Speed * 0.45 * dt;
                    break;

                case BossKind.VortexWeaver:
                    boss.X += Math.Sin(boss.PatternTimer * 1.4) * boss.Speed * 0.9 * dt;
                    boss.Y = 60 + Math.Sin(boss.PatternTimer * 0.9) * 22;
                    break;

                case BossKind.AbyssalLeviathan:
                    boss.X += boss.Direction * boss.Speed * 1.15 * dt;
                    break;

                default:
                    boss.X += boss.Direction * boss.Speed * dt;
                    break;
            }

            if (boss.X <= left)
            {
                boss.X = left;
                boss.Direction = 1;
            }

            if (boss.X >= right)
            {
                boss.X = right;
                boss.Direction = -1;
            }
        }

        private void AimedBurst(Boss boss, List<EnemyBullet> bullets, int count)
        {
            double centerX = boss.X + boss.Width / 2;
            double bottom = boss.Y + boss.Height;

            for (int i = 0; i < count; i++)
            {
                double angle = Math.PI / 2 + (i - (count - 1) / 2.0) * 0.14;

                bullets.Add(CreateBullet(centerX, bottom, Math.Cos(angle) * 260, Math.Sin(angle) * 260, 7));
            }
        }

        private void Fan(Boss boss, List<EnemyBullet> bullets, int count, double spread)
        {
            double centerX = boss.X + boss.Width / 2;
            double bottom = boss.Y + boss.Height;

            for (int i = 0; i < count; i++)
            {
                double angle = Math.PI / 2 + (i - (count - 1) / 2.0) * spread;

                bullets.Add(CreateBullet(centerX, bottom, Math.Cos(angle) * 240, Math.Sin(angle) * 240, 6));
            }
        }

        private void Spiral(Boss boss, List<EnemyBullet> bullets, int count)
        {
            double centerX = boss.X + boss.Width / 2;
            double centerY = boss.Y + boss.Height / 2;
            double baseAngle = boss.PatternTimer * 3.2;

            for (int i = 0; i < count; i++)
            {
                double angle = baseAngle + (Math.PI * 2 / count) * i;

                bullets.Add(CreateBullet(centerX, centerY, Math.Cos(angle) * 205, Math.Sin(angle) * 205, 6));
            }
        }

        private static EnemyBullet CreateBullet(double x, double y, double vx, double vy, double radius)
        {
            EnemyBullet bullet = new EnemyBullet();

            bullet.X = x;
            bullet.Y = y;
            bullet.Vx = vx;
            bullet.Vy = vy;
            bullet.Radius = radius;

            return bullet;
        }
    }
}