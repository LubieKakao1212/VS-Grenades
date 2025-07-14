using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Grenades.Entities.Behavior;

public class EntityBehaviorParticleSpawner : EntityBehavior {

    [NotNull] private AdvancedParticleProperties? Particles { get; set; }

    private float _interval;
    private float _timer;
    
    public EntityBehaviorParticleSpawner(Entity entity) : base(entity) { }

    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        var attribs = attributes;
        _interval = attribs["spawnDelta"].AsFloat();

        var vSpread = attribs["vSpread"].AsFloat();
        var vRise = attribs["vRise"].AsFloat();
        var gravity = attribs["gravity"].AsObject<Vec2f>();
        var life = attribs["lifetime"].AsFloat();
        var size = attribs["size"].AsFloat();
        
        Particles = new AdvancedParticleProperties {
            ParticleModel = EnumParticleModel.Cube,
            Velocity = new[] {
                NatFloat.createGauss(0, vSpread),
                NatFloat.createUniform(vRise, 0),
                NatFloat.createGauss(0, vSpread)
            },
            GravityEffect = NatFloat.createUniform((gravity.X + gravity.Y) / 2f, (gravity.Y - gravity.X) / 2f),
            HsvaColor = new[]
            {
                NatFloat.createUniform(25f, 15f),
                NatFloat.createUniform(byte.MaxValue, 50f),
                NatFloat.createUniform(byte.MaxValue, 0f),
                NatFloat.createUniform(255f, 0f)
            },
            LifeLength = NatFloat.createUniform(life, 0),
            SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEARNULLIFY, -size/life),
            Quantity = NatFloat.createUniform(1, 0),
            Size = NatFloat.createUniform(size, 0),
            VertexFlags = 64
        };
    }

    public override string PropertyName() {
        return "grenades.particles";
    }

    public override void OnGameTick(float deltaTime) {
        base.OnGameTick(deltaTime);
        if (entity.World.Side != EnumAppSide.Server) {
            return;
        }
        
        _timer += deltaTime;
        if (_timer >= _interval) {
            _timer -= _interval;
            Particles.ParentVelocity = entity.Pos.Motion.ToVec3f();
            Particles.basePos = entity.Pos.XYZ;
            entity.World.SpawnParticles(Particles);
        }
    }
}