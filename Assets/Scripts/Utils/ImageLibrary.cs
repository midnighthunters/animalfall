// Task 2.3 — ImageLibrary: static sprite cache, all sprites loaded via Resources.Load at init
using UnityEngine;
using AnimalFall.Core.Animals;
using AnimalFall.Core.Hindrances;

namespace AnimalFall.Utils
{
    public static class ImageLibrary
    {
        // ── Animal sprites (indexed by AnimalSpecies) ─────────────────────────
        private static readonly Sprite[] _animalSprites = new Sprite[14]; // indices 0-13 match AnimalSpecies enum

        // ── Hindrance sprites ─────────────────────────────────────────────────
        private static readonly Sprite[] _hindranceSprites = new Sprite[56]; // indices 0-55 match HindranceType enum

        // ── UI / Panel sprites ────────────────────────────────────────────────
        private static Sprite _panel;
        private static Sprite _panel2;
        private static Sprite _redButtons;
        private static Sprite _levelButton1;
        private static Sprite _levelButton2;
        private static Sprite _clock;
        private static Sprite _coinStack;

        private static Sprite _placeholder;
        private static bool   _loaded;

        // Reset on every play-mode entry so sprite changes (PPU, paths) always take effect
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayMode()
        {
            _loaded = false;
            System.Array.Clear(_animalSprites,   0, _animalSprites.Length);
            System.Array.Clear(_hindranceSprites, 0, _hindranceSprites.Length);
            _panel = _panel2 = _redButtons = _levelButton1 = _levelButton2 = _clock = _coinStack = null;
            _placeholder = null;
        }

        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>Call once during level load (via LevelManager.PrewarmPoolsForLevel).</summary>
        public static void LoadAll()
        {
            if (_loaded) return;
            _placeholder = CreatePlaceholder();
            LoadAnimalSprites();
            LoadHindranceSprites();
            LoadUISprites();
            _loaded = true;
        }

        // ── Animal accessors ──────────────────────────────────────────────────

        public static Sprite GetAnimalSprite(AnimalSpecies species)
        {
            int idx = (int)species;
            if (idx < 0 || idx >= _animalSprites.Length) return _placeholder;
            return _animalSprites[idx] != null ? _animalSprites[idx] : _placeholder;
        }

        // ── Hindrance accessors ───────────────────────────────────────────────

        public static Sprite GetHindranceSprite(HindranceType type)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= _hindranceSprites.Length) return _placeholder;
            return _hindranceSprites[idx] != null ? _hindranceSprites[idx] : _placeholder;
        }

        // ── UI accessors ──────────────────────────────────────────────────────

        public static Sprite GetPanel()        => _panel        ?? _placeholder;
        public static Sprite GetPanel2()       => _panel2       ?? _placeholder;
        public static Sprite GetRedButtons()   => _redButtons   ?? _placeholder;
        public static Sprite GetLevelButton1() => _levelButton1 ?? _placeholder;
        public static Sprite GetLevelButton2() => _levelButton2 ?? _placeholder;
        public static Sprite GetClock()        => _clock        ?? _placeholder;
        public static Sprite GetCoinStack()    => _coinStack    ?? _placeholder;

        // ── Internal loaders ──────────────────────────────────────────────────

        private static void LoadAnimalSprites()
        {
            // AnimalSpecies enum: None(0), Chicken(1), Dog(2), Cow(3), Panda(4),
            //   Monkey(5), Pig(6), Rabbit(7), Penguin(8), Owl(9), Mouse(10), Zebra(11), Duck(12), Raccoon(13)
            // Actual files present: CHICKEN, DOG2, ELEPHANT, MONKEY2, PANDA2, PENGUIN, PIG2
            // Map each species to the closest available sprite; share sprites for missing ones.
            string[] paths = new string[]
            {
                null,                           // 0 = None
                "icons/animals/CHICKEN",        // 1 = Chicken  ✓
                "icons/animals/DOG2",           // 2 = Dog       ✓
                "icons/animals/ELEPHANT",       // 3 = Cow  → Elephant (best substitute)
                "icons/animals/PANDA2",         // 4 = Panda     ✓
                "icons/animals/MONKEY2",        // 5 = Monkey    ✓
                "icons/animals/PIG2",           // 6 = Pig       ✓
                "icons/animals/PANDA2",         // 7 = Rabbit → Panda  (reuse)
                "icons/animals/PENGUIN",        // 8 = Penguin   ✓
                "icons/animals/ELEPHANT",       // 9 = Owl   → Elephant (reuse)
                "icons/animals/DOG2",           // 10 = Mouse → Dog    (reuse)
                "icons/animals/MONKEY2",        // 11 = Zebra → Monkey (reuse)
                "icons/animals/CHICKEN",        // 12 = Duck  → Chicken (reuse)
                "icons/animals/RACOON",         // 13 = Raccoon  ✓ (source filename uses one C)
            };

            for (int i = 1; i < paths.Length; i++)
            {
                if (paths[i] == null) continue;
                _animalSprites[i] = Resources.Load<Sprite>(paths[i]);
                if (_animalSprites[i] == null)
                {
                    // Downgrade to warning — game can still run with placeholder
                    Debug.LogWarning($"[ImageLibrary] Missing animal sprite at: {paths[i]}");
                    _animalSprites[i] = _placeholder;
                }
            }
        }

        private static void LoadHindranceSprites()
        {
            HindranceRegistry registry = Resources.Load<HindranceRegistry>("Hindrances/HindranceRegistry");
            if (registry == null)
            {
                Debug.LogError("[ImageLibrary] HindranceRegistry is missing from Resources/Hindrances.");
                return;
            }

            for (int i = 1; i < _hindranceSprites.Length; i++)
            {
                HindranceData data = registry.GetData((HindranceType)i);
                _hindranceSprites[i] = data != null ? data.icon : null;
                if (_hindranceSprites[i] == null)
                {
                    Debug.LogWarning($"[ImageLibrary] Missing production icon for {(HindranceType)i}.");
                    _hindranceSprites[i] = _placeholder;
                }
            }
        }

        private static void LoadUISprites()
        {
            _panel        = LoadOrWarn("panels/panel");
            _panel2       = LoadOrWarn("panels/panel2");
            _redButtons   = LoadOrWarn("panels/red_buttons");
            _levelButton1 = LoadOrWarn("panels/levelbutton1");
            _levelButton2 = LoadOrWarn("panels/levelbutton2");
            _clock        = LoadOrWarn("icons/clock");
            _coinStack    = LoadOrWarn("icons/coinstack");
        }

        private static Sprite LoadOrWarn(string path)
        {
            var s = Resources.Load<Sprite>(path);
            if (s == null) Debug.LogWarning($"[ImageLibrary] Missing UI sprite: {path}");
            return s ?? _placeholder;
        }

        private static Sprite CreatePlaceholder()
        {
            var tex = new Texture2D(32, 32);
            var cols = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    cols[y * 32 + x] = ((x / 8 + y / 8) & 1) == 0
                        ? new Color(0.22f, 0.25f, 0.30f, 0.9f)
                        : new Color(0.45f, 0.50f, 0.58f, 0.9f);
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
        }
    }
}
