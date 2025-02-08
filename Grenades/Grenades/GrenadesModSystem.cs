using System;
using Grenades.Entities;
using Grenades.Items;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Grenades;

public class GrenadesModSystem : ModSystem {
    
    public override void Start(ICoreAPI api) {
        base.Start(api);
        
        api.RegisterEntity(Mod.Info.ModID + ".ExplosiveProjectile", typeof(EntityExplosiveProjectile));
        api.RegisterItemClass(Mod.Info.ModID + ".ThrownExplosive", typeof(ItemThrownExplosive));
    }
}