using AbyssOverhaul.Content.Items.LoreItems;
using CalamityMod;
using CalamityMod.Items.Potions;
using CalamityMod.NPCs;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.Particles;
using CalamityMod.World;

namespace AbyssOverhaul.Content.NPCs.Bosses.PrimordialWyrm
{
	public class PrimordialWyrm
	{
		[AutoloadBossHead]
		[LongDistanceNetSync]
		public class PrimordialWyrmHead : ModNPC
		{
			private bool TailSpawned = false;

			private const int wyrmLength = 40;

			private const float headOffset = -18f;

			public override void SetStaticDefaults()
			{
				NPCID.Sets.CantTakeLunchMoney[Type] = true;
			}

			public override void SetDefaults()
			{
				NPC.damage = 300;
				NPC.npcSlots = 50f;
				NPC.width = 200;
				NPC.height = 200;
				NPC.lifeMax = 3_000_000;
				NPC.aiStyle = -1;
				AIType = -1;
				NPC.knockBackResist = 0f;
				NPC.behindTiles = true;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				NPC.HitSound = SoundID.NPCHit1;
				NPC.DeathSound = SoundID.NPCDeath6;
				NPC.netAlways = true;
				NPC.boss = true;
				NPC.gfxOffY = 6;
			}

			// Admittedly, this is just the movement of the calamity primordial wyrm.
			// Just a placeholder for now to test visuals of the wyrm ported from AB by Naka
			// All credit here to cal devs for the wyrm movement (unless its from fabsol (FUCK FABSOL))
			public override void AI()
			{
				// Difficulty modes
				bool death = CalamityWorld.death;
				bool revenge = CalamityWorld.revenge;
				bool expertMode = Main.expertMode;

				// Get a target
				if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
					NPC.TargetClosest();

				// Despawn safety, make sure to target another player if the current player target is too far away
				if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
					NPC.TargetClosest();

				// Target variable
				Player player = Main.player[NPC.target];

				bool targetDownDeep = player.Calamity().ZoneAbyssLayer4;

				// Check whether enraged for the sake of the HP bar UI
				NPC.Calamity().CurrentlyEnraged = !targetDownDeep;

				if (NPC.ai[2] > 0f)
					NPC.realLife = (int)NPC.ai[2];

				// Spawn segments
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					if (!TailSpawned && NPC.ai[0] == 0f)
					{
						int Previous = NPC.whoAmI;
						for (int i = 0; i < wyrmLength + 1; i++)
						{
							int lol;
							if (i >= 0 && i < wyrmLength)
								lol = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<PrimordialWyrmBody>(), NPC.whoAmI);
							else
								lol = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<PrimordialWyrmTail>(), NPC.whoAmI);

							Main.npc[lol].realLife = NPC.whoAmI;
							Main.npc[lol].ai[2] = NPC.whoAmI;
							Main.npc[lol].ai[1] = Previous;
							Main.npc[Previous].ai[0] = lol;
							NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
							Previous = lol;
						}
						TailSpawned = true;
					}
				}

				// Despawn if target is dead
				bool targetDead = false;
				if (player.dead)
				{
					NPC.TargetClosest(false);
					player = Main.player[NPC.target];
					if (player.dead)
					{
						targetDead = true;

						NPC.velocity.Y += 3f;
						if (NPC.position.Y > Main.worldSurface * 16.0)
							NPC.velocity.Y += 3f;

						if (NPC.position.Y > Main.rockLayer * 16.0)
						{
							for (int a = 0; a < Main.maxNPCs; a++)
							{
								if (Main.npc[a].type == NPC.type || Main.npc[a].type == ModContent.NPCType<PrimordialWyrmBodyAlt>() || Main.npc[a].type == ModContent.NPCType<PrimordialWyrmBody>() || Main.npc[a].type == ModContent.NPCType<PrimordialWyrmTail>())
									Main.npc[a].active = false;
							}
						}
					}
				}

				// Direction and rotation
				NPC.rotation = NPC.velocity.ToRotation() + MathHelper.PiOver2;
				int direction = NPC.direction;
				NPC.direction = NPC.spriteDirection = (NPC.velocity.X > 0f) ? 1 : (-1);
				if (direction != NPC.direction)
					NPC.netUpdate = true;

				// Default vector to swim to
				Vector2 destination = player.Center;

				// Velocity and turn speed values
				float velocityScale = death ? 1.8f : revenge ? 1.5f : expertMode ? 1.2f : 0f;
				float baseVelocity = (targetDownDeep ? 10f : 15f) + (targetDownDeep ? velocityScale : velocityScale * 1.5f);

