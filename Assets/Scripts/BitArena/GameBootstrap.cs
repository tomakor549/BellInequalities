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
            session.Alice = Hero.Create(HeroKind.Alice, new Vector3(-7.6f, 1.4f, 0f));
            session.Bob = Hero.Create(HeroKind.Bob, new Vector3(7.6f, 1.4f, 0f));
            session.Charlie = CharlieBoss.Create(new Vector3(0f, 6.55f, 0f));
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
            cam.transform.position = new Vector3(0f, 4.15f, -10f);
            var rig = cam.GetComponent<ArenaCamera>();
            if (rig == null)
                rig = cam.gameObject.AddComponent<ArenaCamera>();
            session.Rig = rig;
        }

        static void BuildArena()
        {
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.position = new Vector3(0f, 4.2f, 0f);
            PixelArt.AddSprite(backdrop, PixelArt.Backdrop, -100);

            var floorTint = new Color32(96, 78, 92, 255);
            Solid(new Vector2(0f, -0.8f), new Vector2(22.4f, 1.6f), floorTint, false, true);
            Solid(new Vector2(-10.9f, 6f), new Vector2(0.7f, 16f), new Color32(42, 32, 52, 255), false, false);
            Solid(new Vector2(10.9f, 6f), new Vector2(0.7f, 16f), new Color32(36, 34, 58, 255), false, false);

            var stone = new Color32(108, 96, 112, 255);
            var ledge = new Color32(132, 118, 96, 255);
            Solid(new Vector2(-3.4f, 2.55f), new Vector2(4.4f, 0.5f), stone, true, false);
            Solid(new Vector2(3.4f, 2.55f), new Vector2(4.4f, 0.5f), stone, true, false);
            Solid(new Vector2(-7.9f, 4.9f), new Vector2(2.8f, 0.48f), stone, true, false);
            Solid(new Vector2(7.9f, 4.9f), new Vector2(2.8f, 0.48f), stone, true, false);
            Solid(new Vector2(0f, 5.62f), new Vector2(2.4f, 0.46f), ledge, true, false);
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
