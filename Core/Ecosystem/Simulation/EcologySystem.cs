using AbyssOverhaul.Core.Ecosystem.FoodSystem;
using AbyssOverhaul.Core.Ecosystem.TerritorySystem;
namespace AbyssOverhaul.Core.Ecosystem.Simulation
{

    namespace AbyssOverhaul.Core.Ecosystem.Persistence
    {
        public enum EcologySimulationLevel : byte
        {
            ActiveLoaded,
            WarmOffscreen,
            ColdAggregate
        }

        public enum EcologyIntent : byte
        {
            Idle,
            Roaming,
            Hunting,
            Fleeing,
            Resting,
            Feeding,
            Scavenging,
            DefendingTerritory,
            Migrating,
            Dead
        }


        public sealed class EcologySystem : ModSystem
        {
            public static EcologySystem Instance;

            public readonly Dictionary<long, EcologyActor   > Actors = new();
            public readonly Dictionary<Point, EcologyCell> Cells = new();
            public readonly Dictionary<int, PersistentTerritory> Territories = new();

            private long _nextActorID = 1;
            private int _nextTerritoryID = 1;

            public override void OnWorldLoad()
            {
                Instance = this;
                Actors.Clear();
                Cells.Clear();
                Territories.Clear();
                _nextActorID = 1;
                _nextTerritoryID = 1;
            }

            public override void OnWorldUnload()
            {
                Instance = null;
                Actors.Clear();
                Cells.Clear();
                Territories.Clear();
            }

            public long AllocateActorID() => _nextActorID++;
            public int AllocateTerritoryID() => _nextTerritoryID++;

            public override void PostUpdateWorld()
            {
                MaterializeNearbyActors();
                UpdateLoadedActors();
                //SimulateOffscreenCells();
            }

            public long RegisterFreshLoadedNpc(NPC npc, EcologyGlobalNPC eco)
            {
                long id = AllocateActorID();

                EcologyActor actor = new()
                {
                    ActorID = id,
                    SpeciesID = npc.type,
                    IsLoaded = true,
                    LoadedNpcWhoAmI = npc.whoAmI,
                    ImportantIndividual = HasImportantTraits(eco),
                    LastSimulatedTime = Main.GameUpdateCount
                };


                EcologyActorBridge.CopyNpcToActor(npc, eco, actor);

                Actors[id] = actor;
                AddActorToCell(actor, actor.CellCoord);
                return id;
            }

            #region ActorManagement
            public IEnumerable<EcologyActor> EnumerateActorsInCell(EcologyCell cell)
            {
                foreach (long actorID in cell.ActorIDs)
                {
                    if (Actors.TryGetValue(actorID, out EcologyActor actor))
                        yield return actor;
                }
            }
            public static string GetActorSummary(EcologyActor actor)
            {
                if (actor is null)
                    return "null";

                return $"Actor {actor.ActorID} | Species {actor.SpeciesID} | Intent {actor.Intent} | Loaded {actor.IsLoaded} | Alive {actor.Alive}";
            }
            public void BindNpcToActor(NPC npc, EcologyGlobalNPC eco, EcologyActor actor)
            {
                actor.IsLoaded = true;
                actor.LoadedNpcWhoAmI = npc.whoAmI;
                actor.LastKnownWorldPosition = npc.Center;

                eco.ActorID = actor.ActorID;
                EcologyActorBridge.CopyActorToNpc(actor, npc, eco);
            }
            public bool TryGetActor(NPC npc, out EcologyActor actor)
            {
                actor = null;

                if (npc is null || !npc.active || !EcologyRegistry.HasParticipant(npc.type))
                    return false;

                EcologyGlobalNPC eco = npc.Ecology();
                if (eco.ActorID < 0)
                    return false;

                return Actors.TryGetValue(eco.ActorID, out actor);
            }

            public void HibernateNpc(NPC npc)
            {
                if (!npc.active || !EcologyRegistry.HasParticipant(npc.type))
                    return;

                EcologyGlobalNPC eco = npc.Ecology();
                if (eco.ActorID < 0 || !Actors.TryGetValue(eco.ActorID, out EcologyActor actor))
                    return;

                EcologyActorBridge.CopyNpcToActor(npc, eco, actor);
                actor.IsLoaded = false;
                actor.LoadedNpcWhoAmI = -1;
                actor.LastKnownWorldPosition = npc.Center;
                actor.CellCoord = EcologyMath.WorldToCell(npc.Center);
                actor.LastSimulatedTime = Main.GameUpdateCount;

                npc.active = false;
            }

