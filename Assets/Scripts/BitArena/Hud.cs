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
                    DrawCard(460, 340, 1000, 250, "Paused", "Esc — back to the fight\nR — restart\nM — main menu");
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

        void DrawMainMenu(GameSession session)
        {
            Panel(new Rect(0, 0, W, H), new Color(0.04f, 0.02f, 0.07f, 1f));
            Label(new Rect(260, 180, 1400, 90), "Alice & Bob", title, new Color(1f, 0.93f, 0.75f));
            Label(new Rect(260, 270, 1400, 44), "Alice and Bob against Charlie", body, new Color(1f, 0.84f, 0.5f));
            if (MenuButton(session, 0, 460f, "New Game"))
                session.ConfirmMenu();
            if (MenuButton(session, 1, 560f, "Exit"))
                session.ConfirmMenu();
            Label(new Rect(260, 680, 1400, 40), "Up and Down, then Enter", small, new Color(0.8f, 0.78f, 0.72f));
        }

        bool MenuButton(GameSession session, int index, float y, string text)
        {
            var rect = new Rect(700f, y, 520f, 72f);
            bool chosen = session.MenuCursor == index;
            if (chosen)
                Panel(new Rect(rect.x - 8f, rect.y - 8f, rect.width + 16f, rect.height + 16f), new Color(1f, 0.84f, 0.38f, 1f));
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = chosen ? Color.white : new Color(0.55f, 0.48f, 0.62f);
            bool hit = GUI.Button(rect, text, again);
            GUI.backgroundColor = oldBg;
            if (hit)
                session.MoveMenu(index - session.MenuCursor);
            return hit;
        }

        void DrawMapSelect(GameSession session)
        {
            Panel(new Rect(0, 0, W, H), new Color(0.04f, 0.02f, 0.07f, 1f));
            Label(new Rect(260, 70, 1400, 80), "New Game", title, new Color(1f, 0.93f, 0.75f));
            Label(new Rect(260, 150, 1400, 40), "Choose a field", body, new Color(1f, 0.86f, 0.55f));

            float cardW = 460f;
            float gap = 36f;
            float x0 = (W - (cardW * 3f + gap * 2f)) * 0.5f;
            for (int i = 0; i < ArenaMaps.All.Length; i++)
            {
                var map = ArenaMaps.All[i];
                var rect = new Rect(x0 + i * (cardW + gap), 240f, cardW, 280f);
                if (i == session.PickedMap)
                    Panel(new Rect(rect.x - 8f, rect.y - 8f, rect.width + 16f, rect.height + 16f), new Color(1f, 0.84f, 0.38f, 1f));
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = i == session.PickedMap ? Color.white : map.Card;
                if (GUI.Button(rect, map.Name + "\n\n" + map.Blurb, card))
                    session.HighlightMap(i);
                GUI.backgroundColor = oldBg;
            }

            var start = new Rect(480f, 620f, 440f, 68f);
            var back = new Rect(1000f, 620f, 440f, 68f);
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.84f, 0.38f);
            bool go = GUI.Button(start, "Start", again);
            GUI.backgroundColor = new Color(0.75f, 0.84f, 0.98f);
            bool leave = GUI.Button(back, "Back", again);
            GUI.backgroundColor = old;
            if (go)
                session.Begin();
            if (leave)
                session.ShowMenu();

            Label(new Rect(220, 730, 1480, 120),
                "Left and Right choose the field. Enter or Start begins the match. Esc returns.\n" +
                "<color=#ff5a4a>Alice</color>  A D, W, S drop, J          " +
                "<color=#6aa4ff>Bob</color>  arrows, Down drop, K",
                body, new Color(0.92f, 0.9f, 0.84f));
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
            for (int i = 0; i < Tune.CharlieLives; i++)
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
            string sub = won ? "Charlie is down. His five lives are gone." : LoseText(session);
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

            var againBar = new Rect(x + 80f, y + h - 74f, 520f, 52f);
            var menuBar = new Rect(x + w - 600f, y + h - 74f, 520f, 52f);
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.84f, 0.38f);
            bool hit = GUI.Button(againBar, "Play again", again);
            GUI.backgroundColor = new Color(0.75f, 0.84f, 0.98f);
            bool menu = GUI.Button(menuBar, "Main menu", again);
            GUI.backgroundColor = oldBg;
            if (hit)
                session.RequestPlayAgain();
            if (menu)
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
