using UnityEngine;
using UnityEngine.Rendering;

namespace BitArena
{
    public static class PixelArt
    {
        public static Material SharedMaterial { get; private set; }

        public static Sprite Alice;
        public static Sprite Bob;
        public static Sprite Monster0;
        public static Sprite Monster1;
        public static Sprite Charlie;
        public static Sprite Arrow;
        public static Sprite Slash;
        public static Sprite Chevron;
        public static Sprite SparkDot;
        public static Sprite Ring;
        public static Sprite Backdrop;
        public static Sprite Gate;
        public static Texture2D Heart;

        static readonly string[] GlyphA =
        {
            ".111111.",
            "11....11",
            "11....11",
            "11111111",
            "11....11",
            "11....11",
            "11....11",
            "11....11",
        };

        static readonly string[] GlyphB =
        {
            "1111111.",
            "11....11",
            "11....11",
            "1111111.",
            "11....11",
            "11....11",
            "11....11",
            "1111111.",
        };

        static readonly string[] GlyphC =
        {
            ".111111.",
            "11....11",
            "11......",
            "11......",
            "11......",
            "11......",
            "11....11",
            ".111111.",
        };

        static readonly string[] Glyph0 =
        {
            ".111111.",
            "11....11",
            "11....11",
            "11....11",
            "11....11",
            "11....11",
            "11....11",
            ".111111.",
        };

        static readonly string[] Glyph1 =
        {
            "...11...",
            "..111...",
            "...11...",
            "...11...",
            "...11...",
            "...11...",
            "...11...",
            "..1111..",
        };

        public static void BuildAll()
        {
            if (Alice != null)
                return;

            var shader = Shader.Find("BitArena/Sprite");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            SharedMaterial = new Material(shader) { name = "BitArenaSprite" };

            Alice = BuildAlice();
            Bob = BuildBob();
            Monster0 = BuildZero();
            Monster1 = BuildOne();
            Charlie = BuildCharlie();
            Arrow = BuildArrow();
            Slash = BuildSlash();
            Chevron = BuildChevron();
            SparkDot = BuildSpark();
            Ring = BuildRing();
            Backdrop = BuildBackdrop();
            Gate = BuildGate();
            Heart = BuildHeartTex();
        }

        public static SpriteRenderer AddSprite(GameObject go, Sprite sprite, int order)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = SharedMaterial;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        public static Sprite Stone(Vector2 worldSize, Color32 tint, float ppu = 16f)
        {
            int w = Mathf.Max(4, Mathf.RoundToInt(worldSize.x * ppu));
            int h = Mathf.Max(4, Mathf.RoundToInt(worldSize.y * ppu));
            var canvas = new Canvas(w, h);
            var dark = Mul(tint, 0.55f);
            var light = Mul(tint, 1.25f);
            var mortar = Mul(tint, 0.35f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    int row = y / 5;
                    int bx = (x + (row % 2 == 0 ? 0 : 6)) % 12;
                    bool grout = bx == 0 || y % 5 == 0;
                    Color32 c = grout ? mortar : ((x + y) % 9 == 0 ? light : tint);
                    if (y > h - 3)
                        c = light;
                    if (y < 2)
                        c = dark;
                    if (edge)
                        c = dark;
                    canvas.Set(x, y, c);
                }
            }
            return canvas.ToSprite(ppu, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Floor(float width, float height, float ppu = 16f)
        {
            int w = Mathf.Max(8, Mathf.RoundToInt(width * ppu));
            int h = Mathf.Max(8, Mathf.RoundToInt(height * ppu));
            var canvas = new Canvas(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color32 tint = new Color32(92, 82, 98, 255);
                    int row = y / 5;
                    int bx = (x + (row % 2 == 0 ? 0 : 7)) % 14;
                    bool grout = bx == 0 || y % 5 == 0;
                    Color32 c = grout ? Mul(tint, 0.4f) : tint;
                    if (y > h - 3)
                        c = Mul(tint, 1.35f);
                    if (y < 2 || x < 2 || x > w - 3)
                        c = Mul(tint, 0.45f);
                    canvas.Set(x, y, c);
                }
            }
            return canvas.ToSprite(ppu, new Vector2(0.5f, 0.5f));
        }

