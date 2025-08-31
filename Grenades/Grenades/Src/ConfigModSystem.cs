using Grenades.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Grenades;

public class ConfigModSystem : ModSystem {

    public ConfigFile2 Config { get; private set; } = new ConfigFile2();
    
    public override void StartServerSide(ICoreServerAPI api) {
        base.StartServerSide(api);
    }

    public override void AssetsLoaded(ICoreAPI api) {
        base.AssetsLoaded(api);
        if (api.Side == EnumAppSide.Server) {
            // api.LoadModConfig<ConfigFile2>();
            try {
                Config = api.LoadModConfig<ConfigFile2>("GrenadesServerConfig.json");
                if (Config == null) {
                    Config = new ConfigFile2();
                }
                var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore
                });
                var configObject = JToken.FromObject(Config, serializer);
                api.StoreModConfig(configObject, "GrenadesServerConfig.json");
                api.World.Config[Mod.Info.ModID + ".Config"] = new JsonObject(configObject).ToAttribute(); 
            }
            catch (Exception e) {
                Mod.Logger.Error("Could not load config! Loading default settings instead.");
                Mod.Logger.Error(e);
            }
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        base.StartClientSide(api);
        Config = JsonObject.FromJson(api.World.Config[Mod.Info.ModID + ".Config"].ToJsonToken()).AsObject<ConfigFile2>();
    }

    struct MergeTest {
        public float? f = null;
        public int? i = null;
        public bool? b = null;
        public M2? s = null;

        public MergeTest() {
        }
    }

    struct M2 {
        public float? inner = null;

        public M2() {
        }
    }
    
}