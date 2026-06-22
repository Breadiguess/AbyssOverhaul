using AbyssOverhaul.Core.Ecosystem;
using AbyssOverhaul.Core.Systems;
using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.Systems
{
    public class AbyssWorldDataSystem : ModSystem
    {
        private const string MinXKey = "Abyss_MinX";
        private const string MaxXKey = "Abyss_MaxX";
        private const string TopYKey = "Abyss_TopY";
        private const string BottomYKey = "Abyss_BottomY";
        private const string ChasmXKey = "Abyss_ChasmX";
        private const string OnLeftKey = "Abyss_OnLeft";

		internal static bool _downedSilva = false;

		public static bool downedSilva
		{
			get => _downedSilva;
			set
			{
				if (!value)
					_downedSilva = false;
				else
					NPC.SetEventFlagCleared(ref _downedSilva, -1);
			}
		}

		public override void ClearWorld()
        {
            AbyssGenUtils.Reset();
            PresssureSystem.RefreshAfterBoundsChanged();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (!AbyssGenUtils.Initialized)
                return;

            tag[MinXKey] = AbyssGenUtils.MinX;
            tag[MaxXKey] = AbyssGenUtils.MaxX;
            tag[TopYKey] = AbyssGenUtils.TopY;
            tag[BottomYKey] = AbyssGenUtils.BottomY;
            tag[ChasmXKey] = AbyssGenUtils.ChasmX;
            tag[OnLeftKey] = AbyssGenUtils.OnLeft;

			if (downedSilva)
				tag.Add("downedSilvaFlag", downedSilva);
		}

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.ContainsKey(MinXKey) &&
                tag.ContainsKey(MaxXKey) &&
                tag.ContainsKey(TopYKey) &&
                tag.ContainsKey(BottomYKey) &&
                tag.ContainsKey(ChasmXKey) &&
                tag.ContainsKey(OnLeftKey))
            {
                AbyssGenUtils.SetBounds(
                    tag.GetInt(MinXKey),
                    tag.GetInt(MaxXKey),
                    tag.GetInt(TopYKey),
                    tag.GetInt(BottomYKey),
                    tag.GetInt(ChasmXKey),
                    tag.GetBool(OnLeftKey),
                    Mod
                );
            }
            else
            {
                // Fallback for old worlds with no saved abyss data.
                // This should mainly matter for singleplayer / host-side old worlds.
                AbyssGenUtils.Initialize(Mod);
            }

			if (tag.ContainsKey("downedSilvaFlag"))
				downedSilva = true;

			PresssureSystem.RefreshAfterBoundsChanged();
        }

        public override void OnWorldLoad()
        {
            if (AbyssGenUtils.Initialized)
                PresssureSystem.RefreshAfterBoundsChanged();
			downedSilva = false;
		}

		public override void OnWorldUnload() => downedSilva = false;

		public override void NetSend(BinaryWriter writer)
        {
            writer.Write(AbyssGenUtils.Initialized);

            if (!AbyssGenUtils.Initialized)
                return;

            writer.Write7BitEncodedInt(AbyssGenUtils.MinX);
            writer.Write7BitEncodedInt(AbyssGenUtils.MaxX);
            writer.Write7BitEncodedInt(AbyssGenUtils.TopY);
            writer.Write7BitEncodedInt(AbyssGenUtils.BottomY);
            writer.Write7BitEncodedInt(AbyssGenUtils.ChasmX);
            writer.Write(AbyssGenUtils.OnLeft);
        }

        public override void NetReceive(BinaryReader reader)
        {
            bool initialized = reader.ReadBoolean();

            if (!initialized)
            {
                AbyssGenUtils.Reset();
                PresssureSystem.RefreshAfterBoundsChanged();
                return;
            }

            int minX = reader.Read7BitEncodedInt();
            int maxX = reader.Read7BitEncodedInt();
            int topY = reader.Read7BitEncodedInt();
            int bottomY = reader.Read7BitEncodedInt();
            int chasmX = reader.Read7BitEncodedInt();
            bool onLeft = reader.ReadBoolean();

            AbyssGenUtils.SetBounds(minX, maxX, topY, bottomY, chasmX, onLeft, Mod);
            PresssureSystem.RefreshAfterBoundsChanged();
        }
    }
}