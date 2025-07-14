using System.Text;
using Vintagestory.API.Common;

namespace Grenades.Util;

public static class VariantUtil {
    
    public static AssetLocation WithVariants(this AssetLocation location, IDictionary<string, string> replacements) { 
        var locationStr = new StringBuilder(location);
        foreach (var replacement in replacements) {
            locationStr.Replace($"{{{replacement.Key}}}", replacement.Value);
        }
        return locationStr.ToString();
    }
}