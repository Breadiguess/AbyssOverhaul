using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.SharedSensors
{
    public sealed class CreatureVitalsSensor<TContext> : INpcSensor<TContext>
     where TContext : NpcContext, ICreatureVitals, IThreatAware, IDisturbanceAware
    {
        public float CuriosityRecovery = 0.0025f;
        public float FearRecovery = 0.005f;
        public float ThreatFearGain = 0.03f;
        public float ThreatFearScale = 0.04f;
        public float ThreatCuriosityLoss = 0.02f;
        public float DisturbanceFearGain = 0.01f;
        public float DisturbanceCuriosityGain = 0.01f;

        public void Update(TContext context)
        {
            NPC self = context.Self;
            if (self is null || !self.active)
                return;

            context.Curiosity = MathHelper.Clamp(context.Curiosity + CuriosityRecovery, 0f, 1f);
            context.Fear = MathHelper.Clamp(context.Fear - FearRecovery, 0f, 1f);

            if (context.HasThreat)
            {
                context.Fear = MathHelper.Clamp(context.Fear + ThreatFearGain + context.ThreatLevel * ThreatFearScale, 0f, 1f);
                context.Curiosity = MathHelper.Clamp(context.Curiosity - ThreatCuriosityLoss, 0f, 1f);
            }
            else if (context.HasDisturbance)
            {
                context.Fear = MathHelper.Clamp(context.Fear + DisturbanceFearGain, 0f, 1f);
                context.Curiosity = MathHelper.Clamp(context.Curiosity + DisturbanceCuriosityGain, 0f, 1f);
            }

            float speed = self.velocity.Length();
            context.Fatigue = MathHelper.Clamp(context.Fatigue + speed * 0.0015f - 0.002f, 0f, 1f);
            context.Energy = MathHelper.Clamp(context.Energy - context.Fatigue * 0.002f + 0.0015f, 0f, 1f);

            if (speed < 0.5f)
                context.StuckTime++;
            else
                context.StuckTime = 0;

            context.WanderTimer++;
        }
    }
}
