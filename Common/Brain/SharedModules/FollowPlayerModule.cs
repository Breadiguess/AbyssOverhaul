using AbyssOverhaul.Common.Brain.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class FollowPlayerModule : INpcModule<NpcContext>
    {
        public float FollowRange = 300f;
        public float MoveSpeed = 2.5f;
        public float Score = 10;
        public NpcDirective Evaluate(NpcContext context)
        {
            if (context.ClosestPlayer == null || !context.ClosestPlayer.active)
                return NpcDirective.None;

            Vector2 toPlayer = context.ClosestPlayer.Center - context.Self.Center;
            float dist = toPlayer.Length();

            if (dist > FollowRange)
                return NpcDirective.None;

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score,
                DesiredVelocity = toPlayer.SafeNormalize(Vector2.Zero) * MoveSpeed,
                DebugName = "FollowPlayer"
            };
        }
    }
}
