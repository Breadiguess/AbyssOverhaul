using AbyssOverhaul.Common.Brain;
using AbyssOverhaul.Common.Brain.SharedModules;
using AbyssOverhaul.Core.Graphics;
using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using BreadLibrary.Core.Sounds;
using BreadLibrary.Core.Verlet;
using CalamityMod.BiomeManagers;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;

namespace AbyssOverhaul.Content.Layers.TheVeil.NPCs.VoidDreamerNPC
{
    internal class VoidDreamer : ModNPC, IDrawPixellated
    {

        private const int HeadVariationCount = 6;
        private const int EyeVariationCount = 6;
        private const int SkirtVariationCount = 4;
        public static List<Asset<Texture2D>> HeadPieces;
        public static List<Asset<Texture2D>> EyeVariations;
        public static List<Asset<Texture2D>> Skirts;

        private static string SoundPath = "AbyssOverhaul/Assets/Sounds/NPCs/VoidDreamer";
        public static SoundStyle FoundObservationTarget = new SoundStyle($"{SoundPath}/alert", 3);
        public static SoundStyle ObserveLoop = new SoundStyle($"{SoundPath}/ObserveLoop");
        public static SoundStyle LoopEnd = new SoundStyle($"{SoundPath}/ObserveLoopEnd");

        public LoopedSoundInstance? ObserverLoop
        {
            get; private set;
        }

        public Asset<Texture2D> HeadTex;
        private int SkirtVar;
        public Asset<Texture2D> EyeTex;
        public Asset<Texture2D> SkirtTex;
        public override void Load()
        {
            string Path = this.GetPath();

            HeadPieces = new();
            EyeVariations = new();
            Skirts = new();

            const string suffix = "/VoidDreamer";
            if (Path.EndsWith(suffix))
                Path = Path[..^suffix.Length];
            for (int i = 0; i < EyeVariationCount; i++)
            {
                var thing = ModContent.Request<Texture2D>($"{Path}/Eyes/VoidDreamer_Eye{i}");
                EyeVariations.Add(thing);
            }

            for (int i = 0; i < HeadVariationCount; i++)
            {
                var thing = ModContent.Request<Texture2D>($"{Path}/Heads/Head{i}");
                HeadPieces.Add(thing);
            }

            for (int i = 0; i < SkirtVariationCount; i++)
            {
                var thing = ModContent.Request<Texture2D>($"{Path}/Skirts/Skirt_{i}");
                Skirts.Add(thing);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write7BitEncodedInt(SkirtVar);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            SkirtVar = reader.ReadInt32();
        }
        public ModularNpcBrain<ObservationNpcContext> NpcBrain;
        public List<VerletChain> Tendrils = new();
        public bool HorseMode = false;
        public override void SetStaticDefaults()
        {
            NPCID.Sets.CountsAsCritter[Type] = true;
            NPCID.Sets.ImmuneToAllBuffs[Type] = true;
            NPCID.Sets.TeleportationImmune[Type] = true;

            Main.npcFrameCount[Type] = 1;

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {

            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;

        }
        public override void SetDefaults()
        {
            InitializeEverything();
            NPC.friendly = false;
            NPC.Size = new Vector2(40, 50);
            NPC.lifeMax = 9999;
            NPC.defense = 120;
            NPC.damage = -1;
            NPC.chaseable = false;
            NPC.noGravity = true;
            NPC.HitSound = SoundID.Item146 with { pitch = -1f, volume = 2 };

            SpawnModBiomes = new int[2] { ModContent.GetInstance<AbyssLayer3Biome>().Type, ModContent.GetInstance<AbyssLayer4Biome>().Type };
            NPC.lavaImmune = true;
            Tendrils.EnsureCapacity(6);
            NPC.waterMovementSpeed = 1;
            

        }
        public int HeadVar
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        internal int EyeVar
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }


        public PixelLayer PixelLayer => PixelLayer.AboveTiles;

        public Vector2 EyeDirection;

        public override bool PreAI()
        {
            if (NpcBrain == null || Tendrils == null)
            {
                InitializeEverything();
            }


            if (ObservationInterpolant > 0.5f)
                NPC.dontTakeDamage = false;
            else
                NPC.dontTakeDamage = true;

            return base.PreAI();
        }

        private void InitializeEverything()
        {
            var sensor = new CombatObservationSensor()
            {
                SearchRadius = WorldGen.genRand.NextFloat(200, 400)
            };
            ObservationNpcContext context = new()
            {
                DesiredObservationDistance = sensor.SearchRadius - 120,
                ObservationWindowRadians = MathHelper.ToRadians(110f)
            };

            ModularNpcBrain<ObservationNpcContext> brain = new(context);

            brain.Modules.Add(new ObservationReactionModule());
            brain.Modules.Add(new IdleModule());
            brain.Modules.Add(new AvoidSameTypeModule
            {
                AvoidanceRadius = 60f,
                PreferredSeparation = 60f,
                MaxMoveSpeed = 4f,
                BaseScore = 50f,
                CrowdingScoreMultiplier = 10f
            });
            brain.Sensors.Add(sensor);
            NpcBrain = brain;
            Tendrils = new();
            Tendrils.EnsureCapacity(6);
            for (int i = 0; i < 6; i++)
                Tendrils.Add(new VerletChain(14, 5, NPC.Center));
            EyeVar = Main.rand.Next(0, EyeVariationCount);
            EyeTex = EyeVariations[EyeVar];

            HeadVar = Main.rand.Next(HeadVariationCount);

            if (Main.rand.NextBool(200))
            {
                HorseMode = true;

                string Path = this.GetPath();
                const string suffix = "/VoidDreamer";
                if (Path.EndsWith(suffix))
                    Path = Path[..^suffix.Length];
                if (Main.rand.NextBool())

                    HeadTex = ModContent.Request<Texture2D>($"{Path}/Heads/HorseHead");
                else

                    HeadTex = ModContent.Request<Texture2D>($"{Path}/Heads/Glooby");
            }
            else
                HeadTex = HeadPieces[HeadVar];
            SkirtVar = Main.rand.Next(SkirtVariationCount);
            SkirtTex = Skirts[SkirtVar];
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (ObservationInterpolant > 0.1f)

                return base.DrawHealthBar(hbPosition, ref scale, ref position);
            else
                return false;
        }
        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            if (ObservationInterpolant > 0.2f)
                boundingBox = NPC.Hitbox;

        }

