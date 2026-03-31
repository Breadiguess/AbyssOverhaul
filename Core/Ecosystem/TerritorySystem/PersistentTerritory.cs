using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Ecosystem.TerritorySystem
{
    public sealed class PersistentTerritory
    {
        public int TerritoryID;
        public long OwnerActorID = -1;
        public long OwnerGroupID = -1;
        public int SpeciesID;

        public Rectangle Bounds;
        public Point CoreCell;

        public float ClaimStrength;
        public double LastUpdatedTime;
    }
}
