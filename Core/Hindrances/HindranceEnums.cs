namespace AnimalFall.Core.Hindrances
{
    public enum HindranceType
    {
        None = 0,

        // Category 1: Time & Score Penalties
        Bomb = 1,
        PoisonVial = 2,
        AlarmClock = 3,
        ThiefBird = 4,
        FakeAnimal = 5,

        // Category 2: Tap/Interaction Modifiers
        KnightHelmet = 6,
        TitaniumArmor = 7,
        BubbleShield = 8,
        GhostAnimal = 9,
        Teleporter = 10,
        ZigZagFlyer = 11,
        HeavyWeight = 12,
        IceCube = 13,
        ShrinkingAnimal = 14,
        PairedAnimal = 15,

        // Category 3: Visual & Screen Blockers
        InkSquid = 16,
        StormCloud = 17,
        Flashbang = 18,
        Tornado = 19,
        FallingLeaves = 20,

        // Category 4: Environment Modifiers
        WindGust = 21,
        ZeroGravity = 22,
        BlackHole = 23,
        BouncingBorder = 24,
        LaserBeam = 25,

        // Category 5: Advanced Hindrances
        DecoyChest = 26,
        MagnetTrap = 27,
        MirrorMode = 28,
        CursedSkull = 29,
        StoneGargoyle = 30
    }

    public enum HindranceCategory
    {
        Penalty,
        TapModifier,
        ScreenBlocker,
        EnvironmentModifier,
        Advanced
    }
}
