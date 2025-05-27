using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Grenades.Util;

public class ShrapnelDef {
    
    public required AssetLocation ShrapnelLocation { get; set; }

    public required double Amount { get; set; }
    public double AmountRandomness { get; set; } = 0;

    public double Damage { get; set; }
    public int DamageTier { get; set; }

    public float Velocity { get; set; }
    public float VelocityRandomness { get; set; }
    public Vec3d VelocityBias { get; set; } = Vec3d.Zero;
    
    public double Lifetime { get; set; }
    public double LifetimeRandomness { get; set; }
    
}