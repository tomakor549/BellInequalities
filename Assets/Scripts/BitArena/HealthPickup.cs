using UnityEngine;

namespace BitArena
{
    public class HealthPickup : MonoBehaviour
    {
        public static HealthPickup Instance { get; private set; }

        struct PlatformSpawn
        {
            public float MinX;
            public float MaxX;
            public float Y;

            public PlatformSpawn(float minX, float maxX, float y)
            {
                MinX = minX;
                MaxX = maxX;
                Y = y;
            }
        }

        static readonly PlatformSpawn[] Spawns =
        {
            // Floor
            new PlatformSpawn(-13f, -5f, 0.65f),
            new PlatformSpawn(-4f, 4f, 0.65f),
            new PlatformSpawn(5f, 13f, 0.65f),
            // Low platforms
            new PlatformSpawn(-13.5f, -8.5f, 2.95f),
            new PlatformSpawn(-1.5f, 1.8f, 3.2f),
            new PlatformSpawn(8.5f, 13.5f, 2.85f),
            // Mid platforms
            new PlatformSpawn(-14f, -11f, 5.35f),
            new PlatformSpawn(-6f, -3.5f, 5.1f),
            new PlatformSpawn(-1.5f, 1.5f, 5.95f),
            new PlatformSpawn(4f, 6.5f, 5.5f),
            new PlatformSpawn(11.5f, 14f, 5.65f),
            // High platforms
            new PlatformSpawn(-14f, -11f, 7.7f),
            new PlatformSpawn(-6f, -3.5f, 7.55f),
            new PlatformSpawn(2.5f, 6f, 7.95f),
            new PlatformSpawn(11f, 14f, 8.1f),
            // Crown platforms
            new PlatformSpawn(-13f, -9f, 10.05f),
            new PlatformSpawn(-5f, -1f, 9.9f),
            new PlatformSpawn(1f, 3.5f, 10.45f),
            new PlatformSpawn(5.5f, 7.5f, 10.95f),
            new PlatformSpawn(10f, 13.5f, 10.25f),
            // Spire platform
            new PlatformSpawn(-2f, 2f, 12.65f)
        };

        Transform visual;
        SpriteRenderer sprite;
        SpriteRenderer shadow;
        CircleCollider2D trigger;
        float moveTimer = 5f;
        float sparkleTimer;
        int lastSpawnIndex = -1;
        bool activeInMatch;

        public static HealthPickup Create()
        {
            var go = new GameObject("HealthPickup");
            var pickup = go.AddComponent<HealthPickup>();
            pickup.Build();
            return pickup;
        }

        void Awake()
        {
            Instance = this;
        }

        void Build()
        {
            var vis = new GameObject("Visual");
            vis.transform.SetParent(transform, false);
            visual = vis.transform;
            sprite = PixelArt.AddSprite(vis, PixelArt.HeartPickup, 18);

            var shade = new GameObject("Shadow");
            shade.transform.SetParent(transform, false);
            shade.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            shadow = PixelArt.AddSprite(shade, PixelArt.Shadow, 3);
            shadow.color = new Color(0f, 0f, 0f, 0.35f);

            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            trigger = gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.65f;

            SetVisible(false);
        }

        public void ResetForMatch()
        {
            moveTimer = 5f;
            lastSpawnIndex = -1;
            Relocate(false, false);
            SetVisible(true);
        }

        public void SetVisible(bool visible)
        {
            activeInMatch = visible;
            if (sprite != null) sprite.enabled = visible;
            if (shadow != null) shadow.enabled = visible;
            if (trigger != null) trigger.enabled = visible;
        }

        void Update()
        {
            var session = GameSession.I;
            bool playing = session != null && session.Phase == MatchPhase.Play;
            if (!playing)
            {
                if (activeInMatch)
                    SetVisible(false);
                return;
            }

            if (!activeInMatch)
            {
                ResetForMatch();
            }

            moveTimer -= Time.deltaTime;
            if (moveTimer <= 0f)
            {
                Relocate(false, true);
                moveTimer = 5f;
            }

            if (visual != null)
            {
                float bob = Mathf.Sin(Time.time * 3.5f) * 0.12f;
                visual.localPosition = new Vector3(0f, bob, 0f);
                float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
                visual.localScale = new Vector3(pulse, pulse, 1f);
            }

            sparkleTimer -= Time.deltaTime;
            if (sparkleTimer <= 0f)
            {
                sparkleTimer = Random.Range(0.45f, 0.85f);
                var sparkColor = new Color(1f, 0.45f, 0.65f, 0.8f);
                Vfx.Burst(transform.position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.2f, 0.25f), 0f),
                    sparkColor, 1, 0.8f);
            }

            CheckProximity(session.Alice);
            CheckProximity(session.Bob);
        }

        void CheckProximity(Hero hero)
        {
            if (hero == null || !activeInMatch)
                return;
            float dist = Vector2.Distance(transform.position, hero.transform.position);
            if (dist < 0.95f)
            {
                Collect(hero);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!activeInMatch)
                return;
            var hero = other.GetComponent<Hero>();
            if (hero != null)
            {
                Collect(hero);
            }
        }

        void Collect(Hero hero)
        {
            var session = GameSession.I;
            if (session == null || session.Phase != MatchPhase.Play)
                return;

            bool healed = hero.Hp < Tune.Hearts;
            if (healed)
            {
                hero.Hp++;
                session.Float(hero.transform.position + Vector3.up * 1.15f, "+1 HP", new Color(0.3f, 1f, 0.45f));
            }
            else
            {
                session.Float(hero.transform.position + Vector3.up * 1.15f, "+1 HP (Max)", new Color(1f, 0.85f, 0.4f));
            }

            SfxSynth.Heal();
            Vfx.Burst(transform.position, new Color(1f, 0.35f, 0.55f), 14, 4.5f);
            Vfx.Burst(hero.transform.position, new Color(0.4f, 1f, 0.6f), 8, 3.2f);

            Relocate(true, true);
            moveTimer = 5f;
        }

        void Relocate(bool wasCollected, bool playEffects)
        {
            if (playEffects && !wasCollected)
            {
                Vfx.Burst(transform.position, new Color(1f, 0.5f, 0.7f), 8, 2.5f);
                SfxSynth.ItemTeleport();
            }

            int spawnIndex = Random.Range(0, Spawns.Length);
            if (spawnIndex == lastSpawnIndex && Spawns.Length > 1)
            {
                spawnIndex = (spawnIndex + Random.Range(1, Spawns.Length)) % Spawns.Length;
            }
            lastSpawnIndex = spawnIndex;

            var spawn = Spawns[spawnIndex];
            float x = Random.Range(spawn.MinX, spawn.MaxX);
            float y = spawn.Y;
            transform.position = new Vector3(x, y, 0f);

            if (playEffects)
            {
                Vfx.Burst(transform.position, new Color(1f, 0.85f, 0.4f), 8, 2.8f);
            }
        }
    }
}

