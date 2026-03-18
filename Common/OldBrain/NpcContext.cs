namespace AbyssOverhaul.Common.OldBrain
{
    public sealed class NpcContext
    {
        public NPC Self;

        public Player ClosestPlayer;
        public NPC ClosestThreat;
        public NPC ClosestFood;
        public Vector2 SchoolCenter;
        public List<NPC> NearbyAllies = new();
        public List<NPC> NearbySchoolmates = new();

        public bool IsDaytime;
        public bool InWater;
        public bool HasLineOfSightToFood;
        public bool HasLineOfSightToThreat;

        public Vector2 HomePosition;
        public float LocalCrowding;
        public float LocalDanger;

        public void Sense(NpcMemory memory, NpcNeeds needs)
        {
            NearbyAllies.Clear();
            NearbySchoolmates.Clear();

            ClosestPlayer = null;
            ClosestThreat = null;
            ClosestFood = null;

            float closestThreatDistSq = float.MaxValue;
            float closestFoodDistSq = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.whoAmI == Self.whoAmI)
                    continue;

                float distSq = Vector2.DistanceSquared(Self.Center, npc.Center);

                if (IsAlly(npc))
                {
                    NearbyAllies.Add(npc);

                    if (IsSchoolmate(npc) && distSq < 800f * 800f)
                        NearbySchoolmates.Add(npc);
                }

                if (IsFoodTarget(npc) && distSq < closestFoodDistSq)
                {
                    closestFoodDistSq = distSq;
                    ClosestFood = npc;
                }

                if (IsThreat(npc) && distSq < closestThreatDistSq)
                {
                    closestThreatDistSq = distSq;
                    ClosestThreat = npc;
                }
            }

            SchoolCenter = ComputeSchoolCenter();
            LocalCrowding = NearbySchoolmates.Count;
            LocalDanger = ComputeDanger();
            IsDaytime = Main.dayTime;
            InWater = Self.wet;

            if (ClosestFood != null)
                HasLineOfSightToFood = Collision.CanHitLine(Self.position, Self.width, Self.height, ClosestFood.position, ClosestFood.width, ClosestFood.height);

            if (ClosestThreat != null)
                HasLineOfSightToThreat = Collision.CanHitLine(Self.position, Self.width, Self.height, ClosestThreat.position, ClosestThreat.width, ClosestThreat.height);
        }

        private Vector2 ComputeSchoolCenter()
        {
            if (NearbySchoolmates.Count == 0)
                return Self.Center;

            Vector2 sum = Self.Center;
            foreach (var ally in NearbySchoolmates)
                sum += ally.Center;

            return sum / (NearbySchoolmates.Count + 1);
        }

        private float ComputeDanger()
        {
            if (ClosestThreat is null)
                return 0f;

            float d = Vector2.Distance(Self.Center, ClosestThreat.Center);
            return MathHelper.Clamp(1f - d / 900f, 0f, 1f);
        }

        public bool IsAlly(NPC npc) => npc.type == Self.type;
        public bool IsSchoolmate(NPC npc) => npc.type == Self.type;
        public bool IsThreat(NPC npc) => npc.damage > 0 && !IsAlly(npc);
        public bool IsFoodTarget(NPC npc) => false; // customize
    }
}