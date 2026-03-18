using AbyssOverhaul.Core.Utilities;
using AbyssOverhaul.Core.WorldGen;
using CalamityMod.World;
using Terraria.Localization;

namespace AbyssOverhaul.Core.Systems
{

    public class ReplaceAbyssTask : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {


            int sulphurIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));

            tasks.RemoveAt(sulphurIndex + 1);




            int SeaIndex2 = tasks.FindIndex(p => p.Name.ToLower().Contains("sulphur sea 2"));
            tasks.RemoveAt(SeaIndex2);
            int abyssIndex = tasks.FindIndex(p => p.Name.ToLower().Contains("abyss"));


            tasks.RemoveAt(abyssIndex);
            tasks.Insert(abyssIndex++, new PassLegacy("RevampedSulphurousSea", (progress, config) =>
            {
                progress.Message = Language.GetOrRegister("Mods.CalamityMod.UI.Abyss").Value;
                SulphurousSeaRevamp.PlaceSulphurSea();
            }));
            tasks.Insert(++abyssIndex, new PassLegacy("Abyss", (progress, config) =>
            {
                progress.Message = Language.GetOrRegister("Mods.CalamityMod.UI.Abyss").Value;
                CustomAbyssHole.PlaceAbyss();

            }));
            foreach (var layer in AbyssLayerRegistry.Layers)
            {
                layer.ModifyGenTasks();

                foreach (var entry in layer.Tasks)
                {
                    string passName = $"{layer.GetType().Name}: {entry.Key}";

                    tasks.Insert(++abyssIndex, new PassLegacy(
                        passName,
                        (progress, config) => entry.Value(layer, progress, config)
                    ));
                }

            }


            tasks.Insert(abyssIndex, new PassLegacy("Flood The Sea", (progress, config) =>
            {
                AbyssWorldGenHelper.FloodOpenSpace(AbyssGenUtils.AbyssWorldMinX, AbyssGenUtils.AbyssWorldMaxX, AbyssGenUtils.TopY, AbyssGenUtils.BottomY);
            }));
        }

    }

}