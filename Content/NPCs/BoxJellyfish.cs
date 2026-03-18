using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.Contexts;
using AbyssOverhaul.Common.Brain.SharedModules;
using BreadLibrary.Core;
using BreadLibrary.Core.Verlet;
using CalamityMod;
using CalamityMod.BiomeManagers;
using Luminance.Assets;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace AbyssOverhaul.Content.NPCs
{
    internal class BoxJellyfish : ModNPC
    {

        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;
        public ModularNpcBrain<NpcContext> NpcBrain;

        public List<VerletChain> tentacles;
        internal const int MaxTentacles = 4;
        public override void SetDefaults()
        {
            NPC.friendly = false;

            NPC.lifeMax = 1500;
            NPC.defense = 40;
            NPC.noGravity = true;
            NPC.damage = 3;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.noTileCollide = false;
            NPC.value = Item.buyPrice(silver: 1);
            NPC.HitSound = SoundID.NPCHit25;
            NPC.DeathSound = SoundID.NPCDeath28;
            NPC.knockBackResist = 0;
            NPC.Calamity().VulnerableToHeat = false;
            NPC.Calamity().VulnerableToSickness = false;
            NPC.Calamity().VulnerableToElectricity = true;
            NPC.Calamity().VulnerableToWater = false;
            SpawnModBiomes = new int[1] { ModContent.GetInstance<AbyssLayer1Biome>().Type };

            NPC.Size = new Vector2(40, 40);
            InitializeMissing();
        }
        private void InitializeMissing()
        {
         
            if(tentacles == null)
            {
                tentacles = new List<VerletChain>();

                for (int i = 0; i < MaxTentacles; i++)
                {
                    tentacles.Add(new(50, 10, NPC.Center));

                }
            }
           
            if(NPC.velocity.Length() <= 0.1f)
            {
                NPC.velocity = new Vector2(2, 0).RotatedByRandom(3.14f);
                NPC.netUpdate = true;
            }



        }


        public override bool PreAI()
        {
            if (tentacles is null)
                InitializeMissing();


            return base.PreAI();
        }
        public override void AI()
        {
            if (NPC.wet)
                NPC.noGravity = true;
            else
                NPC.noGravity = false;

            NPC.rotation = NPC.velocity.ToRotation();
            NPC.velocity = new Vector2(1,0).RotatedBy(NPC.rotation) * Math.Clamp(MathF.Tan(Main.GameUpdateCount * 0.01f), 2, 4);

            Vector2 oldVelocity = NPC.velocity;
            Vector2 newVelocity = Collision.TileCollision(NPC.position, NPC.velocity, NPC.width, NPC.height);

            bool hitX = newVelocity.X != oldVelocity.X;
            bool hitY = newVelocity.Y != oldVelocity.Y;

            // Apply resolved movement velocity first.
            NPC.velocity = newVelocity;

            // If either axis got blocked, swap the original components.
            if (hitX || hitY)
            {
                NPC.velocity = new Vector2(oldVelocity.Y, oldVelocity.X);
            }

        }
        public override void PostAI()
        {
            Vector2[] offset = new Vector2[]
            {
                NPC.TopLeft,
                NPC.TopRight,
                NPC.BottomLeft,
                NPC.BottomRight
            };
            for (int i = 0; i< MaxTentacles; i++)
            {
                var t = tentacles[i];
                if (t.Positions[0].Distance(Vector2.Zero) < 120)
                {
                    for(int x = 0; x< t.Positions.Length; x++)
                    {
                        t.Positions[x] = NPC.Center;
                        t.OldPositions[x] = NPC.Center;

                    }

                }



                t.Simulate(Vector2.Zero, offset[i], 0, 0.7f);
                tentacles[i] = t;
            }

           
            base.PostAI();
        }


        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Venom, 60 * 12, false);
        }
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {

            if(tentacles is  not null)
            {
                for(int x = 0; x<  tentacles.Count; x++)
                {
                   var t = tentacles[x];
                    if(t is not null)
                    {
                        for(int i = 0; i< t.Positions.Length; i++)
                        {
                            npcHitbox.Location = (t.Positions[i] - npcHitbox.Size() / 2).ToPoint();

                            if (victimHitbox.IntersectsConeFastInaccurate(t.Positions[i], 30, 0, MathHelper.TwoPi))
                            {
                                
                                
                                damageMultiplier *= 0.2f;

                                return false;
                            }
                        }
                    }
                }
            }



            return base.ModifyCollisionData(victimHitbox, ref immunityCooldownSlot, ref damageMultiplier, ref npcHitbox);
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {




            Drawtentacles(spriteBatch);
            Utils.DrawRect(spriteBatch, NPC.Hitbox, drawColor);


            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        void Drawtentacles(SpriteBatch spriteBatch)
        {
            if (tentacles is not null)
            {
                for(int x = 0; x< tentacles.Count; x++)
                {
                    var t = tentacles[x];
                    for(int i = 0; i< t.Positions.Length-1; i++)
                    {
                        Vector2 start = t.Positions[i];
                        Vector2 end = t.Positions[i + 1];

                        Color color1 = Lighting.GetColor(t.Positions[i].ToTileCoordinates());
                        Color color2 = Lighting.GetColor(t.Positions[i + 1].ToTileCoordinates());
                       Utils.DrawLine(spriteBatch, start, end, Color.White.MultiplyRGB(color1), Color.White.MultiplyRGB(color2), 4);
                    }
                   
                }



            }
        }
    }
}
