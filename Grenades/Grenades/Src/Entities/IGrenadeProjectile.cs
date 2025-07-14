using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Grenades.Entities;

public interface IGrenadeProjectile {
    
    DefGrenadeStatValues GrenadeStats { get; set; }

    Entity FiredBy { get; set; }
    ItemStack GrenadeStack { get; set; }

}