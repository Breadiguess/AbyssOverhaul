using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Subworlds;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content
{
    internal class DebugPlayer : PlayerDrawLayer
    {

        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.ArmOverItem);
        }


        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            string msg = $"";
            Player player = drawInfo.drawPlayer;
            PressurePlayer a = player.GetModPlayer<PressurePlayer>();
            msg += $"Adaption: {a.Adaptation}\n";
            msg += $"Pressure Resistance: {a.PressureResistance}\n";
            msg += $"PressureStress: {a.PressureStress}\n";
            msg += $"ambientPressure: {a.AmbientPressure}\n";
            if (a.CurrentLayer is not null)
                msg += $"Layer: {a.CurrentLayer.FullName}\n";

            msg += $"";
            if (a.CurrentLayer is not null)
            Utils.DrawBorderString(Main.spriteBatch, msg, player.Center - Main.screenPosition + Vector2.UnitY * 60, Color.White, 0.5f);
        }
    }
}
