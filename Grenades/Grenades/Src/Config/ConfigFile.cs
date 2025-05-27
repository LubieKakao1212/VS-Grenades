using System.Collections.Generic;
using Grenades.Items;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Grenades.Config;

public class ConfigFile {

    [JsonProperty("damage")]
    private List<KeyValuePair<string, float>> _damageOverrides = new();
    [JsonProperty("damageTier")]
    private List<KeyValuePair<string, int>> _damageTierOverrides = new();
    [JsonProperty("radius")]
    private List<KeyValuePair<string, float>> _radiusOverrides = new();
    [JsonProperty("fullDamageRadius")]
    private List<KeyValuePair<string, float>> _fullDamageRadiusOverrides = new();
    [JsonProperty("fuse")]
    private List<KeyValuePair<string, float>> _fuseOverrides = new();
    [JsonProperty("throwingForce")]
    private List<KeyValuePair<string, float>> _forceOverrides = new();
    [JsonProperty("shrapnel")]
    private ShrapnelConfig _shrapnelConfig = new();
    
    private Dictionary<CollectibleObject, GrenadeOverrides> _bakedGrenadeOverrides = new ();
    
    public GrenadeOverrides GetGrenadeOverridesFor(CollectibleObject collectible) {
        return _bakedGrenadeOverrides.GetValueOrDefault(collectible, GrenadeOverrides.Default);
    }

    public void ValidateAndApply(IServerWorldAccessor world) {
        foreach (var item in world.Items) {
            if (item is not ItemThrownExplosive) {
                continue;
            }
            
            var damage = MatchValue(item.Code, _damageOverrides);
            var damageTier = MatchValue(item.Code, _damageTierOverrides);
            var radius = MatchValue(item.Code, _radiusOverrides);
            var fullRadius = MatchValue(item.Code, _fullDamageRadiusOverrides);
            var fuse = MatchValue(item.Code, _fuseOverrides);
            var force = MatchValue(item.Code, _forceOverrides);

            if (radius != null && fullRadius != null && radius.Value < fullRadius.Value) {
                fullRadius = radius;
                world.Logger.Warning($"[Grenades!] ConfigFile for {item.Code} has fullDamageRadius > radius, this is not allowed, clamping resulting value");
            }
            
            _bakedGrenadeOverrides.Add(item, 
                new GrenadeOverrides(
                    damage, damageTier, radius, fullRadius, fuse, force, 
                    _shrapnelConfig.Create(item)
                    )
                );
        }
    }

    public ITreeAttribute Encode() {
        var tree = new TreeAttribute();
        foreach (var entry in _bakedGrenadeOverrides) {
            tree.SetAttribute(entry.Key.Code.ToString(), entry.Value.Encode());
        }
        return tree;
    }
    
    public void Decode(ITreeAttribute attributes, IWorldAccessor world) {
        _bakedGrenadeOverrides.Clear();
        foreach (var entry in attributes) {
            if (entry.Value is not ITreeAttribute tree) {
                continue;
            }
            _bakedGrenadeOverrides.Add(world.GetItem(new AssetLocation(entry.Key)), new GrenadeOverrides(null, null, null, null, null, null, null).Decode(tree));
        }
    }
    
    private static T? MatchValue<T>(AssetLocation target, List<KeyValuePair<string, T>> values) where T : struct {
        foreach (var pair in values) {
            var location = new AssetLocation("*", pair.Key);
            if (WildcardUtil.Match(location, target, null)) {
                return pair.Value;
            }
        }
        return default;
    }
    
    public class GrenadeOverrides {
        public static readonly GrenadeOverrides Default = new GrenadeOverrides(null, null, null, null, null, null, null);

        public ShrapnelOverrides Shrapnel => _shrapnel;
        
        private double? _damage;
        private int? _damageTier;
        private double? _radius;
        private double? _fullRadius;
        private double? _force;
        private double? _fuse;

        private ShrapnelOverrides _shrapnel;
        
