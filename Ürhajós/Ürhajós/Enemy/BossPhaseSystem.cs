using System;

namespace SpaceShooter
{
    public sealed class BossPhaseSystem
    {
        public int Phase;
        public double Telegraph;
        public bool Enraged;

        public void Update(Boss boss, double dt)
        {
            double ratio = boss.Health / boss.MaxHealth;

            int nextPhase;

            if (ratio > 0.7)
                nextPhase = 0;
            else if (ratio > 0.35)
                nextPhase = 1;
            else
                nextPhase = 2;

            if (nextPhase != Phase)
            {
                Phase = nextPhase;
                Telegraph = 0.65;
                Enraged = Phase == 2;
            }

            Telegraph = Math.Max(0, Telegraph - dt);
        }

        public double FireInterval(double baseInterval)
        {
            if (Phase == 1)
                return baseInterval * 0.78;

            if (Phase == 2)
                return baseInterval * 0.58;

            return baseInterval;
        }

        public int ProjectileMultiplier
        {
            get
            {
                if (Phase == 1)
                    return 2;

                if (Phase == 2)
                    return 3;

                return 1;
            }
        }
    }
}