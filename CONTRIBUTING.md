# Contributing to Weekly Hermit

## Report a bug

Open a [GitHub issue](https://github.com/GKJ-Super-Fantasy-Kingdom/weekly-hermit/issues) with the mod, game, and BepInEx versions, your kingdom and day, and the steps to reproduce the problem. Include the relevant log excerpt. For interface or input issues, include the UI scale and whether you used a controller or keyboard and mouse.

## Submit a change

Fork the repository, create a branch, and follow the build and packaging instructions below. Use the assemblies from your own game installation and BepInEx profile as build references. Keep game binaries, save files, and local build output out of commits.

Describe the player-visible change in your pull request. List the checks you ran and, where relevant, which controller actions and UI scales you tested.

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

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for each commit. Start the subject with a type, an optional scope, and a short description. Use an exclamation mark before the colon for a breaking change.

Examples include `feat(ui): show controller prompts`, `fix(save): restore the current day`, and `chore: update build tooling`.

Accepted types are `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`. Use the same format for pull request titles so GitHub can use the title as the merge or squash commit subject.
