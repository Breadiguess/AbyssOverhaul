using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Common.Brain.SharedSensors;
using AbyssOverhaul.Core.Carcasses;
using CalamityMod;

namespace AbyssOverhaul.Content.NPCs.CarcassLeech
{
    internal class CarcassLeechNPC : ModNPC
    {
        public ModularNpcBrain<SchoolingNpcContext> NpcBrain;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
        }
        public override void SetDefaults()
        {
            NPC.Size = new(20);

            NPC.friendly = true;
            NPC.dontTakeDamageFromHostiles = true;

            NPC.damage = 40;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;

            NPC.lifeMax = 120;
            Initialize();
        }
        private void Initialize()
        {
            NpcBrain = new(new());


            NpcBrain.Modules.Add(new IdleHomeModule() { ReturnDistance = 0, MoveSpeed = 20f });
            NpcBrain.Modules.Add(new AvoidSameTypeModule()
            {
                AvoidanceRadius = 10,
                CrowdingScoreMultiplier = 0.4f
            });
            NpcBrain.Modules.Add(new FollowPlayerModule()
            {
                Score = 2,
                MoveSpeed = 20
            });
           
            NpcBrain.Sensors.Add(new SchoolingSensor()
            {
                RequireLineOfSight = true
            });
        }
    


        public override int SpawnNPC(int tileX, int tileY)
        {

            if (NpcBrain == null)
                Initialize();

            Vector2 t = new Vector2(tileX, tileY).ToWorldCoordinates();
            var best = 0;
            foreach (var carcass in CarcassSystem.Carcasses)
            {
                if (carcass.Value.position.Distance(t) > 200)
                    continue;

                if (!carcass.Value.Active) continue;


                if (carcass.Value.FleshRemaining.CompareTo(best) > 0.5f)
                    best = carcass.Value.FleshRemaining;

                t = carcass.Value.position;

            }
            var x = t.ToTileCoordinates();

            tileX = x.X;
            tileY = x.Y;

            NpcBrain.Context.HasTargetPoint = true;
            NpcBrain.Context.TargetPoint = t;
            NpcBrain.Context.HomePosition = t;
            return base.SpawnNPC(tileX, tileY);
        }
        public override bool PreAI()
        {


            if (NPC.wet)
                NPC.noGravity = true;
            else
                NPC.noGravity = false;
            if (NpcBrain == null)
            {
                Initialize();
            }
            return base.PreAI();
        }
        public CarcassEntity PrefferedCarcass;

        public int LinkedCarcassID => (int)NPC.ai[0];

        public override void PostAI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!CarcassSystem.TryGetCarcass(LinkedCarcassID, out var carcass) || !carcass.Active)
                {
                    NPC.active = false;
                    NPC.netUpdate = true;
                    return;
                }

                Vector2 toCarcass = carcass.Center - NPC.Center;
                float dist = toCarcass.Length();


                // Periodically feed.
                if (NPC.ai[2]++ >= 90f)
                {
                    NPC.ai[2] = 0f;

                    if (dist < 60f && carcass.FleshRemaining > 0)
                    {
                        carcass.FleshRemaining = Math.Max(0, carcass.FleshRemaining - 1);
                        carcass.NetDirty = true;
                    }
                }

                NPC.netUpdate = true;
            }
        }
        public override void AI()
        {
            if (!NpcBrain.Context.HasTargetPoint || PrefferedCarcass is null)
            {
                var List = CarcassSystem.Carcasses.Values.ToList();

                List.Sort((a, b) => Vector2.Distance(a.Center, NPC.Center).CompareTo(Vector2.Distance(b.Center, NPC.Center)));

                NpcBrain.Context.HomePosition = List[0].Center;

                PrefferedCarcass = List[0];
                NpcBrain.Context.HasTargetPoint = true;
                NPC.netUpdate = true;
            }


            
            if (PrefferedCarcass is not null)
            {

                NpcBrain.Context.HomePosition = PrefferedCarcass.Hitbox.ClosestPointInRect(NPC.Center);
                NpcBrain.Context.SchoolTargetPosition = PrefferedCarcass.Hitbox.ClosestPointInRect(NPC.Center);
                NPC.rotation = NPC.AngleTo(NpcBrain.Context.HomePosition);
                if (NpcBrain.Context.ClosestPlayer is not null)
                    if (NpcBrain.Context.ClosestPlayer.Center.Distance(PrefferedCarcass.Center) < 140)
                    {
                        NPC.friendly = false;

                        NPC.damage = 40;
                        NPC.dontTakeDamage = false;
                        
                        NpcBrain.Modules.Clear();
                        NpcBrain.Modules.Add(new FollowPlayerModule()
                        {
                            MoveSpeed = 20f
                        });
                        NpcBrain.Modules.Add(new SchoolingMovementModule()
                        {
                            MaxMoveSpeed = 20f
                        });
                        NPC.ForceNetUpdate();
                    }

            }



            NpcBrain.Update(NPC);


        }


        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }
    }
}
