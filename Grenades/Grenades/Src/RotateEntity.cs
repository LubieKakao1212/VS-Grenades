using System;
using Vintagestory.API.Common.Entities;

namespace Grenades;

public class RotateEntity : EntityBehavior {
    
    public RotateEntity(Entity entity) : base(entity) {
        
    }

    public override string PropertyName() {
        return "rotate_entity";
    }

    public override void OnGameTick(float deltaTime) {
        base.OnGameTick(deltaTime);

        entity.ServerPos.Yaw += deltaTime;
        entity.ServerPos.Pitch = MathF.PI / 4;
    }
}