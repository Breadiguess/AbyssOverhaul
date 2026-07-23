using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Armor.Silva
{
	public class SilvaEffectsEditor
	{
		public static bool SilvaReviveSpawnsChloroDusts() => true; // Whether the armour can produce chlorothyte dusts (its normal player effects)

		public static void SilvaReviveEffects(Player player) // for doing effects when the revive buff is active
		{
		}

		public static void SilvaReviveEndEffect(Player player) // for doing effects when the revive "ends" (SilvaReviveEffects is called 1 frame after this is called, do keep that in mind)
		{
		}

		public static void SilvaReviveStartEffect(Player player) // For doing effects when the revive is activated
		{
		}
	}
}
