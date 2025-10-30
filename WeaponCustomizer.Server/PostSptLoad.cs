using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;

namespace WeaponCustomizer.Server;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class PostSptLoad(ISptLogger<PostSptLoad> logger, WeaponCustomizer weaponCustomizer) : IOnLoad
{
    public async Task OnLoad()
    {
        await weaponCustomizer.Clean();

        if (weaponCustomizer.Database.Count > 0)
        {
            var customizedWeapons = weaponCustomizer.Database.Values.Where(c => c.CustomizedType == CustomizedObject.Type.Weapon);
            var customizedPresets = weaponCustomizer.Database.Values.Where(c => c.CustomizedType == CustomizedObject.Type.Preset);
            logger.LogWithColor($"WeaponCustomizer loaded {customizedWeapons.Count()} customized weapons and {customizedPresets.Count()} customized presets", LogTextColor.Cyan);
        }
    }
}
