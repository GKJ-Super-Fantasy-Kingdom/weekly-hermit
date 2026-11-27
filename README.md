# Weekly Hermit

Meet the Hermit on Day 2 as usual, then get another chance to recruit every seven days in a standard kingdom run. The first return visits are on **Days 9, 16 and 23**.

## Features

- Choose from up to three eligible recruits from other kingdoms. The Hermit respects your unlocks and banishes and normally offers units you do not already own.
- Compare each recruit’s role and attack style above its quote. Native icons appear when they fit. Hover or move controller focus to see which recruit will receive your selection.
- Inspect a card before choosing. You can reroll for the displayed cost when enough candidates remain, or decline and keep playing.
- Finish the current visitor first. The Hermit waits through dialogue, cutscenes, and open or minimized merchant screens. If the map stays busy until day end, the visit expires and the next regular date still applies.
- Reloading the same dawn with the same eligible units reproduces the opening offer. If no units qualify, the Day 2 conversation still happens and the Hermit leaves without opening recruitment.

## How to Use

1. On a return day, finish any other encounter and return to the map. The Hermit’s recruitment screen opens automatically.
2. To inspect a recruit, click the scroll above its card. On a controller, use the **D-pad** to focus the scroll and press **A** on an Xbox controller. Press **B** to return to the same recruit.
3. Click the card to recruit, or confirm the focused card with **Enter** on a keyboard or **A** on an Xbox controller. Place the recruit using the game’s usual placement controls. To pass for the day, select **No, thanks!** and confirm.
4. If another mod adds a price, select **Buy** for that unit. After inspecting, controller focus returns to Buy if you can afford it, or to the decline button otherwise.

## Screenshots

![Human Weekly Hermit recruitment at 1× with three recruit cards, role icons, and the Reroll and decline buttons.](docs/screenshots/current/human-hermit-1x.png)

Human at 1×. Compare the role and attack style on each card before choosing.

![Undead Weekly Hermit recruitment at 1.5× with Archer, Cyclops, and Runescribe above a disabled Reroll button.](docs/screenshots/current/undead-hermit-1p5x.png)

Undead at 1.5×. The role and attack labels fit beside the native keyword panel, and Reroll is unavailable.

## Compatibility

- The release candidate was tested on Super Fantasy Kingdom **0.8.070** alongside the other [GKJ mods](https://thunderstore.io/c/super-fantasy-kingdom/p/GKJ/).
- [Daily Caravan](https://thunderstore.io/c/super-fantasy-kingdom/p/GKJ/DailyCaravan/) and the Hermit can share a dawn. The Hermit waits for another active or minimized visitor to finish, and [Visitor Calendar](https://thunderstore.io/c/super-fantasy-kingdom/p/GKJ/VisitorCalendar/) can show both visits.
- The Hermit uses the game’s active recruit pool. Banned, jailed, locked, or removed units stay out of offers, including with [Unlimited Jail](https://thunderstore.io/c/super-fantasy-kingdom/p/ownlyme/UnlimitedJail/). An owned unit can rank up only when both the Hermit and merchant allow it.
- Challenges keep their own Hermit schedule.
- These community mods can share the same profile. [Achievement Enabler](https://thunderstore.io/c/super-fantasy-kingdom/p/ownlyme/AchievementEnabler/), [Bakery Uses Wheat](https://thunderstore.io/c/super-fantasy-kingdom/p/ownlyme/BakeryUsesWheat/), [More Relics](https://thunderstore.io/c/super-fantasy-kingdom/p/ownlyme/MoreRelics/), [Right Click Skip](https://thunderstore.io/c/super-fantasy-kingdom/p/ownlyme/RightClickSkip/), and [Tavern Food Priorities](https://thunderstore.io/c/super-fantasy-kingdom/p/ownlyme/TavernFoodPriorities/).

## Installation

1. In Thunderstore Mod Manager or [r2modman](https://thunderstore.io/c/super-fantasy-kingdom/p/ebkr/r2modman/), select **Super Fantasy Kingdom** and install **WeeklyHermit** by **GKJ** with both required packages, [BepInExPack](https://thunderstore.io/c/super-fantasy-kingdom/p/BepInEx/BepInExPack/) and [HideManagerGameObject_Config](https://thunderstore.io/c/super-fantasy-kingdom/p/BepInEx/HideManagerGameObject_Config/).
2. Launch with **Start modded**. To change the seven-day interval, edit `IntervalDays` under `[Schedule]` in `BepInEx/config/gkj.superfantasykingdom.weeklyhermit.cfg` after the first launch. Choose 1–365 days and restart the game. Visits are counted from Day 2.

[Support GKJ Games on Ko-fi](https://ko-fi.com/gkj_games)
