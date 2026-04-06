using AbyssOverhaul.Common;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.BehaviorOverrides.ViperfishOverride
{
    public enum ViperfishPackState
    {
        Idle,
        Stalking,
        Signaling,
        Assaulting,
        Scattering
    }

    public sealed class ViperfishPack
    {
        public int Id;
        public HashSet<int> Members = new();
        public Dictionary<int, int> MemberSlots = new();
        public HashSet<int> ActiveAttackers = new();

        public Vector2 Center;
        public Vector2 AverageVelocity;
        public Vector2 Forward = Vector2.UnitX;
        public Vector2 FormationAnchor;

        public int LeaderWhoAmI = -1;
        public int TargetPlayer = -1;

        public ViperfishPackState State = ViperfishPackState.Idle;
        public int StateTimer;
        public int AttackWaveTimer;

        public bool IsEmpty => Members.Count <= 0;

        private static readonly Vector2[][] SlotLayouts =
        {
            Array.Empty<Vector2>(),

            new[]
            {
                new Vector2(0f, 0f)
            },

            new[]
            {
                new Vector2(-18f, 0f),
                new Vector2(18f, -8f)
            },

            new[]
            {
                new Vector2(0f, 28f),
                new Vector2(-26f, -4f),
                new Vector2(26f, -4f)
            },

            new[]
            {
                new Vector2(0f, 36f),
                new Vector2(-30f, 6f),
                new Vector2(30f, 6f),
                new Vector2(0f, -26f)
            },

            new[]
            {
                new Vector2(0f, 42f),
                new Vector2(-34f, 12f),
                new Vector2(34f, 12f),
                new Vector2(-18f, -24f),
                new Vector2(18f, -24f)
            }
        };

        public void PruneInvalidMembers()
        {
            List<int> remove = new();

            foreach (int whoAmI in Members)
            {
                if (whoAmI < 0 || whoAmI >= Main.maxNPCs)
                {
                    remove.Add(whoAmI);
                    continue;
                }

                NPC npc = Main.npc[whoAmI];
                if (!npc.active || npc.type != ModContent.NPCType<CalamityMod.NPCs.Abyss.Viperfish>())
                    remove.Add(whoAmI);
            }

            foreach (int whoAmI in remove)
            {
                Members.Remove(whoAmI);
                MemberSlots.Remove(whoAmI);
                ActiveAttackers.Remove(whoAmI);
            }
        }

        public void RecalculateCachedState()
        {
            Vector2 centerSum = Vector2.Zero;
            Vector2 velocitySum = Vector2.Zero;
            int validCount = 0;

            int bestLeader = -1;
            float bestLeaderScore = float.MinValue;

            foreach (int whoAmI in Members)
            {
                NPC npc = Main.npc[whoAmI];
                if (!npc.active || npc.type != ModContent.NPCType<CalamityMod.NPCs.Abyss.Viperfish>())
                    continue;

                centerSum += npc.Center;
                velocitySum += npc.velocity;
                validCount++;

                float score = npc.life + npc.lifeMax * 0.25f;
                if (score > bestLeaderScore)
                {
                    bestLeaderScore = score;
                    bestLeader = whoAmI;
                }
            }

            if (validCount > 0)
            {
                Center = centerSum / validCount;
                AverageVelocity = velocitySum / validCount;
            }

            LeaderWhoAmI = bestLeader;

            Vector2 desiredForward = Forward;

            if (AverageVelocity.LengthSquared() > 1f)
                desiredForward = Vector2.Normalize(AverageVelocity);
            else if (TargetPlayer >= 0 && Main.player[TargetPlayer].active && !Main.player[TargetPlayer].dead)
                desiredForward = (Main.player[TargetPlayer].Center - Center).SafeNormalize(Forward);

            if (desiredForward.LengthSquared() > 0.001f)
                Forward = Vector2.Normalize(Vector2.Lerp(Forward, desiredForward, 0.15f));

            AssignSlots();
        }

        public void AssignSlots()
        {
            List<int> validMembers = Members
                .Where(i => i >= 0 && i < Main.maxNPCs && Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<CalamityMod.NPCs.Abyss.Viperfish>())
                .ToList();

            validMembers.Sort((a, b) =>
            {
                float da = Vector2.DistanceSquared(Main.npc[a].Center, Center);
                float db = Vector2.DistanceSquared(Main.npc[b].Center, Center);
                return da.CompareTo(db);
            });

            int count = Math.Min(validMembers.Count, 5);
            Vector2[] layout = SlotLayouts[count];

            MemberSlots.Clear();

            for (int i = 0; i < count; i++)
                MemberSlots[validMembers[i]] = i;

            for (int i = count; i < validMembers.Count; i++)
                MemberSlots[validMembers[i]] = count - 1;
        }

        public Vector2 GetSlotOffset(int whoAmI)
        {
            int memberCount = Math.Min(Members.Count, 5);
            if (memberCount <= 0)
                return Vector2.Zero;

            if (!MemberSlots.TryGetValue(whoAmI, out int slotIndex))
                return Vector2.Zero;

            Vector2[] layout = SlotLayouts[memberCount];
            slotIndex = Utils.Clamp(slotIndex, 0, layout.Length - 1);
            return layout[slotIndex];
        }

        public void ChooseAttackers()
        {
            ActiveAttackers.Clear();

            List<int> candidates = Members
                .Where(i => i >= 0 && i < Main.maxNPCs && Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<CalamityMod.NPCs.Abyss.Viperfish>())
                .ToList();

            if (candidates.Count <= 0)
                return;

            candidates.Sort((a, b) =>
            {
                float da = Vector2.DistanceSquared(Main.npc[a].Center, FormationAnchor);
                float db = Vector2.DistanceSquared(Main.npc[b].Center, FormationAnchor);
                return da.CompareTo(db);
            });

            int attackers = candidates.Count >= 4 ? 2 : 1;

            for (int i = 0; i < attackers && i < candidates.Count; i++)
                ActiveAttackers.Add(candidates[i]);
        }
    }

    public class PackManagerSystem : ModSystem
    {
        public static PackManagerSystem Instance;

        private readonly Dictionary<int, ViperfishPack> _packs = new();
        private int _nextPackId = 1;

        public override void OnWorldLoad()
        {
            Instance = this;
            _packs.Clear();
            _nextPackId = 1;
        }

        public override void OnWorldUnload()
        {
            Instance = null;
            _packs.Clear();
        }

        public override void PreUpdateEntities()
        {
            if (Main.GameUpdateCount % 12 == 0)
                UpdatePacks();
        }

        public int CreatePack()
        {
            int id = _nextPackId++;
            _packs[id] = new ViperfishPack
            {
                Id = id
            };
            return id;
        }

        public ViperfishPack GetPack(int packId)
        {
            if (packId <= 0)
                return null;

            _packs.TryGetValue(packId, out ViperfishPack pack);
            return pack;
        }

        public void RegisterMember(int packId, int npcWhoAmI)
        {
            if (packId <= 0)
                return;

            if (_packs.TryGetValue(packId, out ViperfishPack pack))
                pack.Members.Add(npcWhoAmI);
        }

        public void RemoveMember(int packId, int npcWhoAmI)
        {
            if (packId <= 0)
                return;

            if (!_packs.TryGetValue(packId, out ViperfishPack pack))
                return;

            pack.Members.Remove(npcWhoAmI);
            pack.MemberSlots.Remove(npcWhoAmI);
            pack.ActiveAttackers.Remove(npcWhoAmI);

            if (pack.IsEmpty)
                _packs.Remove(packId);
        }

        public int GetOrCreateNearbyPack(Vector2 position, float joinDistance)
        {
            float bestDistSq = joinDistance * joinDistance;
            int bestPackId = -1;

            foreach (var pair in _packs)
            {
                ViperfishPack pack = pair.Value;
                if (pack.IsEmpty)
                    continue;

                float distSq = Vector2.DistanceSquared(position, pack.Center);
                if (distSq < bestDistSq && pack.Members.Count < 5)
                {
                    bestDistSq = distSq;
                    bestPackId = pair.Key;
                }
            }

            if (bestPackId != -1)
                return bestPackId;

            return CreatePack();
        }

        private void UpdatePacks()
        {
            List<int> emptyPacks = new();

            foreach (var pair in _packs)
            {
                ViperfishPack pack = pair.Value;
                pack.PruneInvalidMembers();

                if (pack.IsEmpty)
                {
                    emptyPacks.Add(pair.Key);
                    continue;
                }

                UpdatePackTarget(pack);
                pack.RecalculateCachedState();
                UpdatePackState(pack);
                UpdatePackFormationAnchor(pack);
                UpdatePackAttackers(pack);
            }

            foreach (int id in emptyPacks)
                _packs.Remove(id);
        }

        private void UpdatePackTarget(ViperfishPack pack)
        {
            const float detectionRange = 900f;
            float bestDistSq = detectionRange * detectionRange;
            int bestPlayer = -1;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;

                float distSq = Vector2.DistanceSquared(pack.Center, player.Center);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestPlayer = i;
                }
            }

            pack.TargetPlayer = bestPlayer;
        }

        private void UpdatePackState(ViperfishPack pack)
        {
            pack.StateTimer++;

            if (pack.TargetPlayer < 0)
            {
                pack.State = ViperfishPackState.Idle;
                pack.StateTimer = 0;
                pack.AttackWaveTimer = 0;
                pack.ActiveAttackers.Clear();
                return;
            }

            Player player = Main.player[pack.TargetPlayer];
            if (!player.active || player.dead)
            {
                pack.State = ViperfishPackState.Idle;
                pack.StateTimer = 0;
                pack.AttackWaveTimer = 0;
                pack.ActiveAttackers.Clear();
                pack.TargetPlayer = -1;
                return;
            }

            float dist = Vector2.Distance(pack.Center, player.Center);

            switch (pack.State)
            {
                case ViperfishPackState.Idle:
                    pack.State = ViperfishPackState.Stalking;
                    pack.StateTimer = 0;
                    break;

                case ViperfishPackState.Stalking:
                    if (dist < 220f && pack.StateTimer > 90)
                    {
                        pack.State = ViperfishPackState.Signaling;
                        pack.StateTimer = 0;
                    }
                    break;

                case ViperfishPackState.Signaling:
                    if (pack.StateTimer > 45)
                    {
                        pack.State = ViperfishPackState.Assaulting;
                        pack.StateTimer = 0;
                        pack.AttackWaveTimer = 0;
                        pack.ChooseAttackers();
                    }
                    break;

                case ViperfishPackState.Assaulting:
                    if (dist > 1000f)
                    {
                        pack.State = ViperfishPackState.Scattering;
                        pack.StateTimer = 0;
                        pack.ActiveAttackers.Clear();
                    }
                    else if (pack.StateTimer > 180)
                    {
                        pack.State = ViperfishPackState.Stalking;
                        pack.StateTimer = 0;
                        pack.AttackWaveTimer = 0;
                        pack.ActiveAttackers.Clear();
                    }
                    break;

                case ViperfishPackState.Scattering:
                    if (pack.StateTimer > 60)
                    {
                        pack.State = ViperfishPackState.Stalking;
                        pack.StateTimer = 0;
                    }
                    break;
            }
        }

        private void UpdatePackFormationAnchor(ViperfishPack pack)
        {
            if (pack.TargetPlayer < 0)
            {
                pack.FormationAnchor = pack.Center + pack.Forward * 30f;
                return;
            }

            Player player = Main.player[pack.TargetPlayer];
            Vector2 toPlayer = (player.Center - pack.Center).SafeNormalize(pack.Forward);
            Vector2 side = new Vector2(-toPlayer.Y, toPlayer.X);

            float sideSign = (pack.Id % 2 == 0) ? 1f : -1f;

            switch (pack.State)
            {
                default:
                case ViperfishPackState.Stalking:
                    pack.FormationAnchor = player.Center - toPlayer * 170f + side * 70f * sideSign;
                    break;

                case ViperfishPackState.Signaling:
                    pack.FormationAnchor = player.Center - toPlayer * 120f + side * 35f * sideSign;
                    break;

                case ViperfishPackState.Assaulting:
                    pack.FormationAnchor = player.Center - toPlayer * 60f;
                    break;

                case ViperfishPackState.Scattering:
                    pack.FormationAnchor = pack.Center - toPlayer * 140f;
                    break;

                case ViperfishPackState.Idle:
                    pack.FormationAnchor = pack.Center + pack.Forward * 20f;
                    break;
            }
        }

        private void UpdatePackAttackers(ViperfishPack pack)
        {
            if (pack.State != ViperfishPackState.Assaulting)
            {
                pack.ActiveAttackers.Clear();
                return;
            }

            pack.AttackWaveTimer++;

            if (pack.AttackWaveTimer == 1 || pack.AttackWaveTimer >= 36)
            {
                pack.AttackWaveTimer = 0;
                pack.ChooseAttackers();
            }
        }
    }



    internal class StalkingViperfish : NPCBehaviorOverride, IEcologyParticipant
    {
        public void SetSpeciesEcology(SpeciesEcologyDefinition definition)
        {
            definition.AddTraits(NpcTraitFlags.Pack, NpcTraitFlags.AmbushPredator);
            definition.FoodConsumer = FoodConsumerType.Carnivore;
        }

        public void SetupIndividualEcology(NPC npc, EcologyGlobalNPC ecology)
        {

        }
        public override string TexturePath => this.GetPath();
        public override int NPCType => ModContent.NPCType<CalamityMod.NPCs.Abyss.Viperfish>();

        public bool Registered { get; private set; }
        public float LocalAttackTimer { get; private set; }
        public int LocalTimer { get; private set; }
        public int PackId { get; private set; }

        public override void SetDefaults(NPC NPC)
        {
            
        }

        public override bool OverrideAI(NPC NPC)
        {
            EnsurePackRegistration(NPC);

            ViperfishPack pack = PackManagerSystem.Instance?.GetPack(PackId);
            if (pack is null)
            {
                NPC.velocity *= 0.96f;
                return false;
            }

            if (pack.State == ViperfishPackState.Assaulting && pack.ActiveAttackers.Contains(NPC.whoAmI))
                DoAttackerBehavior(NPC, pack);
            else
                DoFormationBehavior(NPC, pack);

            NPC.rotation = NPC.velocity.ToRotation();
            NPC.spriteDirection = NPC.direction;


            return true;
        }

        private void EnsurePackRegistration(NPC NPC)
        {
            if (Registered || PackManagerSystem.Instance is null)
                return;

            if (PackId <= 0)
                PackId = PackManagerSystem.Instance.GetOrCreateNearbyPack(NPC.Center, 240f);

            PackManagerSystem.Instance.RegisterMember(PackId, NPC.whoAmI);
            Registered = true;
            NPC.netUpdate = true;
        }

        private void DoFormationBehavior(NPC NPC,ViperfishPack pack)
        {
            Vector2 slotLocal = pack.GetSlotOffset(NPC.whoAmI);

            Vector2 forward = pack.Forward.LengthSquared() > 0.001f ? pack.Forward : Vector2.UnitX;
            Vector2 right = new Vector2(-forward.Y, forward.X);

            Vector2 slotWorldPos = pack.FormationAnchor + right * slotLocal.X + forward * slotLocal.Y;

            Vector2 toSlot = slotWorldPos - NPC.Center;
            Vector2 slotForce = GetSeekForce(toSlot, 90f);
            Vector2 separationForce = GetSeparationForce(NPC, pack, 42f) * 1.8f;
            Vector2 alignmentForce = forward * 0.8f;
            Vector2 cohesionForce = (pack.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 0.35f;
            Vector2 wanderForce = GetWanderForce(NPC) * 0.18f;

            if (pack.State == ViperfishPackState.Signaling)
            {
                slotForce *= 1.35f;
                wanderForce *= 0.5f;
            }
            else if (pack.State == ViperfishPackState.Scattering)
            {
                slotForce *= 0.4f;
                alignmentForce *= 0.4f;
                cohesionForce *= 0.2f;
                wanderForce *= 2f;
            }

            Vector2 desiredDir = slotForce + separationForce + alignmentForce + cohesionForce + wanderForce;
            if (desiredDir.LengthSquared() > 0.001f)
                desiredDir.Normalize();
            else
                desiredDir = forward;

            float desiredSpeed = pack.State switch
            {
                ViperfishPackState.Idle => 3f,
                ViperfishPackState.Stalking => 4.2f,
                ViperfishPackState.Signaling => 3.2f,
                ViperfishPackState.Scattering => 6.2f,
                _ => 4.2f
            };

            Vector2 desiredVelocity = desiredDir * desiredSpeed;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.10f);
            NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
        }

        private void DoAttackerBehavior(NPC NPC, ViperfishPack pack)
        {
            if (pack.TargetPlayer < 0)
            {
                DoFormationBehavior(NPC, pack);
                return;
            }

            Player player = Main.player[pack.TargetPlayer];
            if (!player.active || player.dead)
            {
                DoFormationBehavior(NPC, pack);
                return;
            }

            LocalAttackTimer++;

            Vector2 toPlayer = player.Center - NPC.Center;
            Vector2 desiredDir;

            if (LocalAttackTimer < 18f)
            {
                Vector2 side = new Vector2(-pack.Forward.Y, pack.Forward.X);
                float sideSign = (NPC.whoAmI % 2 == 0) ? 1f : -1f;
                Vector2 windupPos = player.Center - toPlayer.SafeNormalize(Vector2.UnitX) * 100f + side * 55f * sideSign;
                desiredDir = (windupPos - NPC.Center).SafeNormalize(Vector2.Zero);
                NPC.velocity = Vector2.Lerp(NPC.velocity, desiredDir * 6f, 0.16f);
            }
            else if (LocalAttackTimer < 34f)
            {
                desiredDir = toPlayer.SafeNormalize(Vector2.Zero);
                NPC.velocity = Vector2.Lerp(NPC.velocity, desiredDir * 12f, 0.22f);
            }
            else
            {
                LocalAttackTimer = 0f;
                DoFormationBehavior(NPC, pack);
                return;
            }

            NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
        }

        private Vector2 GetSeekForce(Vector2 toTarget, float fullStrengthDistance)
        {
            float len = toTarget.Length();
            if (len < 0.001f)
                return Vector2.Zero;

            Vector2 dir = toTarget / len;
            float strength = MathHelper.Clamp(len / fullStrengthDistance, 0f, 1f);
            return dir * strength * 1.25f;
        }

        private Vector2 GetSeparationForce(NPC NPC, ViperfishPack pack, float preferredDistance)
        {
            Vector2 force = Vector2.Zero;
            float preferredDistanceSq = preferredDistance * preferredDistance;

            foreach (int otherWhoAmI in pack.Members)
            {
                if (otherWhoAmI == NPC.whoAmI || otherWhoAmI < 0 || otherWhoAmI >= Main.maxNPCs)
                    continue;

                NPC other = Main.npc[otherWhoAmI];
                if (!other.active || other.type != this.NPCType)
                    continue;

                Vector2 offset = NPC.Center - other.Center;
                float distSq = offset.LengthSquared();
                if (distSq <= 0.001f || distSq > preferredDistanceSq)
                    continue;

                float dist = (float)Math.Sqrt(distSq);
                float t = 1f - dist / preferredDistance;
                force += offset / dist * t;
            }

            return force;
        }

        private Vector2 GetWanderForce(NPC NPC)
        {
            float t = (float)(Main.GameUpdateCount * 0.05f + NPC.whoAmI * 1.371f);
            return new Vector2((float)Math.Cos(t), (float)Math.Sin(t * 0.7f));
        }

        public override void OnKill(NPC NPC)
        {
            if (PackManagerSystem.Instance is not null && PackId > 0)
                PackManagerSystem.Instance.RemoveMember(PackId, NPC.whoAmI);
        }

        public override void OnSpawn(NPC NPC, IEntitySource source)
        {
            Registered = false;
            LocalAttackTimer = 0f;
        }
     

    }
}
