using AbyssOverhaul.Core.UI.AbyssOverhaul.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.UI;

namespace AbyssOverhaul.Core.UI
{
    public class PressureUISystem : ModSystem
    {
        internal static UserInterface PressureInterface;
        internal static PressureUIState PressureUI;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            PressureInterface = new UserInterface();
            PressureUI = new PressureUIState();
            PressureUI.Activate();
            PressureInterface.SetState(PressureUI);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            PressureInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));

            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
                    "AbyssOverhaul: Pressure UI",
                    delegate
                    {
                        PressureInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void Unload()
        {
            PressureInterface = null;
            PressureUI = null;
        }
    }
}
