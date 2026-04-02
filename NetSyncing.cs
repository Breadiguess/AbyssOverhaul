using AbyssOverhaul.Content.Items.Debug;
using AbyssOverhaul.Content.Layers.FossilShale.Systems;
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
            SyncBrigandsCallingPlayer,

            CreateChain,
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

                case AbyssOverhaulMessageType.SyncBrigandsCallingPlayer:
                    {
                        byte playerIndex = reader.ReadByte();
                        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                            return;

                        Player player = Main.player[playerIndex];
                        if (player is null || !player.active)
                            return;

                        var modPlayer = player.GetModPlayer<Content.Items.Weapons.Ranged.BrigandsCalling.BrigandsCalling_Player>();
                        modPlayer.ReceiveSync(reader);

                        // Relay client -> server -> everyone else.
                        if (Main.netMode == NetmodeID.Server)
                            modPlayer.SendSync(-1, whoAmI);

                        break;
                    }


                case AbyssOverhaulMessageType.CreateChain:
                    {
                        Point16 start = new Point16(reader.ReadInt16(), reader.ReadInt16());
                        Point16 end = new Point16(reader.ReadInt16(), reader.ReadInt16());

                        if (!TileToTileChainSystem.IsValidAnchorTile(start) || !TileToTileChainSystem.IsValidAnchorTile(end))
                            return;

                        if (Main.netMode == NetmodeID.Server)
                        {
                            TileToTileChainSystem.AddChain(
                                start,
                                end,
                                ChainLinker.DefaultPixelsPerSegment,
                                ChainLinker.DefaultGravity,
                                ChainLinker.DefaultDamping,
                                ChainLinker.DefaultSimulateIterations,
                                ChainLinker.DefaultAnchorIterations,
                                ChainLinker.DefaultCollideWithTiles,
                                ChainLinker.DefaultCollisionRadius,
                                Vector2.Zero,
                                Vector2.Zero,
                                ChainLinker.DefaultThickness);

                            ModPacket packet = GetPacket();
                            packet.Write((byte)AbyssOverhaulMessageType.CreateChain);
                            packet.Write((short)start.X);
                            packet.Write((short)start.Y);
                            packet.Write((short)end.X);
                            packet.Write((short)end.Y);
                            packet.Send(-1, whoAmI);
                        }
                        else
                        {
                            TileToTileChainSystem.AddChain(
                                start,
                                end,
                                ChainLinker.DefaultPixelsPerSegment,
                                ChainLinker.DefaultGravity,
                                ChainLinker.DefaultDamping,
                                ChainLinker.DefaultSimulateIterations,
                                ChainLinker.DefaultAnchorIterations,
                                ChainLinker.DefaultCollideWithTiles,
                                ChainLinker.DefaultCollisionRadius,
                                Vector2.Zero,
                                Vector2.Zero,
                                ChainLinker.DefaultThickness);
                        }

                        break;
                    }
            }

        }
    }
}
