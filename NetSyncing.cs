using AbyssOverhaul.Content.Items.Debug;
using AbyssOverhaul.Content.Layers.FossilShale.Systems;
using AbyssOverhaul.Core.Carcasses;
using AbyssOverhaul.Core.ModPlayers;
using AbyssOverhaul.Core.Subworlds;
using SubworldLibrary;
using System.IO;

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
            RequestEnterAbyss,
            ApproveEnterAbyss
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
                case AbyssOverhaulMessageType.RequestEnterAbyss:
                    {
                        if (Main.netMode != NetmodeID.Server)
                            return;

                        Player player = Main.player[whoAmI];
                        if (player is null || !player.active || player.dead)
                            return;

                        if (SubworldSystem.IsActive<AbyssSubworld>())
                            return;

                        // Put any real gatekeeping here later.
                        // For now, the tile itself is the gate.

                        ModPacket approve = GetPacket();
                        approve.Write((byte)AbyssOverhaulMessageType.ApproveEnterAbyss);
                        approve.Send(whoAmI);
                        break;
                    }

                case AbyssOverhaulMessageType.ApproveEnterAbyss:
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            return;

                        if (!SubworldSystem.IsActive<AbyssSubworld>())
                            SubworldSystem.Enter<AbyssSubworld>();

                        break;
                    }
            }
        }

    }
}

