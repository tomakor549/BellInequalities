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
            HealthPickup.Create();
            session.Alice = Hero.Create(HeroKind.Alice, new Vector3(-5.2f, 1.2f, 0f));
            session.Bob = Hero.Create(HeroKind.Bob, new Vector3(5.2f, 1.2f, 0f));
            session.Charlie = CharlieBoss.Create(new Vector3(0f, 12.85f, 0f));
            session.ShowMenu();
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
    }
}
