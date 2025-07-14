using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Server;

namespace Grenades.Util;

public static class PhysicsUtil {

    // public const float PhysicsTickDelta = 1f / 15f;
    public const float MagicNimberPulledFromPassivePhysicsCode = 60f; //dtFactor?
    
    public static void ApplyImpulse(this EntityPos entityPos, Vec3d force) {
        entityPos.Motion.Add(force / MagicNimberPulledFromPassivePhysicsCode);
    }
    
}