namespace AnimalFall.Core.Animals
{
    public enum AnimalType
    {
        Normal,
        Decoy,
        Bomb,
        Shielded,
        Golden,
        Special,
        Paired,
        Ghost,
        Bubble,
        IceCube,
        Shrinking,
        Rainbow,
        FakeAnimal,
        CursedSkull,
        ThiefBird
    }

    public enum AnimalSpecies
    {
        None,
        Chicken,
        Dog,
        Cow,
        Cat,
        Monkey,
        Balloon
    }

    public enum TapResult
    {
        Correct,
        Wrong,
        BombExploded,
        ShieldBroken,
        Golden,
        Rainbow,
        FakeCollected,
        IceCubeFrozen,
        PairedWaiting,
        CursedSkullDestroyed,
        GhostMissed,
        BubblePopped
    }

    public enum MovementPattern
    {
        Static,
        Drift,
        ZigZag,
        Teleport,
        Bounce,
        SineWave,
        FloatUp,
        HeavyFall,
        Erratic
    }
}
