using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ID;

namespace AbyssOverhaul.Content.Items.Accessories.PetriDish
{
    public class MicrobeGlob : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.timeLeft = 300;

            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public enum State
        {
            Fly,
            Fall,
            Stationary
        };

        public State CurrentState;
        public bool FirstFallTick = false;
        public bool FirstIdleTick = false;

        public override void AI()
        {
            Projectile.ai[0]++;
            Tile left = Framing.GetTileSafely(Projectile.Center + new Vector2(18, 0));
            Tile right = Framing.GetTileSafely(Projectile.Center + new Vector2(-18, 0));
            Tile bottom = Framing.GetTileSafely(Projectile.Center + new Vector2(0, 18));

            if (Projectile.ai[0] >= 60)
            {
                if ((left.HasTile || right.HasTile))
                {
                    if (!bottom.HasTile)
                    {
                        CurrentState = State.Fall;
                    }
                }

                if (bottom.HasTile)
                {
                    CurrentState = State.Stationary;
                }
            }

            switch (CurrentState)
            {
                case State.Fly:
                    {
                        Projectile.frame = 0;
                        Projectile.velocity.Y += 0.1f;
                        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                        break;
                    }
                case State.Fall:
                    {
                        Projectile.frame = 1;
                        Projectile.velocity.Y += 0.5f;
                        Projectile.velocity.X = 0;
                        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                        if (!FirstFallTick)
                        {
                            SoundEngine.PlaySound(SoundID.NPCDeath9, Projectile.Center);
                            FirstFallTick = true;
                        }
                        break;
                    }
                case State.Stationary:
                    {
                        Projectile.frame = 2;
                        Projectile.velocity.X = 0;
                        Projectile.velocity.Y += 0.01f;
                        Projectile.rotation = 0;

                        if (!FirstIdleTick)
                        {
                            SoundEngine.PlaySound(SoundID.NPCDeath9, Projectile.Center);
                            FirstIdleTick = true;
                        }
                        break;
                    }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (CurrentState == State.Fly)
            {
                for (int t = 0; t < 4; t++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.t_Flesh, Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f), 50, default, 0.8f);
                }
            }
            return false;
        }

        public static SoundStyle KillSound = new SoundStyle("AbyssOverhaul/Assets/Sounds/Items/Accessory/PetriDishBlobKill");
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(KillSound, Projectile.Center);
            for (int t = 0; t < 4; t++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.t_Flesh, Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f), 50, default, 0.8f);
            }
        }
    }
}
