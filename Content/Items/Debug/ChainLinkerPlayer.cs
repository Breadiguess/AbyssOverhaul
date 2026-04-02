using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Content.Items.Debug
{
    public sealed class ChainLinkerPlayer : ModPlayer
    {
        public bool HasPendingAnchor;
        public Point16 PendingAnchor;

        public void ClearPendingAnchor()
        {
            HasPendingAnchor = false;
            PendingAnchor = Point16.NegativeOne;
        }
    }
}
