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
        public const int MaxMonsters = 3;
    }

    public struct Floater
    {
        public Vector3 Pos;
        public string Text;
        public Color Color;
        public float Born;
    }

    public enum CharlieShift
    {
        Lost,
        Recovered,
        Full
    }

    public struct RoundNote
    {
        public int A;
        public int B;
        public int Ca;
        public int Cb;
        public CharlieShift Shift;
    }

    public class GameSession : MonoBehaviour
    {
        public static GameSession I { get; private set; }

        public MatchPhase Phase = MatchPhase.Title;
        public BitType AliceState = BitType.Zero;
        public BitType BobState = BitType.Zero;
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
        public readonly List<RoundNote> Rounds = new List<RoundNote>();

        float spawnIn = 2f;
        float swapUnlockAt;

        public bool SwapLocked => Time.time < swapUnlockAt;

        bool aliceReady;
        bool bobReady;
        BitType aliceKill;
        BitType bobKill;

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
            aliceReady = false;
            bobReady = false;
            Rounds.Clear();
            AliceState = RollBit();
            BobState = RollBit();
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
                : (Random.value < 0.5f ? BitType.Zero : BitType.One);
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
            var spawned = BitMonster.Spawn(type, new Vector3(x, 14.5f, 0f));
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

        public void RecordKill(BitMonster monster)
        {
            if (Phase != MatchPhase.Play || SwapLocked || Attacker == null)
                return;

            swapUnlockAt = Time.time + 0.4f;
            SfxSynth.Kill();
            if (Rig != null)
                Rig.Shake = 0.16f;
            var burst = monster.Type == BitType.Zero ? new Color(1f, 0.82f, 0.25f) : new Color(0.78f, 0.68f, 1f);
            Vfx.Burst(monster.transform.position, burst, 12, 5.5f);
            Float(monster.transform.position + Vector3.up * 0.4f, BitDigit(monster.Type), Color.white);

            if (Attacker.Kind == HeroKind.Alice)
            {
                aliceKill = monster.Type;
                aliceReady = true;
            }
            else
            {
                bobKill = monster.Type;
                bobReady = true;
            }

            if (aliceReady && bobReady)
            {
                ResolvePair();
                return;
            }

            SwapRoles();
            Announce(false);
            if (Charlie != null)
                Charlie.Cast();
        }

        void ResolvePair()
        {
            int and = And(aliceKill, bobKill);
            int xor = Xor(AliceState, BobState);
            bool holds = and == xor;
            bool alreadyFull = holds && CharlieHp >= Tune.CharlieLives;
            aliceReady = false;
            bobReady = false;

            if (!holds)
            {
                CharlieHp = Mathf.Max(0, CharlieHp - 1);
                if (Rig != null)
                    Rig.Shake = 0.28f;
                if (Charlie != null)
                {
                    Float(Charlie.transform.position + Vector3.up * 1.5f, "-1", new Color(1f, 0.4f, 0.34f));
                    Charlie.Flash(new Color(1f, 0.35f, 0.3f));
                }
            }
            else if (CharlieHp < Tune.CharlieLives)
            {
                CharlieHp++;
                SfxSynth.Wrong();
                if (Charlie != null)
                {
                    Float(Charlie.transform.position + Vector3.up * 1.5f, "+1", new Color(0.55f, 1f, 0.55f));
                    Charlie.Flash(new Color(0.6f, 1f, 0.68f));
                }
            }
            else
            {
                SfxSynth.Wrong();
            }

            Rounds.Add(new RoundNote
            {
                A = BitValue(AliceState),
                B = BitValue(BobState),
                Ca = BitValue(aliceKill),
                Cb = BitValue(bobKill),
                Shift = !holds ? CharlieShift.Lost : alreadyFull ? CharlieShift.Full : CharlieShift.Recovered
            });
            if (!holds && CharlieHp <= 0)
            {
                Win();
                return;
            }

            string verdict = !holds
                ? "I lose a life."
                : alreadyFull
                    ? "I am already full."
                    : "I recover a life.";

            AliceState = RollBit();
            BobState = RollBit();
            SwapRoles();
            Announce(false);
            CharlieLine = verdict;
            BigCallout = holds ? "MATCH" : "BREAK";
            CalloutUntil = Time.unscaledTime + 1.5f;
            if (Charlie != null)
                Charlie.Cast();
            TintGate();
        }

        void SwapRoles()
        {
            var previousAttacker = Attacker;
            Attacker = Runner;
            Runner = previousAttacker;
            if (Alice != null)
                Alice.GrantInvulnerability(0.55f);
            if (Bob != null)
                Bob.GrantInvulnerability(0.55f);
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
            bool aliceAttacks = Attacker != null && Attacker.Kind == HeroKind.Alice;
            string who = aliceAttacks ? "Alice" : "Bob";
            string other = aliceAttacks ? "Bob" : "Alice";
            string whoColor = aliceAttacks ? "#ff5a4a" : "#6aa4ff";
            string otherColor = aliceAttacks ? "#6aa4ff" : "#ff5a4a";
            Objective = "<color=#ff5a4a>Alice</color> state <color=#ffe08a>" + BitDigit(AliceState)
                + "</color>    <color=#6aa4ff>Bob</color> state <color=#ffe08a>" + BitDigit(BobState) + "</color>";
            Subtitle = "<color=" + whoColor + ">" + who + "</color> attacks  ·  <color=" + otherColor + ">" + other + "</color> runs";
            if (aliceReady && !bobReady)
                CharlieLine = "Alice killed " + BitDigit(aliceKill) + ". Bob, choose your bit.";
            else if (bobReady && !aliceReady)
                CharlieLine = "Bob killed " + BitDigit(bobKill) + ". Alice, choose your bit.";
            else
                CharlieLine = "Kill any bit.";
            if (aliceReady && !bobReady)
                BigCallout = "Alice  →  " + BitDigit(aliceKill);
            else if (bobReady && !aliceReady)
                BigCallout = "Bob  →  " + BitDigit(bobKill);
            else
                BigCallout = "A " + BitDigit(AliceState) + "    B " + BitDigit(BobState);
            CalloutUntil = Time.unscaledTime + (first ? 1.7f : 1.15f);
            if (!first)
                SfxSynth.Swap();
        }

        static int BitValue(BitType type)
        {
            return type == BitType.One ? 1 : 0;
        }

        static int And(BitType left, BitType right)
        {
            return (left == BitType.One && right == BitType.One) ? 1 : 0;
        }

        static int Xor(BitType left, BitType right)
        {
            return left != right ? 1 : 0;
        }

        static BitType RollBit()
        {
            return Random.value < 0.5f ? BitType.Zero : BitType.One;
        }

        static string BitDigit(BitType type)
        {
            return type == BitType.Zero ? "0" : "1";
        }

        void TintGate()
        {
            if (Charlie == null || Charlie.Gate == null)
                return;
            Charlie.Gate.color = Xor(AliceState, BobState) == 0
                ? new Color(1f, 0.84f, 0.28f)
                : new Color(0.78f, 0.68f, 1f);
        }
    }
}
