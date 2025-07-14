using System;
using System.IO;
using Grenades.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Grenades.Util;

public static class ExplosionUtil {

    private static readonly Random _shrapnelRandom = new Random();
    
    public const int ExposureResolution = 3;
    
    //TODO add 
    public static void DoExplosionDamage(this IWorldAccessor world, Vec3d pos, float peakDamage, int damageTier, float radius, float fullDamageRadius, float knockback, Entity? source = null, Entity? cause = null) {
        var damageSource = new DamageSource() {
            Source = EnumDamageSource.Explosion,
            Type = EnumDamageType.BluntAttack,
            SourcePos = pos,
            CauseEntity = cause,
            DamageTier = damageTier,
            SourceEntity = source,
            KnockbackStrength = knockback,
        };
        
        var entities = world.GetEntitiesAround(pos, radius, radius, entity => entity.ShouldReceiveDamage(damageSource, peakDamage));
        
        foreach (var entity in entities) {
            if(entity == source) {
                continue;
            }
            
            //TODO check distance
            var exposure = RaycastForExposure(world, pos, entity);
            var distance = (float)entity.CollisionBox.ShortestDistanceFrom(pos.SubCopy(entity.ServerPos.XYZ));
            if (Math.Abs(radius - fullDamageRadius) < 1f / 64f) {
                fullDamageRadius -= 1f / 64f;
            }
            
            var distanceFactor = (radius - distance) / radius;
            distanceFactor /= 1 - fullDamageRadius/radius;
            var damage = peakDamage * exposure * Math.Clamp(distanceFactor, 0, 1);

            if (damage >= 0.25) {
                entity.ReceiveDamage(damageSource, damage);
            }
        }
    }

    public static void DoExplosionEffects(this IWorldAccessor world, Vec3d pos, float power, float radius) {
        AdvancedParticleProperties explosionFireParticles = new AdvancedParticleProperties();// ExplosionParticles.ExplosionFireParticles.CloneParticles(world);
        
        float x = power / 3f;
        float r = radius / 3f;
        float r2 = r * 100f;
        
        // explosionFireParticles.MinPos.Set(pos.X - r, pos.Y - r, pos.Z - r);
        // explosionFireParticles.MinQuantity = 100f * r;
        // explosionFireParticles.AddQuantity = (int) (20.0 * Math.Pow(power, 0.75));
        // explosionFireParticles.AddPos.Set(r * 2f, r * 2f, r * 2f);
        explosionFireParticles.HsvaColor = new NatFloat[]
        {
            NatFloat.createUniform(25f, 15f),
            NatFloat.createUniform(byte.MaxValue, 50f),
            NatFloat.createUniform(byte.MaxValue, 0f),
            NatFloat.createUniform(200f, 30f)
        };
        explosionFireParticles.PosOffset = new NatFloat[]
        {
            NatFloat.createGauss(0.0f, r),
            NatFloat.createGauss(0.0f, r),
            NatFloat.createGauss(0.0f, r)
        };
        explosionFireParticles.LifeLength = NatFloat.createUniform(0.2f, 0f);
        explosionFireParticles.basePos = pos;
        explosionFireParticles.Size = NatFloat.createGauss(0.1f + r * 4f, r / 4f);
        explosionFireParticles.Velocity = new[] {
            NatFloat.createGauss(0f, 5f),
            NatFloat.createGauss(0f + 0.5f, 5f),
            NatFloat.createGauss(0f, 5f),
        };
        explosionFireParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEARNULLIFY, -(0.1f + r * 4.25f));
        explosionFireParticles.GravityEffect = NatFloat.createUniform(-1f, 0.5f);
        explosionFireParticles.Quantity = NatFloat.createGauss(r2, r2/4f);
        explosionFireParticles.VertexFlags = 64;
        world.SpawnParticles(explosionFireParticles);
        
        AdvancedParticleProperties fireTrailCubicles = ExplosionParticles.ExplosionFireTrailCubicles;
        fireTrailCubicles.Velocity = new NatFloat[]
        {
            NatFloat.createUniform(0.0f, 2f + r),
            NatFloat.createUniform(3f + r, 3f + r),
            NatFloat.createUniform(0.0f, 2f + r)
        };
        fireTrailCubicles.basePos.Set(pos.X, pos.Y,  pos.Z);
        fireTrailCubicles.GravityEffect = NatFloat.createUniform(0.5f, 0.0f);
        var lifeDuration = MathF.Pow(x, 0.25f);
        fireTrailCubicles.LifeLength = NatFloat.createUniform(lifeDuration, lifeDuration / 3f);
        var quantity = MathF.Pow(x, 1.5f);
        fireTrailCubicles.Quantity = NatFloat.createUniform(30f * quantity, 10f);
        float num7 = (float) Math.Pow(x, 0.75);
        fireTrailCubicles.Size = NatFloat.createUniform(num7, 0.5f * num7);
        fireTrailCubicles.SecondaryParticles[0].Size = NatFloat.createUniform(0.25f * (float) Math.Pow(x, 0.5), 0.05f * num7);
        world.SpawnParticles(fireTrailCubicles);

