using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedModules
{
    public sealed class ChasePreyModule<TContext> : INpcModule<TContext>
     where TContext : NpcContext, IPreyTargeter, IHungry
    {
        public float Score = 30f;
        public float MoveSpeed = 3.5f;
        public float HungerThreshold = 0.35f;

        public NpcDirective Evaluate(TContext context)
        {
            if (context.Hunger < HungerThreshold || !context.HasPreyTarget)
                return NpcDirective.None;

            Vector2 toPrey = context.PreyTargetPosition - context.Self.Center;
            if (toPrey == Vector2.Zero)
                return NpcDirective.None;

            toPrey.Normalize();

            return new NpcDirective
            {
                WantsControl = true,
                Score = Score + context.Hunger * 15f,
                DesiredVelocity = toPrey * MoveSpeed,
                DebugName = "ChasePrey"
            };
        }
    }
}
