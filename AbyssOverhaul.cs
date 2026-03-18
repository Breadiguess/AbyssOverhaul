global using AbyssOverhaul.Core;
global using Microsoft.Xna.Framework;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using Terraria;
global using Terraria.ID;
global using Terraria.GameContent.Generation;
global using Terraria.IO;
global using Terraria.ModLoader;
global using Terraria.WorldBuilding;
global using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
global using static AbyssOverhaul.AbyssOverhaul;

using AbyssOverhaul.Core.Systems;
using System.IO;
using Wayfarer.API;

namespace AbyssOverhaul
{
	public partial class AbyssOverhaul : Mod
	{
        public override void Unload()
        {
            WayfarerAPI.Shutdown();
        }
    }
}
