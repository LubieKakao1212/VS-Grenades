using Vintagestory.API.Common.Entities;

namespace Grenades.Entities;

public interface IProjectile {
    
    Entity? FiredBy { get; set; }

    double Damage { get; set; }

    int DamageTier { get; set; }

}