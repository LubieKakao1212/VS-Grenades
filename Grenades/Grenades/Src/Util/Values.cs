using System;
using Newtonsoft.Json;
using Vintagestory.GameContent;

namespace Grenades.Util;

public struct GrenadeStatValues<TFloat, TInt, TShrapnel>  {
    
    [JsonProperty("damage")] public TFloat Damage { get; internal set; } = default!;
    [JsonProperty("damageTier")] public TInt DamageTier { get; internal set; } = default!;
    [JsonProperty("radius")] public TFloat Radius { get; internal set; } = default!;
    [JsonProperty("innerRadius")] public TFloat InnerRadius { get; internal set; } = default!;
    [JsonProperty("fuse")] public TFloat Fuse  { get; internal set; } = default!;
    [JsonProperty("knockback")] public TFloat Knockback { get; internal set; } = default!; //TODO implement
    [JsonProperty("launchForce")] public TFloat LaunchForce { get; internal set; } = default!;

    public TShrapnel Shrapnel { get; init; } = default!;

    public GrenadeStatValues() {
    }
}

public struct ShrapnelValues<TFloat, TInt, TBool, TRandom> {

    [JsonProperty("enabled")] public TBool Enabled { get; internal set; } = default!;
    [JsonProperty("damage")] public TFloat Damage { get; internal set; } = default!;
    [JsonProperty("damageTier")] public TInt DamageTier { get; internal set; } = default!;
    [JsonProperty("amount")] public TRandom Amount { get; internal set; } = default!;
    [JsonProperty("lifetime")] public TRandom Lifetime { get; internal set; } = default!;
    [JsonProperty("velocity")] public TRandom Velocity { get; internal set; } = default!;
    [JsonProperty("velocityInheritance")] public TFloat VelocityInheritance { get; internal set; } = default!;
    [JsonProperty("directionPitchMin")] public TFloat DirectionPitchMin { get; internal set; } = default!;
    [JsonProperty("directionPitchMax")] public TFloat DirectionPitchMax { get; internal set; } = default!;

    public ShrapnelValues() {
    }

}

public struct RandomFloat<TFloat> {

    [JsonProperty("value")] public required TFloat Value { get; set; }

    [JsonProperty("randomness")] public TFloat Randomness { get; internal set; } = default!;

    public RandomFloat() {
    }
}

public static class ValueExtensions {

    public static float Sample(this RandomFloat<float> def, Random random) {
        return random.NextSingle() * def.Randomness * 2f + (def.Value - def.Randomness);
    }
    
}