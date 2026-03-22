using BreadLibrary.Core.Verlet;
using CalamityMod;
using CalamityMod.Projectiles.Magic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Events;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.AnchorageSystem
{
    internal class AnchorageHeld : ModProjectile
    {
        public VerletChain rope;
        public Projectile drillProjectile;

        public ref Player Owner => ref Main.player[Projectile.owner];
        public bool hasDrill;
        public static Asset<Texture2D> drillTex;
        public enum state
        {
            Idle,
            Charging,
            Firing,
            Recovering,
            Rest
        }

        public state currentState 
        {
            get => (state)Projectile.ai[1];
            set => Projectile.ai[1] = (float)value;
        }

        private int counter = 0;
        public const int cap = 60;
        public float chargeInterpolant => counter / (float)cap;

        public bool hasChargeFinished;

        public override void Load()
        {
            string path = this.GetPath();
            drillTex = ModContent.Request<Texture2D>($"{path}_Drill");

            base.Load();
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            CheckPlayer();
            stateMachine();

            rope ??= new(20, 5, Projectile.Center);

            if (drillProjectile is not null)
            {
                rope.Positions[^1] = drillProjectile.Center;
                rope.Simulate(Vector2.Zero, Projectile.Center, 0, 0.5f, collideWithTiles: false);
            }

            rope.Positions[0] = Projectile.Center;

            counter = Math.Min(counter, cap);

            Projectile.Center = Owner.Center;
            Projectile.velocity = Owner.DirectionTo(Owner.Calamity().mouseWorld) * 10;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        #region StateMachine
        private void stateMachine()
        {
            switch (currentState)
            {
                case state.Idle:
                    manageIdle();
                    break;
                case state.Charging:
                    manageCharging();
                    break;
                case state.Firing:
                    manageFiring();
                    break;
                case state.Recovering:
                    manageRecovering();
                    break;
                case state.Rest:
                    manageRest();
                    break;
            }
        }

        private void manageIdle()
        {
            if(Owner.controlUseItem)
            {
                currentState = state.Charging;
                Owner.StartChanneling();
            }

            counter = 0;
            hasDrill = true;
            hasChargeFinished = false;
        }

        private void manageCharging()
        {
            if (!Owner.channel)
            {
                if (counter < cap)
                {
                    currentState = state.Idle;
                } else
                {
                    currentState = state.Firing;
                }
            }

            counter++;

            if (counter == cap && !hasChargeFinished)
            {
                SoundEngine.PlaySound(Assets.Sounds.Items.Ranged.AnchorageSystem.AnchorageCharged.Asset with { pitchVariance = 0.2f }, Projectile.Center);
                hasChargeFinished = true;
            }
        }

        private void manageFiring()
        {
            if (hasDrill)
            {
                hasDrill = false;

                Projectile proj = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<AnchorageHeld_Drill>(), this.Projectile.damage, this.Projectile.knockBack);
                drillProjectile = proj;
            }
        }

        private void manageRecovering()
        {

        }

        private void manageRest()
        {

        }

        #endregion

        private void CheckPlayer()
        {
            if (Owner.HeldItem.type == ModContent.ItemType<AnchorageItem>() && !Owner.dead)
            {
                Projectile.timeLeft = 2;
                Owner.heldProj = this.Projectile.whoAmI;
                Owner.direction = Projectile.velocity.X.DirectionalSign();
                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation);
            }
        }


        #region DrawCode
        public override bool PreDraw(ref Color lightColor)
        {
            var tex = TextureAssets.Projectile[Type].Value;

            Rectangle frame = tex.Frame(1, Main.projFrames[Type]);

            Vector2 offset = new(0, frame.Height / 2 + 6);

            drawHead(ref lightColor, offset);

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos, frame, lightColor, Projectile.rotation, frame.Size()/2, Projectile.scale, 0);

            string msg = "";

            msg += currentState.ToString() + $"\n";
            msg += counter.ToString();
            Utils.DrawBorderString(Main.spriteBatch, msg, drawPos, Color.White);

            return false;
        }

        private void drawHead(ref Color lightColor, Vector2 offset)
        {
            if (!hasDrill)
            {
                return;
            }

            var tex = drillTex.Value;
            int FrameCount = Main.projFrames[ModContent.ProjectileType<AnchorageHeld_Drill>()];

            Rectangle frame = tex.Frame(1, FrameCount, 0, (int)(Main.GameUpdateCount % FrameCount * chargeInterpolant)); 

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(tex, drawPos + offset.RotatedBy(Projectile.rotation), frame, lightColor, Projectile.rotation, frame.Size()/2, Projectile.scale, 0);
        }
        #endregion
    }
}
