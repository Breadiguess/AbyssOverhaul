using AbyssOverhaul.Core.Ecosystem.FoodSystem;
using AbyssOverhaul.Core.Ecosystem.Simulation;
using AbyssOverhaul.Core.Ecosystem.Simulation.AbyssOverhaul.Core.Ecosystem.Persistence;
using System.Reflection;

namespace AbyssOverhaul.Core.Ecosystem.Ecology
{
    public sealed class EcologyGlobalNPC : GlobalNPC
    {

        public override void Load()
        {
            On_NPC.EncourageDespawn += HibernateEcologyNPC;
        }

        private void HibernateEcologyNPC(On_NPC.orig_EncourageDespawn orig, NPC self, int despawnTime)
        {
            if (self.Ecology() is not null)
            {
                EcologySystem.Instance.HibernateNpc(self);
            }

            orig(self, despawnTime);
        }

        public override bool InstancePerEntity => true;

        public bool displayDebugInfo;

        public MetabolismState Metabolism;

        // Species baseline for this npc's type.
        public SpeciesEcologyDefinition SpeciesDefinition;

        // Persistent actor link.
        public long ActorID = -1;
        public bool SpawnedFromActor;

        // Per-instance state.
        public NpcTraitFlags IndividualTraitOverrides;
        public int SchoolLeader = -1;

        public int MaxHungerSpecies;
        public int HungerModifier;
        public int MaxHunger => MaxHungerSpecies + HungerModifier;

        public int Hunger;

        public float Aggression;
        public float Fear;
        public float Curiosity;
        public float PreferredDepth;
        public float PreferredSpacing = 64f;

        public FoodConsumerType FoodConsumer;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return EcologyRegistry.HasParticipant(entity.type);
        }

