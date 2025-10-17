using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace WeaponCustomizer.Server;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class PostSptLoad(ISptLogger<PostSptLoad> logger, WeaponCustomizer weaponCustomizer) : IOnLoad
{
    public async Task OnLoad()
    {
        await weaponCustomizer.Clean();

        logger.Success($"WeaponCustomizer loaded {weaponCustomizer.Database.Count} customizations");
    }
}