        public GrenadeOverrides(double? damage, int? damageTier, double? radius, double? fullRadius, double? fuse, double? force, ShrapnelOverrides? shrapnel) {
            _fullRadius = fullRadius;
            _force = force;
            _fuse = fuse;
            _radius = radius;
            _damage = damage;
            _damageTier = damageTier;
            _shrapnel = shrapnel ?? ShrapnelOverrides.Default;
        }

        public double GetDamage(double defaultDamage) {
            return _damage.GetValueOrDefault(defaultDamage);
        }
        
        public int GetDamageTier(int defaultDamageTier) {
            return _damageTier.GetValueOrDefault(defaultDamageTier);
        }
        
        public double GetRadius(double defaultRadius) {
            return _radius.GetValueOrDefault(defaultRadius);
        }
        
        public double GetFullRadius(double defaultFullRadius) {
            return _fullRadius.GetValueOrDefault(defaultFullRadius);
        }
        
        public double GetFuse(double defaultFuse) {
            return _fuse.GetValueOrDefault(defaultFuse);
        }
        
        public double GetThrowingForce(double defaultForce) {
            return _force.GetValueOrDefault(defaultForce);
        }

        public ITreeAttribute Encode() {
            var tree = new TreeAttribute();

            if (_damage != null) {
                tree.SetDouble(nameof(_damage), _damage.Value);
            }
            if (_damageTier != null) {
                tree.SetInt(nameof(_damageTier), _damageTier.Value);
            }
            if (_radius != null) {
                tree.SetDouble(nameof(_radius), _radius.Value);
            }
            if (_fullRadius != null) {
                tree.SetDouble(nameof(_fullRadius), _fullRadius.Value);
            }
            if (_force != null) {
                tree.SetDouble(nameof(_force), _force.Value);
            }
            if (_fuse != null) {
                tree.SetDouble(nameof(_fuse), _fuse.Value);
            }
            tree.Set(nameof(_shrapnel), _shrapnel.Encode());

            return tree;
        }

        public GrenadeOverrides Decode(ITreeAttribute attributes) {
            if (attributes.HasAttribute(nameof(_damage))) {
                _damage = attributes.GetDouble(nameof(_damage));
            }
            if (attributes.HasAttribute(nameof(_damageTier))) {
                _damageTier = attributes.GetInt(nameof(_damageTier));
            }
            
            if (attributes.HasAttribute(nameof(_radius))) {
                _radius = attributes.GetDouble(nameof(_radius));
            }
            if (attributes.HasAttribute(nameof(_fullRadius))) {
                _fullRadius = attributes.GetDouble(nameof(_fullRadius));
            }
            if (attributes.HasAttribute(nameof(_force))) {
                _force = attributes.GetDouble(nameof(_force));
            }
            if (attributes.HasAttribute(nameof(_fuse))) {
                _fuse = attributes.GetDouble(nameof(_fuse));
            }
            if (attributes.HasAttribute(nameof(_shrapnel))) {
                _shrapnel = new ShrapnelOverrides(null, null, null,null, null, null);
                _shrapnel.Decode(attributes.GetTreeAttribute(nameof(_shrapnel)));
            }
            
            return this;
        }
    }

    public class ShrapnelConfig {
        [JsonProperty("damage")]
        public List<KeyValuePair<string, float>> damageOverrides = new();
        [JsonProperty("damageTier")]
        public List<KeyValuePair<string, int>> damageTierOverrides = new();
        
        [JsonProperty("amount")]
        public List<KeyValuePair<string, float>> amountOverrides = new();
        [JsonProperty("amountRandomness")]
        public List<KeyValuePair<string, int>> amountRandomnessOverrides = new();
        
        [JsonProperty("lifetime")]
        public List<KeyValuePair<string, float>> lifetimeOverrides = new();
        [JsonProperty("lifetimeRandomness")]
        public List<KeyValuePair<string, int>> lifetimeRandomnessOverrides = new();