        public float EyeLightRotation;
        public float ObservationInterpolant;
        public override void AI() => NpcBrain.Update(NPC);

        public bool HasPlayedSound { get; private set; }
        public override void PostAI()
        {

            EyeLightRotation = EyeLightRotation.AngleLerp(EyeDirection.ToRotation(), 0.07f);
            float target = NpcBrain.Context.HasObservedThing ? 1f : 0f;
            ObservationInterpolant = MathHelper.Lerp(ObservationInterpolant, target, 0.2f);
            ObservationInterpolant = MathHelper.Clamp(ObservationInterpolant, 0f, 1f);


            var tex = ModContent.Request<Texture2D>("AbyssOverhaul/Assets/Textures/Glow_2").Value;


            if (NpcBrain.Context.HasObservedThing && !HasPlayedSound)
            {
                float VelocityScale = NpcBrain.Context.ObservedObject.velocity.Length();


                float AdjustedPitch = Utils.Remap(VelocityScale, 0, 30f, 0f, 1.2f);
                SoundEngine.PlaySound(FoundObservationTarget with { pitchVariance = 0.2f, MaxInstances = 0, pitch = AdjustedPitch }, NPC.Center);
                HasPlayedSound = true;
            }


            if (NpcBrain.Context.HasObservedThing)
            {
                if (ObserverLoop is null || !ObserverLoop.LoopIsBeingPlayed)
                    ObserverLoop = LoopedSoundManager.CreateNew(ObserveLoop, () => NPC is null || !NPC.active || ObservationInterpolant <= 0);

                float a = Utils.Remap(NpcBrain.Context.ObservedDistance, 0, NpcBrain.Context.DesiredObservationDistance, 0, 1);
                ReworkedAbyssLighting.lights.Add(
                  new()
                  {
                      center = NPC.Center,
                      rotation = EyeLightRotation + MathF.Cos(Main.GameUpdateCount * 0.02f + NPC.whoAmI) * 0.2f,
                      Origin = new Vector2(0, tex.Height / 2),
                      texture = tex,
                      vectorScale = new Vector2(2 * a, 0.8f) * 0.2f

                  });
                ReworkedAbyssLighting.lights.Add(new(center: NpcBrain.Context.ObservedPosition, scale: 2 * ObservationInterpolant));
                Lighting.AddLight(NpcBrain.Context.ObservedPosition, TorchID.UltraBright);

                ;

            }
            else
            {
                HasPlayedSound = false;
            }


            ReworkedAbyssLighting.lights.Add(new(center: NPC.Center, scale: 2 * ObservationInterpolant));
            if (ObserverLoop != null)
            {
                ObserverLoop.Update(NPC.Center, sound =>
                {
                    sound.Pitch = 0.2f * ObservationInterpolant;
                    sound.Volume = 0.4f * ObservationInterpolant;
                });
            }






            float accel = 0.12f;

            // where we WANT to be going
            float desiredVelX = NpcBrain.LastDesiredVelocity.X;

            float steering =
                desiredVelX - NPC.velocity.X;

            steering = MathHelper.Clamp(steering, -accel, accel);

            NPC.spriteDirection = EyeDirection.X.NonZeroSign();

            var referenceSpeed = 1f;
            var maxTilt = MathHelper.ToRadians(20f);
            var normalized = MathHelper.Clamp(NPC.velocity.X / referenceSpeed, -1f, 1f);
            var targetRotation = normalized * maxTilt;

            // Slightly lerp rotation toward the horizontal-velocity-based target.
            NPC.rotation = NPC.rotation.AngleLerp(targetRotation, 0.15f);
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
            EyeDirection = Vector2.Lerp(EyeDirection, NpcBrain.Context.ObservedPosition.DirectionFrom(NPC.Center), 0.2f);
            for (int i = 0; i < Tendrils.Count; i++)
            {
                var t = Tendrils[i];
                if (t is null)
                    continue;

                if (t.Positions[^1].Distance(NPC.Center) > 200)
                    for (int x = 0; x < t.Positions.Length; x++)
                    {
                        t.Positions[x] = NPC.Center;
                    }
                Vector2 Pos = NPC.Center + new Vector2(3 + MathF.Sin(Main.GameUpdateCount * 0.01f + i) * 5, 20);

                Vector2 Velocity = Vector2.UnitX * MathF.Sin(i + Main.GameUpdateCount * 0.05f) * ObservationInterpolant;
                t.Simulate(Velocity,
                    Pos, 0.8f, 0.5f, collideWithTiles: true);

                Tendrils[i] = t;


            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.AbyssOverhaul.Bestiary.VoidDreamer")
            });
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 drawPos = NPC.Center - screenPos;