        static Sprite BuildAlice()
        {
            var canvas = new Canvas(64, 60);
            var red = new Color32(232, 52, 52, 255);
            var dark = new Color32(92, 16, 18, 255);
            var gold = new Color32(255, 206, 84, 255);
            DrawGlyph(canvas, 8, 16, 4, GlyphA, red, dark);
            canvas.Fill(18, 6, 6, 8, dark);
            canvas.Fill(36, 6, 6, 8, dark);
            canvas.Fill(16, 4, 10, 4, red);
            canvas.Fill(34, 4, 10, 4, red);
            canvas.Disc(50, 34, 8, gold);
            canvas.Disc(50, 34, 5, new Color32(0, 0, 0, 0), true);
            canvas.Fill(46, 33, 9, 2, dark);
            return canvas.ToSprite(40f, new Vector2(0.42f, 0.46f));
        }

        static Sprite BuildBob()
        {
            var canvas = new Canvas(64, 60);
            var blue = new Color32(58, 122, 255, 255);
            var dark = new Color32(14, 36, 110, 255);
            var steel = new Color32(236, 246, 255, 255);
            DrawGlyph(canvas, 6, 16, 4, GlyphB, blue, dark);
            canvas.Fill(16, 6, 6, 8, dark);
            canvas.Fill(34, 6, 6, 8, dark);
            canvas.Fill(14, 4, 10, 4, blue);
            canvas.Fill(32, 4, 10, 4, blue);
            canvas.Fill(46, 30, 14, 4, steel);
            canvas.Fill(56, 26, 4, 12, steel);
            canvas.Fill(44, 28, 6, 8, new Color32(70, 110, 170, 255));
            canvas.Fill(48, 31, 10, 2, new Color32(180, 220, 255, 255));
            return canvas.ToSprite(40f, new Vector2(0.40f, 0.46f));
        }

        static Sprite BuildZero()
        {
            var canvas = new Canvas(56, 64);
            var ink = new Color32(255, 214, 70, 255);
            var dark = new Color32(110, 68, 12, 255);
            DrawGlyph(canvas, 8, 14, 5, Glyph0, ink, dark);
            canvas.Fill(22, 36, 4, 4, new Color32(40, 20, 10, 255));
            canvas.Fill(34, 36, 4, 4, new Color32(40, 20, 10, 255));
            canvas.Set(23, 37, new Color32(255, 255, 255, 255));
            canvas.Set(35, 37, new Color32(255, 255, 255, 255));
            canvas.Fill(18, 4, 6, 8, dark);
            canvas.Fill(32, 4, 6, 8, dark);
            return canvas.ToSprite(36f, new Vector2(0.5f, 0.46f));
        }

        static Sprite BuildOne()
        {
            var canvas = new Canvas(56, 64);
            var ink = new Color32(214, 186, 255, 255);
            var dark = new Color32(62, 36, 110, 255);
            DrawGlyph(canvas, 8, 12, 5, Glyph1, ink, dark);
            canvas.Fill(30, 40, 4, 4, new Color32(30, 16, 50, 255));
            canvas.Set(31, 41, new Color32(255, 255, 255, 255));
            canvas.Fill(24, 4, 8, 6, dark);
            return canvas.ToSprite(36f, new Vector2(0.5f, 0.46f));
        }

        static Sprite BuildCharlie()
        {
            var canvas = new Canvas(80, 104);
            var cloak = new Color32(46, 28, 72, 255);
            var cloakDark = new Color32(22, 12, 38, 255);
            var gold = new Color32(232, 184, 78, 255);
            var skin = new Color32(214, 176, 140, 255);
            canvas.Fill(18, 8, 44, 58, cloak);
            canvas.Fill(10, 20, 60, 28, cloakDark);
            canvas.Disc(40, 74, 16, cloak);
            canvas.Disc(40, 70, 8, skin);
            canvas.Fill(34, 70, 3, 3, new Color32(20, 10, 20, 255));
            canvas.Fill(44, 70, 3, 3, new Color32(20, 10, 20, 255));
            canvas.Fill(36, 64, 8, 2, new Color32(120, 50, 60, 255));
            DrawGlyph(canvas, 24, 28, 4, GlyphC, gold, cloakDark);
            canvas.Fill(8, 34, 10, 6, gold);
            canvas.Fill(62, 34, 10, 6, gold);
            canvas.Disc(10, 44, 5, new Color32(255, 220, 120, 255));
            canvas.Disc(70, 44, 5, new Color32(255, 220, 120, 255));
            canvas.Fill(30, 0, 8, 10, cloakDark);
            canvas.Fill(44, 0, 8, 10, cloakDark);
            return canvas.ToSprite(18f, new Vector2(0.5f, 0.42f));
        }

