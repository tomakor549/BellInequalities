using UnityEngine;

namespace BitArena
{
    public class BitMonster : MonoBehaviour
    {
        public BitType Type;
        public bool Alive = true;
        public Rigidbody2D Body;
        public Collider2D Col;

        struct Step
        {
            public float MinX;
            public float MaxX;
            public float Top;

            public Step(float minX, float maxX, float top)
            {
                MinX = minX;
                MaxX = maxX;
                Top = top;
            }

            public bool ContainsX(float x, float pad)
            {
                return x >= MinX - pad && x <= MaxX + pad;
            }

            public float NearestX(float x)
            {
                return Mathf.Clamp(x, MinX, MaxX);
            }
        }

        static int nextSlot;

        static readonly Step[] Steps =
        {
            new Step(-14.5f, -7.8f, 2.47f),
            new Step(-2.2f, 2.5f, 2.71f),
            new Step(7.9f, 14.4f, 2.35f),
            new Step(-14.8f, -10.3f, 4.87f),
            new Step(-6.6f, -3.1f, 4.61f),
            new Step(-1.9f, 2.1f, 5.48f),
            new Step(3.3f, 7.4f, 5.01f),
            new Step(10.6f, 14.9f, 5.17f),
            new Step(-14.7f, -10.4f, 7.21f),
            new Step(-6.7f, -2.4f, 7.07f),
            new Step(1.85f, 6.45f, 7.47f),
            new Step(10.2f, 14.7f, 7.61f),
            new Step(-13.7f, -8.6f, 9.54f),
            new Step(-5.7f, -0.6f, 9.39f),
            new Step(0.4f, 3.9f, 9.94f),
            new Step(5.15f, 7.75f, 10.43f),
            new Step(9.2f, 14.2f, 9.77f),
            new Step(-2.6f, 2.6f, 12.14f)
        };

        Transform visual;
        SpriteRenderer sprite;
        SpriteRenderer shadow;
        float facing = 1f;
        float nextJump;
        float windupUntil;
        float lungeUntil;
        float lungeReady;
        float lungeDir = 1f;
        float nextDust;
        bool groundedVisual;
        bool wasGrounded;
        int slot;
        float dropUntil;
        float dropX;
        int home = -1;

        float JumpHeight => Type == BitType.One ? 3.7f : 3.35f;
        float JumpSpeed => Type == BitType.One ? 15.1f : 14.5f;

        public static BitMonster Spawn(BitType type, Vector3 position)
        {
            var go = new GameObject(type == BitType.Zero ? "Bit0" : "Bit1");
            go.transform.position = position;
            var monster = go.AddComponent<BitMonster>();
            monster.Type = type;
            monster.slot = nextSlot++;
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

            var shade = new GameObject("Shadow");
            shade.transform.SetParent(transform, false);
            shade.transform.localPosition = new Vector3(0f, Type == BitType.Zero ? -0.62f : -0.72f, 0f);
            shadow = PixelArt.AddSprite(shade, PixelArt.Shadow, 4);
            shadow.color = new Color(0f, 0f, 0f, 0.4f);
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
                transform.position = new Vector3(0f, 14.5f, 0f);
                Body.linearVelocity = Vector2.zero;
                return;
            }

            float feet = Col != null ? Col.bounds.min.y : transform.position.y;
            bool grounded = IsGrounded();
            groundedVisual = grounded;
            if (grounded)
                home = SurfaceIndex(feet, transform.position.x);
            Plan(quarry, feet, out float goalX, out bool wantJump);
            Vector2 aim = quarry.transform.position;

            float dx = goalX - transform.position.x;
            float dir = Mathf.Abs(dx) < 0.12f ? 0f : Mathf.Sign(dx);
            ThinkLunge(aim, grounded, wantJump);
            bool winding = Time.time < windupUntil;
            bool lunging = Time.time < lungeUntil;
            if (lunging)
                dir = lungeDir;
            if (dir != 0f)
                facing = dir;

            if (grounded && wantJump && !winding && Time.time >= nextJump)
            {
                Body.linearVelocity = new Vector2(Body.linearVelocity.x, JumpSpeed);
                nextJump = Time.time + (Type == BitType.One ? 0.48f : 0.62f);
            }

