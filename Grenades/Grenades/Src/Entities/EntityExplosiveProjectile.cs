using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Grenades.Entities;

public class EntityExplosiveProjectile : EntityProjectile {
    
    protected double damageRadius;
    protected double blastRadius;
    
    protected double fuse;

    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3dIn) {
        base.Initialize(properties, api, InChunkIndex3dIn);

        var attributes = properties.Attributes;

        var stack = ProjectileStack;
        fuse = attributes["baseFuse"].AsDouble();
        damageRadius = attributes["aoeRadius"].AsDouble();
        blastRadius = attributes["blastRadius"].AsDouble();
        
        // EntityThrownStone
        NonCollectible = true;
        
        if (stack is { Collectible: not null }) {
            var collectibleAttributes = stack.Collectible.Attributes;
            
            fuse = collectibleAttributes["fuse"].AsDouble(1);
            damageRadius = collectibleAttributes["aoeRadius"].AsDouble(1);
            blastRadius = collectibleAttributes["blastRadius"].AsDouble(1);
            
            var stackAttributes = stack.Attributes;
            fuse *= stackAttributes.GetDouble("fuseMult", 1);
            damageRadius *= stackAttributes.GetDouble("aoeRadiusMult", 1);
            blastRadius *= stackAttributes.GetDouble("blastRadiusMult", 1);
        }
    }
    
    
    public override void OnGameTick(float dt) {
        base.OnGameTick(dt);
        if (World.Side == EnumAppSide.Server) {
            if ((fuse -= dt) <= 0) {
                Die();
            }
        }
    }

    public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null) {
        base.Die(reason, damageSourceForDeath);
        if (World.Side == EnumAppSide.Server) {
            ((IServerWorldAccessor) World).CreateExplosion(ServerPos.XYZ.AsBlockPos, EnumBlastType.EntityBlast, blastRadius, damageRadius);
        }
    }
}