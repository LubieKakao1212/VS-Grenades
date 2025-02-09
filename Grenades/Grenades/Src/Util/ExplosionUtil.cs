using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Grenades.Util;

public static class ExplosionUtil {

    public const int ExposureResolution = 3;
    
    public static void DoExplosionDamage(this IWorldAccessor world, Vec3d pos, float peakDamage, int damageTier, float radius, Entity cause) {
        var damageSource = new DamageSource() {
            Source = EnumDamageSource.Explosion,
            Type = EnumDamageType.BluntAttack,
            SourcePos = pos,
            // CauseEntity = cause,
            DamageTier = damageTier
            // SourceEntity = TODO
        };
        
        var entities = world.GetEntitiesAround(pos, radius, radius, entity => entity.ShouldReceiveDamage(damageSource, peakDamage));
        
        foreach (var entity in entities) {
            //TODO check distance
            var exposure = RaycastForExposure(world, pos, entity);
            var damage = exposure * peakDamage;// * (radius - distance);TODO

            if (damage >= 0.25) {
                entity.ReceiveDamage(damageSource, damage);
            }
        }
    }

    public static void DoExplosionEffects(this IWorldAccessor world, Vec3d pos, float power) {
        SimpleParticleProperties explosionFireParticles = ExplosionParticles.ExplosionFireParticles;
        
        float x = power / 3f;
        explosionFireParticles.MinPos.Set((double) pos.X, (double) pos.Y, (double) pos.Z);
        explosionFireParticles.MinQuantity = 100f * x;
        explosionFireParticles.AddQuantity = (float) (int) (20.0 * Math.Pow(power, 0.75));
        world.SpawnParticles(explosionFireParticles);
        
        AdvancedParticleProperties fireTrailCubicles = ExplosionParticles.ExplosionFireTrailCubicles;
        fireTrailCubicles.Velocity = new NatFloat[3]
        {
            NatFloat.createUniform(0.0f, 8f + x),
            NatFloat.createUniform(3f + x, 3f + x),
            NatFloat.createUniform(0.0f, 8f + x)
        };
        fireTrailCubicles.basePos.Set((double) pos.X + 0.5, (double) pos.Y + 0.5, (double) pos.Z + 0.5);
        fireTrailCubicles.GravityEffect = NatFloat.createUniform(0.5f, 0.0f);
        fireTrailCubicles.LifeLength = NatFloat.createUniform(1.5f * x, 0.5f);
        fireTrailCubicles.Quantity = NatFloat.createUniform(30f * x, 10f);
        float num7 = (float) Math.Pow((double) x, 0.75);
        fireTrailCubicles.Size = NatFloat.createUniform(0.5f * num7, 0.2f * num7);
        fireTrailCubicles.SecondaryParticles[0].Size = NatFloat.createUniform(0.25f * (float) Math.Pow((double) x, 0.5), 0.05f * num7);
        world.SpawnParticles((IParticlePropertiesProvider) fireTrailCubicles);
    }
    
    public static float RaycastForExposure(IWorldAccessor world, Vec3d from, Entity target) {
        var pos = target.ServerPos.XYZ;
        var box = target.CollisionBox;
        var exposure = 0f;
        for (int x = 0; x < ExposureResolution; x++) {
            for (int y = 0; y < ExposureResolution; y++) {
                for (int z = 0; z < ExposureResolution; z++) {
                    var xt = x / (float) (ExposureResolution - 1);
                    var yt = y / (float) (ExposureResolution - 1);
                    var zt = z / (float) (ExposureResolution - 1);
                    var targetPos = pos.AddCopy(
                        GameMath.Lerp(box.MinX, box.MaxX, xt),
                        GameMath.Lerp(box.MinY, box.MaxY, yt),
                        GameMath.Lerp(box.MinZ, box.MaxZ, zt)
                        );

                    BlockSelection? blockSelection = null;
                    EntitySelection? entitySelection = null;
                    //Will this work?
                    world.RayTraceForSelection(from, targetPos, ref blockSelection, ref entitySelection, (blockPos, block) => true, entity => false);

                    if (blockSelection == null) {
                        exposure += 1f / (ExposureResolution * ExposureResolution * ExposureResolution);
                    }
                }
            }
        }
        return exposure;
    }
}