            private void UpdateLoadedActors()
            {
                foreach ((long actorID, EcologyActor actor) in Actors)
                {
                    if (!actor.IsLoaded)
                        continue;

                    if (actor.LoadedNpcWhoAmI < 0 || actor.LoadedNpcWhoAmI >= Main.maxNPCs)
                    {
                        actor.IsLoaded = false;
                        actor.LoadedNpcWhoAmI = -1;
                        continue;
                    }

                    NPC npc = Main.npc[actor.LoadedNpcWhoAmI];
                    if (!npc.active || npc.type != actor.SpeciesID)
                    {
                        actor.IsLoaded = false;
                        actor.LoadedNpcWhoAmI = -1;
                        continue;
                    }

                    EcologyGlobalNPC eco = npc.Ecology();
                    EcologyActorBridge.CopyNpcToActor(npc, eco, actor);
                    actor.LastKnownWorldPosition = npc.Center;
                    actor.CellCoord = EcologyMath.WorldToCell(npc.Center);
                    actor.LastSimulatedTime = Main.GameUpdateCount;
                }
            }

            //todo: fix collection was modified error
            private void MaterializeNearbyActors()
            {
                for (int p = 0; p < Main.maxPlayers; p++)
                {
                    Player player = Main.player[p];
                    if (player is null || !player.active || player.dead)
                        continue;

                    Point playerCell = EcologyMath.WorldToCell(player.Center);

                    foreach (Point coord in EcologyMath.GetCellsInRadius(playerCell, 2))
                    {
                        if (!Cells.TryGetValue(coord, out EcologyCell cell))
                            continue;

                        foreach (long actorID in cell.ActorIDs)
                        {
                            if (!Actors.TryGetValue(actorID, out EcologyActor actor))
                                continue;

                            if (!actor.Alive || actor.IsLoaded)
                                continue;

                            if (!ShouldMaterializeActor(actor, player))
                                continue;

                            SpawnNpcFromActor(actor);
                        }
                    }
                }
            }

            private bool ShouldMaterializeActor(EcologyActor actor, Player player)
            {
                
                float distSq = Vector2.DistanceSquared(actor.LastKnownWorldPosition, player.Center);
                return distSq <= 1600f * 1600f && actor.Alive;
            }

            private void SpawnNpcFromActor(EcologyActor actor)
            {
                int x = (int)actor.LastKnownWorldPosition.X;
                int y = (int)actor.LastKnownWorldPosition.Y;

                int npcIndex = NPC.NewNPC(Entity.GetSource_None(), x, y, actor.SpeciesID);
                if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                    return;

                NPC npc = Main.npc[npcIndex];
                if (!npc.active)
                    return;

                EcologyGlobalNPC eco = npc.Ecology();
                eco.ActorID = actor.ActorID;
                eco.SpawnedFromActor = true;


                BindNpcToActor(npc, eco, actor);
            }

            private void AddActorToCell(EcologyActor actor, Point coord)
            {
                EcologyCell cell = GetOrCreateCell(coord);

                if (!cell.ActorIDs.Contains(actor.ActorID))
                    cell.ActorIDs.Add(actor.ActorID);

                actor.CellCoord = coord;
            }
            public void MoveActorToCell(ref EcologyActor actor, Point newCoord)
            {
                Point oldCoord = actor.CellCoord;


                if (oldCoord == newCoord)
                    return;

                if (Cells.TryGetValue(oldCoord, out EcologyCell oldCell))
                {
                    while (oldCell.ActorIDs.Remove(actor.ActorID))
                    {
                        Main.NewText(actor.ActorID);
                    }

                    Cells[oldCoord] = oldCell;
                }

                EcologyCell newCell = GetOrCreateCell(newCoord);

                if (!newCell.ActorIDs.Contains(actor.ActorID))
                    newCell.ActorIDs.Add(actor.ActorID);

                actor.CellCoord = newCoord;
            }
            #endregion

            #region Simulate Actors
            private void SimulateOffscreenCells()
            {
                //throws an error;
                //colection was modified; enumeration operation may not execute.
                foreach (EcologyCell cell in Cells.Values.ToArray())
                {
                    SimulateOffscreenCell(cell);
                }
            }

            private EcologyCell GetOrCreateCell(Point coord)
            {
                if (!Cells.TryGetValue(coord, out EcologyCell cell))
                {
                    cell = new EcologyCell
                    {
                        Coord = coord,
                        WorldBounds = EcologyMath.CellToWorldBounds(coord),
                        PlantFood = 50f,
                        MeatFood = 0f,
                        Carrion = 0f,
                        Shelter = 0.5f,
                        Threat = 0f,
                        HabitatQuality = 1f,
                        LastSimulatedTime = Main.GameUpdateCount
                    };

                    Cells[coord] = cell;
                }

                return cell;
            }