        public ShrapnelOverrides Create(Item item) {
            var damage = MatchValue(item.Code, damageOverrides);
            var damageTier = MatchValue(item.Code, damageTierOverrides);
            var amount = MatchValue(item.Code, amountOverrides);
            var amountRandomness = MatchValue(item.Code, amountRandomnessOverrides);
            var lifetime = MatchValue(item.Code, lifetimeOverrides);
            var lifetimeRandomness = MatchValue(item.Code, lifetimeRandomnessOverrides);

            return new ShrapnelOverrides(damage, damageTier, amount, amountRandomness, lifetime, lifetimeRandomness);
        }
    }

    public class ShrapnelOverrides {
        public static ShrapnelOverrides Default => new ShrapnelOverrides(null, null, null,null, null, null);
        
        private double? _damage;
        private int? _damageTier;
        private double? _amount;
        private double? _amountRandomness;
        private double? _lifetime;
        private double? _lifetimeRandomness;

        public ShrapnelOverrides(double? damage, int? damageTier, double? amount, double? amountRandomness, double? lifetime, double? lifetimeRandomness) {
            _damage = damage;
            _damageTier = damageTier;
            _amount = amount;
            _amountRandomness = amountRandomness;
            _lifetime = lifetime;
            _lifetimeRandomness = lifetimeRandomness;
        }
        
        public double GetDamage(double defaultDamage) {
            return _damage.GetValueOrDefault(defaultDamage);
        }
        public int GetDamageTier(int defaultDamageTier) {
            return _damageTier.GetValueOrDefault(defaultDamageTier);
        }
        
        public double GetAmount(double defaultAmount) {
            return _amount.GetValueOrDefault(defaultAmount);
        }
        public double GetAmountRandomness(double defaultAmountRandomness) {
            return _amountRandomness.GetValueOrDefault(defaultAmountRandomness);
        }
        
        public double GetLifetime(double defaultLifetime) {
            return _lifetime.GetValueOrDefault(defaultLifetime);
        }
        public double GetLifetimeRandomness(double defaultLifetimeRandomness) {
            return _lifetimeRandomness.GetValueOrDefault(defaultLifetimeRandomness);
        }
        
        public ITreeAttribute Encode() {
            var tree = new TreeAttribute();

            if (_damage != null) {
                tree.SetDouble(nameof(_damage), _damage.Value);
            }
            if (_damageTier != null) {
                tree.SetInt(nameof(_damageTier), _damageTier.Value);
            }
            if (_amount != null) {
                tree.SetDouble(nameof(_amount), _amount.Value);
            }
            if (_amountRandomness != null) {
                tree.SetDouble(nameof(_amountRandomness), _amountRandomness.Value);
            }
            if (_lifetime != null) {
                tree.SetDouble(nameof(_lifetime), _lifetime.Value);
            }
            if (_lifetimeRandomness != null) {
                tree.SetDouble(nameof(_lifetimeRandomness), _lifetimeRandomness.Value);
            }

            return tree;
        }

        public ShrapnelOverrides Decode(ITreeAttribute attributes) {
            if (attributes.HasAttribute(nameof(_damage))) {
                _damage = attributes.GetDouble(nameof(_damage));
            }
            if (attributes.HasAttribute(nameof(_damageTier))) {
                _damageTier = attributes.GetInt(nameof(_damageTier));
            }
            
            if (attributes.HasAttribute(nameof(_amount))) {
                _amount = attributes.GetDouble(nameof(_amount));
            }
            if (attributes.HasAttribute(nameof(_amountRandomness))) {
                _amountRandomness = attributes.GetDouble(nameof(_amountRandomness));
            }
            if (attributes.HasAttribute(nameof(_lifetime))) {
                _lifetime = attributes.GetDouble(nameof(_lifetime));
            }
            if (attributes.HasAttribute(nameof(_lifetimeRandomness))) {
                _lifetimeRandomness = attributes.GetDouble(nameof(_lifetimeRandomness));
            }

            return this;
        }
    }
    
}