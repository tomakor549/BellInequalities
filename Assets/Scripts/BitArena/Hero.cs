using System.Collections.Generic;
using UnityEngine;

namespace BitArena
{
    public enum HeroKind
    {
        Alice,
        Bob
    }

    public class Hero : MonoBehaviour
    {
        public HeroKind Kind;
        public int Hp;
        public Rigidbody2D Body;

        BoxCollider2D box;
        Transform visual;
        SpriteRenderer sprite;
        SpriteRenderer ring;
        SpriteRenderer slash;
        float facing = 1f;
        float nextAttack;
        float coyote;
        float jumpBuffer;
        bool jumpHeld;
        bool cutJump;
        float move;
        bool attackDown;
        float squash;
        bool wasGrounded;
        float invulnUntil;
        float rejectUntil;
        float slashUntil;
        float nextDust;
        SpriteRenderer shadow;
        bool dropRequested;
        readonly List<Collider2D> droppingThrough = new List<Collider2D>();

        const float MoveSpeed = 7.7f;
        const float JumpVelocity = 14.2f;

        public bool Attacking => GameSession.I != null && GameSession.I.Attacker == this;

        public static Hero Create(HeroKind kind, Vector3 position)
        {
            var go = new GameObject(kind == HeroKind.Alice ? "Alice" : "Bob");
            go.transform.position = position;
            var hero = go.AddComponent<Hero>();
            hero.Kind = kind;
            hero.Build();
            return hero;
        }

        void Build()
        {
            Hp = Tune.Hearts;
            facing = Kind == HeroKind.Alice ? 1f : -1f;

            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.gravityScale = Tune.Gravity;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.linearDamping = 0f;
            Body.mass = 1f;

            box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.7f, 1.26f);
            box.offset = new Vector2(0f, 0.02f);
            box.sharedMaterial = new PhysicsMaterial2D("HeroSlip") { friction = 0f, bounciness = 0f };

            var vis = new GameObject("Visual");
            vis.transform.SetParent(transform, false);
            visual = vis.transform;
            sprite = PixelArt.AddSprite(vis, Kind == HeroKind.Alice ? PixelArt.Alice : PixelArt.Bob, 20);

            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0f, -0.58f, 0f);
            ring = PixelArt.AddSprite(ringGo, PixelArt.Ring, 5);

            var shade = new GameObject("Shadow");
            shade.transform.SetParent(transform, false);
            shade.transform.localPosition = new Vector3(0f, -0.62f, 0f);
            shadow = PixelArt.AddSprite(shade, PixelArt.Shadow, 4);
            shadow.color = new Color(0f, 0f, 0f, 0.45f);

