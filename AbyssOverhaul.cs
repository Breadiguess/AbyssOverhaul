global using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
global using AbyssOverhaul.Core.NPCOverrides;
global using AbyssOverhaul.Core.Systems;
global using BreadLibrary.Common.Graphics;
global using BreadLibrary.Core.Utilities;
global using CalamityMod.NPCs.Abyss;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using ReLogic.Content;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using Terraria;
global using Terraria.GameContent.Generation;
global using Terraria.ID;
global using Terraria.IO;
global using Terraria.ModLoader;
global using Terraria.WorldBuilding;
global using static AbyssOverhaul.AbyssOverhaul;
using Wayfarer.API;

namespace AbyssOverhaul
{
    public partial class AbyssOverhaul : Mod
    {

        public AbyssOverhaul()
        {
            MusicAutoloadingEnabled = false;
        }
        public override void Unload()
        {
            WayfarerAPI.Shutdown();
        }
    }
}
