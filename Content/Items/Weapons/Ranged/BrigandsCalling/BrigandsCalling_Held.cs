using BreadLibrary.Core.Graphics.Particles;
using CalamityMod;
using Terraria.Audio;
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.BrigandsCalling
{
    internal class BrigandsCalling_Held : ModProjectile
    {
        public ref Player Owner => ref Main.player[Projectile.owner];
        public BrigandsCalling_Player Brigands => Owner.GetModPlayer<BrigandsCalling_Player>();
        public int Time
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public bool IsLeftHand
        {
            get => Projectile.ai[1] == 1f;
            set => Projectile.ai[1] = value ? 1f : 0f;
        }

        public int HandIdentitySign => IsLeftHand ? -1 : 1;
        
        public int VisualSideSign => HandIdentitySign * Projectile.direction;

        public float Recoil;
        public float Dip;


        private float BaseFireRate = 200;
        public float RPM => BaseFireRate + Brigands.RPMBoost;

        public int ShotInterval => Math.Max(1, (int)MathF.Round(3600f / RPM));

        public int HandPhaseOffset => IsLeftHand ? ShotInterval / 2 : 0;

        public static Asset<Texture2D> AltGun;

        public override void Load()
        {
            string path = this.GetPath() + "_Alt";
            AltGun = ModContent.Request<Texture2D>(path);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 2;
            Projectile.Size = new(10);
        }

        public override bool PreAI()
        {
            DoPlayerCheck();
            return true;
        }

        public float BaseRotation => Projectile.rotation;

        public float Rotation => BaseRotation + Recoil * -0.42f * Projectile.direction + Dip * -Projectile.direction * 0.08f;

        public Vector2 aimDirection => (Brigands.ForcedTarget is not null && Brigands.ForceuseItemTime > 0) ? Owner.MountedCenter.DirectionTo(Brigands.ForcedTarget.Center) : Owner.MountedCenter.DirectionTo(Owner.Calamity().mouseWorld);
        public override void AI()
        {
            Time++;


            // Keep projectile facing synced to the player's current direction.
            // This is what makes the pistols swap sides when the player turns around.
            Projectile.direction = Owner.direction;
            Owner.direction = aimDirection.X.DirectionalSign();

            Projectile.rotation = aimDirection.ToRotation();
            Projectile.velocity = aimDirection * (IsLeftHand ? 12f : 16f);
            Projectile.CritChance = Owner.HeldItem.crit;
            UpdateHeldPosition();
            UpdateFiring();
            UpdateRecoilVisuals();
            UpdateArm();
        }

        private void UpdateHeldPosition()
        {
            // Offset from the player body.
            // VisualSideSign changes automatically when Owner.direction flips,
            // so the guns swap sides without changing which gun is "left hand".
            Vector2 sideOffset = new Vector2(4f * HandIdentitySign, (IsLeftHand ? 2f : -2f)*Projectile.direction);
            sideOffset = sideOffset.RotatedBy(BaseRotation);

            Projectile.Center = Owner.MountedCenter + sideOffset;
            
        }

        private void UpdateFiring()
        {
            bool forcedFire = Brigands.ForceuseItemTime > 0;
            bool manualFire = Owner.controlUseItem && Owner.altFunctionUse != 2;

            if (!forcedFire && !manualFire)
                return;

            if (ShotInterval <= 0)
                return;

            // Shared clock keeps both guns phase-locked.
            int firingClock = (int)Main.GameUpdateCount + HandPhaseOffset;

            if (firingClock % ShotInterval == 0)
            {
                FireShot();
                if (!forcedFire)
                    Recoil = 1f;
                else
                    Recoil = 0.2f;
                    Dip = 0f;
            }
        }

        private void FireShot()
        {
            Item weapon = Owner.HeldItem;
            if (weapon == null || weapon.IsAir)
                return;
            Vector2 muzzleDirection = Projectile.rotation.ToRotationVector2();

            // Muzzle shifts to the visual side, so it also swaps correctly when turning around.
            Vector2 muzzleOffset =
                muzzleDirection * 24f +
                muzzleDirection.RotatedBy(MathHelper.PiOver2) * (4f * VisualSideSign);

            Vector2 spawnPos = Projectile.Center + muzzleOffset;
            Vector2 shotVelocity = muzzleDirection * 16f;





            int ammoItemId = 0;
            float shootSpeed = weapon.shootSpeed;
            int projectileType = weapon.shoot;
            int damage = Projectile.damage;
            float knockback = Projectile.knockBack;
            bool canShoot = false;
            var Item = Owner.ChooseAmmo(Owner.HeldItem);
            Owner.PickAmmo(
               weapon,
               ref ammoItemId,
               ref shootSpeed,
               ref canShoot,
               ref damage,
               ref knockback,
               out int usedAmmoItemId,
               false
           );

            projectileType = Item.shoot;

            if (!canShoot)
                return;


            SoundEngine.PlaySound(
                Assets.Sounds.Items.Ranged.BrigandsCalling.BrigandsCallingFire.Asset with
                {
                    Volume = 0.4f,
                    MaxInstances = 0,
                    pitchVariance = 0.4f
                },
                Projectile.Center
            );


            BrigandsCalling_MuzzleFlash particle = new BrigandsCalling_MuzzleFlash();
            particle.Prepare(spawnPos+shotVelocity, shotVelocity.ToRotation(), 15, Projectile.direction);
            ParticleEngine.ShaderParticles.Add(particle);

            Lighting.AddLight(spawnPos + shotVelocity, TorchID.Ice);
            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis($"BrigandsCalling" + (Brigands.ForceuseItemTime > 0 ? "SuperHome" :"")),
                spawnPos,
                shotVelocity,
                projectileType,
                damage,
                knockback,
                Owner.whoAmI
            );

            if (proj.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[proj].DamageType = weapon.DamageType;
                Main.projectile[proj].ApplyStatsFromSource(Projectile.GetItemSource_FromThis());

            }

            Owner.ApplyItemTime(weapon);
            Owner.ApplyItemAnimation(weapon);

            if (ammoItemId > 0)
                Owner.ConsumeItem(ammoItemId);
        }

        private void UpdateRecoilVisuals()
        {
            if (Recoil > 0f)
                Recoil *= 0.78f;
            else
                Recoil = 0f;

            float dipIn = Utils.GetLerpValue(0.15f, 0.45f, 1f - Recoil, true);
            float dipOut = 1f - Utils.GetLerpValue(0.45f, 0.85f, 1f - Recoil, true);
            Dip = dipIn * dipOut * 0.35f;

            if (Recoil < 0.001f)
            {
                Recoil = 0f;
                Dip = 0f;
            }
        }

        private void UpdateArm()
        {
            float armRotation = Rotation - MathHelper.PiOver2 + MathHelper.ToRadians(30f + HandPhaseOffset) * Projectile.direction;

            bool useFrontArm = IsLeftHand;

            if (useFrontArm)
            {
                Owner.SetCompositeArmFront(
                    true,
                    GetStretch(Recoil),
                    armRotation
                );
            }
            else
            {
                Owner.SetCompositeArmBack(
                    true,
                    GetStretch(Recoil),
                    armRotation
                );
            }
        }
        private Player.CompositeArmStretchAmount GetStretch(float Interp)
        {

            if (Interp > .4f)
            {
                return Player.CompositeArmStretchAmount.Quarter;
            }
            else if (Interp > 0.2f)
                return Player.CompositeArmStretchAmount.ThreeQuarters;
            else
                return Player.CompositeArmStretchAmount.Full;



                return Player.CompositeArmStretchAmount.None;
        }


        private void DoPlayerCheck()
        {
            if (Owner.HeldItem.type == ModContent.ItemType<BrigandsCalling_Item>() && !Owner.dead)
            {
                Projectile.timeLeft = 2;
                Owner.heldProj = Projectile.whoAmI;
            }
        }
        public override bool? CanHitNPC(NPC target) => false;

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = IsLeftHand ? TextureAssets.Projectile[Type].Value : AltGun.Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = new Vector2(10f, tex.Height);

            SpriteEffects flip = Projectile.direction == -1
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            float rot = Rotation;

            float xOffset = -10f * Recoil;
            float yOffset = tex.Height / 2f - 10f * Projectile.direction * Recoil + 6f * Dip;

            Vector2 adjustedDrawPos = drawPos + new Vector2(xOffset, yOffset).RotatedBy(rot);

            Main.EntitySpriteDraw(tex, adjustedDrawPos, null, lightColor, rot, origin, 1f, flip);

            if(IsLeftHand)
            Utils.DrawBorderString(Main.spriteBatch, Recoil.ToString(), drawPos, Color.White, anchory:-1);
            return false;
        }
    }
}