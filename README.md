# Weapon Customizer

This SPTarkov mod allows you to adjust the positions of some of your weapon attachments. This is a purely aesthetic mod - it has no effect on weapon statistics.

## How to use

Modify a gun with the context menu -> Modding screen, and you will be able to click and drag the white dot on each attachment. If it changes color, it's adjustable. If not, it's not.

#### Movement axis

By default, movement will be along the same axis as the gun - from the muzzle to the stock.

-   SHIFT-drag will move the attachment up and down on the vertical axis.
-   CTRL-drag will move the attachment to the left and right of the gun.

These secondary directions _usually_ don't make any sense but occasionally you may find an attachment that needs these adjustments.

#### Resetting

You can reset an attachment to its default position by right clicking the dot, or reset all attachments by clicking "Revert" in the upper right corner.

#### In Raid

By default the modding screen is only available out of raid. In the F12 menu you may enable the option to show up in raid, and optionally require a multitool. Note that this is a stripped down version of the modding screen, and you can only use it to customize attachment positions, not swap out attachments [dev note - changing attachments caused a lot of issues].

#### No limits

Note that there is nothing currently enforcing the laws of physics - you can drag attachments into space, or inside of other attachments, and they will stay there. The only way to solve this would be an exhaustive list of every attachement's dimensions (which wouldn't support extra mod content), or a very complicated and likely wrong on-the-fly analysis of where items can be moved to. Use your own judgement.

## Presets

Adjustments can be saved as part of a preset. Changes are treated the same as any other preset change - they do not apply to the weapon until you click "Assemble", and they do not save until you press "Save".

## Configuration Options

These options are available in the F12 menu.

-   Customize Weapons in Raid: Enable the <b>Modding</b> context menu in raid for _unequipped_ weapons. Optionally require a multitool.
-   Step Size: Instead of smooth motion, you can force attachments to move in discreet pixel amounts. Note that this is screen pixels, so rotation affects this.
-   Move Everything: Allows you to move every gun part, including ones that don't make any sense at all. Have fun.

## Supported attachments

### Foregrips

You can adjust foregrips forward and backwards. The operator's hand will stay on the grip.

### Tactical devices

Similarly, you can move lights and lasers and such. Moving them too far back will likely cause shadows!

### Scopes and optics

Scopes can be moved, but be aware that when aiming, different scopes behave differently. Moving iron sights, reflex sights, etc., will affect how the scope appears and how it lines up. Sniper scopes are handled differently by the game, and will still fill the screen the same amount, regardless of position. Check out Fontaine's FOV Fix!

### Stocks

Stocks are adjustable, but this will not affect how far off your chest the weapon is held.
