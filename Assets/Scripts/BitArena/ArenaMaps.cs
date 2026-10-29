using UnityEngine;

namespace BitArena
{
    public struct ArenaShelf
    {
        public Vector2 Center;
        public Vector2 Size;
        public Color32 Tint;

        public float MinX => Center.x - Size.x * 0.5f;
        public float MaxX => Center.x + Size.x * 0.5f;
        public float Top => Center.y + Size.y * 0.5f;
    }

    public struct ArenaDress
    {
        public float X;
        public float Scale;
        public bool Snow;
    }

    public enum ArenaTheme
    {
        Hall,
        Winter,
        Forest
    }

    public sealed class ArenaMap
    {
        public ArenaTheme Theme;
        public string Name;
        public string Blurb;
        public Color Sky;
        public Color Card;
        public Color32 FloorTint;
        public Color32 WallL;
        public Color32 WallR;
        public Vector3 Alice;
        public Vector3 Bob;
        public Vector3 Charlie;
        public float SkyDrop;
        public ArenaShelf[] Shelves;
        public ArenaDress[] Dressing;
    }

    public static class ArenaMaps
    {
        public static readonly ArenaMap[] All = { Hall(), Winter(), Forest() };
        public static int ActiveIndex { get; private set; } = -1;
        public static ArenaMap Active => All[Mathf.Clamp(ActiveIndex, 0, All.Length - 1)];

        static GameObject root;

        public static bool Select(int index)
        {
            int count = All.Length;
            index = (index % count + count) % count;
            if (index == ActiveIndex && root != null)
                return false;
            ActiveIndex = index;
            Build(All[index]);
            return true;
        }

        static void Build(ArenaMap map)
        {
            if (root != null)
                Object.DestroyImmediate(root);
            root = new GameObject("Arena");

            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(root.transform, false);
            backdrop.transform.position = new Vector3(0f, 7.2f, 0f);
            backdrop.transform.localScale = new Vector3(1.48f, 1.85f, 1f);
            PixelArt.AddSprite(backdrop, BackdropFor(map.Theme), -100);

            Solid(new Vector2(0f, -0.8f), new Vector2(33.6f, 1.6f), map.FloorTint, false, true);
            Solid(new Vector2(-16.55f, 9f), new Vector2(0.8f, 24f), map.WallL, false, false);
            Solid(new Vector2(16.55f, 9f), new Vector2(0.8f, 24f), map.WallR, false, false);

            var shelves = map.Shelves;
            for (int i = 0; i < shelves.Length; i++)
                Solid(shelves[i].Center, shelves[i].Size, shelves[i].Tint, true, false);

            var dressing = map.Dressing;
            for (int i = 0; i < dressing.Length; i++)
            {
                var dress = dressing[i];
                var go = new GameObject(dress.Snow ? "SnowPine" : "Tree");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(dress.X, 0.02f, 0f);
                go.transform.localScale = new Vector3(dress.Scale, dress.Scale, 1f);
                var sprite = PixelArt.AddSprite(go, dress.Snow ? PixelArt.SnowPine : PixelArt.LeafPine, -40);
                sprite.color = Color.white;
            }

            BitMonster.Bind(shelves, map.SkyDrop);
            if (HealthPickup.Instance != null)
                HealthPickup.Instance.Bind(shelves);
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = map.Sky;
        }

        static Sprite BackdropFor(ArenaTheme theme)
        {
            if (theme == ArenaTheme.Winter)
                return PixelArt.WinterBackdrop;
            if (theme == ArenaTheme.Forest)
                return PixelArt.ForestBackdrop;
            return PixelArt.Backdrop;
        }

        static void Solid(Vector2 center, Vector2 size, Color32 tint, bool oneWay, bool floor)
        {
            var go = new GameObject(oneWay ? "Platform" : "Solid");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(center.x, center.y, 0f);
            var sprite = floor ? PixelArt.Floor(size.x, size.y, tint) : PixelArt.Stone(size, tint);
            PixelArt.AddSprite(go, sprite, floor ? 0 : 1);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = new PhysicsMaterial2D("Stone") { friction = 0.05f, bounciness = 0f };
            go.AddComponent<SolidLevel>();
            if (!oneWay)
                return;
            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = false;
            effector.surfaceArc = 170f;
            effector.sideArc = 12f;
        }

