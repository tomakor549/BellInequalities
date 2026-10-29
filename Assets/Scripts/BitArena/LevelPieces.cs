using UnityEngine;

namespace BitArena
{
    public class SolidLevel : MonoBehaviour
    {
    }

    public class ArenaCamera : MonoBehaviour
    {
        public float Shake;
        public float CenterY = 4.15f;
        public float HalfWidth = 11.2f;
        public float HalfHeight = 5.9f;

        Camera cam;
        Vector3 look;

        void Awake()
        {
            cam = GetComponent<Camera>();
            look = new Vector3(0f, CenterY, -10f);
        }

        void LateUpdate()
        {
            if (cam == null)
                return;
            float aspect = Mathf.Max(0.6f, cam.aspect);
            cam.orthographicSize = Mathf.Max(HalfHeight, HalfWidth / aspect);
            Vector3 focus = new Vector3(0f, CenterY, -10f);
            var session = GameSession.I;
            if (session != null && session.Phase == MatchPhase.Play && session.Alice != null && session.Bob != null)
            {
                Vector3 mid = (session.Alice.transform.position + session.Bob.transform.position) * 0.5f;
                float x = Mathf.Clamp(mid.x * 0.18f, -1.35f, 1.35f);
                float y = Mathf.Clamp((mid.y - 2.2f) * 0.12f, -0.4f, 0.65f);
                focus = new Vector3(x, CenterY + y, -10f);
            }
            float blend = 1f - Mathf.Exp(-3.5f * Time.unscaledDeltaTime);
            look = Vector3.Lerp(look, focus, blend);
            Vector2 jolt = Shake > 0.001f ? Random.insideUnitCircle * Shake : Vector2.zero;
            cam.transform.position = look + new Vector3(jolt.x, jolt.y, 0f);
            cam.transform.rotation = Quaternion.identity;
            Shake = Mathf.MoveTowards(Shake, 0f, Time.unscaledDeltaTime * 7f);
        }
    }

    public class Spark : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Life = 0.35f;
        SpriteRenderer sprite;
        float age;

        void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            age = Life;
        }

        void Update()
        {
            age -= Time.deltaTime;
            Velocity.y -= 22f * Time.deltaTime;
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            if (sprite != null)
            {
                var color = sprite.color;
                color.a = Mathf.Clamp01(age / Life);
                sprite.color = color;
            }
            if (age <= 0f)
                Destroy(gameObject);
        }
    }

    public static class Vfx
    {
        public static void Burst(Vector3 position, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Spark");
                go.transform.position = position;
                var renderer = PixelArt.AddSprite(go, PixelArt.SparkDot, 40);
                renderer.color = color;
                var spark = go.AddComponent<Spark>();
                spark.Velocity = Random.insideUnitCircle.normalized * Random.Range(speed * 0.4f, speed);
                spark.Life = Random.Range(0.22f, 0.42f);
            }
        }
    }
}
