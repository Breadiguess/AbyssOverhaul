using BreadLibrary.Common.Graphics;
using CalamityMod.Items.Accessories.Wings;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;
using static Daybreak.Common.Features.Hooks.GlobalInfoDisplayHooks;

namespace AbyssOverhaul.Content.Items.Accessories.OmegaBlue_Wings
{
    [AutoloadEquip(EquipType.Wings)]
    internal class OmegaBlue_SailFins : BaseWings
    {

        public static string Path => ModContent.GetInstance<OmegaBlue_SailFins>().GetPath();
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.accessory = true;
            Item.rare = ModContent.RarityType<PureGreen>();
        }
        public override float BonusAscentWhileFalling => 2f;
        public override float BonusAscentWhileRising => 0.15f;
        public override float RisingSpeedThreshold => 1f;
        public override float MaxAscentSpeed => 1.805f;
        public override float BaseAscent => 0.125f;

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<OmegaBlue_Tails>().Active = true;

            if (player.wet)
            {
                player.wingTime = 221;
            }
        }
        public override void UpdateVisibleAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<OmegaBlue_Tails>().Active = !hideVisual;
        }
        public override void SetStaticDefaults() => ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(220, 8f, 2f);


        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            base.HorizontalWingSpeeds(player, ref speed, ref acceleration);
        }



        public bool JustTookOff;
        public int TakeoffTimer;
        public int FlapTimer;
        public int IdleTimer;

        public override bool WingUpdate(Player player, bool inUse)
        {
            const int IdleStart = 0;
            const int IdleFrames = 7;

            const int TakeoffStart = 7;
            const int TakeoffFrames = 4;

            const int FlapStart = 11;
            const int FlapFrames = 7;

            const int ExhaustedFrame = 19;

            bool grounded = player.velocity.Y == 0f && !inUse;

            // Fully reset the takeoff state only once the player is actually back out of wing use.
            if (grounded)
            {
                JustTookOff = false;
                TakeoffTimer = 0;
                FlapTimer = 0;

                IdleTimer++;
                player.wingFrame = IdleStart + (IdleTimer / 6) % IdleFrames;
                return true;
            }

            IdleTimer = 0;

            // No wing time left: force exhausted frame.
            if (player.wingTime <= 0)
            {
                player.wingFrame = ExhaustedFrame;
                return true;
            }

            // Start takeoff once.
            if (inUse && !JustTookOff)
            {
                JustTookOff = true;
                TakeoffTimer = 0;
                FlapTimer = 0;
            }

            // Startup / takeoff animation.
            if (inUse && TakeoffTimer < 20)
            {
                TakeoffTimer++;

                // Custom timing per frame for a more dramatic startup.
                // Frame durations:
                // 7 -> 4 ticks
                // 8 -> 4 ticks
                // 9 -> 8 ticks  (emphasis frame)
                // 10 -> 4 ticks
                if (TakeoffTimer < 2)
                    player.wingFrame = TakeoffStart + 0;
                else if (TakeoffTimer < 6)
                    player.wingFrame = TakeoffStart + 1;
                else if (TakeoffTimer < 14)
                    player.wingFrame = TakeoffStart + 2;
                else
                    player.wingFrame = TakeoffStart + 3;

                return true;
            }

            // Main flap loop.
            if (inUse)
            {
                FlapTimer++;
                player.wingFrame = FlapStart + (FlapTimer / 4) % FlapFrames;
                return true;
            }

            player.wingFrame = IdleStart;
            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SoulofFlight, 20).
                AddIngredient<ReaperTooth>(5).
                AddTile(TileID.MythrilAnvil).
                Register();
        }

    }

    public class OmegaBlue_SailFins_Layer : PlayerDrawLayer
    {
        public static Asset<Texture2D> sailFin_Tex;

        public override void Load()
        {
            sailFin_Tex = ModContent.Request<Texture2D>($"{OmegaBlue_SailFins.Path}_Wings_Real");
        }

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Wings);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.wings == EquipLoader.GetEquipSlot(Mod, "OmegaBlue_SailFins", EquipType.Wings);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;

            if (drawPlayer.dead)
                return;
            Texture2D texture = sailFin_Tex.Value;
            Vector2 Position = drawInfo.Position;
            Vector2 pos = drawInfo.BodyPosition() - new Vector2(12*drawPlayer.direction,-5);// new Vector2((int)(Position.X - Main.screenPosition.X + drawPlayer.width / 2 - 2 * drawPlayer.direction), (int)(Position.Y - Main.screenPosition.Y + (drawPlayer.height / 2 + drawPlayer.HeightOffsetVisual / 2f) - 2f * drawPlayer.gravDir));
            Color lightColor = Lighting.GetColor((int)drawPlayer.Center.X / 16, (int)drawPlayer.Center.Y / 16, Color.White);
            Color color = lightColor * (1 - drawInfo.shadow);
            if (drawPlayer.TryGetModPlayer<OmegaBlue_Tails>(out var tails))
            {
                Rectangle Frame = texture.Frame(1, 20, 0, drawPlayer.wingFrame);
                DrawData d = new DrawData(texture, pos, Frame, color, 0f, Frame.Size()/2f, 1f, drawInfo.playerEffect, 0);
                d.shader = drawInfo.drawPlayer.cWings;
                drawInfo.DrawDataCache.Add(d);
            }
            
        
        }
    }
}
