using AbyssOverhaul.Common.Brain.Interfaces;

namespace AbyssOverhaul.Common.Brain.Contexts
{
    public class PreyNpcContext : CreatureNpcContext
    {
        public float Panic;
        public float Vigilance = 0.4f;
        public float Boldness = 0.5f;


        public bool HasPredatorThreat;
        public NPC PredatorNpc;
        public Vector2 PredatorPosition;
        public int TimeSincePredatorSeen;
        public Vector2 SafeDirection;
        public Vector2 DesiredEscapePoint;
    }
}