        public override bool PreHoverInteract(NPC npc, bool mouseIntersects)
        {
            displayDebugInfo = mouseIntersects;
            return base.PreHoverInteract(npc, mouseIntersects);
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {

            SpeciesDefinition = EcologyRegistry.GetSpecies(npc.type);
            if (SpeciesDefinition is null)
                return;





            if (SpawnedFromActor &&
                ActorID >= 0 &&
                EcologySystem.Instance is not null &&
                EcologySystem.Instance.Actors.TryGetValue(ActorID, out EcologyActor actor))
            {
                EcologyActorBridge.CopyActorToNpc(actor, npc, this);

                actor.IsLoaded = true;
                actor.LoadedNpcWhoAmI = npc.whoAmI;
                actor.LastKnownWorldPosition = npc.Center;
                actor.CellCoord = EcologyMath.WorldToCell(npc.Center);
                actor.LastSimulatedTime = Main.GameUpdateCount;

                return;
            }

            InitializeFromSpecies(npc);

            if (ActorID < 0 && EcologySystem.Instance is not null)
                ActorID = EcologySystem.Instance.RegisterFreshLoadedNpc(npc, this);

        }

        public override void OnKill(NPC npc)
        {

            if (
                EcologySystem.Instance is not null &&
                EcologySystem.Instance.Actors.TryGetValue(ActorID, out EcologyActor actor))
            {
                actor.IsLoaded = false;
                actor.Alive = false;
                EcologyActorBridge.CopyNpcToActor(npc, this, actor);
            }

        }

        private void InitializeFromSpecies(NPC npc)
        {
            MaxHungerSpecies = SpeciesDefinition.BaseMaxHunger;
            Aggression = SpeciesDefinition.BaseAggression;
            Fear = SpeciesDefinition.BaseFear;
            Curiosity = SpeciesDefinition.BaseCuriosity;
            PreferredDepth = SpeciesDefinition.BasePreferredDepth;
            PreferredSpacing = SpeciesDefinition.BasePreferredSpacing;
            FoodConsumer = SpeciesDefinition.FoodConsumer;

            Metabolism = new MetabolismState();
            Metabolism.InitializeFrom(SpeciesDefinition.Metabolism);

            EcologyRegistry.ApplyIndividualSetup(npc, this);
        }

        public override void AI(NPC npc)
        {
            if (SpeciesDefinition is null)
                return;

            Metabolism ??= new MetabolismState();

            if (Main.GameUpdateCount % 65 <= 1)
                UpdateMetabolism(npc, 0.25f);

            ApplyMetabolicEffectsToEcology(npc);

            if (ActorID >= 0 &&
                EcologySystem.Instance is not null &&
                EcologySystem.Instance.Actors.TryGetValue(ActorID, out EcologyActor actor))
            {

                //if(!actor.CellCoord.Equals(EcologyMath.WorldToCell(npc.Center)))

                Main.NewText(actor.CellCoord + ", " + EcologyMath.WorldToCell(npc.Center));

                EcologySystem.Instance.MoveActorToCell(ref actor, EcologyMath.WorldToCell(npc.Center));

                if (Main.GameUpdateCount%10 <= 0.4f)
                {
                    EcologyActorBridge.CopyNpcToActor(npc, this, actor);
                }

            }
        }

        private void UpdateMetabolism(NPC npc, float dt)
        {
            MetabolismDefinition def = SpeciesDefinition.Metabolism;

            float digested = MathF.Min(Metabolism.StomachContent, def.DigestiveRate * dt);
            Metabolism.StomachContent -= digested;
            Metabolism.Energy += digested;

            float movementCost = npc.velocity.Length() * 0.015f * def.ActivityCost * dt;
            float basalCost = def.BasalMetabolicRate * dt;
            float totalCost = basalCost + movementCost;

            Metabolism.Energy -= totalCost;

            if (Metabolism.Energy < def.MaxEnergy * 0.25f && Metabolism.BodyCondition > 0f)
            {
                float reserveBurn = MathF.Min(Metabolism.BodyCondition, def.ReserveConversionRate * dt);
                Metabolism.BodyCondition -= reserveBurn;
                Metabolism.Energy += reserveBurn;
            }

            if (npc.justHit)
                Metabolism.Fatigue += 4f;

            if (npc.velocity.Length() > 3f)
                Metabolism.Fatigue += 1.5f * dt;
            else
                Metabolism.Fatigue -= 1.0f * dt;

            Metabolism.Energy = MathHelper.Clamp(Metabolism.Energy, 0f, def.MaxEnergy);
            Metabolism.StomachContent = MathHelper.Clamp(Metabolism.StomachContent, 0f, def.MaxStomachContent);
            Metabolism.BodyCondition = MathHelper.Clamp(Metabolism.BodyCondition, 0f, def.MaxBodyCondition);
            Metabolism.Fatigue = MathHelper.Clamp(Metabolism.Fatigue, 0f, def.MaxFatigue);

            float energyRatio = Metabolism.Energy / def.MaxEnergy;
            float reserveRatio = Metabolism.BodyCondition / def.MaxBodyCondition;

            Metabolism.Hunger = (1f - (energyRatio * 0.75f + reserveRatio * 0.25f)) * MaxHungerSpecies;
            Metabolism.Hunger = MathHelper.Clamp(Metabolism.Hunger, 0f, MaxHungerSpecies);

            Hunger = (int)Metabolism.Hunger;
        }

        private void ApplyMetabolicEffectsToEcology(NPC npc)
        {
            float hungerRatio = MaxHungerSpecies <= 0 ? 0f : Metabolism.Hunger / MaxHungerSpecies;
            float fatigueRatio = SpeciesDefinition.Metabolism.MaxFatigue <= 0f ? 0f : Metabolism.Fatigue / SpeciesDefinition.Metabolism.MaxFatigue;
            float energyRatio = SpeciesDefinition.Metabolism.MaxEnergy <= 0f ? 1f : Metabolism.Energy / SpeciesDefinition.Metabolism.MaxEnergy;

            Aggression = SpeciesDefinition.BaseAggression;
            Fear = SpeciesDefinition.BaseFear;
            Curiosity = SpeciesDefinition.BaseCuriosity;
            PreferredSpacing = SpeciesDefinition.BasePreferredSpacing;

            if (HasTrait(NpcTraitFlags.Predator))
                Aggression += hungerRatio * 0.45f;

            if (HasTrait(NpcTraitFlags.Prey))
                Fear += hungerRatio * 0.2f;

            Fear += fatigueRatio * 0.3f;
            Curiosity -= fatigueRatio * 0.35f;

            if (HasTrait(NpcTraitFlags.Schooling) && hungerRatio < 0.7f)
                PreferredSpacing *= 0.85f;
            else if (HasTrait(NpcTraitFlags.Schooling))
                PreferredSpacing *= 1.15f;

            if (energyRatio < 0.2f)
            {
                Aggression *= 0.75f;
                Curiosity *= 0.5f;
            }
        }

        public bool HasTrait(NpcTraitFlags flag)
        {
            if (SpeciesDefinition is null)
                return false;

            NpcTraitFlags combined = SpeciesDefinition.Traits | IndividualTraitOverrides;
            return (combined & flag) != 0;
        }

        public void AddIndividualTrait(NpcTraitFlags flag) => IndividualTraitOverrides |= flag;
        public void RemoveIndividualTrait(NpcTraitFlags flag) => IndividualTraitOverrides &= ~flag;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!displayDebugInfo)
                return base.PreDraw(npc, spriteBatch, screenPos, drawColor);

