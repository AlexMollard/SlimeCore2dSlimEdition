using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SlimeCore.GameModes.Factory.Buildings;

public class BuildingDefinition
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Category { get; set; }
    public int Tier { get; set; } = 1;
    public string? TexturePath { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    
    public Dictionary<string, int> Cost { get; set; } = new();

    public List<BuildingComponentConfig> Components { get; set; } = new();

    [JsonIgnore]
    public IntPtr Texture { get; set; } = IntPtr.Zero;
}

public class BuildingComponentConfig
{
    public required string Type { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
