using System.IO;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.Subworlds
{
    internal sealed class AbyssPrebuildSystem : ModSystem
    {
        // Bump this whenever your abyss worldgen changes in a way that invalidates old files.
        public const int CurrentAbyssVersion = 1;

        private static int _preparedVersion;
        private static bool _queuedWarmup;
        private static bool _warmupActive;
        private static bool _returningFromWarmup;
        private static int _exitDelay;

        public static bool WarmupOnly { get; private set; }

        public override void ClearWorld()
        {
            _preparedVersion = 0;
            _queuedWarmup = false;
            _warmupActive = false;
            _returningFromWarmup = false;
            _exitDelay = 0;
            WarmupOnly = false;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            _preparedVersion = tag.GetInt("abyssPreparedVersion");
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["abyssPreparedVersion"] = _preparedVersion;
        }

        public override void OnWorldLoad()
        {
            _queuedWarmup = false;
            _warmupActive = false;
            _returningFromWarmup = false;
            _exitDelay = 0;
            WarmupOnly = false;

            // Singleplayer first. Do MP separately once this path is solid.
            if (Main.netMode != NetmodeID.SinglePlayer)
                return;

            if (Main.ActiveWorldFileData == null)
                return;

            string abyssPath = GetAbyssWorldPath();

            // If the file exists but the version is stale, delete it and rebuild.
            if (_preparedVersion != CurrentAbyssVersion && File.Exists(abyssPath))
                File.Delete(abyssPath);

            if (!IsPrepared())
                _queuedWarmup = true;
        }

        public override void PreUpdatePlayers()
        {
            if (Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer)
                return;

            // Finalize after we have returned to the main world.
            if (_returningFromWarmup && !SubworldSystem.AnyActive())
            {
                _returningFromWarmup = false;
                _warmupActive = false;
                WarmupOnly = false;

                if (File.Exists(GetAbyssWorldPath()))
                    _preparedVersion = CurrentAbyssVersion;

                return;
            }

            if (!_queuedWarmup || _warmupActive || SubworldSystem.AnyActive())
                return;

            _queuedWarmup = false;
            _warmupActive = true;
            WarmupOnly = true;

            // Reuse your unsafe entry-skip so the warmup itself is cheap.
            AbyssEntrySaveSkipSystem.SkipNextEntryPlayerBackup = true;
            /*
            bool success = SubworldSystem.Enter<AbyssSubworld>();
            if (!success)
            {
                AbyssEntrySaveSkipSystem.SkipNextEntryPlayerBackup = false;
                _warmupActive = false;
                WarmupOnly = false;
            }*/
        }

        public override void PreUpdateWorld()
        {
            // Once the warmup subworld has fully loaded, wait a few ticks and leave.
            if (!WarmupOnly)
                return;

            if (_exitDelay <= 0)
            {
                _exitDelay = 5;
                return;
            }

            _exitDelay--;
            if (_exitDelay > 0)
                return;

            _exitDelay = 0;
            _returningFromWarmup = true;
            SubworldSystem.Exit();
        }

        public static bool IsPrepared()
        {
            if (Main.ActiveWorldFileData == null)
                return false;

            return _preparedVersion == CurrentAbyssVersion &&
                   File.Exists(GetAbyssWorldPath());
        }

        public static string GetAbyssWorldPath()
        {
            var subworld = ModContent.GetInstance<AbyssSubworld>();
            string baseDir = Main.ActiveWorldFileData.IsCloudSave ? Main.CloudWorldPath : Main.WorldPath;
            string worldFolder = Path.Combine(baseDir, Main.ActiveWorldFileData.UniqueId.ToString());
            string fileName = subworld.FileName + ".wld";

            Directory.CreateDirectory(worldFolder);
            return Path.Combine(worldFolder, fileName);
        }
    }
}