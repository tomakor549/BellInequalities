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
        GUIStyle howHeader;
        GUIStyle howKey;
        GUIStyle howDesc;
        GUIStyle howNote;
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
                || session.Phase == MatchPhase.HowToPlay
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
                case MatchPhase.HowToPlay:
                    DrawHowToPlay(session);
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
            howHeader = Make(font, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            howKey = Make(font, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            howDesc = Make(font, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            howNote = Make(font, 15, FontStyle.Normal, TextAnchor.MiddleLeft);
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
            float cardH = 640f;
            float cardX = (W - cardW) * 0.5f;
            float cardY = (H - cardH) * 0.5f;

            // Main glowing modal frame
            DrawGlowFrame(new Rect(cardX, cardY, cardW, cardH),
                new Color(0.07f, 0.045f, 0.12f, 0.96f),
                new Color(0.85f, 0.68f, 0.28f, 0.95f),
                new Color(0.45f, 0.22f, 0.6f, 0.4f),
                4f, 10f);

            // Card header plate
            Panel(new Rect(cardX + 4f, cardY + 4f, cardW - 8f, 178f), new Color(0.14f, 0.08f, 0.22f, 0.75f));
            Panel(new Rect(cardX + 4f, cardY + 182f, cardW - 8f, 2f), new Color(1f, 0.82f, 0.35f, 0.85f));

            Label(new Rect(cardX + 20f, cardY + 18f, cardW - 40f, 74f), "ALICE & BOB", title, new Color(1f, 0.94f, 0.68f));
            Label(new Rect(cardX + 20f, cardY + 92f, cardW - 40f, 32f), "Cooperative Quantum Arena · Against Charlie", body, new Color(1f, 0.84f, 0.52f));

            // Hero badges row
            float badgeW = 310f;
            float badgeH = 36f;
            float badgeY = cardY + 132f;

            // Alice badge
            Panel(new Rect(cardX + 60f, badgeY, badgeW, badgeH), new Color(0.38f, 0.08f, 0.08f, 0.85f));
            Panel(new Rect(cardX + 60f, badgeY, badgeW, 2f), new Color(1f, 0.45f, 0.4f, 0.9f));
            Label(new Rect(cardX + 60f, badgeY, badgeW, badgeH), "<color=#ff7675>Alice</color> (Archer)", small, new Color(1f, 0.8f, 0.8f));

            // Bob badge
            Panel(new Rect(cardX + cardW - 60f - badgeW, badgeY, badgeW, badgeH), new Color(0.08f, 0.2f, 0.42f, 0.85f));
            Panel(new Rect(cardX + cardW - 60f - badgeW, badgeY, badgeW, 2f), new Color(0.45f, 0.75f, 1f, 0.9f));
            Label(new Rect(cardX + cardW - 60f - badgeW, badgeY, badgeW, badgeH), "<color=#74b9ff>Bob</color> (Swordsman)", small, new Color(0.8f, 0.9f, 1f));

            // Difficulty selector
            float btnW = 560f;
            float btnH = 68f;
            float btnX = cardX + (cardW - btnW) * 0.5f;

            DrawDifficulty(session, btnX, cardY + 196f, btnW);

            // 3 Action Buttons
            Rect rect0 = new Rect(btnX, cardY + 312f, btnW, btnH);
            Rect rect1 = new Rect(btnX, cardY + 398f, btnW, btnH);
            Rect rect2 = new Rect(btnX, cardY + 484f, btnW, btnH);

            if (rect0.Contains(Event.current.mousePosition) && session.MenuCursor != 0)
                session.MoveMenu(0 - session.MenuCursor);
            else if (rect1.Contains(Event.current.mousePosition) && session.MenuCursor != 1)
                session.MoveMenu(1 - session.MenuCursor);
            else if (rect2.Contains(Event.current.mousePosition) && session.MenuCursor != 2)
                session.MoveMenu(2 - session.MenuCursor);

            if (DrawMenuButton(rect0, "New Game", session.MenuCursor == 0, new Color(1f, 0.84f, 0.32f), 28f))
                session.ConfirmMenu();

            if (DrawMenuButton(rect1, "How to play", session.MenuCursor == 1, new Color(0.38f, 0.85f, 1f), 28f))
                session.ConfirmMenu();

            if (DrawMenuButton(rect2, "Exit Game", session.MenuCursor == 2, new Color(1f, 0.45f, 0.38f), 28f))
                session.ConfirmMenu();

            Label(new Rect(0, H - 38f, W, 28f), "Alice & Bob · Cooperative Quantum Arena", small, new Color(0.72f, 0.7f, 0.78f, 0.75f));
        }

        void DrawHowToPlay(GameSession session)
        {
            DrawBackdrop(PixelArt.Backdrop, new Color(0.55f, 0.5f, 0.65f, 0.95f), 0.78f);

            float cardW = 1480f;
            float cardH = 720f;
            float cardX = (W - cardW) * 0.5f;
            float cardY = (H - cardH) * 0.5f;

            // Main glowing modal frame
            DrawGlowFrame(new Rect(cardX, cardY, cardW, cardH),
                new Color(0.07f, 0.045f, 0.13f, 0.97f),
                new Color(0.38f, 0.85f, 1f, 0.95f),
                new Color(0.2f, 0.4f, 0.8f, 0.35f),
                4f, 12f);

            // Header plate
            Panel(new Rect(cardX + 4f, cardY + 4f, cardW - 8f, 90f), new Color(0.12f, 0.08f, 0.22f, 0.85f));
            Panel(new Rect(cardX + 4f, cardY + 94f, cardW - 8f, 2f), new Color(0.38f, 0.85f, 1f, 0.85f));

            Label(new Rect(cardX + 20f, cardY + 12f, cardW - 40f, 46f), "HOW TO PLAY", title, new Color(1f, 0.94f, 0.68f));
            Label(new Rect(cardX + 20f, cardY + 58f, cardW - 40f, 28f), "Controls, Roles & Game Mechanics for Alice and Bob", small, new Color(0.85f, 0.92f, 1f));

            // 3 Columns layout
            float padding = 28f;
            float gap = 20f;
            float colW = (cardW - padding * 2f - gap * 2f) / 3f;
            float colY = cardY + 110f;

            // ========== COLUMN 1: HERO CONTROLS ==========
            float c1X = cardX + padding;

            // Box 1: Alice
            float box1H = 240f;
            DrawFrame(new Rect(c1X, colY, colW, box1H),
                new Color(0.14f, 0.06f, 0.08f, 0.92f),
                new Color(0.85f, 0.35f, 0.35f, 0.85f), 2.5f);
            Panel(new Rect(c1X + 2f, colY + 2f, colW - 4f, 32f), new Color(0.35f, 0.1f, 0.12f, 0.85f));
            Label(new Rect(c1X, colY + 4f, colW, 26f), "ALICE  (PLAYER 1 · ARCHER)", howHeader, new Color(1f, 0.65f, 0.6f));

            DrawKeyRow(c1X + 14f, colY + 40f, colW - 28f, "A / D", "Move Left / Right", howKey, howDesc, new Color(1f, 0.88f, 0.82f));
            DrawKeyRow(c1X + 14f, colY + 78f, colW - 28f, "W", "Jump", howKey, howDesc, new Color(1f, 0.88f, 0.82f));
            DrawKeyRow(c1X + 14f, colY + 116f, colW - 28f, "S", "Drop through platforms", howKey, howDesc, new Color(1f, 0.88f, 0.82f));
            DrawKeyRow(c1X + 14f, colY + 154f, colW - 28f, "J / L-Ctrl", "Shoot Bow (Ranged attack)", howKey, howDesc, new Color(1f, 0.88f, 0.82f));
            DrawKeyRow(c1X + 14f, colY + 192f, colW - 28f, "ACTIVE", "Can attack only as Attacker", howKey, howDesc, new Color(1f, 0.7f, 0.7f));

            // Box 2: Bob
            float box2Y = colY + box1H + 14f;
            float box2H = 240f;
            DrawFrame(new Rect(c1X, box2Y, colW, box2H),
                new Color(0.06f, 0.1f, 0.18f, 0.92f),
                new Color(0.35f, 0.65f, 0.95f, 0.85f), 2.5f);
            Panel(new Rect(c1X + 2f, box2Y + 2f, colW - 4f, 32f), new Color(0.1f, 0.18f, 0.35f, 0.85f));
            Label(new Rect(c1X, box2Y + 4f, colW, 26f), "BOB  (PLAYER 2 · SWORDSMAN)", howHeader, new Color(0.65f, 0.85f, 1f));

            DrawKeyRow(c1X + 14f, box2Y + 40f, colW - 28f, "◄ / ►", "Move Left / Right", howKey, howDesc, new Color(0.82f, 0.9f, 1f));
            DrawKeyRow(c1X + 14f, box2Y + 78f, colW - 28f, "▲", "Jump", howKey, howDesc, new Color(0.82f, 0.9f, 1f));
            DrawKeyRow(c1X + 14f, box2Y + 116f, colW - 28f, "▼", "Drop through platforms", howKey, howDesc, new Color(0.82f, 0.9f, 1f));
            DrawKeyRow(c1X + 14f, box2Y + 154f, colW - 28f, "K / R-Ctrl", "Slash Sword (Melee strike)", howKey, howDesc, new Color(0.82f, 0.9f, 1f));
            DrawKeyRow(c1X + 14f, box2Y + 192f, colW - 28f, "ACTIVE", "Can attack only as Attacker", howKey, howDesc, new Color(0.7f, 0.85f, 1f));

            // ========== COLUMN 2: SYSTEM & MENU CONTROLS ==========
            float c2X = c1X + colW + gap;

            // Box 3: Menu Navigation
            float box3H = 240f;
            DrawFrame(new Rect(c2X, colY, colW, box3H),
                new Color(0.08f, 0.08f, 0.14f, 0.92f),
                new Color(0.85f, 0.68f, 0.28f, 0.85f), 2.5f);
            Panel(new Rect(c2X + 2f, colY + 2f, colW - 4f, 32f), new Color(0.25f, 0.2f, 0.08f, 0.85f));
            Label(new Rect(c2X, colY + 4f, colW, 26f), "MENU & NAVIGATION", howHeader, new Color(1f, 0.88f, 0.5f));

            DrawKeyRow(c2X + 14f, colY + 40f, colW - 28f, "▲ ▼ / W S", "Navigate menu options", howKey, howDesc, new Color(0.95f, 0.92f, 0.85f));
            DrawKeyRow(c2X + 14f, colY + 78f, colW - 28f, "◄ ► / A D", "Change Difficulty / Arena Map", howKey, howDesc, new Color(0.95f, 0.92f, 0.85f));
            DrawKeyRow(c2X + 14f, colY + 116f, colW - 28f, "ENTER/SPACE", "Confirm selection / Start", howKey, howDesc, new Color(1f, 0.85f, 0.45f));
            DrawKeyRow(c2X + 14f, colY + 154f, colW - 28f, "MOUSE", "Hover and click any option", howKey, howDesc, new Color(0.95f, 0.92f, 0.85f));
            DrawKeyRow(c2X + 14f, colY + 192f, colW - 28f, "ESC", "Back to Menu / Cancel", howKey, howDesc, new Color(0.95f, 0.92f, 0.85f));

            // Box 4: Match Shortcuts
            DrawFrame(new Rect(c2X, box2Y, colW, box2H),
                new Color(0.08f, 0.08f, 0.14f, 0.92f),
                new Color(0.55f, 0.4f, 0.8f, 0.85f), 2.5f);
            Panel(new Rect(c2X + 2f, box2Y + 2f, colW - 4f, 32f), new Color(0.2f, 0.14f, 0.32f, 0.85f));
            Label(new Rect(c2X, box2Y + 4f, colW, 26f), "MATCH SHORTCUTS", howHeader, new Color(0.85f, 0.75f, 1f));

            DrawKeyRow(c2X + 14f, box2Y + 40f, colW - 28f, "ESC", "Pause / Resume match", howKey, howDesc, new Color(0.92f, 0.9f, 0.98f));
            DrawKeyRow(c2X + 14f, box2Y + 78f, colW - 28f, "R", "Quick restart match instantly", howKey, howDesc, new Color(0.92f, 0.9f, 0.98f));
            DrawKeyRow(c2X + 14f, box2Y + 116f, colW - 28f, "M", "Return to Main Menu from Pause", howKey, howDesc, new Color(0.92f, 0.9f, 0.98f));
            DrawKeyRow(c2X + 14f, box2Y + 154f, colW - 28f, "RESULTS", "Change difficulty & map after match", howKey, howDesc, new Color(1f, 0.85f, 0.45f));
            DrawKeyRow(c2X + 14f, box2Y + 192f, colW - 28f, "SPACE / R", "Play again on Results screen", howKey, howDesc, new Color(0.92f, 0.9f, 0.98f));

            // ========== COLUMN 3: GAMEPLAY & QUANTUM RULES ==========
            float c3X = c2X + colW + gap;

            // Box 5: Charlie & Win Condition
            float box5H = 152f;
            DrawFrame(new Rect(c3X, colY, colW, box5H),
                new Color(0.12f, 0.08f, 0.05f, 0.92f),
                new Color(0.95f, 0.65f, 0.2f, 0.85f), 2.5f);
            Panel(new Rect(c3X + 2f, colY + 2f, colW - 4f, 30f), new Color(0.3f, 0.16f, 0.06f, 0.85f));
            Label(new Rect(c3X, colY + 4f, colW, 24f), "OBJECTIVE: DEFEAT CHARLIE", howHeader, new Color(1f, 0.82f, 0.4f));

            Label(new Rect(c3X + 16f, colY + 36f, colW - 32f, 108f),
                "Charlie is the quantum observer. When Alice and Bob coordinate to violate the Bell / CHSH inequality bound, Charlie loses one life!\n\nDefeat Charlie by reducing his lives to 0. If both heroes run out of hearts, the match is lost.",
                howNote, new Color(0.95f, 0.9f, 0.82f));

            // Box 6: Roles & Role Swap
            float box6Y = colY + box5H + 14f;
            float box6H = 154f;
            DrawFrame(new Rect(c3X, box6Y, colW, box6H),
                new Color(0.08f, 0.06f, 0.14f, 0.92f),
                new Color(0.8f, 0.45f, 0.85f, 0.85f), 2.5f);
            Panel(new Rect(c3X + 2f, box6Y + 2f, colW - 4f, 30f), new Color(0.24f, 0.12f, 0.28f, 0.85f));
            Label(new Rect(c3X, box6Y + 4f, colW, 24f), "ROLE SWAP MECHANIC", howHeader, new Color(1f, 0.72f, 0.95f));

            Label(new Rect(c3X + 16f, box6Y + 36f, colW - 32f, 110f),
                "At any time, exactly ONE hero is the <color=#ff5555>Attacker</color> (has glowing attack aura & badge) and the other is the <color=#55aaff>Fleer</color>.\n\nOnly the Attacker can slay monsters. Each time a monster is defeated, the roles SWAP immediately between Alice and Bob!",
                howNote, new Color(0.95f, 0.9f, 0.96f));

            // Box 7: Health Pickups
            float box7Y = box6Y + box6H + 14f;
            float box7H = 160f;
            DrawFrame(new Rect(c3X, box7Y, colW, box7H),
                new Color(0.06f, 0.12f, 0.08f, 0.92f),
                new Color(0.35f, 0.85f, 0.45f, 0.85f), 2.5f);
            Panel(new Rect(c3X + 2f, box7Y + 2f, colW - 4f, 30f), new Color(0.1f, 0.25f, 0.14f, 0.85f));
            Label(new Rect(c3X, box7Y + 4f, colW, 24f), "HEALTH PICKUPS  (+1 HP)", howHeader, new Color(0.6f, 1f, 0.75f));

            Label(new Rect(c3X + 16f, box7Y + 36f, colW - 32f, 116f),
                "A glowing heart item randomly appears on arena platforms. When Alice or Bob touches it, they restore <color=#55ff88>+1 Health</color> instantly!\n\nBe quick: every <color=#ffeaa7>5 seconds</color> the heart automatically teleports to a new random platform on the map.",
                howNote, new Color(0.88f, 0.98f, 0.92f));

            // ========== BOTTOM: BACK TO MENU BUTTON ==========
            float btnW = 460f;
            float btnH = 60f;
            float btnX = cardX + (cardW - btnW) * 0.5f;
            float btnY = cardY + 630f;
            Rect backRect = new Rect(btnX, btnY, btnW, btnH);

            if (DrawMenuButton(backRect, "◄  BACK TO MENU  (Esc)", true, new Color(0.38f, 0.85f, 1f), 26f))
            {
                session.BackToMenu();
            }
        }

        void DrawKeyRow(float x, float y, float w, string key, string desc, GUIStyle keyStyle, GUIStyle descStyle, Color descColor)
        {
            float keyW = 136f;
            Panel(new Rect(x, y + 2f, keyW, 26f), new Color(0f, 0f, 0f, 0.45f));
            DrawFrame(new Rect(x, y + 2f, keyW, 26f), new Color(0.2f, 0.16f, 0.28f, 0.9f), new Color(1f, 0.84f, 0.35f, 0.7f), 1.5f);
            Label(new Rect(x, y + 2f, keyW, 26f), key, keyStyle, new Color(1f, 0.9f, 0.5f));

            Label(new Rect(x + keyW + 12f, y, w - keyW - 12f, 30f), desc, descStyle, descColor);
        }

        void DrawDifficulty(GameSession session, float x, float y, float w)
        {
            Label(new Rect(x, y, w, 28f), "DIFFICULTY (Charlie Lives)", small, new Color(1f, 0.86f, 0.55f));
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

        void DrawMapBar(GameSession session, float x, float y, float w)
        {
            Label(new Rect(x, y, w, 28f), "BATTLEFIELD ARENA", small, new Color(1f, 0.86f, 0.55f));
            float barY = y + 32f;
            float barH = 52f;
            int count = ArenaMaps.All.Length;
            float segW = w / count;
            DrawFrame(new Rect(x, barY, w, barH), new Color(0.1f, 0.07f, 0.16f, 0.96f), new Color(0.55f, 0.42f, 0.18f, 0.95f), 2.5f);
            for (int i = 0; i < count; i++)
            {
                var map = ArenaMaps.All[i];
                var rect = new Rect(x + i * segW, barY, segW, barH);
                bool on = session.PickedMap == i;
                Color themeColor = map.Theme == ArenaTheme.Winter
                    ? new Color(0.35f, 0.85f, 1f)
                    : map.Theme == ArenaTheme.Forest
                        ? new Color(0.45f, 0.95f, 0.5f)
                        : new Color(1f, 0.84f, 0.35f);

                string icon = map.Theme == ArenaTheme.Winter ? "❄ "
                    : map.Theme == ArenaTheme.Forest ? "🌲 " : "🏛 ";

                if (on)
                    Panel(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), themeColor);
                else if (rect.Contains(Event.current.mousePosition))
                    Panel(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f), new Color(themeColor.r, themeColor.g, themeColor.b, 0.22f));

                if (i > 0)
                    Panel(new Rect(rect.x, barY + 8f, 2f, barH - 16f), new Color(1f, 0.82f, 0.35f, 0.35f));

                var style = new GUIStyle(small)
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                Label(rect, icon + map.Name.ToUpper(), style,
                    on ? new Color(0.1f, 0.05f, 0.02f) : new Color(0.94f, 0.9f, 0.82f));
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    session.HighlightMap(i);
            }

            Label(new Rect(x, barY + barH + 6f, w, 26f), ArenaMaps.All[session.PickedMap].Name + " arena selected", small, new Color(1f, 0.84f, 0.5f));
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
            var currentMap = ArenaMaps.All[Mathf.Clamp(session.PickedMap, 0, ArenaMaps.All.Length - 1)];
            Sprite bgSprite = GetMapBackdrop(currentMap.Theme);
            DrawBackdrop(bgSprite, currentMap.Sky, 0.72f);

            const float x = 270f;
            const float y = 55f;
            const float w = 1380f;
            const float h = 970f;

            Color themeBorder = won
                ? new Color(1f, 0.84f, 0.35f, 0.96f)
                : new Color(1f, 0.38f, 0.32f, 0.96f);
            Color themeGlow = won
                ? new Color(1f, 0.75f, 0.2f, 0.45f)
                : new Color(0.95f, 0.2f, 0.25f, 0.45f);

            DrawGlowFrame(new Rect(x, y, w, h),
                new Color(0.06f, 0.04f, 0.11f, 0.97f),
                themeBorder,
                themeGlow,
                4f, 10f);

            // Top Status Badge
            float badgeW = 340f;
            float badgeH = 38f;
            Rect topBadge = new Rect(x + (w - badgeW) * 0.5f, y - 19f, badgeW, badgeH);
            Panel(topBadge, themeBorder);
            string badgeText = won ? "★ ★ ★   VICTORY!   ★ ★ ★" : "☠   DEFEAT   ☠";
            Label(topBadge, badgeText, badge, new Color(0.12f, 0.05f, 0.02f));

            // Header Banner
            Panel(new Rect(x + 4f, y + 4f, w - 8f, 140f), won
                ? new Color(0.18f, 0.12f, 0.05f, 0.85f)
                : new Color(0.2f, 0.06f, 0.08f, 0.85f));
            Panel(new Rect(x + 4f, y + 144f, w - 8f, 2f), themeBorder);

            string winTitle = "CONGRATULATIONS, ALICE & BOB!";
            string loseTitle = "YOU WERE DEFEATED!";
            Label(new Rect(x + 30f, y + 18f, w - 60f, 54f),
                won ? winTitle : loseTitle,
                title,
                won ? new Color(1f, 0.94f, 0.68f) : new Color(1f, 0.65f, 0.58f));

            string winSub = "Charlie's health reached zero! You broke the classical bound and won the match!";
            string loseSub = LoseText(session) + " — Don't give up! Coordinate your strikes and try again!";
            Label(new Rect(x + 30f, y + 74f, w - 60f, 32f),
                won ? winSub : loseSub,
                body,
                won ? new Color(1f, 0.86f, 0.52f) : new Color(1f, 0.82f, 0.78f));

            string winNote = "Charlie is defeated! Choose difficulty and map below to play another match.";
            string loseNote = "Defeat is just a lesson in quantum entanglement. Adjust options and try again!";
            Label(new Rect(x + 30f, y + 108f, w - 60f, 28f),
                won ? winNote : loseNote,
                small,
                new Color(0.85f, 0.82f, 0.92f));

            // Selector Box: Difficulty and Map options for next attempt
            float selY = y + 158f;
            float selW = w - 64f;
            float selX = x + 32f;
            float colGap = 32f;
            float colW = (selW - colGap) * 0.5f;

            DrawDifficulty(session, selX, selY, colW);
            DrawMapBar(session, selX + colW + colGap, selY, colW);

            // Match Summary Table
            float tableX = x + 32f;
            float tableW = w - 64f;
            float inner = tableW - 20f;
            float headY = selY + 128f;

            DrawResultHeader(tableX, headY, inner);
            Panel(new Rect(tableX, headY + 38f, inner, 2f), new Color(1f, 0.82f, 0.45f, 0.8f));

            float listY = headY + 46f;
            float listH = (y + h) - listY - 96f;
            int count = session.Rounds.Count;
            const float rowH = 54f;

            if (count == 0)
            {
                Panel(new Rect(tableX, listY, inner, 80f), new Color(0f, 0f, 0f, 0.35f));
                Label(new Rect(tableX, listY + 22f, inner, 36f), "No finished pairs in this match.", body, new Color(0.8f, 0.78f, 0.72f));
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

            // Bottom Action Buttons
            float btnW = 500f;
            float btnH = 68f;
            float btnY = y + h - 82f;

            var againBar = new Rect(x + 100f, btnY, btnW, btnH);
            var menuBar = new Rect(x + w - 100f - btnW, btnY, btnW, btnH);

            string againText = won ? "Play Again (Enter)" : "Try Again (Enter / R)";
            Color againAccent = won ? new Color(0.35f, 0.95f, 0.55f) : new Color(1f, 0.84f, 0.35f);

            if (DrawMenuButton(againBar, againText, true, againAccent, 27f))
                session.RequestPlayAgain();

            if (DrawMenuButton(menuBar, "Main Menu (M)", false, new Color(0.75f, 0.84f, 0.98f), 27f))
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
                return "Alice took critical damage from a monster.";
            if (session.LostHero != null && session.LostHero.Kind == HeroKind.Bob)
                return "Bob took critical damage from a monster.";
            return "A hero was knocked out in battle.";
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