            float speed = Type == BitType.Zero ? 3.25f : 4.7f;
            if (winding)
                speed *= 0.32f;
            else if (lunging)
                speed *= Type == BitType.One ? 1.72f : 1.38f;
            bool leaving = Time.time < dropUntil;
            float vx = dir * speed + (leaving ? 0f : Separation());
            float cap = speed * 1.25f;
            Body.linearVelocity = new Vector2(Mathf.Clamp(vx, -cap, cap), Body.linearVelocity.y);
        }

        void Plan(Hero quarry, float feet, out float goalX, out bool wantJump)
        {
            float heroX = Mathf.Clamp(quarry.transform.position.x, -15.5f, 15.5f);
            if (quarry.Body != null)
                heroX = Mathf.Clamp(heroX + quarry.Body.linearVelocity.x * 0.2f, -15.5f, 15.5f);
            float heroFeet = quarry.transform.position.y - 0.55f;
            int me = home;
            int them = SurfaceIndex(heroFeet, quarry.transform.position.x);
            float myTop = me < 0 ? 0f : Steps[me].Top;
            float theirTop = them < 0 ? 0f : Steps[them].Top;
            wantJump = false;
            goalX = heroX;

            if (theirTop > myTop + 0.45f)
            {
                int next = NextHop(me, them, heroX);
                if (next >= 0)
                {
                    dropUntil = 0f;
                    Step hop = Steps[next];
                    float gap = GapToSpan(transform.position.x, hop);
                    goalX = gap > 0.25f
                        ? (transform.position.x < hop.MinX ? hop.MinX + 0.4f : hop.MaxX - 0.4f)
                        : hop.NearestX(heroX);
                    float climb = hop.Top - feet;
                    wantJump = gap < 1.65f && climb > 0.4f && climb <= JumpHeight;
                    return;
                }
            }

            if (me >= 0 && feet > heroFeet + 1.05f && Time.time >= dropUntil)
            {
                Step ledge = Steps[me];
                bool goLeft = heroX < transform.position.x - 0.15f
                    || (Mathf.Abs(heroX - transform.position.x) <= 0.15f && (slot & 1) == 0);
                float exit = goLeft ? ledge.MinX - 1.25f : ledge.MaxX + 1.25f;
                dropX = Mathf.Clamp(exit, -15.5f, 15.5f);
                dropUntil = Time.time + 1.45f;
            }

            if (Time.time < dropUntil && feet > heroFeet + 0.45f)
            {
                goalX = dropX;
                return;
            }

            dropUntil = 0f;
            if (Mathf.Abs(heroX - transform.position.x) > 1.7f)
            {
                float flank = ((slot & 1) == 0 ? -1f : 1f) * 0.8f;
                goalX = Mathf.Clamp(heroX + flank, -15.5f, 15.5f);
            }
        }

        int NextHop(int from, int target, float heroX)
        {
            if (CanHop(from, target))
                return target;
            int best = -1;
            float bestCost = float.MaxValue;
            float targetTop = Steps[target].Top;
            for (int i = 0; i < Steps.Length; i++)
            {
                if (!CanHop(from, i) || Steps[i].Top > targetTop + 0.05f)
                    continue;
                float mid = (Steps[i].MinX + Steps[i].MaxX) * 0.5f;
                float cost = Mathf.Abs(Steps[i].NearestX(heroX) - heroX) + Mathf.Abs(mid - transform.position.x) * 0.45f;
                if (cost >= bestCost)
                    continue;
                bestCost = cost;
                best = i;
            }
            return best;
        }

        bool CanHop(int from, int to)
        {
            if (to < 0 || from == to)
                return false;
            float fromTop = from < 0 ? 0f : Steps[from].Top;
            float climb = Steps[to].Top - fromTop;
            if (climb < 0.4f || climb > JumpHeight)
                return false;
            if (from < 0)
                return true;
            return SpanGap(Steps[from], Steps[to]) <= 1.5f;
        }

        static float SpanGap(Step left, Step right)
        {
            if (left.MaxX >= right.MinX && right.MaxX >= left.MinX)
                return 0f;
            if (left.MaxX < right.MinX)
                return right.MinX - left.MaxX;
            return left.MinX - right.MaxX;
        }

        static float GapToSpan(float x, Step hop)
        {
            if (x < hop.MinX)
                return hop.MinX - x;
            if (x > hop.MaxX)
                return x - hop.MaxX;
            return 0f;
        }

