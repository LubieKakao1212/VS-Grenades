using Grenades.Entities;
using Grenades.Entities.Behavior;
using Grenades.Items;
using Vintagestory.API.Common;

namespace Grenades;

public class GrenadesModSystem : ModSystem {
    
    public override void Start(ICoreAPI api) {
        base.Start(api);

        api.RegisterEntity(Mod.Info.ModID + ".ExplosiveProjectile", typeof(EntityExplosiveProjectile));
        api.RegisterEntity(Mod.Info.ModID + ".ShrapnelProjectile", typeof(EntityShrapnelProjectile));
        api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".Particles", typeof(EntityBehaviorParticleSpawner));
        api.RegisterItemClass(Mod.Info.ModID + ".ThrownExplosive", typeof(ItemThrownExplosive));
    }
}