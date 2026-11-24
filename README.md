# Weekly Hermit

Meet the Hermit on Day 2 as usual, then recruit again every seven days in a standard kingdom run. The default return dates start on **Days 9, 16 and 23**. Each visit opens the game's recruitment screen, where you can inspect a unit, recruit it, or decline for the day.

![The Hermit offers Abomination, Devil and Vampire on the Day 16 recruitment screen](https://raw.githubusercontent.com/GKJ-Super-Fantasy-Kingdom/weekly-hermit/f62a63d7ac44f9435cf976ef03941c96ed208f08/docs/screenshots/day-16-hermit.png)

*Day 16 at 1.5× UI scale. Daily Caravan visited first. Once its screen closed, the Hermit offered these three recruits.*

## Visits and recruitment

The Hermit waits through guest dialogue, an open or minimized merchant screen, and cutscenes. If the map stays busy through day end, that visit expires. The next visit follows the regular schedule. Challenges use their authored Hermit schedule.

Inspect a card and confirm to recruit, or use the Hermit's decline button to pass for the day. The stock Hermit excludes units you already own. An owned unit can appear when a mod allows it through both the Hermit and merchant checks, and the native card handles the rank-up. The native screen handles banishes, reroll costs, card details, input prompts and the return to the map. The Day 2 conversation still occurs if no units are eligible. In that case the Hermit leaves without opening an empty screen. The repeat offer draws from eligible unlocked foreign units and skips the visit if the pool is empty.

On each visit and reroll, Weekly Hermit reads the current unlocked and banished IDs and resolves the prefab the merchant will use for the card. Additions and replacements can appear as soon as they are registered there. Missing IDs, including a unit removed before reloading an earlier dawn, drop out of the offer. The repeat picker uses the Hermit's eligibility and price hooks and supplies its own seeded draw order.

![Abomination's native details screen shows its stats and Taunt ability](https://raw.githubusercontent.com/GKJ-Super-Fantasy-Kingdom/weekly-hermit/f62a63d7ac44f9435cf976ef03941c96ed208f08/docs/screenshots/inspect-abomination.png)

*Inspecting Abomination before recruiting, at 1.5× UI scale. In this playtest, simulated gamepad Back returned focus to its recruitment card. If another mod sets a price, Back focuses Buy when you can afford the unit and Decline otherwise.*

## Install and configure

In Thunderstore Mod Manager or r2modman, select **Super Fantasy Kingdom**, install **WeeklyHermit** by **GKJ** with its dependencies, and launch with **Start modded**. The first launch creates `BepInEx/config/gkj.superfantasykingdom.weeklyhermit.cfg`. Set `Schedule → IntervalDays` to a value from 1 to 365 and restart the game to change the interval. Day 2 remains the starting point.

## Local import

1. In Thunderstore Mod Manager or r2modman, select **Super Fantasy Kingdom** and a separate test profile. Install the BepInEx dependencies listed in the manifest.
2. Open **Settings → Import local mod**, select `WeeklyHermit-0.1.0.zip`, and check that WeeklyHermit is enabled. Launch with **Start modded**.

## Controller playtest

- Use Day 2 as the control. On Day 9, finish the Witch Doctor encounter if present. Inspect a Hermit card, back out to the cards, then recruit. Check that precisely one unit or rank is gained and map navigation resumes.
- On a separate pass, banish a card, decline the visit and move around the map. Verify the banished unit stays out of later offers. Try holding Confirm through the transition, and switch from controller to mouse and keyboard and back while inspecting.
- On Day 16, enable Daily Caravan and meet its unlock conditions. Minimize its screen if supported. The Hermit should wait until the Caravan finishes. Resolve each encounter once and verify their prompts, rewards, and final map focus.
- Reload a checkpoint from before the offer. With the same eligible pool, the same opening cards should appear. Finish the visit, complete the day and save through the tavern. Continue and verify the saved roster or decline and the next due date. Try a challenge with a moved or disabled Hermit visit.

The normal game saves at day end and advances the day in the tavern. Returning to an earlier dawn replays that checkpoint, including its roster and banishes. A snapshot containing a resolved repeat visit gets a matching marker in `BepInEx/config/WeeklyHermit/checkpoints` in the mod profile. Copy that folder with saves when transferring a profile. The mod does not add a mid-encounter save action.

## Build

Use a .NET SDK with .NET 8 available for the checks. Supply `GameDir` and `ProfileDir` as MSBuild properties if the defaults in the project differ from your installation. The profile supplies BepInEx 5 and HarmonyX.

```powershell
dotnet build WeeklyHermit.csproj -c Release
dotnet run --project tests/VisitChecks.csproj -c Release
python package.py
```

The local import package is written to `dist/WeeklyHermit-0.1.0.zip`.

## Contributing and license

Bug reports and pull requests are welcome. See the [contribution guide](https://github.com/GKJ-Super-Fantasy-Kingdom/weekly-hermit/blob/main/CONTRIBUTING.md) for local testing and reporting instructions.

The original mod code is available under the [MIT License](https://github.com/GKJ-Super-Fantasy-Kingdom/weekly-hermit/blob/main/LICENSE). Super Fantasy Kingdom, its game assets, and third-party dependencies retain their respective licenses and ownership.
