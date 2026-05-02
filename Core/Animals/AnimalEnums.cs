namespace AnimalFall.Core.Animals
{
    public enum AnimalType
    {
        Normal,
        Decoy,
        Bomb,
        Shielded,
        Golden,
        Special
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
        Golden
    }

    public enum MovementPattern
    {
        Static,
        Drift,
        ZigZag,
        Teleport,
        Bounce
    }
}
