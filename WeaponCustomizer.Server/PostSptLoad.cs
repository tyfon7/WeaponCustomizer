using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace WeaponCustomizer.Server;

[Injectable(TypePriority = OnLoadOrder.PostLoad)]
public class PostSptLoad(ISptLogger<PostSptLoad> logger, WeaponCustomizer weaponCustomizer) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await weaponCustomizer.Clean();

        if (weaponCustomizer.Database.Count > 0)
        {
            var customizedWeapons = weaponCustomizer.Database.Values.Where(c => c.CustomizedType == CustomizedObject.Type.Weapon);
            var customizedPresets = weaponCustomizer.Database.Values.Where(c => c.CustomizedType == CustomizedObject.Type.Preset);
            logger.LogWithColor($"WeaponCustomizer loaded {customizedWeapons.Count()} customized weapons and {customizedPresets.Count()} customized presets", Color.Cyan);
        }
    }
}