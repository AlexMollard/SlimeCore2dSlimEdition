using System;
using System.Text.Json.Serialization;

namespace SlimeCore.GameModes.Factory.Items;

public class ItemDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public int MaxStack { get; init; } = 64;
    public string? IconPath { get; set; }

    [JsonIgnore]
    public IntPtr IconTexture { get; set; } = IntPtr.Zero;
}
