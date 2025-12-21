using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Core.Grid;

internal record Tile<TEnum>
    where TEnum : Enum
{
    public TEnum Type { get; set; }

    public bool Blocked { get; set; }

    public bool Food { get; set; }
}
