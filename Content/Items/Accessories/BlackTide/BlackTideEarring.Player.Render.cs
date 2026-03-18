using AbyssOverhaul.Content.Items.Accessories.OmegaBlue_Wings;
using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.Items.Accessories.BlackTide
{
    internal class BlackTideEarringDrawlayer : IPlayerPixelatedDrawer
    {
        public static Asset<Texture2D> Moon = ModContent.Request<Texture2D>("AbyssOverhaul/Assets/Textures/Moon");
        public PixelLayer PixelLayer => PixelLayer.AboveProjectiles;

        public bool IsActive(Player player)
        {
            if (!player.active || player.dead)
                return false;

            BlackTideEarringPlayer tailsPlayer = player.GetModPlayer<BlackTideEarringPlayer>();
            return
                   tailsPlayer.Active;
        }


        
        public void DrawPixelated(Player player, SpriteBatch spriteBatch)
        {


            Vector2 DrawPos = player.Center - Main.screenPosition + new Vector2(40 * player.direction,-40);


            Main.EntitySpriteDraw(Moon.Value, DrawPos, null, Color.White, 0, Moon.Value.Size() / 2f, 0.05f, 0);

          
        }

       
    }

    [Autoload(Side = ModSide.Client)]
    internal sealed class BlackTideEarringLoad : ModSystem
    {
        private static BlackTideEarringDrawlayer drawer;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            drawer = new BlackTideEarringDrawlayer();
            PlayerPixelRegistry.Register(drawer);
        }

        public override void Unload()
        {
            if (!Main.dedServ && drawer is not null)
                PlayerPixelRegistry.Unregister(drawer);

            drawer = null;
        }
    }
}
