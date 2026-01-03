using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SlimeCore.GameModes.Idle.Store;

public class StoreDefinition
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int BaseCost { get; set; }
    public float CPS { get; set; }
    public int ClickAdd { get; set; }
    public float ClickMult { get; set; }
    public int Cost { get; set; }
    public int Owned { get; set; }
}

