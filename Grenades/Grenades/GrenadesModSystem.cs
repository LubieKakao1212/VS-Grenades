using System;
using System.ComponentModel;
using Grenades.Config;
using Grenades.Entities;
using Grenades.Entities.Behavior;
using Grenades.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Grenades;

public class GrenadesModSystem : ModSystem {

    private static ConfigFile ServerConfig { get; set; } = new();
    private static ConfigFile ClientConfig { get; set; } = new();

    public override void Start(ICoreAPI api) {
        base.Start(api);

        api.RegisterEntity(Mod.Info.ModID + ".ExplosiveProjectile", typeof(EntityExplosiveProjectile));
        api.RegisterEntity(Mod.Info.ModID + ".ShrapnelProjectile", typeof(EntityShrapnelProjectile));
        api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".Particles", typeof(EntityBehaviorParticleSpawner));
        api.RegisterItemClass(Mod.Info.ModID + ".ThrownExplosive", typeof(ItemThrownExplosive));
    }

    public override void StartServerSide(ICoreServerAPI api) {
        try {
            var config = api.LoadModConfig<ConfigFile>("GrenadesServerConfig.json");
            if (config == null) {
                config = new ConfigFile();
            }

            config.ValidateAndApply(api.World);
            ServerConfig = config;
            api.StoreModConfig(ServerConfig, "GrenadesServerConfig.json");
            api.World.Config[Mod.Info.ModID + ".Config"] = config.Encode();
        }
        catch (Exception e) {
            Mod.Logger.Error("Could not load config! Loading default settings instead.");
            Mod.Logger.Error(e);
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        ClientConfig.Decode(api.World.Config.GetTreeAttribute(Mod.Info.ModID + ".Config"), api.World);
    }

    public static ConfigFile GetSidedConfig(EnumAppSide side) {
        if (side == EnumAppSide.Client) {
            return ClientConfig;
        }
        if (side == EnumAppSide.Server) {
            return ServerConfig;
        }

        throw new InvalidEnumArgumentException("Invalid side");
    }
}