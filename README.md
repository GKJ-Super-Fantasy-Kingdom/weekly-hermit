# Weekly Hermit

In a standard kingdom run, the Hermit returns on Days 9, 16 and 23 after the original Day 2 meeting. Repeat visits open the game's recruitment screen directly when the map is free. The Hermit waits through guest dialogue, an open or minimized merchant screen, and cutscenes. A passive guest marker can stay on the map. Challenges use their authored Hermit schedule.

Inspect a card and confirm to recruit or rank up, or use the Hermit's decline button to pass for the day. The native screen handles banishes, reroll costs, card details, input prompts and the return to the map. The repeat offer draws from eligible unlocked foreign units and skips the visit if the pool is empty.

## Local import

1. In Thunderstore Mod Manager or r2modman, select **Super Fantasy Kingdom** and a separate test profile. Install the BepInEx dependencies listed in the manifest.
2. Open **Settings → Import local mod**, select `WeeklyHermit-0.1.0.zip`, and check that WeeklyHermit is enabled. Launch with **Start modded**. Coordinate use of a shared game installation before launching or switching profiles.
3. To change the interval, edit `Schedule → IntervalDays` in `BepInEx/config/gkj.superfantasykingdom.weeklyhermit.cfg` after the first launch. Restart the game. The default is 7, counted from Day 2.

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

Bug reports and pull requests are welcome. See the [contribution guide](CONTRIBUTING.md) for local testing and reporting instructions.

The original mod code is available under the [MIT License](LICENSE). Super Fantasy Kingdom, its game assets, and third-party dependencies retain their respective licenses and ownership.