            Vector2 drawPos = npc.Center - screenPos;
            string msg = BuildDebugText(npc);

            Utils.DrawBorderString(spriteBatch, msg, drawPos, Color.White, 1f);
            displayDebugInfo = false;
            return base.PreDraw(npc, spriteBatch, screenPos, drawColor);
        }

        private static string BuildContextDebugText(object context)
        {
            StringBuilder sb = new();
            Type type = context.GetType();

            sb.AppendLine(type.Name);

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(context);
                sb.AppendLine($"{field.Name}: {FormatDebugValue(value)}");
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                object value;
                try
                {
                    value = prop.GetValue(context);
                }
                catch
                {
                    continue;
                }

                sb.AppendLine($"{prop.Name}: {FormatDebugValue(value)}");
            }

            return sb.ToString();
        }

        private static string FormatDebugValue(object value)
        {
            if (value is null)
                return "null";

            switch (value)
            {
                case float f:
                    return f.ToString("0.00");

                case double d:
                    return d.ToString("0.00");

                case Vector2 v:
                    return $"({v.X:0.0}, {v.Y:0.0})";

                case Entity e:
                    return $"{e.GetType().Name}#{e.whoAmI}";

                case Enum:
                    return value.ToString();

                default:
                    return value.ToString();
            }
        }

        private string BuildDebugText(NPC npc)
        {
            StringBuilder sb = new();

            sb.AppendLine($"NPC: {npc.GivenOrTypeName}");
            sb.AppendLine($"Type: {npc.type}");
            sb.AppendLine($"ActorID: {ActorID}");
            sb.AppendLine($"Loaded: true");

            if (SpeciesDefinition is not null)
            {
                sb.AppendLine($"SpeciesTraits: {SpeciesDefinition.Traits}");
                sb.AppendLine($"FoodConsumer: {FoodConsumer}");
            }

            sb.AppendLine($"Hunger: {Hunger}/{MaxHunger}");
            sb.AppendLine($"Aggression: {Aggression:0.00}");
            sb.AppendLine($"Fear: {Fear:0.00}");
            sb.AppendLine($"Curiosity: {Curiosity:0.00}");
            sb.AppendLine($"PreferredDepth: {PreferredDepth:0.00}");
            sb.AppendLine($"PreferredSpacing: {PreferredSpacing:0.00}");

            if (Metabolism is not null)
            {
                sb.AppendLine($"Energy: {Metabolism.Energy:0.00}");
                sb.AppendLine($"Stomach: {Metabolism.StomachContent:0.00}");
                sb.AppendLine($"Condition: {Metabolism.BodyCondition:0.00}");
                sb.AppendLine($"Fatigue: {Metabolism.Fatigue:0.00}");
            }

            return sb.ToString();
        }
    }

    public static class EcologyExtensions
    {
        public static EcologyGlobalNPC Ecology(this NPC npc) =>
            npc.GetGlobalNPC<EcologyGlobalNPC>();
    }
}