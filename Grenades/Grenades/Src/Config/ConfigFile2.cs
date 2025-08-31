using System.Collections.Generic;
using Grenades.Items;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Grenades.Config;

public class ConfigFile2 {

    [JsonProperty("overrides")]
    private List<KeyValuePair<string, ConfigGrenadeStatValues>> _overrides = new();

    // private Dictionary<CollectibleObject, ConfigGrenadeStatValues> _bakedOverrides = new();
    //
    public void Apply(ref DefGrenadeStatValues values, AssetLocation code, ILogger? logger) {
        var configOverride = new ConfigGrenadeStatValues();
        foreach (var pair in _overrides) {
            var location = new AssetLocation("*", pair.Key);
            if (WildcardUtil.Match(location, code, null)) {
                configOverride = Merger.MergeRecursive<ConfigGrenadeStatValues>(configOverride, pair.Value).Value;
            }
        }
        
        values = values.MergeNotNullRecursive<DefGrenadeStatValues, ConfigGrenadeStatValues>(configOverride);
        if (values.InnerRadius < 0) {
            values.InnerRadius = 0;
            logger?.Warning($"[Grenades!] ConfigFile for {code} has innerRadius < 0, this is not allowed, clamping resulting value");
        }
        if (values.InnerRadius > 1) {
            values.InnerRadius = 1;
            logger?.Warning($"[Grenades!] ConfigFile for {code} has innerRadius > 1, this is not allowed, clamping resulting value");
        }
    }
    
    // public void ValidateAndApply(IServerWorldAccessor world) {
    //     foreach (var item in world.Items) {
    //         if (item is not ItemThrownExplosive explosive) {
    //             continue;
    //         }
    //
    //         var code = item.Code;
    //         var configOverride = new ConfigGrenadeStatValues();
    //         foreach (var pair in _overrides) {
    //             var location = new AssetLocation("*", pair.Key);
    //             if (WildcardUtil.Match(location, code, null)) {
    //                 Merger.MergeRecursive<ConfigGrenadeStatValues>(configOverride, pair.Value);
    //             }
    //         }
    //         
    //         if (configOverride.Radius != null && configOverride.InnerRadius != null && configOverride.Radius.Value < configOverride.InnerRadius.Value) {
    //             configOverride.InnerRadius = configOverride.Radius;
    //             world.Logger.Warning($"[Grenades!] ConfigFile for {item.Code} has fullDamageRadius > radius, this is not allowed, clamping resulting value");
    //         }
    //         
    //         _bakedOverrides.Add(item, configOverride);
    //     }
    // }
    
    // public struct ConfigValues {
    //     public float? damage = null;
    //     public float? damageTier = null;
    //     public float? radius = null;
    //     public float? innerRadius = null;
    //     public float? fuse = null;
    //     public float? knockback = null;
    //     public float? mass = null;
    //
    //     public Shrapnel? shrapnel = null;
    //
    //     public ConfigValues() {
    //     }
    // }
    //
    // public struct Shrapnel {
    //     public bool? enabled = null;
    //     public float? damage = null;
    //     public float? damageTier = null;
    //     public RandomFloat? amount = null;
    //     public RandomFloat? lifetime = null;
    //
    //     public Shrapnel() {
    //     }
    // }
    //
    // public struct RandomFloat {
    //     public float? value;
    //     public float? randomness;
    // }
}