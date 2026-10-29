# Alice & Bob

A local two-player game for one keyboard. Alice and Bob fight Charlie’s monsters in a tall stair hall. Charlie scores each pair of kills and loses when his five lives are gone.

Built with Unity 6000.6.7f1 and the Universal Render Pipeline. Open the project, open `Assets/Scenes/SampleScene.unity`, click the Game view, and press Play. The match is created when the scene starts.

If a play session is already running, stop it and press Play again after a script change.

## Controls

| | Move | Jump | Drop through a platform | Attack |
| --- | --- | --- | --- |
| Alice, red A, bow | A D | W | S | J or Left Ctrl |
| Bob, blue B, sword | Left Right | Up | Down | K or Right Ctrl |

S or Down passes through the platform under that hero. The stone floor stays solid.

Enter or a click starts the match. Enter also starts again from the results table. Esc pauses. R restarts.

## Rules

Only one hero attacks at a time. Alice starts each pair. After that kill, Bob attacks and Alice can only run. A monster hit hurts whoever it touches.

At most three monsters are alive. There is always at least one 0 and one 1. Bob’s sword hits the nearest living monster. Alice fires an arrow.

Each hero has a state, 0 or 1, shown during play. Alice’s kill is Ca and Bob’s kill is Cb. When both have killed once, the pair is tested:

```
Ca AND Cb  ==  a XOR b
```

If the equation fails, Charlie loses a life. If it holds, he recovers a life, up to 5. If he is already full, nothing changes and he says so. Both states then roll again. Drain all five lives to win. If either hero loses all four hearts, the match is lost.

The equation is not printed during play. The results table at the end lists every pair: A, B, Ca, Cb, and whether Charlie lost a life, recovered a life, or was already full.

## Project

Gameplay code is in `Assets/Scripts/BitArena`. `GameBootstrap` builds the hall, the heroes, and Charlie when Play starts. Sprites are drawn in code. Monsters use the same platform tops as the colliders when they climb and drop.
