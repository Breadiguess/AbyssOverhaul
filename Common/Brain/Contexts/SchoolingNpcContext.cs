using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Common.Brain.Contexts
{
    internal class SchoolingNpcContext : NpcContext
    {
        public int NeighborCount;
        public NPC nearestNeighbor;
        public float NearestNeighborDistance;

        public Vector2 GroupCenter;
        public Vector2 AverageNeighborVelocity;

        public Vector2 SeparationVector;
        public Vector2 AlignmentVector;
        public Vector2 CohesionVector2;

        public bool HasSchoolTarget;
        public Vector2 SchoolTargetPosition;

        public float NeighborRadius = 180f;
        public float SeparationRadius = 56f;
        public float DesiredSchoolDistanceFromTarget = 160f;

        public override void Update(NPC npc)
        {
            base.Update(npc);
            NeighborCount = 0;
            nearestNeighbor = null;
            NearestNeighborDistance = float.MaxValue;

            GroupCenter = Vector2.Zero;
            AverageNeighborVelocity = Vector2.Zero;
            SeparationVector = Vector2.Zero;
            AlignmentVector = Vector2.Zero;
            CohesionVector2 = Vector2.Zero;

            HasSchoolTarget = false;
            SchoolTargetPosition = Vector2.Zero;
        }
    }
}
