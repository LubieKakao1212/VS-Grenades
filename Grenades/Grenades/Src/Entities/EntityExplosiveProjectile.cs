using System;
using Grenades.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace Grenades.Entities;

public class EntityExplosiveProjectile : EntityProjectile {
    
    protected double damageRadius;
    protected double fullDamageRadius;
    // protected double blastRadius;
    protected double peakDamage;
    protected int damageTier;
    
    protected double fuse;

    protected Entity? entityHit;
    
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3dIn) {
        base.Initialize(properties, api, InChunkIndex3dIn);

        var attributes = properties.Attributes;

        var stack = ProjectileStack;
        fuse = attributes["baseFuse"].AsDouble();
        damageRadius = attributes["aoeRadius"].AsDouble();
        // blastRadius = attributes["blastRadius"].AsDouble();
        
        // EntityThrownStone
        NonCollectible = true;
        
        if (stack is { Collectible: not null }) {
            var collectibleAttributes = stack.Collectible.Attributes;

            var overrides = GrenadesModSystem.GetSidedConfig(api.Side).GetExplosionOverridesFor(stack.Collectible);
            
            fuse = overrides.GetFuse(collectibleAttributes["fuse"].AsDouble(1));
            damageRadius = overrides.GetRadius(collectibleAttributes["damageRadius"].AsDouble(1));
            fullDamageRadius = overrides.GetFullRadius(damageRadius / 6f);
            
            // blastRadius = collectibleAttributes["blastRadius"].AsDouble(1);
            peakDamage = overrides.GetDamage(collectibleAttributes["damage"].AsDouble(1));
            damageTier = overrides.GetDamageTier(collectibleAttributes["damageTier"].AsInt(1));
            
            var stackAttributes = stack.Attributes;
            fuse *= stackAttributes.GetDouble("fuseMult", 1);
            damageRadius *= stackAttributes.GetDouble("aoeRadiusMult", 1);
            // blastRadius *= stackAttributes.GetDouble("blastRadiusMult", 1);
            peakDamage *= stackAttributes.GetDouble("damageMult", 1);
            damageTier += stackAttributes.GetInt("damageTierAdd", 0);
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
            var pos = ServerPos.XYZ;
            entityHit?.WatchedAttributes?.UnregisterListener(ResetInvulnerability);
            
            World.DoExplosionDamage(pos, (float)peakDamage, damageTier, (float)damageRadius,  (float)fullDamageRadius, this, FiredBy);
            World.DoExplosionEffects(pos, (float)peakDamage, (float)damageRadius);
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
        entityHit = entity;
        entity.WatchedAttributes.RegisterModifiedListener("onHurt", ResetInvulnerability);
        base.impactOnEntity(entity);
    }

    private void ResetInvulnerability() {
        entityHit.SetActivityRunning("invulnerable", -1);
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