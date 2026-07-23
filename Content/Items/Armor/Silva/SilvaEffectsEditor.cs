using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Armor.Silva
{
	public class SilvaEffectsEditor
	{
		public static bool SilvaReviveSpawnsChloroDusts() => false;

		public static void SilvaReviveEffects(Player player)
		{
			Main.NewText("SILVA EFFECTS");
		}

		public static void SilvaReviveEndEffect(Player player)
		{
			Main.NewText("SILVA EFFECTS End");
		}

		public static void SilvaReviveStartEffect(Player player)
		{
			Main.NewText("SILVA EFFECTS Start");
		}
	}
}
