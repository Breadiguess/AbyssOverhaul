global using AbyssOverhaul.Common.Brain;
global using AbyssOverhaul.Common.Brain.AbyssOverhaul.Common.Brain;
global using AbyssOverhaul.Common.Brain.Contexts;
global using AbyssOverhaul.Common.Brain.SharedModules;
global using AbyssOverhaul.Core.DataStructures;
global using AbyssOverhaul.Core.Ecosystem.Ecology;
global using AbyssOverhaul.Core.Graphics;
global using AbyssOverhaul.Core.NPCOverrides;
global using AbyssOverhaul.Core.Systems;
global using BreadLibrary.Common.Graphics;
global using BreadLibrary.Core.Graphics.Pixelation;
global using BreadLibrary.Core.MultiSegment;
global using BreadLibrary.Core.Utilities;
global using CalamityMod.NPCs.Abyss;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using ReLogic.Content;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Text;
global using Terraria;
global using Terraria.Audio;
global using Terraria.DataStructures;
global using Terraria.GameContent;
global using Terraria.GameContent.Generation;
global using Terraria.ID;
global using Terraria.IO;
global using Terraria.Localization;
global using Terraria.ModLoader;
global using Terraria.WorldBuilding;
global using static AbyssOverhaul.AbyssOverhaul;
using log4net;
using Wayfarer.API;

[assembly: IgnoresAccessChecksTo("CalamityMod")]
namespace AbyssOverhaul
{
    public partial class AbyssOverhaul : Mod
    {
		public static ILog? Log = null;

		public AbyssOverhaul()
        {
            MusicAutoloadingEnabled = false;
        }

		public override void Load()
		{
            Log = Logger;
		}

        public override void Unload()
        {
            WayfarerAPI.Shutdown();
            Log = null;
        }
    }
}