            var slashGo = new GameObject("Slash");
            slashGo.transform.SetParent(visual, false);
            slashGo.transform.localPosition = new Vector3(0.95f, 0.08f, 0f);
            slash = PixelArt.AddSprite(slashGo, PixelArt.Slash, 24);
            slash.enabled = false;
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnUntil = Mathf.Max(invulnUntil, Time.time + seconds);
        }

        public void TakeDamage(Vector2 source, string cause)
        {
            var session = GameSession.I;
            if (session == null || session.Phase != MatchPhase.Play)
                return;
            if (Time.time < invulnUntil || Hp <= 0)
                return;

            Hp--;
            invulnUntil = Time.time + 1.15f;
            Vector2 away = (Vector2)transform.position - source;
            float sign = Mathf.Abs(away.x) < 0.05f ? facing * -1f : Mathf.Sign(away.x);
            Body.linearVelocity = new Vector2(sign * 8.4f, 9.4f);
            session.OnHeroHurt(this, cause);
        }

        void Update()
        {
            ReadKeys();
            var session = GameSession.I;
            bool playing = session != null && session.Phase == MatchPhase.Play;
            if (!playing)
            {
                move = 0f;
                attackDown = false;
                jumpBuffer = 0f;
                dropRequested = false;
            }
            else if (attackDown)
            {
                TryAttack();
            }

            if (slash != null)
                slash.enabled = Time.time < slashUntil;
            Animate();
        }

        void ReadKeys()
        {
            move = 0f;
            attackDown = false;

            if (Kind == HeroKind.Alice)
            {
                if (GameInput.Held(KeyCode.A)) move -= 1f;
                if (GameInput.Held(KeyCode.D)) move += 1f;
                if (GameInput.Down(KeyCode.W)) jumpBuffer = 0.14f;
                jumpHeld = GameInput.Held(KeyCode.W);
                if (GameInput.Down(KeyCode.S)) dropRequested = true;
                attackDown = GameInput.Down(KeyCode.J) || GameInput.Down(KeyCode.LeftControl);
            }
            else
            {
                if (GameInput.Held(KeyCode.LeftArrow)) move -= 1f;
                if (GameInput.Held(KeyCode.RightArrow)) move += 1f;
                if (GameInput.Down(KeyCode.UpArrow)) jumpBuffer = 0.14f;
                jumpHeld = GameInput.Held(KeyCode.UpArrow);
                if (GameInput.Down(KeyCode.DownArrow)) dropRequested = true;
                attackDown = GameInput.Down(KeyCode.K) || GameInput.Down(KeyCode.RightControl);
            }
        }

        void FixedUpdate()
        {
            var session = GameSession.I;
            bool playing = session != null && session.Phase == MatchPhase.Play;
            if (!playing)
            {
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                return;
            }

            ReleaseClearedPlatforms();
            bool grounded = CheckGround();
            bool dropping = false;
            if (dropRequested)
            {
                dropRequested = false;
                dropping = grounded && TryDropThrough();
            }
            if (dropping)
            {
                grounded = false;
                coyote = 0f;
                jumpBuffer = 0f;
                squash = 0.08f;
                var tint = Kind == HeroKind.Alice ? new Color(1f, 0.45f, 0.2f) : new Color(0.45f, 0.75f, 1f);
                Vfx.Burst(transform.position + Vector3.down * 0.55f, tint, 3, 1.6f);
            }
            else if (grounded)
            {
                coyote = 0.12f;
                if (!wasGrounded)
                    squash = 0.1f;
            }
            else
            {
                coyote -= Time.fixedDeltaTime;
            }
            wasGrounded = grounded;

            if (jumpBuffer > 0f)
                jumpBuffer -= Time.fixedDeltaTime;

            float accel = grounded ? 130f : 75f;
            float vx = Mathf.MoveTowards(Body.linearVelocity.x, move * MoveSpeed, accel * Time.fixedDeltaTime);
            float vy = Body.linearVelocity.y;
            if (jumpBuffer > 0f && coyote > 0f)
            {
                vy = JumpVelocity;
                jumpBuffer = 0f;
                coyote = 0f;
                cutJump = false;
                squash = 0.06f;
                SfxSynth.Jump();
                var tint = Kind == HeroKind.Alice ? new Color(1f, 0.45f, 0.2f) : new Color(0.45f, 0.75f, 1f);
                Vfx.Burst(transform.position + Vector3.down * 0.6f, tint, 4, 2.2f);
            }
            else if (!jumpHeld && vy > 0f && !cutJump)
            {
                vy *= 0.5f;
                cutJump = true;
            }

            if (dropping)
                vy = Mathf.Min(vy, -3.4f);

            Body.linearVelocity = new Vector2(vx, vy);
            if (Mathf.Abs(move) > 0.1f)
                facing = Mathf.Sign(move);
        }

        bool CheckGround()
        {
            if (Body.linearVelocity.y > 0.2f)
                return false;
            Vector2 feet = (Vector2)transform.position + box.offset + Vector2.down * (box.size.y * 0.5f);
            var hits = Physics2D.OverlapCircleAll(feet, 0.12f);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == box || hit.isTrigger || droppingThrough.Contains(hit) || hit.GetComponent<SolidLevel>() == null)
                    continue;
                if (hit.bounds.max.y > feet.y + 0.3f && hit.bounds.min.y < feet.y)
                    continue;
                return true;
            }
            return false;
        }

        bool TryDropThrough()
        {
            Vector2 feet = (Vector2)transform.position + box.offset + Vector2.down * (box.size.y * 0.5f);
            var hits = Physics2D.OverlapCircleAll(feet, 0.2f);
            bool dropped = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || hit == box || hit.isTrigger || droppingThrough.Contains(hit))
                    continue;
                if (hit.GetComponent<PlatformEffector2D>() == null)
                    continue;
                if (hit.bounds.max.y > feet.y + 0.22f)
                    continue;
                Physics2D.IgnoreCollision(box, hit, true);
                droppingThrough.Add(hit);
                dropped = true;
            }
            return dropped;
        }

        void ReleaseClearedPlatforms()
        {
            var body = box.bounds;
            for (int i = droppingThrough.Count - 1; i >= 0; i--)
            {
                var platform = droppingThrough[i];
                if (platform == null || Separated(body, platform.bounds))
                {
                    if (platform != null)
                        Physics2D.IgnoreCollision(box, platform, false);
                    droppingThrough.RemoveAt(i);
                }
            }
        }

        static bool Separated(Bounds body, Bounds platform)
        {
            if (body.max.x < platform.min.x || body.min.x > platform.max.x)
                return true;
            if (body.max.y < platform.min.y - 0.02f || body.min.y > platform.max.y + 0.05f)
                return true;
            return false;
        }

        void OnDisable()
        {
            for (int i = 0; i < droppingThrough.Count; i++)
            {
                if (droppingThrough[i] != null && box != null)
                    Physics2D.IgnoreCollision(box, droppingThrough[i], false);
            }
            droppingThrough.Clear();
        }

        void TryAttack()
        {
            var session = GameSession.I;
            if (session.Attacker != this)
            {
                if (Time.time >= rejectUntil)
                {
                    rejectUntil = Time.time + 0.75f;
                    session.Float(transform.position + Vector3.up * 1.15f, "not now", new Color(1f, 0.82f, 0.35f));
                }
                return;
            }
            if (Time.time < nextAttack)
                return;

            if (Kind == HeroKind.Alice)
            {
                nextAttack = Time.time + 0.36f;
                ArrowShot.Spawn(this, (Vector2)transform.position + new Vector2(facing * 0.78f, 0.2f), facing);
                SfxSynth.Shoot();
            }
            else
            {
                nextAttack = Time.time + 0.48f;
                slashUntil = Time.time + 0.12f;
                Swing();
                SfxSynth.Swing();
            }
        }

        void Swing()
        {
            Vector2 center = (Vector2)transform.position + new Vector2(facing * 0.98f, 0.12f);
            var hits = Physics2D.OverlapBoxAll(center, new Vector2(1.5f, 1.25f), 0f);
            BitMonster nearest = null;
            float nearestDist = 99f;
            for (int i = 0; i < hits.Length; i++)
            {
                var monster = hits[i].GetComponent<BitMonster>();
                if (monster == null || !monster.Alive)
                    continue;
                float dist = Vector2.Distance(center, monster.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = monster;
                }
            }

            if (nearest != null)
                nearest.TakeHit(this);
        }

        void Animate()
        {
            bool grounded = box != null && CheckGround();
            float bob = 0f;
            var session = GameSession.I;
            if (session != null && session.Phase == MatchPhase.Play && grounded && Mathf.Abs(move) > 0.1f)
                bob = Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.055f;

            float sx = 1f;
            float sy = 1f;
            if (!grounded)
            {
                sx = 0.92f;
                sy = 1.08f;
            }
            if (squash > 0f)
            {
                sx = 1.14f;
                sy = 0.84f;
                squash -= Time.deltaTime;
            }

            visual.localScale = new Vector3(facing * sx, sy, 1f);
            visual.localPosition = new Vector3(0f, bob, 0f);
            if (shadow != null)
            {
                float lift = grounded ? 1f : 0.62f;
                shadow.transform.localScale = new Vector3(lift, lift * 0.85f, 1f);
                shadow.color = new Color(0f, 0f, 0f, grounded ? 0.42f : 0.2f);
            }
            if (grounded && Mathf.Abs(move) > 0.2f && Time.time >= nextDust)
            {
                nextDust = Time.time + 0.16f;
                var tint = Kind == HeroKind.Alice ? new Color(1f, 0.55f, 0.35f) : new Color(0.55f, 0.75f, 1f);
                Vfx.Burst(transform.position + Vector3.down * 0.58f, tint, 1, 1.2f);
            }
            bool blink = Time.time < invulnUntil && ((int)(Time.unscaledTime * 16f) & 1) == 0;
            sprite.color = blink ? new Color(1f, 1f, 1f, 0.28f) : Color.white;
            if (ring != null)
                ring.enabled = Attacking;
        }
    }
}
