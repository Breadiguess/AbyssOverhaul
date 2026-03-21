using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.BehaviorOverrides.GooGazerNPC
{
    public sealed class GooGazerContext : PreyNpcContext
    {
        public bool HasFoodTile;
        public Point FoodTile;

        public float WanderAngle;
        public int LocalTimer;

        public bool WantsFood =>
            Hunger <= 0.42f && !HasThreat;

        public float FoodDrive
        {
            get
            {
                // 0 when energy is healthy, ramps up as it gets low.
                float t = Utilities.InverseLerp(0.42f, 0.0f, Hunger);
                return MathHelper.Clamp(t, 0f, 1f);
            }
        }

        public override void Update(NPC npc)
        {
            base.Update(npc);

            Hunger = MathHelper.Clamp(Hunger + 0.0007f, 0f, 1f);
            HasFoodTile = false;
            FoodTile = Point.Zero;
        }
    }
}
