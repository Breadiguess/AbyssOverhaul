using AbyssOverhaul.Common.Brain.Interfaces;

namespace AbyssOverhaul.Common.Brain.Contexts
{
    public class CreatureNpcContext : NpcContext, IThreatAware, IDisturbanceAware, ICreatureVitals, IHasPreferredSpacing, IHungry
    {
        public float Energy { get; set; } = 1f;
        public float Fatigue { get; set; }
        public float Fear { get; set; }
        public float Curiosity { get; set; } = 0.25f;
        public float PreferredSpacing { get; set; } = 64f;

        public bool HasThreat { get; set; }
        public Vector2 ThreatPosition { get; set; }
        public float ThreatLevel { get; set; }
        public int TimeSinceThreatSeen { get; set; }

        public bool HasDisturbance { get; set; }
        public Vector2 DisturbancePosition { get; set; }

        public int WanderTimer { get; set; }
        public int StuckTime { get; set; }
        public float Hunger { get; set; }
    }
}
