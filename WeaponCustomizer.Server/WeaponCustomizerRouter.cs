using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace WeaponCustomizer.Server;

[Injectable]
public class WeaponCustomizerRouter(JsonUtil jsonUtil, WeaponCustomizer weaponCustomizer)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<SaveRequestData>(
                "/weaponcustomizer/save",
                async (url, info, sessionId, output) => await weaponCustomizer.SaveCustomizations(info)
            ),
            new RouteAction(
                "/weaponcustomizer/load",
                async (url, info, sessionId, output) => await new ValueTask<string>(jsonUtil.Serialize(weaponCustomizer.Database))
            )
        ]
    )
{ }
