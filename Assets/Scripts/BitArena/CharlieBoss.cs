using UnityEngine;

namespace BitArena
{
    public class CharlieBoss : MonoBehaviour
    {
        public SpriteRenderer Sprite;
        public SpriteRenderer Gate;
        Vector3 home;
        float castUntil;
        float flashUntil;
        Color flashColor = Color.white;

        public static CharlieBoss Create(Vector3 position)
        {
            var go = new GameObject("Charlie");
            go.transform.position = position;
            var boss = go.AddComponent<CharlieBoss>();
            boss.home = position;
            boss.Sprite = PixelArt.AddSprite(go, PixelArt.Charlie, -20);

            var gate = new GameObject("Gate");
            gate.transform.SetParent(go.transform, false);
            gate.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            boss.Gate = PixelArt.AddSprite(gate, PixelArt.Gate, -15);
            return boss;
        }

        public void MoveHome(Vector3 position)
        {
            home = position;
            transform.position = position;
        }

        public void Cast()
        {
            castUntil = Time.time + 0.42f;
        }

        public void Flash(Color color)
        {
            flashUntil = Time.time + 0.28f;
            flashColor = color;
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * 2.05f) * 0.05f;
            bool casting = Time.time < castUntil;
            float punch = casting ? Mathf.Sin((castUntil - Time.time) * 30f) * 0.06f : 0f;
            transform.position = home + new Vector3(0f, bob, 0f);
            transform.localScale = Vector3.one * (1f + Mathf.Abs(punch));
            if (Sprite != null)
            {
                if (Time.time < flashUntil)
                    Sprite.color = flashColor;
                else
                    Sprite.color = casting ? new Color(1f, 0.82f, 0.45f) : Color.white;
            }
            if (Gate != null)
            {
                float pulse = 0.75f + Mathf.Sin(Time.time * 6f) * 0.25f;
                Gate.transform.localScale = Vector3.one * (casting ? 1.35f : pulse);
            }
        }
    }
}
