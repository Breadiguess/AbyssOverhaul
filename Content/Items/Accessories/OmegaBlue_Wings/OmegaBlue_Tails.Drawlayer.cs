using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;

namespace AbyssOverhaul.Content.Items.Accessories.OmegaBlue_Wings
{
    internal class OmegaBlue_TailsLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Wings);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.wings == EquipLoader.GetEquipSlot(Mod, "OmegaBlue_SailFins", EquipType.Wings);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player Owner = drawInfo.drawPlayer;
            return;
            if(Owner.TryGetModPlayer<OmegaBlue_Tails>(out var _Tails))
            {
                if(_Tails.Tails is not null)
                    foreach (var tail in _Tails.Tails)
                    {
                        if(tail is not null)
                        for (int i = 0; i < tail.Positions.Length - 1; i++)
                        {
                           Utils.DrawLine(Main.spriteBatch, tail.Positions[i], tail.Positions[i + 1], Color.White);
                        }

                    }
            }
            
        }
    }
}