        static ArenaShelf S(float min, float max, float top, Color32 tint, float thick)
        {
            return new ArenaShelf
            {
                Center = new Vector2((min + max) * 0.5f, top - thick * 0.5f),
                Size = new Vector2(max - min, thick),
                Tint = tint
            };
        }

        static ArenaMap Hall()
        {
            var low = new Color32(118, 102, 116, 255);
            var mid = new Color32(96, 108, 126, 255);
            var high = new Color32(118, 100, 138, 255);
            var crown = new Color32(150, 126, 86, 255);
            var nest = new Color32(166, 112, 78, 255);
            var spire = new Color32(196, 164, 92, 255);
            return new ArenaMap
            {
                Theme = ArenaTheme.Hall,
                Name = "The Hall",
                Blurb = "Stone stairs and a center spire.",
                Sky = new Color(0.05f, 0.03f, 0.09f),
                Card = new Color(0.86f, 0.72f, 0.96f),
                FloorTint = new Color32(96, 78, 92, 255),
                WallL = new Color32(42, 32, 52, 255),
                WallR = new Color32(36, 34, 58, 255),
                Alice = new Vector3(-5.2f, 1.2f, 0f),
                Bob = new Vector3(5.2f, 1.2f, 0f),
                Charlie = new Vector3(0f, 12.85f, 0f),
                SkyDrop = 14.5f,
                Dressing = new ArenaDress[0],
                Shelves = new[]
                {
                    S(-14.5f, -7.8f, 2.47f, low, 0.5f),
                    S(-2.2f, 2.5f, 2.71f, low, 0.5f),
                    S(7.9f, 14.4f, 2.35f, low, 0.5f),
                    S(-14.8f, -10.3f, 4.87f, mid, 0.46f),
                    S(-6.6f, -3.1f, 4.61f, mid, 0.46f),
                    S(-1.9f, 2.1f, 5.48f, mid, 0.46f),
                    S(3.3f, 7.4f, 5.01f, mid, 0.46f),
                    S(10.6f, 14.9f, 5.17f, mid, 0.46f),
                    S(-14.7f, -10.4f, 7.21f, high, 0.46f),
                    S(-6.7f, -2.4f, 7.07f, high, 0.46f),
                    S(1.85f, 6.45f, 7.47f, high, 0.46f),
                    S(10.2f, 14.7f, 7.61f, high, 0.46f),
                    S(-13.7f, -8.6f, 9.54f, crown, 0.44f),
                    S(-5.7f, -0.6f, 9.39f, crown, 0.44f),
                    S(0.4f, 3.9f, 9.94f, crown, 0.44f),
                    S(5.15f, 7.75f, 10.43f, nest, 0.42f),
                    S(9.2f, 14.2f, 9.77f, crown, 0.44f),
                    S(-2.6f, 2.6f, 12.14f, spire, 0.44f)
                }
            };
        }

        static ArenaMap Winter()
        {
            var low = new Color32(176, 206, 214, 255);
            var mid = new Color32(140, 176, 196, 255);
            var high = new Color32(210, 226, 232, 255);
            var crown = new Color32(232, 240, 246, 255);
            var throne = new Color32(168, 196, 214, 255);
            return new ArenaMap
            {
                Theme = ArenaTheme.Winter,
                Name = "Snow Court",
                Blurb = "Ice terraces up to a frozen throne.",
                Sky = new Color(0.04f, 0.07f, 0.13f),
                Card = new Color(0.72f, 0.86f, 1f),
                FloorTint = new Color32(186, 206, 214, 255),
                WallL = new Color32(70, 96, 120, 255),
                WallR = new Color32(60, 88, 112, 255),
                Alice = new Vector3(-7.6f, 1.2f, 0f),
                Bob = new Vector3(7.4f, 1.2f, 0f),
                Charlie = new Vector3(7.5f, 12.86f, 0f),
                SkyDrop = 14.5f,
                Dressing = new[]
                {
                    new ArenaDress { X = -15.1f, Scale = 2.1f, Snow = true },
                    new ArenaDress { X = -13.3f, Scale = 1.45f, Snow = true },
                    new ArenaDress { X = 13.2f, Scale = 1.55f, Snow = true },
                    new ArenaDress { X = 15.05f, Scale = 2.25f, Snow = true }
                },
                Shelves = new[]
                {
                    S(-14.4f, -9.2f, 2.35f, low, 0.46f),
                    S(-6.6f, -2.2f, 2.85f, low, 0.46f),
                    S(1.4f, 5.8f, 2.55f, low, 0.46f),
                    S(8.6f, 14.2f, 2.2f, low, 0.46f),
                    S(-14.8f, -11f, 5.15f, mid, 0.46f),
                    S(-8.4f, -4.6f, 5.45f, mid, 0.46f),
                    S(-1.6f, 2.4f, 5.05f, mid, 0.46f),
                    S(4.2f, 8f, 5.35f, mid, 0.46f),
                    S(10.4f, 14.6f, 5.2f, mid, 0.46f),
                    S(-13.6f, -9.8f, 7.85f, high, 0.46f),
                    S(-6.8f, -3.2f, 8.15f, high, 0.46f),
                    S(0.2f, 4f, 7.95f, high, 0.46f),
                    S(7.2f, 11.4f, 8.25f, high, 0.46f),
                    S(-12.2f, -8.2f, 10.55f, crown, 0.44f),
                    S(-4.4f, -0.4f, 10.85f, crown, 0.44f),
                    S(2.6f, 6.6f, 10.7f, crown, 0.44f),
                    S(9f, 13.4f, 11.05f, crown, 0.44f),
                    S(4.8f, 10.2f, 12.15f, throne, 0.44f)
                }
            };
        }

