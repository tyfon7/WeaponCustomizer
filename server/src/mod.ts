import type { DependencyContainer } from "tsyringe";

import type { ProfileHelper } from "@spt/helpers/ProfileHelper";
import type { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import { VFS } from "@spt/utils/VFS";
import fs from "node:fs";
import path from "node:path";

type Vector3 = {
    x: number;
    y: number;
    z: number;
};

type CustomPosition = {
    original: Vector3;
    modified: Vector3;
};

type CustomizePayload = {
    id: string;
    slots: Record<string, CustomPosition>;
};

type Customizations = Record<string, Record<string, CustomPosition>>;

class WeaponCustomizer implements IPreSptLoadMod, IPostSptLoadMod {
    private logger: ILogger;
    private vfs: VFS;
    private profileHelper: ProfileHelper;
    private customizations: Customizations = null;
    private filepath: string;

    public preSptLoad(container: DependencyContainer): void {
        this.logger = container.resolve<ILogger>("PrimaryLogger");
        this.vfs = container.resolve<VFS>("VFS");

        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");

        this.filepath = path.resolve(__dirname, "../customizations.json");
        this.load();

        staticRouterModService.registerStaticRouter(
            "WeaponCustomizerRoutes",
            [
                {
                    url: "/weaponcustomizer/save",
                    action: async (url, info: CustomizePayload, sessionId, output) => this.saveCustomization(info)
                },
                {
                    url: "/weaponcustomizer/load",
                    action: async (url, info, sessionId, output) => JSON.stringify(this.customizations)
                }
            ],
            "custom-static-weapon-customizer"
        );
    }

    public postSptLoad(container: DependencyContainer): void {
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        this.clean();
    }

    private async saveCustomization(payload: CustomizePayload): Promise<string> {
        //this.logger.info(`WeaponCustomizer: Saving customization for weapon ${payload.weaponId}`);
        if (Object.keys(payload.slots).length === 0) {
            delete this.customizations[payload.id];
        } else {
            this.customizations[payload.id] = payload.slots;
        }
        await this.save();

        return JSON.stringify({ success: true });
    }

    private load() {
        try {
            if (this.vfs.exists(this.filepath)) {
                this.customizations = JSON.parse(this.vfs.readFile(this.filepath));
            } else {
                this.customizations = {};

                // Create the file with fs - vfs.writeFile pukes on windows paths if it needs to create the file
                fs.writeFileSync(this.filepath, JSON.stringify(this.customizations));
            }

            const count = Object.keys(this.customizations).length;
            if (count > 0) {
                this.logger.logWithColor(`WeaponCustomizer: ${count} weapon customizations loaded.`, LogTextColor.CYAN);
            }
        } catch (error) {
            this.logger.error("WeaponCustomizer: Failed to load weapon customization! " + error);
            this.customizations = {};
        }
    }

    // Remove any customizations for items that no longer exist
    private async clean() {
        const map = new Map<string, boolean>();
        for (const weaponId of Object.keys(this.customizations)) {
            map.set(weaponId, false);
        }

        for (const profile of Object.values(this.profileHelper.getProfiles())) {
            const items = profile.characters?.pmc?.Inventory?.items ?? [];
            for (const item of items) {
                if (map.has(item._id)) {
                    map.set(item._id, true);
                }
            }

            const presets = profile.userbuilds?.weaponBuilds ?? [];
            for (const preset of presets) {
                if (map.has(preset.Id)) {
                    map.set(preset.Id, true);
                }
            }
        }

        let dirtyCount = 0;
        for (const [id, found] of Object.entries(map)) {
            if (!found) {
                delete this.customizations[id];
                dirtyCount++;
            }
        }

        if (dirtyCount > 0) {
            // this.logger.logWithColor(
            //     `WeaponCustomizer: Cleaned up ${dirtyCount} customizations for weapons/presets that no longer exist`,
            //     LogTextColor.CYAN
            // );

            await this.save();
        }
    }

    private async save() {
        try {
            await this.vfs.writeFileAsync(this.filepath, JSON.stringify(this.customizations, null, 2));
        } catch (error) {
            this.logger.error("WeaponCustomizer: Failed to save weapon customization! " + error);
        }
    }
}

export const mod = new WeaponCustomizer();
