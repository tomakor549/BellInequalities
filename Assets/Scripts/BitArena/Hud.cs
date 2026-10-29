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
        Texture2D pixel;
        bool ready;

        const float W = 1920f;
        const float H = 1080f;

        void OnGUI()
        {
            var session = GameSession.I;
            if (session == null)
                return;

            GameInput.Observe(Event.current);
            Ensure();

            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / W, Screen.height / H, 1f));
            switch (session.Phase)
            {
                case MatchPhase.Title:
                    DrawTitle(session);
                    break;
                case MatchPhase.Paused:
                    DrawPlay(session);
                    DrawCard(460, 360, 1000, 220, "Paused", "Esc — back to the fight\nR — restart");
                    break;
                case MatchPhase.Won:
                    DrawPlay(session);
                    DrawCard(420, 320, 1080, 280, "Pair closed", "Charlie is down. His five lives are gone.\nEnter — play again");
                    break;
                case MatchPhase.Lost:
                    DrawPlay(session);
                    DrawCard(420, 320, 1080, 300, "Defeat", LoseText(session) + "\nEnter — play again");
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

        void DrawTitle(GameSession session)
        {
            Panel(new Rect(0, 0, W, H), new Color(0.02f, 0.01f, 0.05f, 0.45f));
            Panel(new Rect(300, 70, 1320, 940), new Color(0.07f, 0.04f, 0.12f, 0.9f));
            Label(new Rect(340, 90, 1240, 80), "Alice & Bob", title, new Color(1f, 0.93f, 0.75f));
            Label(new Rect(340, 170, 1240, 40), "Alice and Bob against Charlie", body, new Color(1f, 0.8f, 0.45f));
            string text =
                "Two players, one keyboard. Press Enter or click to start.\n\n" +
                "<color=#ff5a4a>Alice</color> — red A, bow.   A D move, W jumps, J shoots.\n" +
                "<color=#6aa4ff>Bob</color> — blue B, sword.   Arrow keys move and jump, K swings.\n\n" +
                "Only one of you attacks. The other cannot strike, but a monster hit hurts either of you.\n" +
                "At most three monsters are out, always including at least one 0 and one 1.\n" +
                "Alice and Bob each have a state, 0 or 1. On your turn, kill either bit.\n" +
                "Alice strikes while Bob runs, then Bob strikes while Alice runs.\n" +
                "After both kills, test Ca AND Cb against a XOR b.\n" +
                "If the pair breaks the equation, Charlie loses a life.\n" +
                "If the pair matches, Charlie heals. Empty his five lives to win.\n" +
                "Both states then roll again.\n\n" +
                "Enter — start      Esc — pause      R — restart";
            Label(new Rect(360, 240, 1200, 620), text, body, new Color(0.94f, 0.93f, 0.9f));
            if (!string.IsNullOrEmpty(session.CharlieLine))
                Label(new Rect(360, 860, 1200, 80), "Charlie: " + session.CharlieLine, small, new Color(1f, 0.84f, 0.45f));
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
                "Alice: A D, W, J          Bob: arrow keys, K          break a XOR b to hurt Charlie          R restart, Esc pause",
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