        static Sprite BuildArrow()
        {
            var canvas = new Canvas(16, 6);
            var c = new Color32(255, 90, 70, 255);
            var d = new Color32(120, 24, 16, 255);
            canvas.Fill(0, 2, 10, 2, c);
            canvas.Fill(8, 1, 3, 4, c);
            canvas.Fill(11, 0, 2, 6, c);
            canvas.Fill(13, 1, 2, 4, d);
            canvas.Fill(14, 2, 2, 2, new Color32(255, 230, 140, 255));
            return canvas.ToSprite(16f, new Vector2(0.15f, 0.5f));
        }

        static Sprite BuildSlash()
        {
            var canvas = new Canvas(20, 20);
            var c = new Color32(200, 230, 255, 230);
            for (int i = 0; i < 12; i++)
            {
                canvas.Set(3 + i, 2 + i, c);
                canvas.Set(4 + i, 2 + i, new Color32(255, 255, 255, 255));
                canvas.Set(3 + i, 3 + i, c);
            }
            return canvas.ToSprite(16f, new Vector2(0.2f, 0.2f));
        }

        static Sprite BuildChevron()
        {
            var canvas = new Canvas(10, 8);
            var c = new Color32(255, 246, 180, 255);
            for (int x = 0; x < 5; x++)
            {
                canvas.Set(x, x, c);
                canvas.Set(9 - x, x, c);
                canvas.Set(x, x + 1, c);
                canvas.Set(9 - x, x + 1, c);
            }
            return canvas.ToSprite(16f, new Vector2(0.5f, 0f));
        }

        static Sprite BuildSpark()
        {
            var canvas = new Canvas(4, 4);
            canvas.Fill(1, 0, 2, 4, new Color32(255, 255, 255, 255));
            canvas.Fill(0, 1, 4, 2, new Color32(255, 255, 255, 255));
            return canvas.ToSprite(16f, new Vector2(0.5f, 0.5f));
        }

        static Sprite BuildRing()
        {
            var canvas = new Canvas(24, 10);
            var c = new Color32(255, 220, 120, 180);
            canvas.Disc(12, 4, 10, c);
            canvas.Disc(12, 4, 6, new Color32(0, 0, 0, 0), true);
            return canvas.ToSprite(16f, new Vector2(0.5f, 0.5f));
        }

        static Sprite BuildGate()
        {
            var canvas = new Canvas(28, 28);
            canvas.Ring(14, 14, 12, 8, new Color32(255, 210, 90, 255));
            canvas.Disc(14, 14, 5, new Color32(255, 255, 220, 200));
            return canvas.ToSprite(16f, new Vector2(0.5f, 0.5f));
        }

        static Texture2D BuildHeartTex()
        {
            var canvas = new Canvas(16, 15);
            var c = new Color32(255, 255, 255, 255);
            canvas.Disc(4, 9, 4, c);
            canvas.Disc(11, 9, 4, c);
            for (int y = 0; y < 9; y++)
            {
                int half = 7 - Mathf.Abs(y - 2);
                int width = Mathf.Clamp(y + 2, 2, 14);
                canvas.Fill(8 - width / 2, y, width, 1, c);
            }
            var tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels32(canvas.Pixels);
            tex.Apply(false);
            return tex;
        }

        static Sprite BuildBackdrop()
        {
            const int w = 480;
            const int h = 270;
            var canvas = new Canvas(w, h);
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                var sky = Lerp(new Color32(10, 6, 22, 255), new Color32(42, 18, 48, 255), v);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    var side = Lerp(new Color32(48, 32, 64, 255), sky, 0.55f);
                    float wing = Mathf.Abs(u - 0.5f) * 2f;
                    var c = Lerp(sky, side, wing * 0.85f);
                    float dx = (u - 0.5f) * 1.4f;
                    float dy = (v - 0.45f) * 1.6f;
                    float vig = Mathf.Clamp01((dx * dx + dy * dy) * 0.55f);
                    c = Lerp(c, new Color32(4, 2, 10, 255), vig * 0.45f);
                    if ((x * 17 + y * 13) % 97 == 0 && y > 140)
                        c = Lerp(c, new Color32(255, 230, 180, 255), 0.7f);
                    canvas.Set(x, y, c);
                }
            }

