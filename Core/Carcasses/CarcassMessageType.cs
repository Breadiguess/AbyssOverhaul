using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverhaul.Core.Carcasses
{
    public enum CarcassMessageType : byte
    {
        FullSync,
        AddCarcass,
        UpdateCarcass,
        RemoveCarcass
    }
}