            private void SimulateOffscreenCell(EcologyCell cell)
            {
                double now = Main.GameUpdateCount;
                double elapsed = now - cell.LastSimulatedTime;
                if (elapsed <= 16)
                    return;

                int steps = (int)Math.Min(12, Math.Ceiling(elapsed / 30.0));
                if (steps <= 0)
                    steps = 1;

                float dt = (float)(elapsed / steps) / 60f;

                for (int s = 0; s < steps; s++)
                {
                    RegenerateResources(cell, dt);

                    foreach (long actorID in cell.ActorIDs)
                    {
                        if (!Actors.TryGetValue(actorID, out EcologyActor actor))
                            continue;

                        if (actor.IsLoaded || !actor.Alive)
                            continue;

                        SimulateActor(actor, dt);
                    }
                }

                cell.LastSimulatedTime = now;
            }

            private void SimulateActor(EcologyActor actor, float dt)
            {
                SpeciesEcologyDefinition species = EcologyRegistry.GetSpecies(actor.SpeciesID);
                if (species is null || !actor.Alive)
                    return;

                SimulateMetabolism(actor, species.Metabolism, dt);
                ChooseIntent(actor, species);
                ResolveIntent(actor, species, dt);

                actor.LastSimulatedTime = Main.GameUpdateCount;
            }

            private void SimulateMetabolism(EcologyActor actor, MetabolismDefinition def, float dt)
            {
                float digested = MathF.Min(actor.Metabolism.StomachContent, def.DigestiveRate * dt);
                actor.Metabolism.StomachContent -= digested;
                actor.Metabolism.Energy += digested;

                float activityMult = actor.Intent switch
                {
                    EcologyIntent.Resting => 0.4f,
                    EcologyIntent.Roaming => 1.0f,
                    EcologyIntent.Hunting => 1.5f,
                    EcologyIntent.Fleeing => 1.8f,
                    EcologyIntent.Migrating => 1.2f,
                    _ => 0.8f
                };

                float totalCost = def.BasalMetabolicRate * dt + def.ActivityCost * activityMult * dt;
                actor.Metabolism.Energy -= totalCost;

                if (actor.Metabolism.Energy < def.MaxEnergy * 0.25f && actor.Metabolism.BodyCondition > 0f)
                {
                    float reserveBurn = MathF.Min(actor.Metabolism.BodyCondition, def.ReserveConversionRate * dt);
                    actor.Metabolism.BodyCondition -= reserveBurn;
                    actor.Metabolism.Energy += reserveBurn;
                }

                actor.Metabolism.Energy = MathHelper.Clamp(actor.Metabolism.Energy, 0f, def.MaxEnergy);
                actor.Metabolism.StomachContent = MathHelper.Clamp(actor.Metabolism.StomachContent, 0f, def.MaxStomachContent);
                actor.Metabolism.BodyCondition = MathHelper.Clamp(actor.Metabolism.BodyCondition, 0f, def.MaxBodyCondition);
                actor.Metabolism.Fatigue = MathHelper.Clamp(actor.Metabolism.Fatigue, 0f, def.MaxFatigue);

                float energyRatio = def.MaxEnergy <= 0f ? 0f : actor.Metabolism.Energy / def.MaxEnergy;
                float reserveRatio = def.MaxBodyCondition <= 0f ? 0f : actor.Metabolism.BodyCondition / def.MaxBodyCondition;

                actor.Metabolism.Hunger = (1f - (energyRatio * 0.75f + reserveRatio * 0.25f)) * actor.MaxHungerSpecies;
                actor.Metabolism.Hunger = MathHelper.Clamp(actor.Metabolism.Hunger, 0f, actor.MaxHungerSpecies);
                actor.Hunger = (int)actor.Metabolism.Hunger;

                if (actor.Metabolism.Energy <= 0f && actor.Metabolism.BodyCondition <= 0f)
                {
                    actor.Alive = false;
                    actor.Intent = EcologyIntent.Dead;
                }
            }

