namespace GameModes.Dude;

public class DudeStats
{
    // Physics
    public float MoveSpeed = 32.0f;
    public float Acceleration = 85.0f;
    public float DashCooldown = 0.8f;
    public float Drag = 0.90f;
    public float AccelMult = 1.0f;
    public float SpeedMult = 1.0f;

    // Interaction
    public float PickupRange = 2.5f; // legacy backing field
    public float MagnetRange { get => PickupRange; set => PickupRange = value; }
    public float XPMultiplier = 1.0f; // legacy backing field for XP-related bonuses
    public float PickupBonus { get => XPMultiplier; set => XPMultiplier = value; }
    public float Luck = 1.0f; // Multiplier for rare spawns

    // Combat / Survival
    public float ShieldDuration = 4.0f;
    public float ChillDuration = 4.0f;
    public float PlayerSize = 1.0f;

    // Reset to defaults
    public void Reset()
    {
        MoveSpeed = 32.0f;
        Acceleration = 85.0f;
        DashCooldown = 0.8f;
        Drag = 0.90f;
        AccelMult = 1.0f;
        SpeedMult = 1.0f;
        PickupRange = 2.5f;
        XPMultiplier = 1.0f;
        Luck = 1.0f;
        ShieldDuration = 4.0f;
        ChillDuration = 4.0f;
        PlayerSize = 1.0f;
    }
}