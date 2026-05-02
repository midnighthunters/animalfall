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
    Hindrances/    - 30 distinct hindrance types organized by category
      Penalties/       - Bomb, PoisonVial, AlarmClock, ThiefBird, FakeAnimal
      TapModifiers/    - KnightHelmet, TitaniumArmor, BubbleShield, Ghost, Teleporter,
                         ZigZag, HeavyWeight, IceCube, Shrinking, PairedAnimal
      ScreenBlockers/  - InkSquid, StormCloud, Flashbang, Tornado, FallingLeaves
      EnvironmentMods/ - WindGust, ZeroGravity, BlackHole, BouncingBorder, LaserBeam
      Advanced/        - DecoyChest, MagnetTrap, MirrorMode, CursedSkull, StoneGargoyle
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

1. Open the project in Unity 2022.3 LTS or later
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
- Progressive difficulty with level unlocks (50 levels)
- In-app purchases for coins and bundles
- Splash screen with 3 loading phases
- Firebase authentication (email/password + Google)
- Cloud save via Firestore

### 30 Hindrance Types

Obstacles organized into 5 categories that progressively unlock as players advance:

| Category | Hindrances |
|----------|-----------|
| **Time & Score Penalties** | Bombs (-5s), Poison Vials (-1 life), Alarm Clocks (speed boost), Thief Birds (steal animals), Fake Animals (-points) |
| **Tap/Interaction Modifiers** | Knight Helmet (3 taps), Titanium Armor (5 taps), Bubble Shield (float up), Ghost Animals (invisible), Teleporters (teleport mid-screen), Zig-Zag Flyers (sine-wave), Heavy Weights (3x speed), Ice Cubes (swipe to melt), Shrinking Animals (shrink as they fall), Paired Animals (simultaneous tap) |
| **Visual & Screen Blockers** | Ink Squids (ink splatter), Storm Clouds (obscure view), Flashbang (white out), Tornadoes (toss animals), Falling Leaves (visual clutter) |
| **Environment Modifiers** | Wind Gusts (push horizontally), Zero Gravity (float for 2s), Black Hole (pull to center), Bouncing Borders (bounce off bottom), Laser Beams (destroy animals) |
| **Advanced** | Decoy Chests (release 3 bombs), Magnet Traps (offset taps), Mirror Mode (reverse horizontal), Cursed Skulls (tap or lose time), Stone Gargoyles (swipe to remove) |

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