            private void ChooseIntent(EcologyActor actor, SpeciesEcologyDefinition species)
            {
                float hungerRatio = actor.MaxHungerSpecies <= 0 ? 0f : actor.Metabolism.Hunger / actor.MaxHungerSpecies;
                float fatigueRatio = species.Metabolism.MaxFatigue <= 0f ? 0f : actor.Metabolism.Fatigue / species.Metabolism.MaxFatigue;

                NpcTraitFlags traits = species.Traits | actor.IndividualTraitOverrides;

                bool predator = (traits & NpcTraitFlags.Predator) != 0;
                bool prey = (traits & NpcTraitFlags.Prey) != 0;
                bool scavenger = (traits & NpcTraitFlags.Scavager) != 0 || actor.FoodConsumer.HasFlag(FoodConsumerType.Scavenger);

                if (!actor.Alive)
                {
                    actor.Intent = EcologyIntent.Dead;
                    return;
                }

                if (fatigueRatio > species.Metabolism.RestThreshold)
                {
                    actor.Intent = EcologyIntent.Resting;
                    return;
                }

                if (hungerRatio > species.Metabolism.HuntThreshold)
                {
                    if (predator)
                        actor.Intent = EcologyIntent.Hunting;
                    else if (scavenger)
                        actor.Intent = EcologyIntent.Scavenging;
                    else
                        actor.Intent = EcologyIntent.Feeding;

                    return;
                }

                if (prey && IsHighThreatCell(actor.CellCoord))
                {
                    actor.Intent = EcologyIntent.Fleeing;
                    return;
                }

                actor.Intent = EcologyIntent.Roaming;
            }

            private void ResolveIntent(EcologyActor actor, SpeciesEcologyDefinition species, float dt)
            {
                EcologyCell cell = GetOrCreateCell(actor.CellCoord);

                switch (actor.Intent)
                {
                    case EcologyIntent.Resting:
                        actor.Metabolism.Fatigue = MathHelper.Clamp(actor.Metabolism.Fatigue - 6f * dt, 0f, species.Metabolism.MaxFatigue);
                        break;

                    case EcologyIntent.Feeding:
                        ResolveFeeding(actor, cell, dt);
                        break;

                    case EcologyIntent.Scavenging:
                        ResolveScavenging(actor, cell, dt);
                        break;

                    case EcologyIntent.Hunting:
                        ResolveHunting(actor, cell, dt);
                        break;

                    case EcologyIntent.Fleeing:
                        ResolveFleeing(actor, cell, dt);
                        break;

                    case EcologyIntent.Roaming:
                        ResolveRoaming(actor, cell, dt);
                        break;
                }
            }

            private void ResolveFeeding(EcologyActor actor, EcologyCell cell, float dt)
            {
                float stomachSpace = MathF.Max(0f, 100f - actor.Metabolism.StomachContent);
                float needed = MathF.Min(10f * dt, stomachSpace);
                float eaten = MathF.Min(cell.PlantFood, needed);

                cell.PlantFood -= eaten;
                actor.Metabolism.StomachContent += eaten;
            }

            private void ResolveScavenging(EcologyActor actor, EcologyCell cell, float dt)
            {
                float stomachSpace = MathF.Max(0f, 100f - actor.Metabolism.StomachContent);
                float needed = MathF.Min(12f * dt, stomachSpace);
                float eaten = MathF.Min(cell.Carrion, needed);

                cell.Carrion -= eaten;
                actor.Metabolism.StomachContent += eaten;
            }

            private void ResolveHunting(EcologyActor actor, EcologyCell cell, float dt)
            {
                long preyActorID = FindPreyActorInCell(actor, cell);
                if (preyActorID >= 0 && Actors.TryGetValue(preyActorID, out EcologyActor prey) && prey.Alive)
                {
                    float chance = ComputePredationChance(actor, prey, cell) * dt;
                    if (Main.rand.NextFloat() < chance)
                    {
                        prey.Alive = false;
                        prey.Intent = EcologyIntent.Dead;
                        cell.Carrion += 8f;
                        actor.Metabolism.StomachContent += 20f;
                    }

                    return;
                }

                PopulationBucket bucket = FindPreyPopulationInCell(actor, cell);
                if (bucket is not null && bucket.Count > 0)
                {
                    float chance = ComputePredationChance(actor, bucket, cell) * dt;
                    if (Main.rand.NextFloat() < chance)
                    {
                        bucket.Count--;
                        actor.Metabolism.StomachContent += 18f;
                    }
                }
            }

            private void ResolveFleeing(EcologyActor actor, EcologyCell currentCell, float dt)
            {
                Point bestCoord = actor.CellCoord;
                float bestThreat = currentCell.Threat;

                foreach (Point coord in EcologyMath.GetCellsInRadius(actor.CellCoord, 1))
                {
                    EcologyCell candidate = GetOrCreateCell(coord);
                    if (candidate.Threat < bestThreat)
                    {
                        bestThreat = candidate.Threat;
                        bestCoord = coord;
                    }
                }

                if (bestCoord != actor.CellCoord)
                {
                    MoveActorToCell(ref actor, bestCoord);
                    actor.LastKnownWorldPosition = GetOrCreateCell(bestCoord).WorldBounds.Center.ToVector2();
                }

                actor.Metabolism.Fatigue += 3f * dt;
            }

