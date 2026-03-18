using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using Microsoft.Xna.Framework;
using Terraria;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class CreatureSwimWanderModule : INpcModule<CreatureNpcContext>
    {
        public float Score = 2.5f;
        public float MoveSpeed = 2.1f;
        public float TurnRate = 0.055f;
        public float VerticalStrength = 0.55f;
        public float HomeRadius = 160f;
        public float DepthSlack = 56f;

        public NpcDirective Evaluate(CreatureNpcContext context)
        {
            NPC self = context.Self;
            if (self is null || !self.active)
                return NpcDirective.None;

            // Base heading from current motion.
            Vector2 currentDir = self.velocity;
            if (currentDir.LengthSquared() < 0.01f)
            {
                float seedAngle = self.whoAmI * 0.73f;
                currentDir = seedAngle.ToRotationVector2();
            }
            else
                currentDir.Normalize();


            Vector2 wanderDir = context.TargetPoint;

            

            Vector2 desired = currentDir * 0.65f + wanderDir * 0.75f;
            if (!context.HomePosition.Equals(Vector2.zeroVector))
            {
                // Soft pull back toward home area.
                Vector2 toHome = context.HomePosition - self.Center;
                float homeDist = toHome.Length();

                if (homeDist > HomeRadius && homeDist > 0.001f)
                    desired += toHome / homeDist * MathHelper.Clamp((homeDist - HomeRadius) / 120f, 0f, 1f);

            }

            // Soft pull toward preferred depth.
            float depthError = 0;
            if (System.Math.Abs(depthError) > DepthSlack)
                desired += Vector2.UnitY * MathHelper.Clamp(depthError / 160f, -1f, 1f) * 0.7f;

            if (desired.LengthSquared() <= 0.0001f)
                return NpcDirective.None;

            desired.Normalize();
            desired *= MoveSpeed;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = desired,
                DebugName = nameof(CreatureSwimWanderModule),
                
            };
        }
    }
}
