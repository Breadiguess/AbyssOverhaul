using Mono.Cecil.Cil;
using MonoMod.Cil;
using SubworldLibrary;
using System.Reflection;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace AbyssOverhaul.Core.Subworlds
{
    internal class AbyssEntrySaveSkipSystem : ModSystem
    {
        internal static bool SkipNextEntryPlayerBackup;

        private static bool ShouldSkipEntryMapSave() => SkipNextEntryPlayerBackup;

        private static bool ShouldSkipEntryPlayerWrite()
        {
            bool skip = SkipNextEntryPlayerBackup;
            SkipNextEntryPlayerBackup = false; // consume it on the actual file-write skip
            return skip;
        }

        public override void Load()
        {
            IL_Player.SavePlayer += PatchSavePlayer;
        }

        public override void Unload()
        {
            SkipNextEntryPlayerBackup = false;
        }

        private void PatchSavePlayer(ILContext il)
        {
            ILCursor c, cc, ccc;
            if (!(c = new ILCursor(il)).TryGotoNext(i => i.MatchCall(typeof(Player), "InternalSaveMap"))
             || !(cc = c.Clone()).TryGotoNext(MoveType.AfterLabel, i => i.MatchLdsfld(typeof(Main), "ServerSideCharacter"))
             || !(ccc = cc.Clone()).TryGotoNext(MoveType.After, i => i.MatchCall(typeof(FileUtilities), "ProtectedInvoke")))
            {
                Mod.Logger.Error("AbyssEntrySaveSkipSystem: failed to find SavePlayer injection sites.");
                return;
            }

            c.Index -= 3;
            ILLabel afterMapSave = cc.DefineLabel();
            c.EmitDelegate(ShouldSkipEntryMapSave);
            c.Emit(OpCodes.Brtrue, afterMapSave);
            cc.MarkLabel(afterMapSave);

            ILLabel afterPlayerWrite = ccc.DefineLabel();
            cc.EmitDelegate(ShouldSkipEntryPlayerWrite);
            cc.Emit(OpCodes.Brtrue, afterPlayerWrite);
            ccc.MarkLabel(afterPlayerWrite);
        }
    }
}