        static ArenaMap Forest()
        {
            var low = new Color32(96, 122, 64, 255);
            var mid = new Color32(118, 96, 58, 255);
            var high = new Color32(72, 110, 62, 255);
            var crown = new Color32(150, 124, 62, 255);
            var canopy = new Color32(86, 130, 70, 255);
            return new ArenaMap
            {
                Theme = ArenaTheme.Forest,
                Name = "Greenwild",
                Blurb = "Mossy branches under a high canopy.",
                Sky = new Color(0.03f, 0.07f, 0.04f),
                Card = new Color(0.62f, 0.84f, 0.5f),
                FloorTint = new Color32(78, 92, 58, 255),
                WallL = new Color32(42, 58, 36, 255),
                WallR = new Color32(36, 52, 32, 255),
                Alice = new Vector3(-3.1f, 1.2f, 0f),
                Bob = new Vector3(3.5f, 1.2f, 0f),
                Charlie = new Vector3(1.2f, 12.81f, 0f),
                SkyDrop = 14.5f,
                Dressing = new[]
                {
                    new ArenaDress { X = -15.15f, Scale = 2.35f, Snow = false },
                    new ArenaDress { X = -12.7f, Scale = 1.7f, Snow = false },
                    new ArenaDress { X = -10.4f, Scale = 1.25f, Snow = false },
                    new ArenaDress { X = 11.2f, Scale = 1.35f, Snow = false },
                    new ArenaDress { X = 13.5f, Scale = 1.85f, Snow = false },
                    new ArenaDress { X = 15.15f, Scale = 2.4f, Snow = false }
                },
                Shelves = new[]
                {
                    S(-14.6f, -10.8f, 2.15f, low, 0.46f),
                    S(-8.2f, -4.4f, 2.7f, low, 0.46f),
                    S(-1.8f, 2.2f, 2.3f, low, 0.46f),
                    S(4.8f, 8.6f, 2.9f, low, 0.46f),
                    S(10.8f, 14.6f, 2.25f, low, 0.46f),
                    S(-14.2f, -11.2f, 4.95f, mid, 0.46f),
                    S(-9.6f, -6f, 5.35f, mid, 0.46f),
                    S(-3.6f, 0.2f, 5.1f, mid, 0.46f),
                    S(2.8f, 6.6f, 5.5f, mid, 0.46f),
                    S(9.2f, 13.2f, 5.05f, mid, 0.46f),
                    S(-13.4f, -9.6f, 7.7f, high, 0.46f),
                    S(-7.2f, -3.4f, 8.05f, high, 0.46f),
                    S(0.6f, 4.4f, 7.85f, high, 0.46f),
                    S(6.8f, 10.6f, 8.2f, high, 0.46f),
                    S(11.6f, 14.8f, 7.6f, high, 0.46f),
                    S(-11.8f, -7.8f, 10.4f, crown, 0.44f),
                    S(-2.2f, 2f, 10.55f, crown, 0.44f),
                    S(5.2f, 9.4f, 10.9f, crown, 0.44f),
                    S(10.2f, 14.2f, 10.35f, crown, 0.44f),
                    S(-1f, 3.4f, 12.1f, canopy, 0.44f)
                }
            };
        }
    }
}
