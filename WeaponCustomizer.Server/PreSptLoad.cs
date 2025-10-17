using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace WeaponCustomizer.Server;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public partial class PreSptLoad(WeaponCustomizer weaponCustomizer) : IOnLoad
{
    public Task OnLoad()
    {
        new ReplaceIdsPatch().Enable();

        return weaponCustomizer.Load();
    }
}