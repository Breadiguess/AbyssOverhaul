using AbyssOverhaul.Content.NPCs.CarcassLeech;
using BreadLibrary.Core.Graphics;
using BreadLibrary.Core.Graphics.PixelationShit;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AbyssOverhaul.Core.Carcasses
{
    public class CarcassSystem : ModSystem, IDrawPixellated
    {
        public static Dictionary<int, CarcassEntity> Carcasses = new();
        public static int NextID;

        PixelLayer IDrawPixellated.PixelLayer => PixelLayer.BehindTiles;

        public bool ShouldDrawPixelated => Carcasses.Count > 0;

        public override void OnWorldLoad()
        {
            Carcasses.Clear();
            NextID = 0;
        }

        public override void OnWorldUnload()
        {
            Carcasses.Clear();
            NextID = 0;
        }

        public override void Load()
        {
            if (!Main.dedServ)
                PixelDrawRegistry.Register(this);
        }

        public override void Unload()
        {
            if (!Main.dedServ)
                PixelDrawRegistry.Unregister(this);
        }
        private static int CarcassLeechType => ModContent.NPCType<CarcassLeechNPC>();

        private static void TrySpawnLeeches(CarcassEntity carcass)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (carcass is null || !carcass.Active || carcass.PendingDelete)
                return;

            // No meat, no leeches.
            if (carcass.FleshRemaining <= 0)
                return;

            carcass.LeechSpawnTimer--;
            carcass.LastLeechCheckTimer++;

            if (carcass.LeechSpawnTimer > 0)
                return;

            // Only really "grow" leeches when the area is not actively occupied.
            bool playersNearby = AnyPlayersNear(carcass.Center, 600f);

            // If players are close, delay instead of popping enemies into existence beside them.
            if (playersNearby)
            {
                carcass.LeechSpawnTimer = Main.rand.Next(240, 420);
                return;
            }

            int activeLinkedLeeches = CountNearbyLinkedLeeches(carcass.ID, carcass.Center, 900f);
            int maxAllowed = GetMaxLeechesFor(carcass);

            if (activeLinkedLeeches >= maxAllowed)
            {
                carcass.LeechSpawnTimer = Main.rand.Next(300, 480);
                return;
            }

            if (!TryFindLeechSpawnPosition(carcass, out Vector2 spawnPos))
            {
                carcass.LeechSpawnTimer = Main.rand.Next(120, 240);
                return;
            }

            IEntitySource source = new EntitySource_Misc("CarcassLeechSpawn");

            int idx = NPC.NewNPC(
                source,
                (int)spawnPos.X,
                (int)spawnPos.Y,
                CarcassLeechType);

            if (idx.WithinBounds(Main.maxNPCs))
            {
                NPC leech = Main.npc[idx];

                // Link the spawned leech back to the carcass.
                leech.ai[0] = carcass.ID;
                leech.ai[1] = Main.rand.NextFloat();
                leech.netUpdate = true;

                // Spawning costs meat.
                carcass.FleshRemaining = Math.Max(0, carcass.FleshRemaining - Main.rand.Next(8, 16));
                carcass.TotalLeechesSpawned++;
                carcass.NetDirty = true;

                if (carcass.FleshRemaining <= 0)
                {
                    carcass.PendingDelete = true;
                    carcass.active = false;
                }
            }

            carcass.LeechSpawnTimer = Main.rand.Next(300, 720);
        }

        private static int GetMaxLeechesFor(CarcassEntity carcass)
        {
            // More flesh supports more parasites, but keep it capped.
            int byFlesh = 1 + carcass.FleshRemaining / 35;
            int byLifetime = carcass.TimeAlive / 1800; // older carcasses can host a little more
            return Utils.Clamp(Math.Max(byFlesh, 1) + byLifetime, 1, 6);
        }

        private static bool AnyPlayersNear(Vector2 worldPos, float range)
        {
            float rangeSq = range * range;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player is null || !player.active || player.dead)
                    continue;

                if (Vector2.DistanceSquared(player.Center, worldPos) <= rangeSq)
                    return true;
            }

            return false;
        }

        private static int CountNearbyLinkedLeeches(int carcassID, Vector2 center, float range)
        {
            float rangeSq = range * range;
            int count = 0;
            int leechType = CarcassLeechType;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc is null || !npc.active || npc.type != leechType)
                    continue;

                if ((int)npc.ai[0] != carcassID)
                    continue;

                if (Vector2.DistanceSquared(npc.Center, center) <= rangeSq)
                    count++;
            }

            return count;
        }

        private static bool TryFindLeechSpawnPosition(CarcassEntity carcass, out Vector2 spawnPos)
        {
            Vector2 center = carcass.Center;

            for (int i = 0; i < 16; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(72f, 48f) * Main.rand.NextFloat(0.35f, 1f);
                Vector2 candidate = center + offset;

                int tileX = (int)(candidate.X / 16f);
                int tileY = (int)(candidate.Y / 16f);

                if (!Terraria.WorldGen.InWorld(tileX, tileY, 10))
                    continue;

                Rectangle hitbox = new Rectangle((int)candidate.X - 10, (int)candidate.Y - 10, 20, 20);

                if (Collision.SolidCollision(hitbox.TopLeft(), hitbox.Width, hitbox.Height))
                    continue;

                spawnPos = candidate;
                return true;
            }

            spawnPos = center;
            return false;
        }
        public static CarcassEntity CreateCarcass(Vector2 center, NPC sourceNPC, int fleshAmount, Rectangle size)
        {
            CarcassEntity carcass = new CarcassEntity
            {
                LeechSpawnTimer = Main.rand.Next(180, 420),
                TotalLeechesSpawned = 0,
                LastLeechCheckTimer = 0,
                ID = NextID++,
                active = true,
                width = size.Width,
                height = size.Height,
                position = center - new Vector2(size.Width * 0.5f, size.Height * 0.5f),
                velocity = Vector2.Zero,
                FleshRemaining = fleshAmount,
                PendingDelete = false,
                TimeAlive = 0,
                Rotation = sourceNPC.rotation,
                Snapshot = CarcassSnapshot.FromNPC(sourceNPC)
            };

            Carcasses[carcass.ID] = carcass;

            ModContent.GetInstance<AbyssOverhaul>().Logger.Info(
                            $"Created new carcass:{carcass.ToString()}");
            return carcass;
        }

        public static bool TryGetCarcass(int id, out CarcassEntity carcass) => Carcasses.TryGetValue(id, out carcass);

        public static void RemoveCarcass(int id)
        {
            if (Carcasses.TryGetValue(id, out var carcass))
                carcass.PendingDelete = true;
        }

        public override void PreUpdateWorld()
        {
            // Only the server should simulate authoritative carcass state in MP.
            // In singleplayer, this still runs fine.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            List<int> toRemove = new();

            foreach (var pair in Carcasses)
            {
                CarcassEntity carcass = pair.Value;
                carcass.Update();

                if (carcass.PendingDelete)
                {
                    toRemove.Add(pair.Key);
                    continue;
                }
                TrySpawnLeeches(carcass);

                if (carcass.NetSyncCooldown > 0)
                    carcass.NetSyncCooldown--;

                bool moving = carcass.velocity.LengthSquared() > 0.04f;

                if (carcass.NetDirty && (carcass.NetSyncCooldown <= 0 || !moving))
                {
                    SyncUpdate(carcass);
                    carcass.NetDirty = false;
                    carcass.NetSyncCooldown = moving ? 0 : 15;
                }
            }

            foreach (int id in toRemove)
            {
                Carcasses.Remove(id);
                SyncRemove(id);
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            List<TagCompound> list = new();

            foreach (var carcass in Carcasses.Values)
                list.Add(carcass.Save());

            tag["Carcasses"] = list;
            tag["CarcassNextID"] = NextID;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Carcasses.Clear();

            if (tag.ContainsKey("Carcasses"))
            {
                foreach (TagCompound carcassTag in tag.GetList<TagCompound>("Carcasses"))
                {
                    CarcassEntity carcass = CarcassEntity.Load(carcassTag);
                    Carcasses[carcass.ID] = carcass;
                }
            }

            NextID = tag.ContainsKey("CarcassNextID") ? tag.GetInt("CarcassNextID") : 0;
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Carcasses.Count);
            writer.Write(NextID);

            foreach (var carcass in Carcasses.Values)
                carcass.NetSend(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            Carcasses.Clear();

            int count = reader.ReadInt32();
            NextID = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                CarcassEntity carcass = CarcassEntity.NetReceive(reader);
                Carcasses[carcass.ID] = carcass;
            }
        }

        

        public static void DrawCarcass(SpriteBatch spriteBatch, CarcassEntity carcass, bool debug = false)
        {
            if (carcass is null || !carcass.active)
                return;

            if (carcass.Snapshot is null)
                return;

            int npcType = carcass.Snapshot.NPCType;

            if (npcType < 0 || npcType >= TextureAssets.Npc.Length)
            {
                ModContent.GetInstance<AbyssOverhaul>().Logger.Warn(
                    $"Invalid carcass NPCType {npcType} for carcass ID {carcass.ID}");
                return;
            }

            if (npcType < 0 || npcType >= Main.npcFrameCount.Length)
                return;

            if (TextureAssets.Npc[npcType] is null)
                return;

            Texture2D tex = TextureAssets.Npc[npcType].Value;
            if (tex is null)
                return;

            int frameCount = Main.npcFrameCount[npcType];
            if (frameCount <= 0)
                frameCount = 1;

            Rectangle frame = tex.Frame(1, frameCount);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 screenPos = carcass.Center - Main.screenPosition;

            float maxNutrition = Math.Max(1f, carcass.Snapshot.LifeMax);
            float nutritionLerp = MathHelper.Clamp(carcass.FleshRemaining / maxNutrition, 0f, 1f);
            Color drawColor = Color.Lerp(Color.DarkGreen, Color.White, nutritionLerp).MultiplyRGB(Lighting.GetColor(carcass.position.ToTileCoordinates().X, carcass.position.ToTileCoordinates().Y));

            spriteBatch.Draw(
                tex,
                screenPos,
                frame,
                drawColor,
                carcass.Rotation,
                origin,
                carcass.Snapshot.Scale,
                carcass.Snapshot.SpriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f
            );
            if (!debug)
                return;
            Utils.DrawRectangle(spriteBatch, carcass.BottomLeft, carcass.TopRight, Color.White, Color.White, 4);

            string msg = "";
            msg += $"{carcass.TimeAlive}\n{carcass.FleshRemaining}";

            Utils.DrawBorderString(spriteBatch, msg, screenPos, drawColor);

            }
        void IDrawPixellated.DrawPixelated(SpriteBatch spriteBatch)
        {
            foreach (var carcass in Carcasses.Values)
                DrawCarcass(spriteBatch, carcass, true);
        }
        public static void SyncFull(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = CreatePacket(CarcassMessageType.FullSync);
            packet.Write(Carcasses.Count);
            packet.Write(NextID);

            foreach (var carcass in Carcasses.Values)
                carcass.NetSend(packet);

            packet.Send(toWho, fromWho);
        }

        public static void SyncAdd(CarcassEntity carcass, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = CreatePacket(CarcassMessageType.AddCarcass);
            carcass.NetSend(packet);
            packet.Send(toWho, fromWho);
        }

        public static void SyncUpdate(CarcassEntity carcass, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = CreatePacket(CarcassMessageType.UpdateCarcass);
            carcass.NetSend(packet);
            packet.Send(toWho, fromWho);
        }

        public static void SyncRemove(int id, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            ModPacket packet = CreatePacket(CarcassMessageType.RemoveCarcass);
            packet.Write(id);
            packet.Send(toWho, fromWho);
        }
        private static ModPacket CreatePacket(CarcassMessageType type)
        {
            ModPacket packet = ModContent.GetInstance<AbyssOverhaul>().GetPacket();
            packet.Write((byte)AbyssOverhaul.AbyssOverhaulMessageType.Carcass);
            packet.Write((byte)type);
            return packet;
        }
        public static void HandlePacket(BinaryReader reader, int whoAmI)
        {
            CarcassMessageType type = (CarcassMessageType)reader.ReadByte();

            switch (type)
            {
                case CarcassMessageType.FullSync:
                    {
                        if (Main.netMode == NetmodeID.Server)
                            return; // clients should receive this, not send it

                        Carcasses.Clear();
                        int count = reader.ReadInt32();
                        NextID = reader.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            CarcassEntity carcass = CarcassEntity.NetReceive(reader);
                            Carcasses[carcass.ID] = carcass;
                        }
                        break;
                    }

                case CarcassMessageType.AddCarcass:
                    {
                        if (Main.netMode == NetmodeID.Server)
                            return;

                        CarcassEntity carcass = CarcassEntity.NetReceive(reader);
                        Carcasses[carcass.ID] = carcass;
                        break;
                    }

                case CarcassMessageType.UpdateCarcass:
                    {
                        if (Main.netMode == NetmodeID.Server)
                            return;

                        CarcassEntity carcass = CarcassEntity.NetReceive(reader);
                        Carcasses[carcass.ID] = carcass;
                        break;
                    }

                case CarcassMessageType.RemoveCarcass:
                    {
                        if (Main.netMode == NetmodeID.Server)
                            return;

                        int id = reader.ReadInt32();
                        Carcasses.Remove(id);
                        break;
                    }
            }
        }

        
    }
}
