using AbyssOverhaul.Common.Brain.Contexts;

namespace AbyssOverhaul.Common.Brain.SharedModules
{



    /// <summary>
    /// Pushes the NPC away from nearby active NPCs of the same type.
    /// Useful for preventing clumping / overlap / dogpiling.
    /// </summary>
    public sealed class AvoidSameTypeModule : INpcModule<NpcContext>
    {
        /// <summary>
        /// How far away same-type NPCs are considered for avoidance.
        /// </summary>
        public float AvoidanceRadius { get; set; } = 96f;

        /// <summary>
        /// Preferred minimum spacing between same-type NPCs.
        /// Inside this radius, repulsion ramps up sharply.
        /// </summary>
        public float PreferredSeparation { get; set; } = 48f;

        /// <summary>
        /// Caps the returned desired velocity magnitude.
        /// </summary>
        public float MaxMoveSpeed { get; set; } = 3.5f;

        /// <summary>
        /// Base priority when this behavior is active.
        /// </summary>
        public float BaseScore { get; set; } = 3f;

        /// <summary>
        /// Extra score added based on crowding intensity.
        /// </summary>
        public float CrowdingScoreMultiplier { get; set; } = 8f;

        /// <summary>
        /// If true, only consider same-type NPCs that can be seen directly.
        /// </summary>
        public bool RequireLineOfSight { get; set; } = false;

        /// <summary>
        /// If true, ignores inactive / immortal weird cases less aggressively.
        /// </summary>
        public bool IgnoreNonChasingFriendlies { get; set; } = false;

        public NpcDirective Evaluate(NpcContext context)
        {
            NPC self = context.Self;
            if (self is null || !self.active)
                return NpcDirective.None;

            Vector2 totalRepulsion = Vector2.Zero;
            int contributors = 0;
            float crowding = 0f;

            float avoidRadiusSq = AvoidanceRadius * AvoidanceRadius;
            float minSep = Math.Max(8f, PreferredSeparation);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (other is null || !other.active || other.whoAmI == self.whoAmI)
                    continue;

                if (other.type != self.type)
                    continue;

                if (IgnoreNonChasingFriendlies && other.friendly)
                    continue;

                Vector2 offset = self.Center - other.Center;
                float distSq = offset.LengthSquared();

                if (distSq <= 0.0001f || distSq > avoidRadiusSq)
                    continue;

                if (RequireLineOfSight && !Collision.CanHitLine(self.position, self.width, self.height, other.position, other.width, other.height))
                    continue;

                float dist = (float)Math.Sqrt(distSq);

                // Normalized "how much should I care" factor:
                // 1 near/inside preferred separation, falling toward 0 at edge of avoidance radius.
                float t;

                if (dist <= minSep)
                    t = 1f;
                else
                    t = 1f - (dist - minSep) / Math.Max(1f, AvoidanceRadius - minSep);

                if (t <= 0f)
                    continue;

                Vector2 away = offset / dist;

                // Square t to make close crowding matter more than distant crowding.
                float weight = t * t;

                totalRepulsion += away * weight;
                crowding += weight;
                contributors++;
            }

            if (contributors == 0 || totalRepulsion.LengthSquared() <= 0.0001f)
                return NpcDirective.None;

            Vector2 desiredVelocity = totalRepulsion;
            if (desiredVelocity != Vector2.Zero)
                desiredVelocity = Vector2.Normalize(desiredVelocity) * MathHelper.Lerp(0.75f, MaxMoveSpeed, MathHelper.Clamp(crowding, 0f, 1f));

            float score = BaseScore + crowding * CrowdingScoreMultiplier;

            return new NpcDirective
            {
                WantsControl = true,
                Score = score,
                DesiredVelocity = desiredVelocity,
                DebugName = nameof(AvoidSameTypeModule),
                DebugInfo = $"SameType={contributors}, Crowd={crowding:0.00}, Vel={desiredVelocity}"
            };
        }
    }

}
