using UnityEngine;

namespace AnimalFall.Utils
{
    public static class ImageLibrary
    {
        // Animals
        public static Sprite ChickenSprite => Resources.Load<Sprite>("Sprites/Animals/CHICKEN");
        public static Sprite DogSprite => Resources.Load<Sprite>("Sprites/Animals/DOG");
        public static Sprite CowSprite => Resources.Load<Sprite>("Sprites/Animals/COW");
        public static Sprite CatSprite => Resources.Load<Sprite>("Sprites/Animals/CAT");
        public static Sprite MonkeySprite => Resources.Load<Sprite>("Sprites/Animals/MONKEY");

        // Blockers
        public static Sprite BalloonSprite => Resources.Load<Sprite>("Sprites/Blocks/Balloon");
        public static Sprite DuckSprite => Resources.Load<Sprite>("Sprites/Blocks/Duck");

        // Power Ups
        public static Sprite ParrotSprite => Resources.Load<Sprite>("Sprites/Powerups/PARROT");
        public static Sprite EagleSprite => Resources.Load<Sprite>("Sprites/Powerups/EAGLE");
        public static Sprite TigerSprite => Resources.Load<Sprite>("Sprites/Powerups/TIGER");
        public static Sprite BullSprite => Resources.Load<Sprite>("Sprites/Powerups/BUFALLO");
        public static Sprite GorillaSprite => Resources.Load<Sprite>("Sprites/Powerups/GORILLA");
        public static Sprite FoxSprite => Resources.Load<Sprite>("Sprites/Powerups/FOX");
        public static Sprite WolfSprite => Resources.Load<Sprite>("Sprites/Powerups/WOLF");

        // UI
        public static Sprite BackgroundSprite => Resources.Load<Sprite>("Sprites/Buttons/BACK_GROUND");
        public static Sprite MudSprite => Resources.Load<Sprite>("Sprites/Blocks/MUD");
        public static Sprite PoisonSprite => Resources.Load<Sprite>("Sprites/Blocks/POISON");
        public static Sprite VinesSprite => Resources.Load<Sprite>("Sprites/Blocks/VINE");
        public static Sprite JailSprite => Resources.Load<Sprite>("Sprites/Blocks/JAIL");
        public static Sprite KeySprite => Resources.Load<Sprite>("Sprites/Blocks/KEY");
    }
}
