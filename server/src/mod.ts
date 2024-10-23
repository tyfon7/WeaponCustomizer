import type { DependencyContainer } from "tsyringe";

import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { DatabaseService } from "@spt/services/DatabaseService";

class WeaponCustomizer implements IPreSptLoadMod {
    private databaseService: DatabaseService;
    private logger: ILogger;

    public preSptLoad(container: DependencyContainer): void {
        this.databaseService = container.resolve<DatabaseService>("DatabaseService");
        this.logger = container.resolve<ILogger>("PrimaryLogger");
    }
}

export const mod = new WeaponCustomizer();
