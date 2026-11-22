using System;

namespace SpaceShooter
{
    public sealed class CombatFeedback
    {
        public int Combo;
        public double ComboTimer;
        public double Multiplier = 1;

        public void RegisterKill()
        {
            Combo++;
            ComboTimer = 2.4;
            Multiplier = Math.Min(5, 1 + Combo * 0.08);
        }

        public void Update(double dt)
        {
            if (ComboTimer <= 0)
                return;

            ComboTimer -= dt;

            if (ComboTimer <= 0)
            {
                Combo = 0;
                Multiplier = 1;
            }
        }

        public int ApplyScore(int baseScore)
        {
            return (int)(baseScore * Multiplier);
        }
    }
}