            //Utils.DrawBorderString(spriteBatch, ObservationInterpolant.ToString(), drawPos, Color.White);
            try
            {
                if (NpcBrain is null)
                    return false;
            }
            catch { }

            if (NPC.IsABestiaryIconDummy)
            {
                DrawBodyPieces(spriteBatch, drawPos + Main.screenPosition, NPC.spriteDirection.ToSpriteDirection());
                DrawTendrilsForCurrentContext(spriteBatch);
            }

            if (!EyeDirection.Equals(Vector2.Zero) || NPC.IsABestiaryIconDummy)
            {
                if (NPC.IsABestiaryIconDummy)
                    EyeDirection = NPC.Center.DirectionTo(Main.MouseWorld);

                Vector2 lookDir = EyeDirection;
                if (lookDir != Vector2.Zero)
                    lookDir.Normalize();

                Vector2 baseEyeScale = Vector2.One * 0.04f;

                Vector2 eyeSocketOffset = new Vector2(lookDir.X * 8f, lookDir.Y * 4f);

                float widthScale = MathHelper.Lerp(1f, 0.65f, MathF.Abs(lookDir.X));
                float heightScale = MathHelper.Lerp(1f, 0.9f, MathF.Abs(lookDir.Y));
                Vector2 eyeScale = new Vector2(baseEyeScale.X * widthScale, baseEyeScale.Y * heightScale);

                Vector2 eyeDrawPos = drawPos + eyeSocketOffset;
                float eyeRotation = 0f;
                eyeRotation = eyeRotation.AngleLerp(Main.rand.NextFloat() * MathF.Sin(Main.GlobalTimeWrappedHourly + NPC.whoAmI * 10f), 0.2f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, default, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);

                const float iterations = 12;
                for (int i = 0; i < iterations; i++)
                {
                    float rotationOffset = i / (float)iterations * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly * 0.3f;
                    Main.EntitySpriteDraw(
                        EyeTex.Value,
                        eyeDrawPos + new Vector2(1, 0).RotatedBy(rotationOffset),
                        null,
                        Color.SkyBlue * 0.4f * ObservationInterpolant,
                        eyeRotation,
                        EyeTex.Value.Size() / 2f,
                        eyeScale * 1.2f,
                        0
                    );
                }

                if (!NPC.IsABestiaryIconDummy)
                    Main.spriteBatch.ResetToDefault();
                else
                    Main.spriteBatch.ResetToDefaultUI();

                Main.EntitySpriteDraw(
                    EyeTex.Value,
                    eyeDrawPos,
                    null,
                    Color.White with { A = 0 } * ObservationInterpolant,
                    eyeRotation,
                    EyeTex.Value.Size() / 2f,
                    eyeScale,
                    0
                );
            }

