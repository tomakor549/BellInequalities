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
        public const int CharlieLives = 5;
        public const int MaxMonsters = 5;
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
        public int CharlieHp = Tune.CharlieLives;
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
            CharlieHp = Tune.CharlieLives;
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
            CharlieHp = Tune.CharlieLives;
            Required = Random.value < 0.5f ? BitType.Zero : BitType.One;
            Attacker = Alice;
            Runner = Bob;
            Alice.Hp = Tune.Hearts;
            Bob.Hp = Tune.Hearts;
            Alice.GrantInvulnerability(1.3f);
            Bob.GrantInvulnerability(1.3f);
            LostHero = null;
            LostCause = "";
            Announce(true);
            TintGate();
            SpawnMonster(BitType.Zero, -0.4f);
            SpawnMonster(BitType.One, 0.4f);
            spawnIn = 1.15f;
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

            CountAlive(out int zeros, out int ones);
            int alive = zeros + ones;
            if (alive < Tune.MaxMonsters && (zeros == 0 || ones == 0))
                spawnIn = 0f;

            spawnIn -= Time.deltaTime;
            if (spawnIn > 0f || alive >= Tune.MaxMonsters)
                return;

            BitType type = zeros == 0 ? BitType.Zero
                : ones == 0 ? BitType.One
                : zeros < ones ? BitType.Zero
                : ones < zeros ? BitType.One
                : Required;
            bool neitherPresent = zeros == 0 && ones == 0;
            SpawnMonster(type, Random.Range(-0.55f, 0.55f));
            spawnIn = neitherPresent ? 0.05f : 1.15f;
        }

        void CountAlive(out int zeros, out int ones)
        {
            zeros = 0;
            ones = 0;
            for (int i = Monsters.Count - 1; i >= 0; i--)
            {
                var monster = Monsters[i];
                if (monster == null || !monster.Alive)
                {
                    Monsters.RemoveAt(i);
                    continue;
                }
                if (monster.Type == BitType.Zero)
                    zeros++;
                else
                    ones++;
            }
        }

        void SpawnMonster(BitType type, float x)
        {
            var spawned = BitMonster.Spawn(type, new Vector3(x, 8.35f, 0f));
            spawned.Body.linearVelocity = new Vector2(Random.Range(-3.5f, 3.5f), 0.3f);
            Remember(spawned);
            if (Charlie != null)
                Charlie.Cast();
            TintGate();
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
            SfxSynth.Kill();
            if (Rig != null)
                Rig.Shake = 0.2f;
            var burst = monster.Type == BitType.Zero ? new Color(1f, 0.82f, 0.25f) : new Color(0.78f, 0.68f, 1f);
            Vfx.Burst(monster.transform.position, burst, 12, 5.5f);
            Float(monster.transform.position + Vector3.up * 0.4f, "YES!", Color.white);
            CharlieHp = Mathf.Max(0, CharlieHp - 1);
            if (Charlie != null)
            {
                Float(Charlie.transform.position + Vector3.up * 1.5f, "-1", new Color(1f, 0.4f, 0.34f));
                Charlie.Flash(new Color(1f, 0.35f, 0.3f));
            }

            if (CharlieHp <= 0)
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

        public void WrongKill(BitMonster monster)
        {
            if (Phase != MatchPhase.Play)
                return;

            SfxSynth.Wrong();
            Vfx.Burst(monster.transform.position, new Color(1f, 0.45f, 0.38f), 8, 4f);
            Float(monster.transform.position + Vector3.up * 0.85f, "wrong bit", new Color(1f, 0.45f, 0.38f));
            if (CharlieHp >= Tune.CharlieLives)
            {
                CharlieLine = "Wrong bit. I am already full.";
                return;
            }

            CharlieHp++;
            CharlieLine = "Wrong bit. I recover a life.";
            if (Charlie != null)
            {
                Float(Charlie.transform.position + Vector3.up * 1.5f, "+1", new Color(0.55f, 1f, 0.55f));
                Charlie.Flash(new Color(0.6f, 1f, 0.68f));
            }
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