        var soundPower = power * 0.75f + radius * 0.25f;
        string str = "effect/smallexplosion";
        if (soundPower > 12.0) {
            str = "effect/largeexplosion";
        }
        else if (soundPower > 6.0) {
            str = "effect/mediumexplosion";
        }

        world.PlaySoundAt("sounds/" + str, pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5, randomizePitch: false, range: (float) (24.0 * Math.Pow(radius, 0.5)));

    }

    public static void DoShrapnel(this IWorldAccessor world, Vec3d pos, ShrapnelSpawnData spawnData, Entity? cause = null) {
        var cr = world.ClassRegistry;
        
        var entityCode = spawnData.ShrapnelLocation;
        EntityProperties entityType = world.GetEntityType(entityCode);

        var values = spawnData.Values;

        var amount = (int)MathF.Round(values.Amount.Sample(_shrapnelRandom));

        var cosPhiMin = MathF.Sin(GameMath.DEG2RAD * spawnData.Values.DirectionPitchMin);
        var cosPhiMax = MathF.Sin(GameMath.DEG2RAD * spawnData.Values.DirectionPitchMax);
        
        for (int i = 0; i < amount; i++) {
            var entity = cr.CreateEntity(entityType);

            var speed = values.Velocity.Sample(_shrapnelRandom);

            var theta = _shrapnelRandom.NextSingle() * MathF.PI * 2;
            var cosPhi = cosPhiMin + (cosPhiMax - cosPhiMin) * _shrapnelRandom.NextSingle();
            var phi = MathF.Acos(cosPhi);

            var sinPhi = MathF.Sin(phi);

            var forward = new Vec3d(
                sinPhi * MathF.Cos(theta),
                cosPhi,
                sinPhi * MathF.Sin(theta)
            );
                        
            var velocity = forward.Mul(speed).Add(spawnData.VelocityBias);
            
            var sPos = entity.ServerPos;
            sPos.ApplyImpulse(velocity);
            sPos.SetPos(pos + forward.Mul(0.1f)); // We do not use Clone() since forward will not be used anymore
            entity.Pos.SetFrom(sPos);
            if (entity is IProjectile projectile) {
                projectile.Damage = values.Damage;
                projectile.DamageTier = values.DamageTier;
                projectile.FiredBy = cause;
            }
            if (entity is IEntityLifetime entLife) {
                entLife.Lifetime = values.Lifetime.Sample(_shrapnelRandom);
            }
            
            world.SpawnEntity(entity);
        }
    }
    
    public static float RaycastForExposure(IWorldAccessor world, Vec3d from, Entity target) {
        var pos = target.ServerPos.XYZ;
        var box = target.CollisionBox.OffsetCopy(
            (float) pos.X,
            (float) pos.Y,
            (float) pos.Z);

        if (box.Contains(from.X, from.Y, from.Z)) {
            return 1f;
        }
        
        var exposure = 0f;
        for (int x = 0; x < ExposureResolution; x++) {
            for (int y = 0; y < ExposureResolution; y++) {
                for (int z = 0; z < ExposureResolution; z++) {
                    var xt = x / (float) (ExposureResolution - 1);
                    var yt = y / (float) (ExposureResolution - 1);
                    var zt = z / (float) (ExposureResolution - 1);
                    var targetPos = new Vec3d(
                        GameMath.Lerp(box.MinX, box.MaxX, xt),
                        GameMath.Lerp(box.MinY, box.MaxY, yt),
                        GameMath.Lerp(box.MinZ, box.MaxZ, zt)
                        );
                    
                    
                    BlockSelection? blockSelection = null;
                    EntitySelection? entitySelection = null;
                    //Will this work?
                    world.RayTraceForSelection(from, targetPos, ref blockSelection, ref entitySelection, (blockPos, block) => true, entity => entity == target);
                    
                    if (blockSelection != null) {
                        bool flag = box.Contains(
                            blockSelection.FullPosition.X,
                            blockSelection.FullPosition.Y,
                            blockSelection.FullPosition.Z);
                        if (!flag) {
                            continue;
                        }
                    }
                    exposure += 1f;
                }
            }
        }
        return exposure / (ExposureResolution * ExposureResolution * ExposureResolution);
    }
    
    public static SimpleParticleProperties CloneParticles(this SimpleParticleProperties particles, IWorldAccessor worldForResovle) 
    {
        SimpleParticleProperties particleProperties = new SimpleParticleProperties();
        using var memoryStream = new MemoryStream();
        
        using var writer = new BinaryWriter(memoryStream);
        particles.ToBytes(writer);
        
        memoryStream.Position = 0L;
        using var reader = new BinaryReader(memoryStream);
        particleProperties.FromBytes(reader, worldForResovle);
        
        return particleProperties;
    }
}