            private void ResolveRoaming(EcologyActor actor, EcologyCell currentCell, float dt)
            {
                if (Main.rand.NextBool(20))
                {
                    Point newCoord = new(
                        actor.CellCoord.X + Main.rand.Next(-1, 2),
                        actor.CellCoord.Y + Main.rand.Next(-1, 2));

                    if (newCoord != actor.CellCoord)
                    {
                        MoveActorToCell(ref actor, newCoord);
                        actor.LastKnownWorldPosition = GetOrCreateCell(newCoord).WorldBounds.Center.ToVector2();
                    }
                }
            }

            private bool IsHighThreatCell(Point coord)
            {
                EcologyCell cell = GetOrCreateCell(coord);
                return cell.Threat >= 0.65f;
            }

            private long FindPreyActorInCell(EcologyActor predator, EcologyCell cell)
            {
                SpeciesEcologyDefinition predatorSpecies = EcologyRegistry.GetSpecies(predator.SpeciesID);
                if (predatorSpecies is null)
                    return -1;

                foreach (long actorID in cell.ActorIDs)
                {
                    if (actorID == predator.ActorID)
                        continue;

                    if (!Actors.TryGetValue(actorID, out EcologyActor candidate))
                        continue;

                    if (!candidate.Alive || candidate.IsLoaded)
                        continue;

                    SpeciesEcologyDefinition preySpecies = EcologyRegistry.GetSpecies(candidate.SpeciesID);
                    if (preySpecies is null)
                        continue;

                    NpcTraitFlags preyTraits = preySpecies.Traits | candidate.IndividualTraitOverrides;
                    if ((preyTraits & NpcTraitFlags.Prey) != 0)
                        return candidate.ActorID;
                }

                return -1;
            }

            private PopulationBucket FindPreyPopulationInCell(EcologyActor predator, EcologyCell cell)
            {
                foreach ((int _, PopulationBucket bucket) in cell.Populations)
                {
                    if (bucket.Count <= 0)
                        continue;

                    SpeciesEcologyDefinition species = EcologyRegistry.GetSpecies(bucket.SpeciesID);
                    if (species is null)
                        continue;

                    if ((species.Traits & NpcTraitFlags.Prey) != 0)
                        return bucket;
                }

                return null;
            }

            private float ComputePredationChance(EcologyActor predator, EcologyActor prey, EcologyCell cell)
            {
                float predatorDrive = 0.35f + predator.Aggression * 0.35f;
                float preyResistance = prey.Fear * 0.20f + cell.Shelter * 0.25f;
                return MathHelper.Clamp(predatorDrive - preyResistance, 0.05f, 0.95f);
            }

            private float ComputePredationChance(EcologyActor predator, PopulationBucket prey, EcologyCell cell)
            {
                float predatorDrive = 0.35f + predator.Aggression * 0.35f;
                float preyResistance = cell.Shelter * 0.25f;
                return MathHelper.Clamp(predatorDrive - preyResistance, 0.05f, 0.95f);
            }

            private void RegenerateResources(EcologyCell cell, float dt)
            {
                cell.PlantFood = MathHelper.Clamp(cell.PlantFood + 2.0f * dt, 0f, 100f);
                cell.Carrion = MathHelper.Clamp(cell.Carrion - 0.35f * dt, 0f, 100f);

                int predatorCount = 0;
                foreach (long actorID in cell.ActorIDs)
                {
                    if (!Actors.TryGetValue(actorID, out EcologyActor actor))
                        continue;

                    SpeciesEcologyDefinition species = EcologyRegistry.GetSpecies(actor.SpeciesID);
                    if (species is null)
                        continue;

                    NpcTraitFlags traits = species.Traits | actor.IndividualTraitOverrides;
                    if ((traits & NpcTraitFlags.Predator) != 0&& actor.Alive)
                        predatorCount++;


                    if (!actor.Alive)
                        cell.Carrion++;
                }

                cell.Threat = MathHelper.Clamp(predatorCount * 0.15f, 0f, 1f);
            }

            private bool HasImportantTraits(EcologyGlobalNPC eco)
            {
                return eco.HasTrait(NpcTraitFlags.Territorial)
                    || eco.HasTrait(NpcTraitFlags.AmbushPredator)
                    || eco.HasTrait(NpcTraitFlags.Scavager)
                    || eco.HasTrait(NpcTraitFlags.Predator);
            }

            #endregion
        }
    }
}
