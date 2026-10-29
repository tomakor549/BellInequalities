using UnityEngine;

namespace BitArena
{
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<GameSession>() != null)
                return;
            var root = new GameObject("BitArena");
            root.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            GameInput.Install();
            var session = gameObject.AddComponent<GameSession>();
            gameObject.AddComponent<Hud>();
            PixelArt.BuildAll();
            SfxSynth.Init(gameObject);
            SetupCamera(session);
            BuildArena();
            session.Alice = Hero.Create(HeroKind.Alice, new Vector3(-5.2f, 1.2f, 0f));
            session.Bob = Hero.Create(HeroKind.Bob, new Vector3(5.2f, 1.2f, 0f));
            session.Charlie = CharlieBoss.Create(new Vector3(0f, 12.85f, 0f));
            session.ConfigureTitle();
        }

        void LateUpdate()
        {
            GameInput.EndFrame();
        }

        void OnDestroy()
        {
            GameInput.Shutdown();
        }

        static void SetupCamera(GameSession session)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.03f, 0.09f);
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 50f;
            cam.transform.position = new Vector3(0f, 7.4f, -10f);
            var rig = cam.GetComponent<ArenaCamera>();
            if (rig == null)
                rig = cam.gameObject.AddComponent<ArenaCamera>();
            session.Rig = rig;
        }

        static void BuildArena()
        {
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.position = new Vector3(0f, 7.2f, 0f);
            backdrop.transform.localScale = new Vector3(1.48f, 1.85f, 1f);
            PixelArt.AddSprite(backdrop, PixelArt.Backdrop, -100);

            var floorTint = new Color32(96, 78, 92, 255);
            Solid(new Vector2(0f, -0.8f), new Vector2(33.6f, 1.6f), floorTint, false, true);
            Solid(new Vector2(-16.55f, 9f), new Vector2(0.8f, 24f), new Color32(42, 32, 52, 255), false, false);
            Solid(new Vector2(16.55f, 9f), new Vector2(0.8f, 24f), new Color32(36, 34, 58, 255), false, false);

            var low = new Color32(118, 102, 116, 255);
            var mid = new Color32(96, 108, 126, 255);
            var high = new Color32(118, 100, 138, 255);
            var crown = new Color32(150, 126, 86, 255);
            var nest = new Color32(166, 112, 78, 255);
            var spire = new Color32(196, 164, 92, 255);

            Solid(new Vector2(-11.15f, 2.22f), new Vector2(6.7f, 0.5f), low, true, false);
            Solid(new Vector2(0.15f, 2.46f), new Vector2(4.7f, 0.5f), low, true, false);
            Solid(new Vector2(11.15f, 2.1f), new Vector2(6.5f, 0.5f), low, true, false);

            Solid(new Vector2(-12.55f, 4.64f), new Vector2(4.5f, 0.46f), mid, true, false);
            Solid(new Vector2(-4.85f, 4.38f), new Vector2(3.5f, 0.46f), mid, true, false);
            Solid(new Vector2(0.1f, 5.25f), new Vector2(4f, 0.46f), mid, true, false);
            Solid(new Vector2(5.35f, 4.78f), new Vector2(4.1f, 0.46f), mid, true, false);
            Solid(new Vector2(12.75f, 4.94f), new Vector2(4.3f, 0.46f), mid, true, false);

            Solid(new Vector2(-12.55f, 6.98f), new Vector2(4.3f, 0.46f), high, true, false);
            Solid(new Vector2(-4.55f, 6.84f), new Vector2(4.3f, 0.46f), high, true, false);
            Solid(new Vector2(4.15f, 7.24f), new Vector2(4.6f, 0.46f), high, true, false);
            Solid(new Vector2(12.45f, 7.38f), new Vector2(4.5f, 0.46f), high, true, false);

            Solid(new Vector2(-11.15f, 9.32f), new Vector2(5.1f, 0.44f), crown, true, false);
            Solid(new Vector2(-3.15f, 9.17f), new Vector2(5.1f, 0.44f), crown, true, false);
            Solid(new Vector2(2.15f, 9.72f), new Vector2(3.5f, 0.44f), crown, true, false);
            Solid(new Vector2(6.45f, 10.22f), new Vector2(2.6f, 0.42f), nest, true, false);
            Solid(new Vector2(11.7f, 9.55f), new Vector2(5f, 0.44f), crown, true, false);

            Solid(new Vector2(0f, 11.92f), new Vector2(5.2f, 0.44f), spire, true, false);
        }

        static void Solid(Vector2 center, Vector2 size, Color32 tint, bool oneWay, bool floor)
        {
            var go = new GameObject(oneWay ? "Platform" : "Solid");
            go.transform.position = new Vector3(center.x, center.y, 0f);
            var sprite = floor ? PixelArt.Floor(size.x, size.y) : PixelArt.Stone(size, tint);
            PixelArt.AddSprite(go, sprite, floor ? 0 : 1);
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.sharedMaterial = new PhysicsMaterial2D("Stone") { friction = 0.05f, bounciness = 0f };
            go.AddComponent<SolidLevel>();
            if (!oneWay)
                return;

            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = false;
            effector.surfaceArc = 170f;
            effector.sideArc = 12f;
        }
    }
}
