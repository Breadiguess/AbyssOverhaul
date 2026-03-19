using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.Items.Weapons.Melee.Eschaton
{
    internal class EschatonSoulProjectile : ModProjectile
    {
        public override string Texture =>
            "AbyssOverhaul/Content/Items/Weapons/Melee/Eschaton/GhastlySoulSmall";
        public enum SoulType
        {
            Small,
            Medium,
            Large
        }
        public static List<Asset<Texture2D>> Textures;


        public SoulType CurrentStage
        {
            get => (SoulType)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        public NPC target
        {
            get => TargetWhoami != -1 ? Main.npc[TargetWhoami] : null;

        }
        public int TargetWhoami
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }


        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 3;

            string path = "AbyssOverhaul/Content/Items/Weapons/Melee/Eschaton/GhastlySoul";
            Textures = new List<Asset<Texture2D>>();
            foreach (var val in Enum.GetValues<SoulType>())
            {
                var a = ModContent.Request<Texture2D>(path + val.ToString());
                Textures.Add(a);
            }
        }

        public override void SetDefaults()
        {
            Projectile.Size = new(50);
            Projectile.DamageType = ModContent.GetInstance<TrueMeleeNoSpeedDamageClass>();
            Projectile.maxPenetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
        }
        public override void OnSpawn(IEntitySource source)
        {

        }

        public override void AI()
        {
            if (target is not null)
            {
                if (!target.active)
                {

                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * 10;
                    TargetWhoami = -1;
                    return;
                }


                Projectile.Center = target.Center + new Vector2(0, target.Size.Length() * 1.4f).RotatedBy(Main.GameUpdateCount * 0.02f + Projectile.whoAmI);
                Projectile.rotation = Projectile.Center.AngleTo(target.Center + new Vector2(-10, target.Size.Length()).RotatedBy(Main.GameUpdateCount * 0.02f + Projectile.whoAmI));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Textures[(int)CurrentStage].Value;

            Vector2 DrawPos = Projectile.Center - Main.screenPosition;
            Rectangle Frame = tex.Frame(1, 5, 0, (int)(Main.GlobalTimeWrappedHourly * 10.1f % 5));
            Main.EntitySpriteDraw(tex, DrawPos, Frame, Color.White, Projectile.rotation, Frame.Size() / 2, Projectile.scale, Projectile.spriteDirection.ToSpriteDirection());


            return false;
        }
    }
}
