using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using Microsoft.Xna.Framework;
using Terraria;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class AvoidTilesSwimModule : INpcModule<CreatureNpcContext>
    {
        public float Score = 12f;
        public float ProbeDistance = 36f;
        public float SideProbeDistance = 28f;
        public float MoveSpeed = 3.2f;

        public NpcDirective Evaluate(CreatureNpcContext context)
        {
            NPC self = context.Self;
            if (self is null || !self.active)
                return NpcDirective.None;

            Vector2 forward = self.velocity;
            if (forward.LengthSquared() < 0.05f)
                forward = new Vector2(self.direction == 0 ? 1f : self.direction, 0f);

            forward.Normalize();

            Vector2 left = forward.RotatedBy(-MathHelper.PiOver2);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            bool blockedForward = HitsSolid(self, forward, ProbeDistance);
            bool blockedLeft = HitsSolid(self, (forward + left * 0.75f).SafeNormalize(Vector2.Zero), SideProbeDistance);
            bool blockedRight = HitsSolid(self, (forward + right * 0.75f).SafeNormalize(Vector2.Zero), SideProbeDistance);
            bool blockedUp = HitsSolid(self, -Vector2.UnitY, 24f);
            bool blockedDown = HitsSolid(self, Vector2.UnitY, 24f);

            if (!blockedForward && !blockedLeft && !blockedRight && !blockedUp && !blockedDown)
                return NpcDirective.None;

            Vector2 steer = Vector2.Zero;

            if (blockedForward)
                steer -= forward * 1.3f;

            if (blockedLeft)
                steer += right * 1.1f;

            if (blockedRight)
                steer += left * 1.1f;

            if (blockedUp)
                steer += Vector2.UnitY * 0.9f;

            if (blockedDown)
                steer -= Vector2.UnitY * 0.9f;

            if (steer.LengthSquared() <= 0.0001f)
                steer = left;

            steer.Normalize();

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = steer * MoveSpeed,
                DebugName = nameof(AvoidTilesSwimModule),
                DebugInfo = $"F={blockedForward} L={blockedLeft} R={blockedRight} U={blockedUp} D={blockedDown}"
            };
        }

        private static bool HitsSolid(NPC npc, Vector2 dir, float distance)
        {
            if (dir.LengthSquared() <= 0.0001f)
                return false;

            Vector2 end = npc.Center + dir * distance;
            return Collision.CanHitLine(npc.position, npc.width, npc.height, end, 1, 1) == false;
        }
    }
}
