using AbyssOverhaul.Common.Brain.Interfaces;

namespace AbyssOverhaul.Common.Brain.Contexts
{
    public class PreyNpcContext : CreatureNpcContext, ISchoolingAware
    {
        public float Panic;
        public float Vigilance = 0.4f;
        public float Boldness = 0.5f;

        public bool WantsSchool { get; set; } = true;
        public Vector2 SchoolCenter { get; set; }
        public int SchoolmateCount { get; set; }
        public Vector2 SeparationForce { get; set; }
        public Vector2 AlignmentForce { get; set; }
        public Vector2 CohesionForce { get; set; }

        public bool HasPredatorThreat;
        public NPC PredatorNpc;
        public Vector2 PredatorPosition;
        public int TimeSincePredatorSeen;
        public Vector2 SafeDirection;
        public Vector2 DesiredEscapePoint;
    }
}
