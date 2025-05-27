using System;
using Grenades.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Grenades.Entities;

public class EntityExplosiveProjectile : EntityProjectile {
    
    protected double _damageRadius;
    protected double _fullDamageRadius;
    // protected double blastRadius;
    protected double _peakDamage;
    protected int _damageTier;
    
    protected double _fuse;

    protected Entity? _entityHit;

    protected ShrapnelDef? _shrapnel;
    
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3dIn) {
        base.Initialize(properties, api, InChunkIndex3dIn);

        var attributes = properties.Attributes;

        var stack = ProjectileStack;
        _fuse = attributes["baseFuse"].AsDouble();
        _damageRadius = attributes["aoeRadius"].AsDouble();
        // blastRadius = attributes["blastRadius"].AsDouble();

        if (attributes["shrapnel"].Exists) {

            var shrapnelAttributes = attributes["shrapnel"];

            var velocityBias = shrapnelAttributes["velocityBias"].AsArray(new float[] {0,0,0});
            
            _shrapnel = new ShrapnelDef {
                ShrapnelLocation = AssetLocation.Create(shrapnelAttributes["entity-code"].AsString(), properties.Code.Domain),
                Amount = shrapnelAttributes["amount"].AsInt(),
                AmountRandomness = shrapnelAttributes["amountRandomness"].AsInt(),
                VelocityBias = new Vec3d(
                    velocityBias[0],
                    velocityBias[1],
                    velocityBias[2]),
            };
        }
        
        NonCollectible = true;
        
        if (stack is { Collectible: not null }) {
            var collectibleAttributes = stack.Collectible.Attributes;

            var overrides = GrenadesModSystem.GetSidedConfig(api.Side).GetGrenadeOverridesFor(stack.Collectible);
            
            _fuse = overrides.GetFuse(collectibleAttributes["fuse"].AsDouble(1));
            _damageRadius = overrides.GetRadius(collectibleAttributes["damageRadius"].AsDouble(1));
            _fullDamageRadius = overrides.GetFullRadius(_damageRadius / 6f);
            
            // blastRadius = collectibleAttributes["blastRadius"].AsDouble(1);
            _peakDamage = overrides.GetDamage(collectibleAttributes["damage"].AsDouble(1));
            _damageTier = overrides.GetDamageTier(collectibleAttributes["damageTier"].AsInt(1));
            
            if (_shrapnel != null) {
                var collectibleShrapnel = collectibleAttributes["shrapnel"];
                var shrapnelOverrides = overrides.Shrapnel;
                _shrapnel.Damage = shrapnelOverrides.GetDamage(collectibleShrapnel["damage"].AsDouble(0));
                _shrapnel.DamageTier = shrapnelOverrides.GetDamageTier(collectibleShrapnel["damageTier"].AsInt(0));
                _shrapnel.Amount = shrapnelOverrides.GetAmount(collectibleShrapnel["amount"].AsDouble(1));
                _shrapnel.AmountRandomness = shrapnelOverrides.GetAmountRandomness(collectibleShrapnel["amountRand"].AsDouble(0));
                _shrapnel.Velocity = collectibleShrapnel["velocity"].AsFloat(1);//shrapnelOverrides.Get_shrapnel.Velocity);
                _shrapnel.VelocityRandomness = collectibleShrapnel["velocityRand"].AsFloat(0);//shrapnelOverrides.Get_shrapnel.Velocity);
                _shrapnel.Lifetime = shrapnelOverrides.GetLifetime(collectibleShrapnel["lifetime"].AsDouble(1));
                _shrapnel.LifetimeRandomness = shrapnelOverrides.GetLifetimeRandomness(collectibleShrapnel["lifetimeRand"].AsDouble(0));
            }
            
            var stackAttributes = stack.Attributes;
            _fuse *= stackAttributes.GetDouble("fuseMult", 1);
            _damageRadius *= stackAttributes.GetDouble("aoeRadiusMult", 1);
            // blastRadius *= stackAttributes.GetDouble("blastRadiusMult", 1);
            _peakDamage *= stackAttributes.GetDouble("damageMult", 1);
            _damageTier += stackAttributes.GetInt("damageTierAdd", 0);
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
            _entityHit?.WatchedAttributes?.UnregisterListener(ResetInvulnerability);
            
            World.DoExplosionDamage(pos, (float)_peakDamage, _damageTier, (float)_damageRadius,  (float)_fullDamageRadius, this, FiredBy);
            World.DoExplosionEffects(pos, (float)_peakDamage, (float)_damageRadius);

            if (_shrapnel != null) {
                World.DoShrapnel(pos, 
                    _shrapnel.ShrapnelLocation,
                    (int)(_shrapnel.Amount - (float)_shrapnel.AmountRandomness / 2),
                    (int)(_shrapnel.Amount + (float)_shrapnel.AmountRandomness / 2),
                    _shrapnel.Velocity - _shrapnel.VelocityRandomness / 2f,
                    _shrapnel.Velocity + _shrapnel.VelocityRandomness / 2f,
                    _shrapnel.VelocityBias,
                    _shrapnel.Lifetime - _shrapnel.LifetimeRandomness / 2.0,
                    _shrapnel.Lifetime + _shrapnel.LifetimeRandomness / 2.0,
                    _shrapnel.Damage,
                    _shrapnel.DamageTier,
                    FiredBy
                    );
            }
            
            // var samples = 1L << 27;
            //
            // var generator1 = NatFloat.createGauss(0f, 1f);
            // var generator2 = NatFloat.createNarrowGauss(0f, 1f);
            //
            // var stopwatch = new System.Diagnostics.Stopwatch();
            // stopwatch.Start();
            // for (long i=0; i<samples; i++) {
            //     var a = NextGaussian();
            // }
            //
            // var res1 = stopwatch.Elapsed;
            // stopwatch.Restart();
            //
            // for (long i=0; i<samples; i++) {
            //     var a = generator1.nextFloat();
            // }
            // var res2 = stopwatch.Elapsed;
            //
            // for (long i=0; i<samples; i++) {
            //     var a = generator2.nextFloat();
            // }
            // var res3 = stopwatch.Elapsed;
            // stopwatch.Stop();
            //
            // ((ICoreServerAPI)Api).Logger.Chat("Box Muller: " + res1 / 2);
            // ((ICoreServerAPI)Api).Logger.Chat("NatFloat: " + res2);
            // ((ICoreServerAPI)Api).Logger.Chat("NatFloat (narrow): " + res3);
        }
    }

    private static float gaussian2;
    
    protected override void impactOnEntity(Entity entity) {
        _entityHit = entity;
        entity.WatchedAttributes.RegisterModifiedListener("onHurt", ResetInvulnerability);
        base.impactOnEntity(entity);
    }

    private void ResetInvulnerability() {
        _entityHit.SetActivityRunning("invulnerable", -1);
    }
    
    public static float NextGaussian() {
        float v1, v2;
        v1 = Random.Shared.NextSingle();
        v2 = Random.Shared.NextSingle();

        var a1 = MathF.Sqrt(-2 * MathF.Log(v1));
        var a2 = v2 * MathF.PI * 2;

        gaussian2 = a1 * MathF.Cos(a2);
        return a1 * MathF.Sin(a2);
    }
}