				float turnSpeed = baseVelocity * 0.015f;

				//movement
				if (!targetDead)
				{
					// Ensure that speed stays within a specific range.
					NPC.velocity = NPC.velocity.ClampMagnitude(baseVelocity * 0.2f, baseVelocity * 1.3f);
					Vector2 idealVelocity = NPC.SafeDirectionTo(destination) * baseVelocity;

					if ((NPC.velocity.X > 0f && idealVelocity.X > 0f) || (NPC.velocity.X < 0f && idealVelocity.X < 0f) || (NPC.velocity.Y > 0f && idealVelocity.Y > 0f) || (NPC.velocity.Y < 0f && idealVelocity.Y < 0f))
					{
						// Accelerate towards the ideal velocity.
						NPC.velocity.X += (NPC.velocity.X < idealVelocity.X).ToDirectionInt() * turnSpeed;
						NPC.velocity.Y += (NPC.velocity.Y < idealVelocity.Y).ToDirectionInt() * turnSpeed;

						// Swim more quickly towards the ideal velocity if there isn't much speed currently or if the velocity goes against the ideal velocity.
						if (Math.Abs(idealVelocity.Y) < baseVelocity * 0.2 && ((NPC.velocity.X > 0f && idealVelocity.X < 0f) || (NPC.velocity.X < 0f && idealVelocity.X > 0f)))
							NPC.velocity.Y += NPC.velocity.Y.DirectionalSign() * turnSpeed * 2f;

						if (Math.Abs(idealVelocity.X) < baseVelocity * 0.2 && ((NPC.velocity.Y > 0f && idealVelocity.Y < 0f) || (NPC.velocity.Y < 0f && idealVelocity.Y > 0f)))
							NPC.velocity.X += NPC.velocity.X.DirectionalSign() * turnSpeed * 2f;
					}

					// Choose whichever axis the Wyrm is closest to it's destination on and emphasize moving in that direction.
					else if (MathHelper.Distance(destination.X, NPC.Center.X) > MathHelper.Distance(destination.Y, NPC.Center.Y))
					{
						NPC.velocity.X += (NPC.velocity.X < idealVelocity.X).ToDirectionInt() * turnSpeed * 1.1f;
						if (NPC.velocity.ManhattanDistance(Vector2.Zero) < baseVelocity * 0.5)
							NPC.velocity.Y += NPC.velocity.Y.DirectionalSign() * turnSpeed;
					}
					else
					{
						NPC.velocity.Y += (NPC.velocity.Y < idealVelocity.Y).ToDirectionInt() * turnSpeed * 1.1f;
						if (NPC.velocity.ManhattanDistance(Vector2.Zero) < baseVelocity * 0.5)
							NPC.velocity.X += NPC.velocity.X.DirectionalSign() * turnSpeed;
					}
				}
			}

			public override bool CheckActive() => false;

			public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
			{
				scale = 1.5f;
				return null;
			}

			public override void BossLoot(ref int potionType)
			{
				potionType = ModContent.ItemType<SupremeHealingPotion>();
			}

			public override void ModifyNPCLoot(NPCLoot npcLoot)
			{
				// Lore
				npcLoot.AddConditionalPerPlayer(() => !DownedBossSystem.downedPrimordialWyrm, ModContent.ItemType<LorePrimordialWyrm>(), desc: DropHelper.FirstKillText);
				npcLoot.AddConditionalPerPlayer(() => !DownedBossSystem.downedPrimordialWyrm, ModContent.ItemType<LoreTerminus>(), desc: DropHelper.FirstKillText);
			}

			public override void OnKill()
			{
				DownedBossSystem.downedPrimordialWyrm = true;
				CalamityNetcode.SyncWorld();
			}

			public override void DrawEffects(ref Color drawColor)
			{
				var particle = new GlowOrbParticle(
					NPC.Center + Main.rand.NextVector2Circular(4f, 4f) - new Vector2(0f, 48f + headOffset).RotatedBy(NPC.rotation),
					Main.rand.NextVector2Circular(8f, 8f),
					false,
					60,
					1f,
					NPC.GetAlpha(new Color(255, 244, 0))
				);

				GeneralParticleHandler.SpawnParticle(particle);
			}

			public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
			{
				var texture = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmHead").Value;

				var position = NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY);
				var headOrigin = (texture.Size() / 2f) + new Vector2(0, headOffset);

				Main.EntitySpriteDraw(
					texture,
					position,
					null,
					NPC.GetAlpha(drawColor),
					NPC.rotation,
					headOrigin,
					NPC.scale,
					SpriteEffects.None
				);

				var glow = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmHead_Glow").Value;

				spriteBatch.End();
				spriteBatch.Begin(
					SpriteSortMode.Deferred,
					BlendState.Additive,
					Main.DefaultSamplerState,
					default,
					Main.Rasterizer,
					default,
					Main.GameViewMatrix.TransformationMatrix
				);

				Main.EntitySpriteDraw(
					glow,
					position,
					null,
					NPC.GetAlpha(Color.White),
					NPC.rotation,
					headOrigin,
					NPC.scale,
					SpriteEffects.None
				);

				spriteBatch.End();
				spriteBatch.Begin(
					default,
					default,
					Main.DefaultSamplerState,
					default,
					Main.Rasterizer,
					default,
					Main.GameViewMatrix.TransformationMatrix
				);

				var outline = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmHead_Outline").Value;

				Main.EntitySpriteDraw(
					outline,
					position,
					null,
					NPC.GetAlpha(Color.White),
					NPC.rotation,
					headOrigin,
					NPC.scale,
					SpriteEffects.None
				);
				return false;
			}
		}

		[LongDistanceNetSync(SyncWith = typeof(PrimordialWyrmHead))]
		public class PrimordialWyrmBody : ModNPC // Would we need an alternating segment? maybe not
		{
			public override LocalizedText DisplayName => CalamityUtils.GetText("NPCs.PrimordialWyrmHead.DisplayName");

			public override void SetDefaults()
			{
				NPC.damage = 0; // No contact damage
				NPC.width = 90;
				NPC.height = 130;
				NPC.defense = 0;
				NPC.lifeMax = 3_000_000;
				NPC.aiStyle = -1;
				AIType = -1;
				NPC.knockBackResist = 0f;
				NPC.behindTiles = true;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				NPC.HitSound = SoundID.NPCHit1;
				NPC.DeathSound = SoundID.NPCDeath6;
				NPC.netAlways = true;
				NPC.dontCountMe = true;
				NPC.dontTakeDamage = true;
				NPC.chaseable = false;
			}
			
			// Same as the head, will be replaced at a later date
			// Again, credit Calamity Mod
			public override void AI()
			{
				if (NPC.ai[2] > 0f)
					NPC.realLife = (int)NPC.ai[2];

				// Check if other segments are still alive. If not, die.
				int wyrmHeadID = ModContent.NPCType<PrimordialWyrmHead>();
				bool shouldDespawn = !NPC.AnyNPCs(wyrmHeadID);
				if (!shouldDespawn)
				{
					if (NPC.ai[1] <= 0f)
						shouldDespawn = true;
					else if (Main.npc[(int)NPC.ai[1]].life <= 0)
						shouldDespawn = true;
				}
				if (shouldDespawn)
				{
					NPC.life = 0;
					NPC.HitEffect(0, 10.0);
					NPC.checkDead();
					NPC.active = false;
				}

				// Decide segment offset stuff.
				NPC aheadSegment = Main.npc[(int)NPC.ai[1]];
				Vector2 directionToNextSegment = aheadSegment.Center - NPC.Center;
				if (aheadSegment.rotation != NPC.rotation)
				{
					directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - NPC.rotation) * 0.08f);
					directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - NPC.rotation).ToRotationVector2(), 1f);
				}

				NPC.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
				NPC.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * NPC.scale * NPC.width;
				NPC.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
			}

			public override bool CheckActive() => false;

			public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

			public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
			{
				var texture = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmBody").Value;

				var position = NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY);

				Main.EntitySpriteDraw(
					texture,
					position,
					null,
					NPC.GetAlpha(drawColor),
					NPC.rotation,
					texture.Size() / 2f,
					NPC.scale,
					SpriteEffects.None
				);

				var glow = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmBody_Glow").Value;

				spriteBatch.End();
				spriteBatch.Begin(
					SpriteSortMode.Deferred,
					BlendState.Additive,
					Main.DefaultSamplerState,
					default,
					Main.Rasterizer,
					default,
					Main.GameViewMatrix.TransformationMatrix
				);

				Main.EntitySpriteDraw(
					glow,
					position,
					null,
					NPC.GetAlpha(Color.White),
					NPC.rotation,
					glow.Size() / 2f,
					NPC.scale,
					SpriteEffects.None
				);

				spriteBatch.End();
				spriteBatch.Begin(
					default,
					default,
					Main.DefaultSamplerState,
					default,
					Main.Rasterizer,
					default,
					Main.GameViewMatrix.TransformationMatrix
				);

				var outline = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmBody_Outline").Value;

				Main.EntitySpriteDraw(
					outline,
					position,
					null,
					NPC.GetAlpha(Color.White),
					NPC.rotation,
					outline.Size() / 2f,
					NPC.scale,
					SpriteEffects.None
				);

				return false;
			}
		}

		[LongDistanceNetSync(SyncWith = typeof(PrimordialWyrmHead))]
		public class PrimordialWyrmTail : ModNPC
		{
			public override LocalizedText DisplayName => CalamityUtils.GetText("NPCs.PrimordialWyrmHead.DisplayName");

			public override void SetDefaults()
			{
				NPC.damage = 0; // No contact damage
				NPC.width = 80;
				NPC.height = 100;
				NPC.defense = 0;
				NPC.lifeMax = 3_000_000;
				NPC.aiStyle = -1;
				AIType = -1;
				NPC.knockBackResist = 0f;
				NPC.behindTiles = true;
				NPC.noGravity = true;
				NPC.noTileCollide = true;
				NPC.HitSound = SoundID.NPCHit1;
				NPC.DeathSound = SoundID.NPCDeath6;
				NPC.netAlways = true;
				NPC.dontCountMe = true;
				NPC.dontTakeDamage = true;
				NPC.chaseable = false;
			}

			// Same as the head, will be replaced at a later date
			// Again, credit Calamity Mod
			public override void AI()
			{
				if (NPC.ai[2] > 0f)
					NPC.realLife = (int)NPC.ai[2];

				// Check if other segments are still alive. If not, die.
				int wyrmHeadID = ModContent.NPCType<PrimordialWyrmHead>();
				bool shouldDespawn = !NPC.AnyNPCs(wyrmHeadID);
				if (!shouldDespawn)
				{
					if (NPC.ai[1] <= 0f)
						shouldDespawn = true;
					else if (Main.npc[(int)NPC.ai[1]].life <= 0)
						shouldDespawn = true;
				}
				if (shouldDespawn)
				{
					NPC.life = 0;
					NPC.HitEffect(0, 10.0);
					NPC.checkDead();
					NPC.active = false;
				}

				// Decide segment offset stuff.
				NPC aheadSegment = Main.npc[(int)NPC.ai[1]];
				Vector2 directionToNextSegment = aheadSegment.Center - NPC.Center;
				if (aheadSegment.rotation != NPC.rotation)
				{
					directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - NPC.rotation) * 0.08f);
					directionToNextSegment = directionToNextSegment.MoveTowards((aheadSegment.rotation - NPC.rotation).ToRotationVector2(), 1f);
				}

				NPC.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
				NPC.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * NPC.scale * NPC.width;
				NPC.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();
			}

			public override bool CheckActive() => false;

			public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

			public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
			{
				var texture = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmTail").Value;

				var position = NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY);

				Main.EntitySpriteDraw(
					texture,
					position,
					null,
					NPC.GetAlpha(drawColor),
					NPC.rotation,
					texture.Size() / 2f,
					NPC.scale,
					SpriteEffects.None
				);

				var glow = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmTail_Glow").Value;

				spriteBatch.End();
				spriteBatch.Begin(
					SpriteSortMode.Deferred,
					BlendState.Additive,
					Main.DefaultSamplerState,
					default,
					Main.Rasterizer,
					default,
					Main.GameViewMatrix.TransformationMatrix
				);

				Main.EntitySpriteDraw(
					glow,
					position,
					null,
					NPC.GetAlpha(Color.White),
					NPC.rotation,
					glow.Size() / 2f,
					NPC.scale,
					SpriteEffects.None
				);

				spriteBatch.End();
				spriteBatch.Begin(
					default,
					default,
					Main.DefaultSamplerState,
					default,
					Main.Rasterizer,
					default,
					Main.GameViewMatrix.TransformationMatrix
				);

				var outline = ModContent.Request<Texture2D>("AbyssOverhaul/Content/NPCs/Bosses/PrimordialWyrm/PrimordialWyrmTail_Outline").Value;

				Main.EntitySpriteDraw(
					outline,
					position,
					null,
					NPC.GetAlpha(Color.White),
					NPC.rotation,
					outline.Size() / 2f,
					NPC.scale,
					SpriteEffects.None
				);

				return false;
			}
		}
	}
}
