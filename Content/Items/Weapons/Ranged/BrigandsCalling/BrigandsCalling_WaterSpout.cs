
using Terraria.GameContent;

namespace AbyssOverhaul.Content.Items.Weapons.Ranged.BrigandsCalling
{
    internal class BrigandsCalling_WaterSpout : ModProjectile
    {
        public enum State
        {
            WaitingForCollection,

            AttackNearbyEnemies
        }

        public State CurrentState
        {
            get => (State)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }

        public int Time;
        public ref Player Owner => ref Main.player[Projectile.owner];



        public BrigandsCalling_Player BrigandsCalling_Player => Owner.GetModPlayer<BrigandsCalling_Player>();
        public bool Active => CurrentState == State.AttackNearbyEnemies;

        public float Depth => Projectile.ai[2];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] =4;
        }
        public override void SetDefaults()
        {
            Projectile.hostile = false;
            Projectile.friendly = true;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.width = 60;
            Projectile.height = 100;

            Projectile.timeLeft = 1200;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }


        public override bool? CanDamage() => Active;
        public override bool? CanCutTiles() => Active;

        public override void AI()
        {
            Projectile.Opacity = float.Lerp(Projectile.Opacity, 1, 0.1f);
            if (!Active)
            {

                foreach (Player player in Main.ActivePlayers)
                {
                    if (player.Distance(Projectile.Center) < 50)
                    {

                        Owner.AddBuff(ModContent.BuffType<BrigandsCalling_Buff>(), 60* 9);
                        CurrentState = State.AttackNearbyEnemies;
                        break;
                    }
                }
            }
            else
            {

                float RadiusX = 90;
                float RadiusY = 10;
               
                float AdjTime = Main.GameUpdateCount *0.05f+BrigandsCalling_Player.WaterSpouts.FindIndex(a => a.Equals(Projectile)); 
                Vector2 orbitOffset = new(
                    MathF.Cos(AdjTime) * RadiusX,
                    MathF.Sin(AdjTime) * RadiusY
                );
                Projectile.ai[2] = MathF.Sin(AdjTime);

                Projectile.Center = Vector2.Lerp(Projectile.Center, Owner.Center + orbitOffset, 0.4f);



                NPC target = Projectile.FindTargetWithinRange(1200, true);
                if (target is null)
                {
                    Projectile.velocity = Vector2.Zero;
                }
                else
                {
                    Projectile.velocity = Projectile.DirectionTo(target.Center) * 10;
                }
            }

            Time++;

        }


        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if(Depth>0)
               overPlayers.Add(index);
            
        }


        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D Tex = TextureAssets.Projectile[Type].Value;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Rectangle Frame = Tex.Frame(1, 4, 0, (int)((Main.GlobalTimeWrappedHourly*15.1f + Projectile.whoAmI) % 4));


            Color adjust = Color.Lerp(Color.White, Color.Blue, MathF.Sin(Main.GlobalTimeWrappedHourly * 15.1f + Projectile.whoAmI));
            adjust.MultiplyRGB(lightColor);
            Main.EntitySpriteDraw(Tex, drawPos, Frame, adjust*Projectile.Opacity, 0, Frame.Size() / 2, 1, 0);

            return false;
        }

    }
}
