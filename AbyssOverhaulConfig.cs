using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AbyssOverhaul
{
	public class AbyssOverhaulClientConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[DefaultValue(true)]
		public bool UniqueRarityFont { get; set; }
	}
}
