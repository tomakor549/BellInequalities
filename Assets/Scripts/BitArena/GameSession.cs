using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BitArena
{
    public enum MatchPhase
    {
        Title,
        Play,
        Paused,
        Won,
        Lost
    }

    public enum BitType
    {
        Zero,
        One
    }

    public static class Tune
    {
        public const float Gravity = 3.1f;
        public const int Hearts = 4;
        public const int KillsToWin = 8;
    }

    public struct Floater
    {
        public Vector3 Pos;
        public string Text;
        public Color Color;
        public float Born;
    }

    public class GameSession : MonoBehaviour
    {
        public static GameSession I { get; private set; }

        public MatchPhase Phase = MatchPhase.Title;
        public BitType Required = BitType.One;
        public Hero Alice;
        public Hero Bob;
        public Hero Attacker;
        public Hero Runner;
        public CharlieBoss Charlie;
        public ArenaCamera Rig;
        public int Kills;
        public string Objective = "";
        public string Subtitle = "";
        public string CharlieLine = "";
        public string BigCallout = "";
        public float CalloutUntil;
        public string LostCause = "";
        public Hero LostHero;
        public readonly List<BitMonster> Monsters = new List<BitMonster>();
        public readonly List<Floater> Floaters = new List<Floater>();

        float spawnIn = 2f;
        float swapUnlockAt;

        public bool SwapLocked => Time.time < swapUnlockAt;

        void Awake()
        {
            I = this;
        }

        public void ConfigureTitle()
        {
            Phase = MatchPhase.Title;
            Time.timeScale = 1f;
            Kills = 0;
            CharlieLine = "I am Charlie. Zeros and ones are on the way.";
            if (Alice != null) Alice.Hp = Tune.Hearts;
            if (Bob != null) Bob.Hp = Tune.Hearts;
        }

        public void PollMenu()
        {
            bool start = GameInput.Down(KeyCode.Return)
                || GameInput.Down(KeyCode.KeypadEnter)
                || GameInput.Down(KeyCode.Space)
                || (Phase == MatchPhase.Title && GameInput.Clicked);
            bool restart = GameInput.Down(KeyCode.R);
            bool pause = GameInput.Down(KeyCode.Escape);

            if (Phase == MatchPhase.Title && start)
                Begin();
            else if ((Phase == MatchPhase.Won || Phase == MatchPhase.Lost) && (start || restart))
                Restart();
            else if (Phase == MatchPhase.Play && restart)
                Restart();
            else if (pause && Phase == MatchPhase.Play)
            {
                Phase = MatchPhase.Paused;
                Time.timeScale = 0f;
            }
            else if (pause && Phase == MatchPhase.Paused)
            {
                Phase = MatchPhase.Play;
                Time.timeScale = 1f;
            }
        }

        public void Begin()
        {
            Phase = MatchPhase.Play;
            Time.timeScale = 1f;
            Kills = 0;
            Required = Random.value < 0.5f ? BitType.Zero : BitType.One;
            Attacker = Alice;
            Runner = Bob;
            Alice.Hp = Tune.Hearts;
            Bob.Hp = Tune.Hearts;
            Alice.GrantInvulnerability(1.3f);
            Bob.GrantInvulnerability(1.3f);
            spawnIn = 0.65f;
            LostHero = null;
            LostCause = "";
            Announce(true);
            TintGate();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void Update()
        {
            PollMenu();
            Floaters.RemoveAll(item => Time.time - item.Born > 0.95f);
            if (Phase != MatchPhase.Play)
                return;

            int alive = 0;
            bool hasRequired = false;
            for (int i = Monsters.Count - 1; i >= 0; i--)
            {
                var monster = Monsters[i];
                if (monster == null || !monster.Alive)
                {
                    Monsters.RemoveAt(i);
                    continue;
                }
                alive++;
                if (monster.Type == Required)
                    hasRequired = true;
            }

            spawnIn -= Time.deltaTime;
            int cap = Mathf.Min(6, 4 + Kills / 2);
            if (spawnIn > 0f || alive >= cap)
                return;

            BitType type = !hasRequired || Random.value < 0.58f ? Required : Opposite(Required);
            var spawned = BitMonster.Spawn(type, new Vector3(Random.Range(-0.45f, 0.45f), 8.35f, 0f));
            spawned.Body.linearVelocity = new Vector2(Random.Range(-3.5f, 3.5f), 0.3f);
            Remember(spawned);
            if (Charlie != null)
                Charlie.Cast();
            TintGate();
            float rush = Mathf.Clamp(Kills * 0.15f, 0f, 1.3f);
            spawnIn = Mathf.Max(1.02f, 2.35f - rush);
        }

        public void Remember(BitMonster monster)
        {
            if (!Monsters.Contains(monster))
                Monsters.Add(monster);
            for (int i = 0; i < Monsters.Count; i++)
            {
                var other = Monsters[i];
                if (other == null || other == monster || other.Col == null || monster.Col == null)
                    continue;
                Physics2D.IgnoreCollision(monster.Col, other.Col);
            }
        }

        public void CorrectKill(BitMonster monster)
        {
            if (Phase != MatchPhase.Play || SwapLocked)
                return;

            swapUnlockAt = Time.time + 0.4f;
            Kills++;
            SfxSynth.Kill();
            if (Rig != null)
                Rig.Shake = 0.2f;
            var burst = monster.Type == BitType.Zero ? new Color(1f, 0.82f, 0.25f) : new Color(0.78f, 0.68f, 1f);
            Vfx.Burst(monster.transform.position, burst, 12, 5.5f);
            Float(monster.transform.position + Vector3.up * 0.4f, "YES!", Color.white);

            if (Kills >= Tune.KillsToWin)
            {
                Win();
                return;
            }

            var previousAttacker = Attacker;
            Attacker = Runner;
            Runner = previousAttacker;
            Required = Opposite(Required);
            Alice.GrantInvulnerability(0.55f);
            Bob.GrantInvulnerability(0.55f);
            Announce(false);
            if (Charlie != null)
                Charlie.Cast();
            TintGate();
        }

        public void WrongBit(BitMonster monster)
        {
            SfxSynth.Wrong();
            Float(monster.transform.position + Vector3.up * 0.85f, "wrong bit", new Color(1f, 0.45f, 0.38f));
            CharlieLine = "Wrong bit!";
        }

        public void OnHeroHurt(Hero hero, string cause)
        {
            SfxSynth.Hurt();
            if (Rig != null)
                Rig.Shake = 0.34f;
            Vfx.Burst(hero.transform.position, Color.white, 8, 3.4f);
            if (hero.Hp <= 0)
            {
                Lose(hero, cause);
                return;
            }

            CharlieLine = hero.Kind == HeroKind.Alice ? "Alice, look out!" : "Bob, look out!";
        }

        public void Float(Vector3 pos, string text, Color color)
        {
            Floaters.Add(new Floater { Pos = pos, Text = text, Color = color, Born = Time.time });
        }

        void Win()
        {
            Phase = MatchPhase.Won;
            Time.timeScale = 0f;
            SfxSynth.Win();
            CharlieLine = "Impossible... the pair is closed!";
            BigCallout = "YOU WIN";
            if (Rig != null)
                Rig.Shake = 0.45f;
        }

        void Lose(Hero hero, string cause)
        {
            Phase = MatchPhase.Lost;
            Time.timeScale = 0f;
            LostHero = hero;
            LostCause = cause;
            SfxSynth.Lose();
            CharlieLine = "Only the bits remain.";
        }

        void Announce(bool first)
        {
            string bit = Required == BitType.Zero ? "0" : "1";
            bool aliceAttacks = Attacker != null && Attacker.Kind == HeroKind.Alice;
            string who = aliceAttacks ? "Alice" : "Bob";
            string weapon = aliceAttacks ? "bow" : "sword";
            string other = aliceAttacks ? "Bob" : "Alice";
            string whoColor = aliceAttacks ? "#ff5a4a" : "#6aa4ff";
            string otherColor = aliceAttacks ? "#6aa4ff" : "#ff5a4a";
            Objective = "<color=" + whoColor + ">" + who + "</color>  ·  " + weapon + "  ·  target <color=#ffe08a>" + bit + "</color>";
            Subtitle = "<color=" + otherColor + ">" + other + "</color> cannot attack — monsters hurt both of you";
            CharlieLine = aliceAttacks
                ? (Required == BitType.One ? "Alice, shoot the one." : "Alice, shoot the zero.")
                : (Required == BitType.Zero ? "Bob, cut the zero." : "Bob, cut the one.");
            BigCallout = who + "  →  " + bit;
            CalloutUntil = Time.unscaledTime + (first ? 1.7f : 1.15f);
            if (!first)
                SfxSynth.Swap();
        }

        void TintGate()
        {
            if (Charlie == null || Charlie.Gate == null)
                return;
            Charlie.Gate.color = Required == BitType.Zero
                ? new Color(1f, 0.84f, 0.28f)
                : new Color(0.78f, 0.68f, 1f);
        }

        static BitType Opposite(BitType type)
        {
            return type == BitType.Zero ? BitType.One : BitType.Zero;
        }
    }
}
