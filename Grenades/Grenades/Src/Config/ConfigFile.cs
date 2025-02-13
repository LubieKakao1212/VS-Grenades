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
    private List<KeyValuePair<string, int>> _damageTier = new();
    [JsonProperty("radius")]
    private List<KeyValuePair<string, float>> _radiusOverrides = new();
    [JsonProperty("fullDamageRadius")]
    private List<KeyValuePair<string, float>> _fullDamageRadiusOverrides = new();
    [JsonProperty("fuse")]
    private List<KeyValuePair<string, float>> _fuseOverrides = new();
    [JsonProperty("throwingForce")]
    private List<KeyValuePair<string, float>> _forceOverrides = new();

    private Dictionary<CollectibleObject, GrenadeOverrides> _bakedOverrides = new ();
    
    public GrenadeOverrides GetExplosionOverridesFor(CollectibleObject collectible) {
        return _bakedOverrides.GetValueOrDefault(collectible, GrenadeOverrides.Default);
    }

    public void ValidateAndApply(IServerWorldAccessor world) {
        foreach (var item in world.Items) {
            if (item is not ItemThrownExplosive) {
                continue;
            }
            
            var damage = MatchValue(item.Code, _damageOverrides);
            var damageTier = MatchValue(item.Code, _damageTier);
            var radius = MatchValue(item.Code, _radiusOverrides);
            var fullRadius = MatchValue(item.Code, _fullDamageRadiusOverrides);
            var fuse = MatchValue(item.Code, _fuseOverrides);
            var force = MatchValue(item.Code, _forceOverrides);

            if (radius != null && fullRadius != null && radius.Value < fullRadius.Value) {
                fullRadius = radius;
                world.Logger.Warning($"[Grenades!] ConfigFile for {item.Code} has fullDamageRadius > radius, this is not allowed, clamping resulting value");
            }
            
            _bakedOverrides.Add(item, new GrenadeOverrides(damage, damageTier, radius, fullRadius, fuse, force));
        }
    }

    public ITreeAttribute Encode() {
        var tree = new TreeAttribute();
        foreach (var entry in _bakedOverrides) {
            tree.SetAttribute(entry.Key.Code.ToString(), entry.Value.Encode());
        }
        return tree;
    }
    
    public void Decode(ITreeAttribute attributes, IWorldAccessor world) {
        _bakedOverrides.Clear();
        foreach (var entry in attributes) {
            if (entry.Value is not ITreeAttribute tree) {
                continue;
            }
            _bakedOverrides.Add(world.GetItem(new AssetLocation(entry.Key)), new GrenadeOverrides(null, null, null, null, null, null).Decode(tree));
        }
    }
    
    private T? MatchValue<T>(AssetLocation target, List<KeyValuePair<string, T>> values) where T : struct {
        foreach (var pair in values) {
            var location = new AssetLocation("*", pair.Key);
            if (WildcardUtil.Match(location, target, null)) {
                return pair.Value;
            }
        }
        return default;
    }
    
    public class GrenadeOverrides {
        public static readonly GrenadeOverrides Default = new GrenadeOverrides(null, null, null, null, null, null);
        
        private double? _damage;
        private int? _damageTier;
        private double? _radius;
        private double? _fullRadius;
        private double? _force;
        private double? _fuse;

        public GrenadeOverrides(double? damage, int? damageTier, double? radius, double? fullRadius, double? fuse, double? force) {
            _fullRadius = fullRadius;
            _force = force;
            _fuse = fuse;
            _radius = radius;
            _damage = damage;
            _damageTier = damageTier;
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

            return this;
        }
    }
}