            canvas.Fill(36, 40, 28, 150, new Color32(32, 22, 48, 255));
            canvas.Fill(416, 40, 28, 150, new Color32(28, 24, 58, 255));
            canvas.Fill(70, 70, 18, 110, new Color32(58, 42, 72, 255));
            canvas.Fill(392, 70, 18, 110, new Color32(58, 42, 72, 255));
            canvas.Fill(150, 90, 16, 90, new Color32(48, 30, 58, 255));
            canvas.Fill(314, 90, 16, 90, new Color32(36, 32, 64, 255));

            for (int x = 168; x < 312; x++)
            {
                int arch = (int)(Mathf.Sqrt(Mathf.Max(0f, 70f * 70f - (x - 240) * (x - 240))) * 0.9f);
                canvas.Fill(x, 120, 1, 16 + arch / 8, new Color32(20, 12, 32, 255));
                if (arch > 8)
                    canvas.Set(x, 120 + arch / 3, new Color32(210, 160, 70, 255));
            }

            canvas.Fill(0, 0, w, 36, new Color32(18, 12, 24, 255));
            return canvas.ToSprite(18f, new Vector2(0.5f, 0.42f));
        }

        static void DrawGlyph(Canvas canvas, int x, int bottom, int scale, string[] rows, Color32 ink, Color32 outline)
        {
            var offsets = new[]
            {
                new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1),
                new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(1, 1)
            };
            foreach (var offset in offsets)
                Stamp(canvas, x + offset.x, bottom + offset.y, scale, rows, outline);
            Stamp(canvas, x, bottom, scale, rows, ink);
        }

        static void Stamp(Canvas canvas, int x, int bottom, int scale, string[] rows, Color32 color)
        {
            for (int row = 0; row < rows.Length; row++)
            {
                string line = rows[row];
                int py = bottom + (rows.Length - 1 - row) * scale;
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] != '1')
                        continue;
                    canvas.Fill(x + col * scale, py, scale, scale, color);
                }
            }
        }

        static Color32 Mul(Color32 c, float k)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * k), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * k), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * k), 0, 255),
                c.a);
        }

        static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t));
        }

        sealed class Canvas
        {
            public readonly int W;
            public readonly int H;
            public readonly Color32[] Pixels;

            public Canvas(int w, int h)
            {
                W = w;
                H = h;
                Pixels = new Color32[w * h];
            }

            public void Set(int x, int y, Color32 color)
            {
                if ((uint)x >= (uint)W || (uint)y >= (uint)H)
                    return;
                if (color.a == 0)
                {
                    Pixels[y * W + x] = new Color32(0, 0, 0, 0);
                    return;
                }
                Pixels[y * W + x] = color;
            }

            public void Fill(int x, int y, int w, int h, Color32 color)
            {
                for (int iy = 0; iy < h; iy++)
                for (int ix = 0; ix < w; ix++)
                    Set(x + ix, y + iy, color);
            }

            public void Disc(int cx, int cy, int radius, Color32 color, bool erase = false)
            {
                int r2 = radius * radius;
                for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > r2)
                        continue;
                    Set(cx + x, cy + y, erase ? new Color32(0, 0, 0, 0) : color);
                }
            }

            public void Ring(int cx, int cy, int outer, int inner, Color32 color)
            {
                int o2 = outer * outer;
                int i2 = inner * inner;
                for (int y = -outer; y <= outer; y++)
                for (int x = -outer; x <= outer; x++)
                {
                    int d = x * x + y * y;
                    if (d <= o2 && d >= i2)
                        Set(cx + x, cy + y, color);
                }
            }

            public Sprite ToSprite(float ppu, Vector2 pivot)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0
                };
                tex.SetPixels32(Pixels);
                tex.Apply(false);
                return Sprite.Create(tex, new Rect(0, 0, W, H), pivot, ppu, 0, SpriteMeshType.FullRect);
            }
        }
    }
}
