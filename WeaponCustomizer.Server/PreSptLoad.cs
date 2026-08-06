using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

namespace WeaponCustomizer.Server;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public partial class PreSptLoad(WeaponCustomizer weaponCustomizer, IEnumerable<IRuntimePatch> patches) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }

        return weaponCustomizer.Load();
    }
}