using AbyssOverhaul.Core.Ecosystem.Ecology;
using Terraria.Utilities;
namespace AbyssOverhaul.Core.Utilities
{
    public static partial class AbyssUtilities
    {

        public static NPC FindClosestAbyssPredator(this NPC npc, out float distanceToClosestPredator)
        {
            NPC closestPredator = null;
            float closestDistSq = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];

                if (!other.active || other.whoAmI == npc.whoAmI)
                    continue;

                if (!EcologyRegistry.HasParticipant(other.type))
                    continue;

                var eco = other.Ecology();
                if (!eco.HasTrait(NpcTraitFlags.Predator))
                    continue;

                float extraDistance = (other.width * 0.5f) + (other.height * 0.5f);
                float allowedDistSqPadding = extraDistance * extraDistance;

                Vector2 diff = other.Center - npc.Center;
                float distSq = diff.LengthSquared();

                if (distSq >= closestDistSq + allowedDistSqPadding)
                    continue;

                if (!Collision.CanHit(npc.Center, 1, 1, other.Center, 1, 1))
                    continue;

                closestDistSq = distSq;
                closestPredator = other;
            }

            distanceToClosestPredator = closestPredator is null ? float.MaxValue : MathF.Sqrt(closestDistSq);
            return closestPredator;
        }

        public static void TargetClosestAbyssPredator(NPC searcher, bool passiveToPlayers, float preySearchDistance, float playerSearchDistance)
        {
            bool playerSearchFilter(Player p)
            {
                return !passiveToPlayers && p.WithinRange(searcher.Center, playerSearchDistance);
            }
            bool npcSearchFilter(NPC n)
            {
                return n.Ecology().HasTrait(NpcTraitFlags.Predator) && n.WithinRange(searcher.Center, preySearchDistance);
            }

            NPCUtils.TargetSearchResults searchResults = NPCUtils.SearchForTarget(searcher, NPCUtils.TargetSearchFlag.All, playerSearchFilter, npcSearchFilter);
            if (searchResults.FoundTarget)
            {
                NPCUtils.TargetType value = searchResults.NearestTargetType;
                if (searchResults.FoundTank && !searchResults.NearestTankOwner.dead && !passiveToPlayers)
                    value = NPCUtils.TargetType.Player;

                searcher.target = searchResults.NearestTargetIndex;
                searcher.targetRect = searchResults.NearestTargetHitbox;
            }
        }

        public static void SpawnSchoolOfFish(NPC npc, int MinSchoolSize, int MaxSchoolSize)
        {
            // Larger schools are made rarer by this exponent by effectively "squashing" randomness.
            float fishInterpolant = MathF.Pow(Main.rand.NextFloat(), 4f);
            int fishCount = (int)float.Lerp(MinSchoolSize, MaxSchoolSize, fishInterpolant);

            for (int i = 0; i < fishCount; i++)
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, npc.type, npc.whoAmI, 1f);

            npc.ai[0] = 1f;
            npc.netUpdate = true;
        }

        public static void TurnAroundBehavior(NPC npc, Vector2 ahead, bool aboutToLeaveWorld)
        {
            float distanceToTileOnLeft = BreadLibrary.Core.Utilities.Utilities.DistanceToTileCollisionHit(npc.Center, npc.velocity.RotatedBy(-MathHelper.PiOver2)) ?? 999f;
            float distanceToTileOnRight = BreadLibrary.Core.Utilities.Utilities.DistanceToTileCollisionHit(npc.Center, npc.velocity.RotatedBy(MathHelper.PiOver2)) ?? 999f;
            float turnDirection = distanceToTileOnLeft > distanceToTileOnRight ? -1f : 1f;
            Vector2 idealVelocity = npc.velocity.RotatedBy(MathHelper.PiOver2 * turnDirection);
            if (aboutToLeaveWorld)
                idealVelocity = ahead.X >= Main.maxTilesX * 16f - 700f ? -Vector2.UnitX * 4f : Vector2.UnitX * 4f;

            npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.15f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f);
        }

        
    }
}

