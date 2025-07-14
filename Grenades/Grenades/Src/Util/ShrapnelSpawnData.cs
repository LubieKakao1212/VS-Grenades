using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Grenades.Util;

public struct ShrapnelSpawnData {
    public ShrapnelSpawnData() {
        Values = default;
    }

    public required AssetLocation ShrapnelLocation { get; init; }
    
    public Vec3d VelocityBias { get; set; } = Vec3d.Zero;

    public required DefShrapnelValues Values { get; init; }
}