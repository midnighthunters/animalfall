# AnimalFall

A mobile tap-to-catch animal game built with Unity. Players tap falling animals to meet level goals within time limits, using power-ups, combos, and strategic gameplay.

## Project Structure

```
Runtime/
  Core/
    Animals/       - Animal entity, movement, species definitions
    Goals/         - Goal system (per-species tracking, goal panel)
    Levels/        - Level data and level progression
    PowerUps/      - Power-up data and type definitions
  Data/
    Schemas/       - Firebase data schemas (UserProfile, PlayerProgress, etc.)
    MockData/      - Development mock data configuration
  Managers/        - Singleton managers (Game, Level, Score, Combo, Audio, etc.)
  Services/
    Auth/          - Firebase authentication service
    IAP/           - In-app purchase service
    Save/          - Local save/persistence service
  UI/
    Screens/       - Full-screen UI panels (MainMenu, GameHUD, Results)
    Components/    - Reusable UI components (FloatingText, Toast, ProgressBar)
    Splash/        - Splash screen manager and loading screens
  Effects/         - Visual effects controller
  Utils/           - Utility classes (CanvasHelper, UIFitter, ImageLibrary)
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
- Progressive difficulty with level unlocks
- In-app purchases for coins and bundles
- Splash screen with 3 loading phases
- Firebase authentication (email/password + Google)
- Cloud save via Firestore
