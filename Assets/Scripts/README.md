# AnimalFall

A mobile tap-to-catch animal game built with Unity. Players tap falling animals to meet level goals within time limits, using power-ups, combos, and strategic gameplay.

## Project Structure

```
Runtime/
  Core/
    Animals/       - Animal entity, movement, species definitions
    Goals/         - Goal system (per-species tracking, goal panel)
    Levels/        - Level data, level database, and level progression
    PowerUps/      - Power-up data and type definitions
    Hindrances/    - 50 production hindrance types plus shared lifecycle/input/movement code
      Penalties/       - Bomb, Alarm Clock, Poison Vial, Thief Bird
      TapModifiers/    - Knight Helmet, Bubble Shield, Ice Cube, Ghost Animal
      ScreenBlockers/  - Ink Squid, Storm Cloud, Flashbang, Falling Leaves
      EnvironmentMods/ - Wind Gust, Zero Gravity, Black Hole, Tornado
      Advanced/        - Magnet Trap, Mirror Mode, Cursed Skull, Paired Animals
      New/             - interaction-rule, physical-movement, visibility/memory,
                         and dynamic risk/reward mechanic families (IDs 21-50)
    MegaLevel/     - Villain, VillainAI, VillainProjectile, MegaLevelController
    Skins/         - Skin data and skin enums
    Events/        - Game events, daily quests, event scheduler
  Data/
    Schemas/       - Firebase data schemas (UserProfile, PlayerProgress, etc.)
    MockData/      - Development mock data configuration
  Managers/        - Singleton managers (Game, GameState, Level, Score, Combo, Audio,
                     Lives, Hindrance, Shop, Event, EasterEgg, PowerUp, Input)
  Services/
    Auth/          - Firebase authentication service
    IAP/           - In-app purchase service
    Save/          - Local save/persistence service (coins, skins, stats)
  UI/
    Screens/       - Full-screen UI panels (MainMenu, JourneyMap, Shop, Events,
                     Results, Pause, VillainHUD)
    Components/    - Reusable UI components (FloatingText, Toast, ProgressBar)
    Splash/        - Splash screen manager and loading screens
  Effects/         - ScreenEffects, EnvironmentEffects, EffectsController
  Utils/           - GestureDetector, MathUtils, CanvasHelper, UIFitter
```

## Setup

1. Open the project in Unity 6000.0.42f1 or the compatible Unity 6 patch configured by the project
2. Import Firebase SDK for Unity (Authentication, Firestore)
3. Place `google-services.json` (Android) or `GoogleService-Info.plist` (iOS) in `Assets/`
4. Configure Firebase project in Firebase Console
5. Build and run

## Firebase Configuration

The game uses Firebase for:
- **Authentication**: Email/password and Google Sign-In
- **Firestore**: Player profiles, progress, leaderboards

See `Runtime/Data/Schemas/` for Firestore document schemas.

## Features

- Tap-to-catch gameplay with falling animals
- Per-species goal tracking
- 9 power-up types (SlowTime, Magnet, MultiTap, AutoTap, etc.)
- Combo multiplier system
- Curated normal-level difficulty with preserved 100-slot level database and mega cadence
- In-app purchases for coins and bundles
- Splash screen with 3 loading phases
- Firebase authentication (email/password + Google)
- Cloud save via Firestore

### 50 Hindrance Types

The persisted IDs `0-20` remain unchanged. IDs `21-50` are appended and grouped into four new mechanic families:

| Category | Hindrances |
|----------|-----------|
| **Current repaired set (1-20)** | Bomb, Alarm Clock, Poison Vial, Thief Bird, Knight Helmet, Bubble Shield, Ice Cube, Ghost Animal, Ink Squid, Storm Cloud, Flashbang, Falling Leaves, Wind Gust, Zero Gravity, Black Hole, Tornado, Magnet Trap, Mirror Mode, Cursed Skull, Paired Animal |
| **Interaction rules (21-30)** | Spiderweb Curtain, Firefly Lock and Key, Rhythm Totem, Traffic-Light Owl, Tracking Rescue Cage, Lasso Ring, Echo Tap Rune, Numbered Flock, Moving Safe Halo, Keeper's Whistle |
| **Physical movement (31-40)** | Spring Mushroom Bumpers, Conveyor Clouds, Crumbling Perches, Pendulum Vines, Seesaw Branch, Carousel Nests, Trapdoor Clouds, Rolling Log, Acorn Hail, Windmill Gate |
| **Visibility and memory (41-44)** | Lantern Spotlight, Eclipse Silhouettes, Memory Fog, Colour-Wash Rain |
| **Dynamic risk/reward (45-50)** | Timer Moth, Goal-Swap Monkey, Bee Swarm Guard, Porcupine Pulse, Venus Flytrap Rescue, Raccoon Coin Heist |

Definitions live in `Assets/Resources/Hindrances/Definitions`, use purpose-built sprites from the hindrance atlas, and are indexed through `HindranceRegistry`. Run `Animal Fall > Hindrances > Rebuild Production Assets` after changing source sheets, `Validate All` for registry checks, or open `Showcase` to inspect a type independently. Normal hindrances are excluded from mega levels unless a level explicitly enables `AllowNormalHindrancesInMegaLevel`.

### Mega-Level Boss Fights

Every 5 levels features a boss fight with dual objectives:
- Collect required animals AND defeat the Villain
- Villain has HP bar, shield phases, and projectile attacks
- Deflect projectiles back at the Villain by tapping them
- Boss enters harder phases at 50% and 25% HP

### Lives System

- 5 lives max with 30-minute regeneration timer
- Lives deducted on level failure and poison vial taps
- Timer persists across sessions with offline regeneration

### Journey Map

- Scrollable Candy Crush-style level selection
- Shows completed, current, unlocked, and locked levels
- Mega levels highlighted with special glow

### Shop System

- Buy power-ups, cosmetic skins, and temporary buffs
- Skins persist via SaveService
- Power-up inventory tracked per item

### Daily Quests & Events

- 3 daily quests that rotate each day (seeded by date)
- Quest types: collect animals, reach score, achieve combo, complete levels
- Timed events with countdown timers

### Easter Eggs

- **Konami Code**: Tap the 4 corners of the main menu to unlock Golden Animal skin
- **Rainbow Animal**: 1-in-1000 spawn chance, grants massive coin bonus + achievement
- **Cloud Tapping**: Tap background clouds 10 times for a coin rain shower
- **Shopkeeper Secret**: Drag the shopkeeper off-screen for a hidden daily reward
