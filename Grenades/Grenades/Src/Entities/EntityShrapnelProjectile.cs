using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Grenades.Entities;

public class EntityShrapnelProjectile : Entity, IProjectile, IEntityLifetime {

    protected bool _persists;
    protected int _maxHitCount = int.MaxValue;
    
    public EnumDamageType damageType = EnumDamageType.PiercingAttack;
    
    [NotNull] protected EntityPartitioning? Ep { get; set; }
    [NotNull] public Entity? FiredBy { get; set; }
    public double Damage { get; set; }
    public int DamageTier { get; set; }
    public double Lifetime { get; set; }

    private ConcurrentQueue<Entity> _entitiesHit = new();

    private float _damageRadius;
    
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d) {
        base.Initialize(properties, api, InChunkIndex3d);
        GetBehavior<EntityBehaviorPassivePhysics>().OnPhysicsTickCallback = OnPhysicsTickCallback;
        Ep = api.ModLoader.GetModSystem<EntityPartitioning>();

        damageType = properties.Attributes["damageType"].AsObject<EnumDamageType>();
        _persists = properties.Attributes["persists"].AsBool();
        _damageRadius = properties.Attributes["damageRadius"].AsFloat();
        // Lifetime = properties.Attributes["lifetime"].AsFloat();
    }

    private void OnPhysicsTickCallback(float dt) {
        var sidedPos = SidedPos;
        Cuboidd projectileBox = SelectionBox.ToDouble().Translate(sidedPos.X, sidedPos.Y, sidedPos.Z);
        
        if (sidedPos.Motion.X < 0.0)
            projectileBox.X1 += sidedPos.Motion.X * dt;
        else
            projectileBox.X2 += sidedPos.Motion.X * dt;
        if (sidedPos.Motion.Y < 0.0)
            projectileBox.Y1 += sidedPos.Motion.Y * dt;
        else
            projectileBox.Y2 += sidedPos.Motion.Y * dt;
        if (sidedPos.Motion.Z < 0.0)
            projectileBox.Z1 += sidedPos.Motion.Z * dt;
        else
            projectileBox.Z2 += sidedPos.Motion.Z * dt;

        var center = (projectileBox.Start + projectileBox.End) / 2;

        var box = new Cuboidd().GrowBy(_damageRadius, _damageRadius, _damageRadius).Translate(center);
            
        Ep.WalkEntities(center, _damageRadius, (ActionConsumable<Entity>) (e => {
            // if (this.entitiesHit.Contains(e.EntityId))
            //     return false;
            if (box.Intersects(e.SelectionBox, e.Pos.XYZ)) {
                _entitiesHit.Enqueue(e);
            }
            return false;
        }), EnumEntitySearchType.Creatures);
    }
    
    protected void ImpactOnEntity(Entity entity) {
        if (World.Api is not ICoreServerAPI api) {
            return;
        }
        
        if (entity.IsActivityRunning("invulnerable") || _maxHitCount <= 0) {
            return;
        }
        
        var serverPlayer = (IServerPlayer?) null;
        
        if (FiredBy is EntityPlayer entityPlayer)
            serverPlayer = entityPlayer.Player as IServerPlayer;
        
        var flag1 = entity is EntityPlayer;
        var flag2 = entity is EntityAgent;
        var flag3 = true;
        if (serverPlayer != null)
        {
            if (flag1 && (!api.Server.Config.AllowPvP || !serverPlayer.HasPrivilege("attackplayers")))
                flag3 = false;
            if (flag2 && !serverPlayer.HasPrivilege("attackcreatures"))
                flag3 = false;
        }

        if (flag3) {
            _maxHitCount--;

            var source = new DamageSource {
                Source = EnumDamageSource.Entity,
                Type = damageType,
                DamageTier = DamageTier,
                SourceEntity = this,
                CauseEntity = FiredBy,
                KnockbackStrength = 0
            };
            
            entity.ReceiveDamage(source, (float)Damage);
        }
    }

    public override void OnGameTick(float dt) {
        base.OnGameTick(dt);
        if (World.Side == EnumAppSide.Server) {
            if ((Lifetime -= dt) <= 0 || (Collided && !_persists)) {
                Die(EnumDespawnReason.Expire);
            }
            while (_entitiesHit.TryDequeue(out var entity)) {
                ImpactOnEntity(entity);
            }
        }
    }
}