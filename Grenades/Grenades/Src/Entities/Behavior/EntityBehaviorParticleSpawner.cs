using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Grenades.Entities.Behavior;

public class EntityBehaviorParticleSpawner : EntityBehavior {

    private AdvancedParticleProperties particles;
    private float interval;
    private float timer;
    
    public EntityBehaviorParticleSpawner(Entity entity) : base(entity) { }

    public override void Initialize(EntityProperties properties, JsonObject attributes) {
        var attribs = attributes;
        interval = attribs["spawnDelta"].AsFloat();

        var vSpread = attribs["vSpread"].AsFloat();
        var vRise = attribs["vRise"].AsFloat();
        var gravity = attribs["gravity"].AsObject<Vec2f>();
        var life = attribs["lifetime"].AsFloat();
        var size = attribs["size"].AsFloat();
        
        particles = new AdvancedParticleProperties {
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
        
        timer += deltaTime;
        if (timer >= interval) {
            timer -= interval;
            particles.ParentVelocity = entity.Pos.Motion.ToVec3f();
            particles.basePos = entity.Pos.XYZ;
            entity.World.SpawnParticles(particles);
        }
    }
}