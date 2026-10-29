using UnityEngine;

namespace BitArena
{
    public class ArrowShot : MonoBehaviour
    {
        Hero owner;
        Rigidbody2D body;
        Vector2 velocity;
        float dieAt;
        float trail;

        public static void Spawn(Hero owner, Vector2 position, float direction)
        {
            var go = new GameObject("Arrow");
            go.transform.position = position;
            go.transform.localScale = new Vector3(Mathf.Sign(direction), 1f, 1f);
            PixelArt.AddSprite(go, PixelArt.Arrow, 23);
            var arrow = go.AddComponent<ArrowShot>();
            arrow.owner = owner;
            arrow.velocity = new Vector2(Mathf.Sign(direction) * 16.5f, 0f);
            arrow.dieAt = Time.time + 1.35f;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            arrow.body = rb;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.72f, 0.24f);
        }

        void FixedUpdate()
        {
            if (body == null || Time.time >= dieAt)
            {
                Destroy(gameObject);
                return;
            }
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
        }

        void Update()
        {
            trail -= Time.deltaTime;
            if (trail > 0f)
                return;
            trail = 0.035f;
            Vfx.Burst(transform.position, new Color(1f, 0.78f, 0.35f), 1, 0.7f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Hero>() != null)
                return;
            var monster = other.GetComponent<BitMonster>();
            if (monster != null)
            {
                monster.TakeHit(owner);
                Destroy(gameObject);
                return;
            }
            if (other.GetComponent<SolidLevel>() != null)
                Destroy(gameObject);
        }
    }
}
