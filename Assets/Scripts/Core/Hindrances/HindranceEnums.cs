using System;

namespace AnimalFall.Core.Hindrances
{
    public enum HindranceType
    {
        None = 0,
        // Penalties
        Bomb          = 1,
        AlarmClock    = 2,
        PoisonVial    = 3,
        ThiefBird     = 4,
        // TapModifiers
        KnightHelmet  = 5,
        BubbleShield  = 6,
        IceCube       = 7,
        GhostAnimal   = 8,
        // ScreenBlockers
        InkSquid      = 9,
        StormCloud    = 10,
        Flashbang     = 11,
        FallingLeaves = 12,
        // EnvironmentMods
        WindGust      = 13,
        ZeroGravity   = 14,
        BlackHole     = 15,
        Tornado       = 16,
        // Advanced
        MagnetTrap    = 17,
        MirrorMode    = 18,
        CursedSkull   = 19,
        PairedAnimal  = 20,
        SpiderwebCurtain      = 21,
        FireflyLockAndKey     = 22,
        RhythmTotem           = 23,
        TrafficLightOwl       = 24,
        TrackingRescueCage    = 25,
        LassoRing             = 26,
        EchoTapRune           = 27,
        NumberedFlock         = 28,
        MovingSafeHalo        = 29,
        KeepersWhistle        = 30,
        SpringMushroomBumpers = 31,
        ConveyorClouds        = 32,
        CrumblingPerches      = 33,
        PendulumVines         = 34,
        SeesawBranch          = 35,
        CarouselNests         = 36,
        TrapdoorClouds        = 37,
        RollingLog            = 38,
        AcornHail             = 39,
        WindmillGate          = 40,
        LanternSpotlight      = 41,
        EclipseSilhouettes    = 42,
        MemoryFog             = 43,
        ColourWashRain        = 44,
        TimerMoth             = 45,
        GoalSwapMonkey        = 46,
        BeeSwarmGuard         = 47,
        PorcupinePulse        = 48,
        VenusFlytrapRescue    = 49,
        RaccoonCoinHeist      = 50,

        // Level 11–19 animal hindrances
        DogHelmet            = 51,
        Octopus              = 52,
        SpiderGun            = 53,
        Pufferfish           = 54,

        // Level 21 set-piece hindrance
        FrogSnatcher         = 55,

        // Classic hazards introduced across normal levels 40-100.
        // Append-only IDs preserve every previously persisted enum value.
        Jellyfish            = 56,
        Laser                = 57,
        Eagle                = 58,
        WoodenPig            = 59,
        Portal               = 60,
        Fan                  = 61,
        BatSwarm             = 62,

        // Paired falling interaction used on normal levels 63-67.
        PandaJailKey         = 63,

        // Opposing crushers close across the playfield and clear caught animals.
        Crusher              = 64,

        // Tappable switch that reverses the falling direction of animals.
        GravitySwitch        = 65,

        // A rising balloon wave that attaches to falling animals.
        BalloonWave          = 66,

        // A goal-panel slime gun that temporarily captures animals.
        SlimeGun             = 67,

        // A moving group of clouds that sweeps across the playfield.
        CloudWave            = 68
    }

    public enum HindranceCategory
    {
        Penalty,
        TapModifier,
        ScreenBlocker,
        EnvironmentModifier,
        Advanced,
        InteractionRule,
        PhysicalMovement,
        VisibilityMemory,
        DynamicRiskReward
    }

    [Flags]
    public enum HindranceCompatibilityTag
    {
        None = 0,
        InputTransform = 1 << 0,
        ExclusiveGesture = 1 << 1,
        FullScreenVisibility = 1 << 2,
        GlobalMotion = 1 << 3,
        PhysicalHolder = 1 << 4,
        GoalRule = 1 << 5,
        ExclusiveTarget = 1 << 6,
        OptionalReward = 1 << 7
    }

    public enum HindranceInputMode { None, Tap, Hold, Drag, Swipe, Trace, Lasso, Rhythm }
    public enum HindranceTargetScope { Global, Animal, World, UserInterface, OptionalReward }
}
