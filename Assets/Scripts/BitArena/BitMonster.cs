using UnityEngine;

namespace BitArena
{
    public class BitMonster : MonoBehaviour
    {
        public BitType Type;
        public bool Alive = true;
        public Rigidbody2D Body;
        public Collider2D Col;

        Transform visual;
        SpriteRenderer sprite;
        SpriteRenderer marker;
        float facing = 1f;
        float nextJump;

        public static BitMonster Spawn(BitType type, Vector3 position)
        {
            var go = new GameObject(type == BitType.Zero ? "Bit0" : "Bit1");
            go.transform.position = position;
            var monster = go.AddComponent<BitMonster>();
            monster.Type = type;
            monster.Build();
            return monster;
        }

        void Build()
        {
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.gravityScale = Tune.Gravity;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.mass = Type == BitType.Zero ? 1.45f : 0.72f;
            Body.linearDamping = 0.05f;

            var material = new PhysicsMaterial2D("BitSlip") { friction = 0f, bounciness = 0f };
            if (Type == BitType.Zero)
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = 0.64f;
                circle.offset = new Vector2(0f, 0.02f);
                circle.sharedMaterial = material;
                Col = circle;
            }
            else
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.5f, 1.48f);
                box.offset = new Vector2(0.02f, 0.04f);
                box.sharedMaterial = material;
                Col = box;
            }

            var vis = new GameObject("Visual");
            vis.transform.SetParent(transform, false);
            visual = vis.transform;
            sprite = PixelArt.AddSprite(vis, Type == BitType.Zero ? PixelArt.Monster0 : PixelArt.Monster1, 12);

            var mark = new GameObject("Mark");
            mark.transform.SetParent(transform, false);
            mark.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            marker = PixelArt.AddSprite(mark, PixelArt.Chevron, 26);
            marker.enabled = false;
        }

        public void TakeHit(Hero attacker)
        {
            if (!Alive || attacker == null)
                return;
            var session = GameSession.I;
            if (session == null || session.Phase != MatchPhase.Play)
                return;
            if (attacker != session.Attacker || session.SwapLocked)
                return;

            Alive = false;
            session.RecordKill(this);
            Destroy(gameObject);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (!Alive)
                return;
            var hero = collision.collider.GetComponent<Hero>();
            var session = GameSession.I;
            if (hero == null || session == null || session.Phase != MatchPhase.Play)
                return;
            hero.TakeDamage(transform.position, "monster");
        }

        void FixedUpdate()
        {
            var session = GameSession.I;
            Hero quarry = session != null ? NearestHero(session) : null;
            if (Body == null || session == null || session.Phase != MatchPhase.Play || quarry == null)
            {
                if (Body != null)
                    Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                return;
            }

            if (transform.position.y < -4f)
            {
                transform.position = new Vector3(0f, 8.2f, 0f);
                Body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 target = quarry.transform.position;
            float dx = target.x - transform.position.x;
            float dir = Mathf.Abs(dx) < 0.18f ? 0f : Mathf.Sign(dx);
            if (dir != 0f)
                facing = dir;

            bool grounded = IsGrounded();
            bool wall = dir != 0f && HitsSolid((Vector2)transform.position + new Vector2(dir * 0.42f, 0.3f), Vector2.right * dir, 0.2f);
            float dy = target.y - transform.position.y;
            if (grounded && Time.time >= nextJump && (wall || dy > 1.25f))
            {
                float jump = Type == BitType.One ? 14f : 9.5f;
                Body.linearVelocity = new Vector2(Body.linearVelocity.x, jump);
                nextJump = Time.time + (Type == BitType.One ? 0.82f : 1.35f);
            }

            float speed = Type == BitType.Zero ? 2.9f : 4.25f;
            float vx = dir * speed;
            vx += Separation();
            Body.linearVelocity = new Vector2(vx, Body.linearVelocity.y);
        }

        Hero NearestHero(GameSession session)
        {
            Hero best = null;
            float bestDist = float.MaxValue;
            Consider(session.Alice, ref best, ref bestDist);
            Consider(session.Bob, ref best, ref bestDist);
            return best;
        }

        void Consider(Hero hero, ref Hero best, ref float bestDist)
        {
            if (hero == null || hero.Hp <= 0)
                return;
            float dx = hero.transform.position.x - transform.position.x;
            float dy = hero.transform.position.y - transform.position.y;
            float dist = dx * dx + dy * dy;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hero;
            }
        }

        float Separation()
        {
            var session = GameSession.I;
            if (session == null)
                return 0f;
            float push = 0f;
            for (int i = 0; i < session.Monsters.Count; i++)
            {
                var other = session.Monsters[i];
                if (other == null || other == this || !other.Alive)
                    continue;
                float dx = transform.position.x - other.transform.position.x;
                float dist = Mathf.Abs(dx);
                if (dist < 0.01f)
                    push += 0.8f;
                else if (dist < 0.85f)
                    push += Mathf.Sign(dx) * (0.85f - dist) * 3.5f;
            }
            return push;
        }

        bool IsGrounded()
        {
            if (Body.linearVelocity.y > 0.3f)
                return false;
            Vector2 origin = Col != null ? (Vector2)Col.bounds.center : (Vector2)transform.position;
            float reach = Col != null ? Col.bounds.extents.y + 0.1f : 0.8f;
            return HitsSolid(origin, Vector2.down, reach);
        }

        bool HitsSolid(Vector2 origin, Vector2 direction, float distance)
        {
            var hits = Physics2D.RaycastAll(origin, direction, distance);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == Col || hit.collider.isTrigger)
                    continue;
                if (hit.collider.GetComponent<SolidLevel>() != null)
                    return true;
            }
            return false;
        }

        void Update()
        {
            if (visual != null)
                visual.localScale = new Vector3(facing, 1f, 1f);
            if (marker != null)
                marker.enabled = false;
            if (sprite != null)
                sprite.color = Color.white;
        }

        void OnDestroy()
        {
            if (GameSession.I != null)
                GameSession.I.Monsters.Remove(this);
        }
    }
}