            return false;
        }
        public void DrawPixelated(SpriteBatch spriteBatch)
        {
            Vector2 drawPos = NPC.Center - Main.screenPosition;
            SpriteEffects flip = NPC.spriteDirection.ToSpriteDirection();

            // Bestiary dummy renders in UI-like space, so undo the world screen offset.
            if (NPC.IsABestiaryIconDummy)
                drawPos += Main.screenPosition;

            try
            {
                DrawTendrilsForCurrentContext(spriteBatch);
                DrawBodyPieces(spriteBatch, drawPos, flip);
            }
            catch
            {
            }
        }

        private void DrawTendrilsForCurrentContext(SpriteBatch spriteBatch)
        {
            if (Tendrils is null)
                return;

            bool inBestiary = NPC.IsABestiaryIconDummy;

            if (inBestiary)
            {
                for (int i = 0; i < Tendrils.Count; i++)
                {
                    var t = Tendrils[i];
                    if (t is null)
                        continue;

                    if (t.Positions[^1].Distance(NPC.Center) > 200f)
                    {
                        for (int x = 0; x < t.Positions.Length; x++)
                            t.Positions[x] = NPC.Center;
                    }

                    Vector2 attachPos = NPC.Center + new Vector2(
                        3f + MathF.Sin(Main.GameUpdateCount * 0.01f + i) * 5f,
                        20f
                    );

                    Vector2 velocity = Vector2.UnitX * MathF.Sin(i + Main.GameUpdateCount * 0.05f) * ObservationInterpolant;

                    t.Simulate(velocity, attachPos, 0.8f, 0.5f, collideWithTiles: false);
                    Tendrils[i] = t;
                }
            }

            for (int i = 0; i < Tendrils.Count; i++)
            {
                var t = Tendrils[i];
                if (t is null)
                    continue;

                for (int x = 0; x < t.Positions.Length - 1; x++)
                {
                    Vector2 start = t.Positions[x];
                    Vector2 end = t.Positions[x + 1];

                    if (start == end)
                        continue;

                    Color color = Color.Lerp(Color.Black, Color.Blue * ObservationInterpolant, x / (float)(t.Positions.Length - 1));
                    Utils.DrawLine(spriteBatch, start, end, color, color, 3f);
                }
            }
        }

        public void DrawBodyPieces(SpriteBatch spriteBatch, Vector2 drawPos, SpriteEffects flip)
        {
            Color drawColor = NPC.IsABestiaryIconDummy
                ? Color.White
                : Lighting.GetColor(NPC.Center.ToTileCoordinates());

            Texture2D headTex = HeadTex.Value;
            Vector2 headOrigin = new Vector2(headTex.Width / 2f, headTex.Height - 15f);
            Main.EntitySpriteDraw(
                headTex,
                drawPos + Vector2.UnitY * 10f,
                null,
                drawColor,
                NPC.rotation,
                headOrigin,
                NPC.scale * 1.5f,
                flip
            );

            Texture2D skirtTex = SkirtTex.Value;
            Vector2 skirtOrigin = new Vector2(skirtTex.Width / 2f, -8f);
            Color skirtColor = NPC.IsABestiaryIconDummy
                ? Color.White * MathHelper.Lerp(0.35f, 1f, ObservationInterpolant)
                : Color.Lerp(Color.Black, Color.White, ObservationInterpolant);

            Main.EntitySpriteDraw(
                skirtTex,
                drawPos,
                null,
                skirtColor,
                NPC.rotation,
                skirtOrigin,
                NPC.scale * 2f,
                flip
            );
        }
    }
}

