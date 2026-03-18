using BreadLibrary.Common.Graphics;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.Items.Accessories.SeaShell
{
    internal class SeaShellPlayerDrawLayer : PlayerDrawLayer
    {
        public Asset<Texture2D> shell;

        public override void Load()
        {
            shell = ModContent.Request<Texture2D>("AbyssOverhaul/Content/Items/Accessories/SeaShell/seashell_eq");
        }
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.Backpacks);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            SeashellPlayer a = drawInfo.drawPlayer.GetModPlayer<SeashellPlayer>();


            return a.Active && a.HasShell && a.Visible;
                }


        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Vector2 DrawPos = drawInfo.BodyPosition() + new Vector2(11*-drawInfo.drawPlayer.direction,8);

            var tex = this.shell.Value;

            float Rot = MathHelper.PiOver4 * -drawInfo.drawPlayer.direction;
            Vector2 Origin = new Vector2(tex.Width/2, tex.Height/2);
            DrawData shell = new DrawData(tex, DrawPos, null,Color.White, Rot, Origin, 1, drawInfo.drawPlayer.direction.ToSpriteDirection() );
            shell.shader = drawInfo.cBackpack;
            shell.color = drawInfo.colorArmorBody;
            drawInfo.DrawDataCache.Add(shell);
            
        }
    }
}
