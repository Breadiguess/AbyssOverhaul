using AbyssOverhaul.Core.Carcasses;
using AbyssOverhaul.Core.ModPlayers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace AbyssOverhaul
{
    public partial class AbyssOverhaul
    {
        public enum AbyssOverhaulMessageType : byte
        {
            SyncPressurePlayer,

            Carcass,
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            AbyssOverhaulMessageType msgType = (AbyssOverhaulMessageType)reader.ReadByte();

            switch (msgType)
            {
                case AbyssOverhaulMessageType.SyncPressurePlayer:
                    {
                        byte playerWhoAmI = reader.ReadByte();
                        PressurePlayer pressurePlayer = Main.player[playerWhoAmI].GetModPlayer<PressurePlayer>();

                        pressurePlayer.ReceiveSync(reader);

                        // If server received it from a client, send to everyone else.
                        if (Main.netMode == NetmodeID.Server)
                        {
                            pressurePlayer.SyncPlayer(toWho: -1, fromWho: whoAmI, newPlayer: false);
                        }

                        break;
                    }

                case AbyssOverhaulMessageType.Carcass:

                    CarcassSystem.HandlePacket(reader, whoAmI);
                    break;
            }

        }
    }
}
