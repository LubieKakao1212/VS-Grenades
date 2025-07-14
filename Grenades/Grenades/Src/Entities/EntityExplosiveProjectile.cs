using Grenades.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Grenades.Entities;

public class EntityExplosiveProjectile : EntityProjectile, IGrenadeProjectile {

    public DefGrenadeStatValues GrenadeStats { get; set; }

    Entity IGrenadeProjectile.FiredBy {
        get => FiredBy;
        set => FiredBy = value;
    }

    public ItemStack GrenadeStack { get; set; } = null!;

    protected float _fuse;
    protected float _verticalVelocityBias;
    
    protected Entity? _entityHit;

    protected ShrapnelSpawnData? _shrapnel;
        
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3dIn) {
        base.Initialize(properties, api, InChunkIndex3dIn);
        NonCollectible = true;

        var attributes = properties.Attributes;

        ProjectileStack = GrenadeStack;
        var stack = GrenadeStack;

        if (attributes["shrapnel"].Exists) {
            var shrapnelAttributes = attributes["shrapnel"];
            _verticalVelocityBias = shrapnelAttributes["velocityBias"].AsFloat(0f);
            _shrapnel = new ShrapnelSpawnData {
                ShrapnelLocation = AssetLocation.Create(shrapnelAttributes["entity-code"].AsString(), properties.Code.Domain),
                Values = GrenadeStats.Shrapnel
            };
        }
        
        NonCollectible = true;
        _fuse = GrenadeStats.Fuse;
        
        if (stack is { Collectible: not null }) {
            var stackAttributes = stack.Attributes;
            _fuse *= stackAttributes.GetFloat("fuseMult", 1);
            var stats = GrenadeStats;
            
            stats.Fuse *= stackAttributes.GetFloat("radiusMult", 1);
            // blastRadius *= stackAttributes.GetDouble("blastRadiusMult", 1);
            stats.Damage *= stackAttributes.GetFloat("damageMult", 1);
            stats.DamageTier += stackAttributes.GetInt("damageTierAdd", 0);
            
            GrenadeStats = stats;
        }
    }

    public override void OnGameTick(float dt) {
        base.OnGameTick(dt);
        if (World.Side == EnumAppSide.Server) {
            if ((_fuse -= dt) <= 0) {
                Die();
            }
        }
    }

    public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null) {
        base.Die(reason, damageSourceForDeath);
        if (World.Side == EnumAppSide.Server) {
            var pos = ServerPos.XYZ;
            var motion = ServerPos.Motion;
            _entityHit?.WatchedAttributes?.UnregisterListener(ResetInvulnerability);

            var stats = GrenadeStats;
            
            World.DoExplosionDamage(pos, stats.Damage, stats.DamageTier, stats.Radius, stats.Radius * stats.InnerRadius, GrenadeStats.Knockback,this, FiredBy);
            World.DoExplosionEffects(pos,  stats.Damage, stats.Radius);

            if (_shrapnel is { Values.Enabled: true }) {
                World.DoShrapnel(pos, 
                    _shrapnel.Value with {
                        VelocityBias = (motion * stats.Shrapnel.VelocityInheritance)
                    },
                    FiredBy);
            }
        }
    }

    protected override void impactOnEntity(Entity entity) {
        _entityHit = entity;
        entity.WatchedAttributes.RegisterModifiedListener("onHurt", ResetInvulnerability);
        base.impactOnEntity(entity);
    }

    private void ResetInvulnerability() {
        _entityHit.SetActivityRunning("invulnerable", -1);
    }

    //Gaussian sampler which is much faster than NatFloat's NARROWGAUSSIAN
    // private static float gaussian2;
    // public static float NextGaussian() {
    //     float v1, v2;
    //     v1 = Random.Shared.NextSingle();
    //     v2 = Random.Shared.NextSingle();
    //
    //     var a1 = MathF.Sqrt(-2 * MathF.Log(v1));
    //     var a2 = v2 * MathF.PI * 2;
    //
    //     gaussian2 = a1 * MathF.Cos(a2);
    //     return a1 * MathF.Sin(a2);
    // }

}