        int SurfaceIndex(float feet, float x)
        {
            int best = -1;
            float bestDy = 0.48f;
            for (int i = 0; i < Steps.Length; i++)
            {
                float dy = Mathf.Abs(feet - Steps[i].Top);
                if (dy >= bestDy || !Steps[i].ContainsX(x, 0.2f))
                    continue;
                bestDy = dy;
                best = i;
            }
            return best;
        }

        void ThinkLunge(Vector2 aim, bool grounded, bool wantJump)
        {
            bool sameBand = Mathf.Abs(aim.y - transform.position.y) < 0.95f;
            float adx = aim.x - transform.position.x;
            float abs = Mathf.Abs(adx);
            bool ready = grounded && !wantJump && sameBand && abs > 0.55f && abs < 2.9f && Time.time >= lungeReady;
            bool winding = Time.time < windupUntil;
            bool lunging = Time.time < lungeUntil;
            if (ready && !winding && !lunging && (windupUntil <= 0f || Time.time > windupUntil + 0.08f))
                windupUntil = Time.time + (Type == BitType.One ? 0.16f : 0.26f);
            if (!lunging && windupUntil > 0f && Time.time >= windupUntil && Time.time <= windupUntil + 0.08f)
            {
                lungeDir = Mathf.Abs(adx) < 0.05f ? facing : Mathf.Sign(adx);
                lungeUntil = Time.time + (Type == BitType.One ? 0.32f : 0.24f);
                lungeReady = lungeUntil + (Type == BitType.One ? 0.9f : 1.35f);
            }
        }

        Hero NearestHero(GameSession session)
        {
            Hero alice = session.Alice;
            Hero bob = session.Bob;
            bool aliceOk = alice != null && alice.Hp > 0;
            bool bobOk = bob != null && bob.Hp > 0;
            if (!aliceOk)
                return bobOk ? bob : null;
            if (!bobOk)
                return alice;
            float aliceScore = QuarryScore(session, alice, bob);
            float bobScore = QuarryScore(session, bob, alice);
            if (Mathf.Abs(aliceScore - bobScore) < 0.4f)
                return (slot & 1) == 0 ? alice : bob;
            return aliceScore < bobScore ? alice : bob;
        }

        float QuarryScore(GameSession session, Hero hero, Hero other)
        {
            float dx = hero.transform.position.x - transform.position.x;
            float dy = hero.transform.position.y - transform.position.y;
            float score = Mathf.Sqrt(dx * dx + dy * dy);
            if (hero == session.Runner)
                score -= 1.7f;
            int crowd = 0;
            for (int i = 0; i < session.Monsters.Count; i++)
            {
                var mate = session.Monsters[i];
                if (mate == null || mate == this || !mate.Alive)
                    continue;
                float toHero = (hero.transform.position - mate.transform.position).sqrMagnitude;
                float toOther = other == null ? float.MaxValue : (other.transform.position - mate.transform.position).sqrMagnitude;
                if (toHero < toOther)
                    crowd++;
            }
            score += crowd * 1.7f;
            return score;
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
            bool winding = Time.time < windupUntil && Time.time >= lungeUntil;
            bool lunging = Time.time < lungeUntil;
            float sx = lunging ? 0.9f : winding ? 1.14f : 1f;
            float sy = lunging ? 1.14f : winding ? 0.84f : 1f;
            if (visual != null)
                visual.localScale = new Vector3(facing * sx, sy, 1f);
            if (sprite != null)
            {
                if (lunging)
                    sprite.color = new Color(1f, 0.82f, 0.82f);
                else if (winding)
                    sprite.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.time * 42f));
                else
                    sprite.color = Color.white;
            }
            if (shadow != null)
            {
                float lift = groundedVisual ? 1f : 0.58f;
                shadow.transform.localScale = new Vector3(lift, lift * 0.85f, 1f);
                shadow.color = new Color(0f, 0f, 0f, groundedVisual ? 0.4f : 0.18f);
            }
            if (groundedVisual && !wasGrounded)
                Vfx.Burst(transform.position + Vector3.down * 0.55f, new Color(0.85f, 0.8f, 0.9f), 3, 1.6f);
            else if (groundedVisual && lunging && Time.time >= nextDust)
            {
                nextDust = Time.time + 0.08f;
                Vfx.Burst(transform.position + Vector3.down * 0.55f, new Color(1f, 0.55f, 0.4f), 1, 1.4f);
            }
            wasGrounded = groundedVisual;
        }

        void OnDestroy()
        {
            if (GameSession.I != null)
                GameSession.I.Monsters.Remove(this);
        }
    }
}
