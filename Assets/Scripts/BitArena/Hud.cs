using UnityEngine;

namespace BitArena
{
    public class Hud : MonoBehaviour
    {
        GUIStyle title;
        GUIStyle banner;
        GUIStyle body;
        GUIStyle small;
        GUIStyle huge;
        GUIStyle cell;
        GUIStyle again;
        GUIStyle card;
        GUIStyle badge;
        Texture2D pixel;
        bool ready;
        Vector2 tableScroll;

        const float W = 1920f;
        const float H = 1080f;

        void OnGUI()
        {
            var session = GameSession.I;
            if (session == null)
                return;

            var evt = Event.current;
            GameInput.Observe(evt);
            if (session.Phase == MatchPhase.Menu || session.Phase == MatchPhase.MapSelect
                || session.Phase == MatchPhase.Won || session.Phase == MatchPhase.Lost)
                GUIUtility.keyboardControl = 0;
            if (WantsFreshMatch(session, evt))
                session.RequestPlayAgain();
            Ensure();

            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / W, Screen.height / H, 1f));
            switch (session.Phase)
            {
                case MatchPhase.Menu:
                    DrawMainMenu(session);
                    break;
                case MatchPhase.MapSelect:
                    DrawMapSelect(session);
                    break;
                case MatchPhase.Paused:
                    DrawPlay(session);
                    DrawPauseMenu(session);
                    break;
                case MatchPhase.Won:
                    DrawPlay(session);
                    DrawResults(session, true);
                    break;
                case MatchPhase.Lost:
                    DrawPlay(session);
                    DrawResults(session, false);
                    break;
                default:
                    DrawPlay(session);
                    break;
            }
            GUI.matrix = previous;
        }

        void Ensure()
        {
            if (ready)
                return;
            ready = true;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();

            title = Make(font, 64, FontStyle.Bold, TextAnchor.MiddleCenter);
            banner = Make(font, 36, FontStyle.Bold, TextAnchor.MiddleCenter);
            huge = Make(font, 72, FontStyle.Bold, TextAnchor.MiddleCenter);
            cell = Make(font, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
            again = new GUIStyle(GUI.skin.button)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            if (font != null)
                again.font = font;
            again.normal.textColor = new Color(0.16f, 0.08f, 0.02f);
            again.hover.textColor = new Color(0.16f, 0.08f, 0.02f);
            again.active.textColor = new Color(0.16f, 0.08f, 0.02f);
            card = new GUIStyle(again)
            {
                fontSize = 24,
                wordWrap = true
            };
            body = Make(font, 28, FontStyle.Normal, TextAnchor.MiddleCenter);
            small = Make(font, 22, FontStyle.Normal, TextAnchor.MiddleCenter);
            badge = Make(font, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        static GUIStyle Make(Font font, int size, FontStyle style, TextAnchor anchor)
        {
            var gui = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = style,
                alignment = anchor,
                wordWrap = true,
                richText = true
            };
            if (font != null)
                gui.font = font;
            gui.normal.textColor = Color.white;
            return gui;
        }

        static Sprite GetMapBackdrop(ArenaTheme theme)
        {
            if (theme == ArenaTheme.Winter)
                return PixelArt.WinterBackdrop;
            if (theme == ArenaTheme.Forest)
                return PixelArt.ForestBackdrop;
            return PixelArt.Backdrop;
        }

        void DrawBackdrop(Sprite sprite, Color tint, float darkOverlay = 0.65f)
        {
            if (sprite != null && sprite.texture != null)
            {
                var oldColor = GUI.color;
                GUI.color = tint;
                GUI.DrawTexture(new Rect(0, 0, W, H), sprite.texture, ScaleMode.ScaleAndCrop);
                GUI.color = oldColor;
            }
            Panel(new Rect(0, 0, W, H), new Color(0.04f, 0.02f, 0.08f, darkOverlay));
            Panel(new Rect(0, 0, W, 6f), new Color(1f, 0.82f, 0.35f, 0.85f));
            Panel(new Rect(0, H - 6f, W, 6f), new Color(1f, 0.82f, 0.35f, 0.85f));
        }

        void DrawFrame(Rect rect, Color fill, Color border, float borderThickness = 3f)
        {
            Panel(rect, border);
            Panel(new Rect(rect.x + borderThickness, rect.y + borderThickness,
                rect.width - borderThickness * 2f, rect.height - borderThickness * 2f), fill);
            Panel(new Rect(rect.x + borderThickness, rect.y + borderThickness,
                rect.width - borderThickness * 2f, 2f), new Color(1f, 1f, 1f, 0.18f));
        }

        void DrawGlowFrame(Rect rect, Color fill, Color border, Color glow, float borderThickness = 4f, float glowSize = 8f)
        {
            Panel(new Rect(rect.x - glowSize, rect.y - glowSize,
                rect.width + glowSize * 2f, rect.height + glowSize * 2f), glow);
            DrawFrame(rect, fill, border, borderThickness);
        }

        bool DrawMenuButton(Rect rect, string text, bool chosen, Color accentColor, float fontSize = 28f)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);

            // Drop shadow
            Panel(new Rect(rect.x + 4f, rect.y + 6f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.45f));

            if (chosen)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * 5.5f) * 0.5f + 0.5f;
                Rect expRect = new Rect(rect.x - 8f, rect.y - 3f, rect.width + 16f, rect.height + 6f);

                Color glow = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f + pulse * 0.35f);
                Color border = Color.Lerp(accentColor, Color.white, 0.25f + pulse * 0.45f);
                Color fill = Color.Lerp(accentColor, new Color(1f, 0.94f, 0.65f), 0.28f);

                DrawGlowFrame(expRect, fill, border, glow, 4f, 8f + pulse * 4f);

                string labelText = "▶   " + text.ToUpper() + "   ◀";
                var style = new GUIStyle(banner)
                {
                    fontSize = (int)fontSize + 2,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                // Text shadow
                var oldColor = style.normal.textColor;
                style.normal.textColor = new Color(0.7f, 0.45f, 0.1f, 0.5f);
                GUI.Label(new Rect(expRect.x + 2f, expRect.y + 2f, expRect.width, expRect.height), labelText, style);
                style.normal.textColor = new Color(0.12f, 0.05f, 0.02f, 1f);
                GUI.Label(expRect, labelText, style);
                style.normal.textColor = oldColor;

                return GUI.Button(expRect, GUIContent.none, GUIStyle.none);
            }
            else
            {
                Color border = hovered
                    ? Color.Lerp(accentColor, Color.white, 0.45f)
                    : new Color(0.38f, 0.32f, 0.52f, 0.85f);
                Color fill = hovered
                    ? new Color(0.24f, 0.18f, 0.36f, 0.98f)
                    : new Color(0.12f, 0.09f, 0.19f, 0.95f);
                Color textColor = hovered
                    ? Color.white
                    : new Color(0.88f, 0.86f, 0.94f);

                if (hovered)
                {
                    Panel(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f),
                        new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f));
                }

                DrawFrame(rect, fill, border, hovered ? 3.5f : 2.5f);

                var style = new GUIStyle(banner)
                {
                    fontSize = (int)fontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                Label(rect, text, style, textColor);

                return GUI.Button(rect, GUIContent.none, GUIStyle.none);
            }
        }

        void DrawMainMenu(GameSession session)
        {
            DrawBackdrop(PixelArt.Backdrop, new Color(0.65f, 0.6f, 0.75f, 0.9f), 0.66f);

            float cardW = 820f;
            float cardH = 750f;
            float cardX = (W - cardW) * 0.5f;
            float cardY = 160f;

            // Main glowing modal frame
            DrawGlowFrame(new Rect(cardX, cardY, cardW, cardH),
                new Color(0.07f, 0.045f, 0.12f, 0.96f),
                new Color(0.85f, 0.68f, 0.28f, 0.95f),
                new Color(0.45f, 0.22f, 0.6f, 0.4f),
                4f, 10f);

            // Card header plate
            Panel(new Rect(cardX + 4f, cardY + 4f, cardW - 8f, 190f), new Color(0.14f, 0.08f, 0.22f, 0.75f));
            Panel(new Rect(cardX + 4f, cardY + 194f, cardW - 8f, 2f), new Color(1f, 0.82f, 0.35f, 0.85f));

            Label(new Rect(cardX + 20f, cardY + 24f, cardW - 40f, 80f), "ALICE & BOB", title, new Color(1f, 0.94f, 0.68f));
            Label(new Rect(cardX + 20f, cardY + 104f, cardW - 40f, 36f), "Cooperative Quantum Arena · Against Charlie", body, new Color(1f, 0.84f, 0.52f));

            // Hero badges row
            float badgeW = 310f;
            float badgeH = 36f;
            float badgeY = cardY + 146f;

            // Alice badge
            Panel(new Rect(cardX + 60f, badgeY, badgeW, badgeH), new Color(0.38f, 0.08f, 0.08f, 0.85f));
            Panel(new Rect(cardX + 60f, badgeY, badgeW, 2f), new Color(1f, 0.45f, 0.4f, 0.9f));
            Label(new Rect(cardX + 60f, badgeY, badgeW, badgeH), "<color=#ff7675>Alice</color> (Bow · Jump W)", small, new Color(1f, 0.8f, 0.8f));

            // Bob badge
            Panel(new Rect(cardX + cardW - 60f - badgeW, badgeY, badgeW, badgeH), new Color(0.08f, 0.2f, 0.42f, 0.85f));
            Panel(new Rect(cardX + cardW - 60f - badgeW, badgeY, badgeW, 2f), new Color(0.45f, 0.75f, 1f, 0.9f));
            Label(new Rect(cardX + cardW - 60f - badgeW, badgeY, badgeW, badgeH), "<color=#74b9ff>Bob</color> (Sword · Jump Up)", small, new Color(0.8f, 0.9f, 1f));

            float btnW = 560f;
            float btnX = cardX + (cardW - btnW) * 0.5f;
            DrawDifficulty(session, btnX, cardY + 208f, btnW);

            // Buttons
            float btnH = 72f;
            Rect rect0 = new Rect(btnX, cardY + 360f, btnW, btnH);
            Rect rect1 = new Rect(btnX, cardY + 448f, btnW, btnH);

            if (rect0.Contains(Event.current.mousePosition) && session.MenuCursor != 0)
                session.MoveMenu(-session.MenuCursor);
            else if (rect1.Contains(Event.current.mousePosition) && session.MenuCursor != 1)
                session.MoveMenu(1 - session.MenuCursor);

            if (DrawMenuButton(rect0, "New Game", session.MenuCursor == 0, new Color(1f, 0.84f, 0.32f), 30f))
                session.ConfirmMenu();

            if (DrawMenuButton(rect1, "Exit Game", session.MenuCursor == 1, new Color(1f, 0.45f, 0.38f), 30f))
                session.ConfirmMenu();

            // Instructions footer panel inside card
            Panel(new Rect(cardX + 60f, cardY + 540f, cardW - 120f, 150f), new Color(0.04f, 0.02f, 0.08f, 0.75f));
            Panel(new Rect(cardX + 60f, cardY + 540f, cardW - 120f, 2f), new Color(1f, 0.82f, 0.35f, 0.35f));

            Label(new Rect(cardX + 80f, cardY + 552f, cardW - 160f, 32f),
                "▲ ▼ / W S  Navigate Options", body, new Color(0.96f, 0.94f, 0.88f));
            Label(new Rect(cardX + 80f, cardY + 590f, cardW - 160f, 32f),
                "◀ ▶ / A D  Set Difficulty", body, new Color(0.96f, 0.94f, 0.88f));
            Label(new Rect(cardX + 80f, cardY + 628f, cardW - 160f, 32f),
                "ENTER or SPACE  Confirm Selection", body, new Color(1f, 0.84f, 0.42f));

            Label(new Rect(0, H - 42f, W, 32f), "Two Players · One Keyboard · Defeat Charlie", small, new Color(0.72f, 0.7f, 0.78f));
        }

        void DrawDifficulty(GameSession session, float x, float y, float w)
        {
            Label(new Rect(x, y, w, 28f), "DIFFICULTY", small, new Color(1f, 0.86f, 0.55f));
            float barY = y + 32f;
            float barH = 52f;
            int count = GameSession.DifficultyNames.Length;
            float segW = w / count;
            DrawFrame(new Rect(x, barY, w, barH), new Color(0.1f, 0.07f, 0.16f, 0.96f), new Color(0.55f, 0.42f, 0.18f, 0.95f), 2.5f);
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(x + i * segW, barY, segW, barH);
                bool on = session.Difficulty == i;
                if (on)
                    Panel(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(1f, 0.84f, 0.32f, 1f));
                else if (rect.Contains(Event.current.mousePosition))
                    Panel(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(1f, 0.84f, 0.32f, 0.18f));
                if (i > 0)
                    Panel(new Rect(rect.x, barY + 8f, 2f, barH - 16f), new Color(1f, 0.82f, 0.35f, 0.35f));

                var style = new GUIStyle(small)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                Label(rect, GameSession.DifficultyNames[i].ToUpper(), style,
                    on ? new Color(0.12f, 0.05f, 0.02f) : new Color(0.94f, 0.9f, 0.82f));
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    session.SetDifficulty(i);
            }

            Label(new Rect(x, barY + barH + 6f, w, 26f), "Charlie has " + session.CharlieMax + " lives", small, new Color(1f, 0.84f, 0.5f));
        }

        void DrawMapSelect(GameSession session)
        {
            var currentMap = ArenaMaps.All[Mathf.Clamp(session.PickedMap, 0, ArenaMaps.All.Length - 1)];
            Sprite bgSprite = GetMapBackdrop(currentMap.Theme);
            DrawBackdrop(bgSprite, currentMap.Sky, 0.62f);

            // Title Header
            Label(new Rect(260, 40, 1400, 70), "SELECT BATTLEFIELD", title, new Color(1f, 0.93f, 0.68f));
            Label(new Rect(260, 114, 1400, 36), "Choose the arena where Alice and Bob face Charlie", body, new Color(1f, 0.84f, 0.5f));

            float cardW = 480f;
            float gap = 38f;
            float x0 = (W - (cardW * 3f + gap * 2f)) * 0.5f;
            float baseCardY = 175f;
            float baseCardH = 410f;

            for (int i = 0; i < ArenaMaps.All.Length; i++)
            {
                var map = ArenaMaps.All[i];
                bool chosen = i == session.PickedMap;

                Color themeColor = map.Theme == ArenaTheme.Winter
                    ? new Color(0.35f, 0.85f, 1f)
                    : map.Theme == ArenaTheme.Forest
                        ? new Color(0.45f, 0.95f, 0.5f)
                        : new Color(1f, 0.84f, 0.35f);

                string themeIcon = map.Theme == ArenaTheme.Winter ? "❄"
                    : map.Theme == ArenaTheme.Forest ? "🌲" : "🏛";

                Rect cardRect = chosen
                    ? new Rect(x0 + i * (cardW + gap) - 6f, baseCardY - 8f, cardW + 12f, baseCardH + 16f)
                    : new Rect(x0 + i * (cardW + gap), baseCardY, cardW, baseCardH);

                bool hovered = cardRect.Contains(Event.current.mousePosition);

                // Drop shadow
                Panel(new Rect(cardRect.x + 6f, cardRect.y + 8f, cardRect.width, cardRect.height), new Color(0f, 0f, 0f, 0.55f));

                if (chosen)
                {
                    float pulse = Mathf.Sin(Time.unscaledTime * 5.5f) * 0.5f + 0.5f;
                    Color glow = new Color(themeColor.r, themeColor.g, themeColor.b, 0.4f + pulse * 0.35f);
                    Color border = Color.Lerp(themeColor, Color.white, 0.35f + pulse * 0.35f);
                    Color fill = new Color(0.09f, 0.06f, 0.16f, 0.96f);

                    DrawGlowFrame(cardRect, fill, border, glow, 4f, 10f + pulse * 4f);

                    // Top Selection Pill Banner
                    float pillW = 280f;
                    float pillH = 38f;
                    Rect pillRect = new Rect(cardRect.x + (cardRect.width - pillW) * 0.5f, cardRect.y - 19f, pillW, pillH);
                    Panel(pillRect, border);
                    Label(pillRect, "★  SELECTED ARENA  ★", badge, new Color(0.12f, 0.05f, 0.02f));

                    // Header banner inside card
                    Panel(new Rect(cardRect.x + 4f, cardRect.y + 22f, cardRect.width - 8f, 84f),
                        new Color(themeColor.r * 0.35f, themeColor.g * 0.35f, themeColor.b * 0.35f, 0.85f));
                    Panel(new Rect(cardRect.x + 4f, cardRect.y + 106f, cardRect.width - 8f, 2f), border);

                    Label(new Rect(cardRect.x + 10f, cardRect.y + 28f, cardRect.width - 20f, 44f),
                        themeIcon + "  " + map.Name.ToUpper(), banner, Color.white);
                    Label(new Rect(cardRect.x + 10f, cardRect.y + 72f, cardRect.width - 20f, 26f),
                        "ACTIVE BATTLEFIELD", small, themeColor);

                    // Card body blurb
                    Label(new Rect(cardRect.x + 24f, cardRect.y + 130f, cardRect.width - 48f, 130f),
                        map.Blurb, body, new Color(0.96f, 0.94f, 0.88f));

                    // Shelf count / arena detail pill
                    Panel(new Rect(cardRect.x + 36f, cardRect.y + 285f, cardRect.width - 72f, 44f),
                        new Color(0f, 0f, 0f, 0.45f));
                    Panel(new Rect(cardRect.x + 36f, cardRect.y + 285f, cardRect.width - 72f, 2f),
                        new Color(themeColor.r, themeColor.g, themeColor.b, 0.5f));
                    int shelfCount = map.Shelves != null ? map.Shelves.Length : 0;
                    Label(new Rect(cardRect.x + 36f, cardRect.y + 287f, cardRect.width - 72f, 40f),
                        "Platforms: " + shelfCount + " shelves · Ready",
                        small, themeColor);

                    // Press Enter prompt tag
                    Label(new Rect(cardRect.x + 20f, cardRect.y + 355f, cardRect.width - 40f, 34f),
                        "▶ Press ENTER to play here ◀", small, Color.white);
                }
                else
                {
                    Color border = hovered
                        ? Color.Lerp(themeColor, Color.white, 0.45f)
                        : new Color(0.32f, 0.28f, 0.42f, 0.8f);
                    Color fill = hovered
                        ? new Color(0.18f, 0.14f, 0.26f, 0.96f)
                        : new Color(0.08f, 0.05f, 0.13f, 0.92f);

                    if (hovered)
                    {
                        Panel(new Rect(cardRect.x - 3f, cardRect.y - 3f, cardRect.width + 6f, cardRect.height + 6f),
                            new Color(themeColor.r, themeColor.g, themeColor.b, 0.2f));
                    }

                    DrawFrame(cardRect, fill, border, hovered ? 3.5f : 2.5f);

                    // Header
                    Panel(new Rect(cardRect.x + 3f, cardRect.y + 12f, cardRect.width - 6f, 70f),
                        new Color(0f, 0f, 0f, 0.35f));
                    Label(new Rect(cardRect.x + 10f, cardRect.y + 22f, cardRect.width - 20f, 42f),
                        themeIcon + "  " + map.Name, banner, hovered ? Color.white : new Color(0.85f, 0.82f, 0.92f));

                    // Blurb
                    Label(new Rect(cardRect.x + 20f, cardRect.y + 115f, cardRect.width - 40f, 140f),
                        map.Blurb, body, new Color(0.78f, 0.76f, 0.84f));

                    // Click to pick hint
                    Label(new Rect(cardRect.x + 20f, cardRect.y + 340f, cardRect.width - 40f, 34f),
                        hovered ? "Click to Select" : "← → or Click to pick", small, hovered ? themeColor : new Color(0.6f, 0.58f, 0.68f));
                }

                if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
                {
                    session.HighlightMap(i);
                }
            }

            // Bottom Action Buttons
            float btnW = 440f;
            float btnH = 76f;
            float btnY = 630f;

            var startRect = new Rect(W * 0.5f - btnW - 25f, btnY, btnW, btnH);
            var backRect = new Rect(W * 0.5f + 25f, btnY, btnW, btnH);

            var emeraldAccent = new Color(0.35f, 0.95f, 0.55f);
            var purpleAccent = new Color(0.72f, 0.68f, 0.95f);

            if (DrawMenuButton(startRect, "Start Match", true, emeraldAccent, 28f))
                session.Begin();

            if (DrawMenuButton(backRect, "Back to Menu", false, purpleAccent, 28f))
                session.ShowMenu();

            // Controls Guide Banner
            Panel(new Rect(180f, 745f, W - 360f, 96f), new Color(0.04f, 0.02f, 0.08f, 0.88f));
            Panel(new Rect(180f, 745f, W - 360f, 2f), new Color(1f, 0.82f, 0.35f, 0.55f));

            Label(new Rect(200f, 755f, W - 400f, 34f),
                "Left / Right  choose field   ·   Enter  starts match   ·   Esc  returns to menu",
                small, new Color(1f, 0.9f, 0.65f));

            Label(new Rect(200f, 792f, W - 400f, 36f),
                "<color=#ff5a4a>Alice:</color> A D move, W jump, S drop, J shoot          " +
                "<color=#6aa4ff>Bob:</color> Arrows move & jump, Down drop, K slash",
                body, new Color(0.92f, 0.9f, 0.84f));
        }

        void DrawPauseMenu(GameSession session)
        {
            Panel(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.65f));

            float cardW = 720f;
            float cardH = 500f;
            float cardX = (W - cardW) * 0.5f;
            float cardY = (H - cardH) * 0.5f;

            DrawGlowFrame(new Rect(cardX, cardY, cardW, cardH),
                new Color(0.08f, 0.05f, 0.14f, 0.96f),
                new Color(1f, 0.82f, 0.35f, 0.9f),
                new Color(0.5f, 0.3f, 0.7f, 0.45f),
                4f, 10f);

            Label(new Rect(cardX + 20f, cardY + 28f, cardW - 40f, 60f), "GAME PAUSED", title, new Color(1f, 0.92f, 0.68f));
            Panel(new Rect(cardX + 60f, cardY + 98f, cardW - 120f, 2f), new Color(1f, 0.82f, 0.35f, 0.6f));

            float btnW = 540f;
            float btnH = 68f;
            float btnX = cardX + (cardW - btnW) * 0.5f;

            if (DrawMenuButton(new Rect(btnX, cardY + 130f, btnW, btnH), "Resume Fight (Esc)", true, new Color(0.35f, 0.95f, 0.55f), 26f))
            {
                session.Phase = MatchPhase.Play;
                Time.timeScale = 1f;
            }

            if (DrawMenuButton(new Rect(btnX, cardY + 225f, btnW, btnH), "Restart Match (R)", false, new Color(1f, 0.82f, 0.35f), 26f))
            {
                session.RequestPlayAgain();
            }

            if (DrawMenuButton(new Rect(btnX, cardY + 320f, btnW, btnH), "Main Menu (M)", false, new Color(0.7f, 0.65f, 0.95f), 26f))
            {
                session.OpenMenu();
            }

            Label(new Rect(cardX + 20f, cardY + 430f, cardW - 40f, 34f), "Click any button or use shortcut keys", small, new Color(0.78f, 0.76f, 0.84f));
        }

        void DrawPlay(GameSession session)
        {
            Panel(new Rect(0, 0, W, 176), new Color(0.03f, 0.02f, 0.06f, 0.78f));
            Label(new Rect(24, 10, 110, 34), "Alice", small, new Color(1f, 0.4f, 0.34f));
            DrawHearts(session.Alice, 150, 12, new Color(1f, 0.32f, 0.28f));
            DrawHearts(session.Bob, 1610, 12, new Color(0.35f, 0.62f, 1f));
            Label(new Rect(1770, 10, 120, 34), "Bob", small, new Color(0.45f, 0.7f, 1f));
            Label(new Rect(280, 48, 1360, 44), session.Objective, banner, Color.white);
            Label(new Rect(220, 96, 1480, 32), session.Subtitle, small, new Color(0.95f, 0.93f, 0.86f));
            Label(new Rect(220, 128, 1480, 32), session.CharlieLine, small, new Color(1f, 0.82f, 0.4f));
            DrawCharlieLives(session);

            if (session.Phase == MatchPhase.Play && Time.unscaledTime < session.CalloutUntil)
            {
                Panel(new Rect(610, 430, 700, 120), new Color(0.05f, 0.03f, 0.08f, 0.78f));
                Label(new Rect(610, 445, 700, 90), session.BigCallout, huge, new Color(1f, 0.92f, 0.7f));
            }

            DrawRoleTags(session);
            DrawFloaters(session);
            Panel(new Rect(0, 1010, W, 70), new Color(0.03f, 0.02f, 0.06f, 0.7f));
            Label(new Rect(40, 1020, 1840, 46),
                "Alice: A D, W, S drop, J          Bob: arrows, Down drop, K          R restart, Esc pause",
                small, new Color(0.9f, 0.9f, 0.86f));
        }

        void DrawHearts(Hero hero, float x, float y, Color color)
        {
            if (hero == null || PixelArt.Heart == null)
                return;
            for (int i = 0; i < Tune.Hearts; i++)
            {
                var old = GUI.color;
                GUI.color = i < hero.Hp ? color : new Color(1f, 1f, 1f, 0.16f);
                GUI.DrawTexture(new Rect(x + i * 38f, y, 32, 30), PixelArt.Heart);
                GUI.color = old;
            }
        }

        void DrawCharlieLives(GameSession session)
        {
            if (PixelArt.Heart == null)
                return;
            Label(new Rect(790, 8, 130, 34), "Charlie", small, new Color(1f, 0.84f, 0.4f));
            var color = new Color(1f, 0.78f, 0.28f);
            float x = 920f;
            for (int i = 0; i < session.CharlieMax; i++)
            {
                var old = GUI.color;
                GUI.color = i < session.CharlieHp ? color : new Color(1f, 1f, 1f, 0.16f);
                GUI.DrawTexture(new Rect(x + i * 34f, 10, 28, 26), PixelArt.Heart);
                GUI.color = old;
            }
        }

        void DrawRoleTags(GameSession session)
        {
            if (session.Phase != MatchPhase.Play && session.Phase != MatchPhase.Paused)
                return;
            var cam = Camera.main;
            if (cam == null)
                return;
            TagHero(cam, session.Alice, session.Attacker == session.Alice);
            TagHero(cam, session.Bob, session.Attacker == session.Bob);
        }

        void TagHero(Camera cam, Hero hero, bool attacker)
        {
            if (hero == null)
                return;
            Vector3 screen = cam.WorldToScreenPoint(hero.transform.position + Vector3.up * 1.15f);
            if (screen.z < 0f)
                return;
            float x = screen.x / Screen.width * W;
            float y = (Screen.height - screen.y) / Screen.height * H;
            string text = attacker ? "ATTACK" : "FLEE";
            var color = attacker ? new Color(1f, 0.9f, 0.45f) : new Color(1f, 0.55f, 0.45f);
            Label(new Rect(x - 80, y - 18, 160, 32), text, small, color);
        }

        void DrawFloaters(GameSession session)
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            for (int i = 0; i < session.Floaters.Count; i++)
            {
                var floater = session.Floaters[i];
                float age = Time.time - floater.Born;
                Vector3 screen = cam.WorldToScreenPoint(floater.Pos + Vector3.up * age * 0.7f);
                if (screen.z < 0f)
                    continue;
                float x = screen.x / Screen.width * W;
                float y = (Screen.height - screen.y) / Screen.height * H;
                var color = floater.Color;
                color.a = Mathf.Clamp01(1f - age / 0.95f);
                Label(new Rect(x - 140, y - 16, 280, 36), floater.Text, small, color);
            }
        }

        void DrawResults(GameSession session, bool won)
        {
            const float x = 310f;
            const float y = 78f;
            const float w = 1300f;
            const float h = 900f;
            Panel(new Rect(x, y, w, h), new Color(0.05f, 0.03f, 0.09f, 0.94f));
            Panel(new Rect(x, y, w, 8f), won ? new Color(1f, 0.82f, 0.35f) : new Color(1f, 0.35f, 0.32f));
            Label(new Rect(x + 40, y + 24, w - 80, 64), won ? "Pair closed" : "Defeat", title, new Color(1f, 0.9f, 0.7f));
            string sub = won ? "Charlie is down. His lives are gone." : LoseText(session);
            Label(new Rect(x + 40, y + 92, w - 80, 40), sub, body, new Color(0.92f, 0.9f, 0.84f));

            float tableX = x + 36f;
            float tableW = w - 72f;
            float inner = tableW - 24f;
            float headY = y + 150f;
            DrawResultHeader(tableX, headY, inner);
            Panel(new Rect(tableX, headY + 42f, inner, 2f), new Color(1f, 0.82f, 0.45f, 0.9f));

            float listY = headY + 52f;
            float listH = h - (listY - y) - 88f;
            int count = session.Rounds.Count;
            const float rowH = 58f;
            if (count == 0)
            {
                Label(new Rect(tableX, listY + 16f, inner, 40f), "No finished pairs.", body, new Color(0.8f, 0.78f, 0.72f));
            }
            else
            {
                var view = new Rect(tableX, listY, tableW, listH);
                float maxScroll = Mathf.Max(0f, count * rowH - listH);
                GUI.BeginGroup(view);
                if (Event.current.type == EventType.ScrollWheel)
                {
                    tableScroll.y = Mathf.Clamp(tableScroll.y + Event.current.delta.y * 24f, 0f, maxScroll);
                    Event.current.Use();
                }
                GUI.BeginGroup(new Rect(0f, -tableScroll.y, inner, count * rowH));
                for (int i = 0; i < count; i++)
                    DrawResultRow(session.Rounds[i], i, inner, rowH);
                GUI.EndGroup();
                GUI.EndGroup();
            }

            var againBar = new Rect(x + 80f, y + h - 78f, 520f, 60f);
            var menuBar = new Rect(x + w - 600f, y + h - 78f, 520f, 60f);

            if (DrawMenuButton(againBar, "Play Again (Enter)", true, new Color(1f, 0.84f, 0.35f), 26f))
                session.RequestPlayAgain();
            if (DrawMenuButton(menuBar, "Main Menu (M)", false, new Color(0.75f, 0.84f, 0.98f), 26f))
                session.OpenMenu();
        }

        static bool WantsFreshMatch(GameSession session, Event evt)
        {
            if (session == null || evt == null)
                return false;
            bool results = session.Phase == MatchPhase.Won || session.Phase == MatchPhase.Lost;
            if (evt.type != EventType.KeyDown)
                return false;
            bool keyR = evt.keyCode == KeyCode.R || evt.character == 'r' || evt.character == 'R';
            if (keyR && (results || session.Phase == MatchPhase.Paused))
                return true;
            if (!results)
                return false;
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Space)
                return true;
            char c = evt.character;
            return c == '\r' || c == '\n' || c == ' ';
        }

        static void ColumnSpans(out float a, out float b, out float ca, out float cb)
        {
            a = 150f;
            b = 150f;
            ca = 170f;
            cb = 170f;
        }

        void DrawResultHeader(float x, float y, float w)
        {
            ColumnSpans(out float a, out float b, out float ca, out float cb);
            Label(new Rect(x, y, a, 36), "A", cell, new Color(1f, 0.48f, 0.4f));
            Label(new Rect(x + a, y, b, 36), "B", cell, new Color(0.5f, 0.74f, 1f));
            Label(new Rect(x + a + b, y, ca, 36), "Ca", cell, new Color(1f, 0.86f, 0.45f));
            Label(new Rect(x + a + b + ca, y, cb, 36), "Cb", cell, new Color(0.8f, 0.72f, 1f));
            Label(new Rect(x + a + b + ca + cb, y, w - (a + b + ca + cb), 36), "Charlie", cell, new Color(1f, 0.84f, 0.45f));
        }

        void DrawResultRow(RoundNote note, int index, float w, float rowH)
        {
            ColumnSpans(out float a, out float b, out float ca, out float cb);
            float y = index * rowH;
            var bg = index % 2 == 0 ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 0.86f, 0.45f, 0.07f);
            Panel(new Rect(0f, y + 4f, w, rowH - 8f), bg);
            ValueChip(new Rect(16f, y + 8f, 114f, rowH - 16f), note.A.ToString(), new Color(0.55f, 0.16f, 0.14f, 0.95f), new Color(1f, 0.66f, 0.56f));
            ValueChip(new Rect(a + 16f, y + 8f, 114f, rowH - 16f), note.B.ToString(), new Color(0.12f, 0.24f, 0.48f, 0.95f), new Color(0.72f, 0.86f, 1f));
            ValueChip(new Rect(a + b + 22f, y + 8f, 124f, rowH - 16f), note.Ca.ToString(), new Color(0.42f, 0.3f, 0.08f, 0.95f), new Color(1f, 0.9f, 0.55f));
            ValueChip(new Rect(a + b + ca + 22f, y + 8f, 124f, rowH - 16f), note.Cb.ToString(), new Color(0.28f, 0.2f, 0.46f, 0.95f), new Color(0.86f, 0.78f, 1f));

            string outcome = note.Shift == CharlieShift.Lost
                ? "lost one life"
                : note.Shift == CharlieShift.Recovered
                    ? "recovered one life"
                    : "already full";
            var ink = note.Shift == CharlieShift.Lost
                ? new Color(1f, 0.55f, 0.48f)
                : note.Shift == CharlieShift.Recovered
                    ? new Color(0.55f, 1f, 0.72f)
                    : new Color(0.82f, 0.8f, 0.74f);
            Label(new Rect(a + b + ca + cb, y, w - (a + b + ca + cb) - 8f, rowH), outcome, cell, ink);
        }

        void ValueChip(Rect rect, string text, Color fill, Color ink)
        {
            Panel(rect, fill);
            Label(rect, text, cell, ink);
        }

        void DrawCard(float x, float y, float w, float h, string heading, string text)
        {
            Panel(new Rect(x, y, w, h), new Color(0.05f, 0.03f, 0.09f, 0.92f));
            Label(new Rect(x + 30, y + 24, w - 60, 70), heading, title, new Color(1f, 0.9f, 0.7f));
            Label(new Rect(x + 40, y + 110, w - 80, h - 130), text, body, Color.white);
        }

        static string LoseText(GameSession session)
        {
            if (session.LostHero != null && session.LostHero.Kind == HeroKind.Alice)
                return "A monster hit Alice.";
            return "A monster hit Bob.";
        }

        void Panel(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }

        void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            var previous = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = previous;
        }
    }
}
