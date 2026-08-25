// Task 2.1 — AnimalEnums: 13 species, full type / pattern / result enums
namespace AnimalFall.Core.Animals
{
    /// <summary>12 playable animal species (None = unassigned).</summary>
    public enum AnimalSpecies
    {
        None, Chicken, Dog, Cow, Cat, Monkey,
        Pig, Rabbit, Penguin, Owl, Mouse, Zebra, Duck
    }

    /// <summary>Controls special tap and movement behaviour.</summary>
    public enum AnimalType
    {
        Normal, Decoy, Bomb, Shielded, Golden, Special,
        Paired, Ghost, Bubble, IceCube, Shrinking,
        Rainbow, FakeAnimal, CursedSkull, ThiefBird
    }

    /// <summary>Fall / movement pattern applied each frame.</summary>
    public enum MovementPattern
    {
        Static, Drift, ZigZag, SineWave, Bounce,
        Teleport, FloatUp, HeavyFall, Erratic
    }

    /// <summary>Result returned from Animal.HandleTap().</summary>
    public enum TapResult
    {
        Correct, Wrong, BombExploded, ShieldBroken, Golden, Rainbow,
        FakeCollected, IceCubeFrozen, PairedWaiting,
        CursedSkullDestroyed, GhostMissed, BubblePopped, HindranceBlocked
    }
}
