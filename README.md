# Alice & Bob

A local two-player game for one keyboard. Alice and Bob fight Charlie’s monsters on one of three fields. Charlie scores each pair of kills and loses when the lives chosen on the main menu are gone.

Built with Unity 6000.6.7f1 and the Universal Render Pipeline. Open the project, open `Assets/Scenes/SampleScene.unity`, click the Game view, and press Play. The match is created when the scene starts.

If a play session is already running, stop it and press Play again after a script change.

## Controls

| | Move | Jump | Drop through a platform | Attack |
| --- | --- | --- | --- |
| Alice, red A, bow | A D | W | S | J or Left Ctrl |
| Bob, blue B, sword | Left Right | Up | Down | K or Right Ctrl |

S or Down passes through the platform under that hero. The stone floor stays solid.

The game opens on a main menu. Above New Game, a difficulty control sets how many lives Charlie has: Easy 2, Medium 3, Hard 5. Medium is the default. Left and Right change it. Up and Down choose New Game or Exit, then Enter confirms. New Game leads to the field list: The Hall, Snow Court, or Greenwild. Left and Right choose a field, then Start begins the match. Exit closes the game. On the results screen, Play again, Enter, Space, or R starts the same field again at the same difficulty. Main menu or M returns to the main menu. Esc pauses. R restarts.

## Rules

Only one hero attacks at a time. Alice starts each pair. After that kill, Bob attacks and Alice can only run. A monster hit hurts whoever it touches.

At most three monsters are alive. There is always at least one 0 and one 1. Bob’s sword hits the nearest living monster. Alice fires an arrow.

Each hero has a state, 0 or 1, shown during play. Alice’s kill is Ca and Bob’s kill is Cb. When both have killed once, the pair is tested:

```
Ca AND Cb  ==  a XOR b
```

If the equation fails, Charlie loses a life. If it holds, he recovers a life, up to the maximum for the chosen difficulty (Easy 2, Medium 3, Hard 5). If he is already full, nothing changes and he says so. Both states then roll again. Drain every one of those lives to win. If either hero loses all four hearts, the match is lost.

The equation is not printed during play. The results table at the end lists every pair: A, B, Ca, Cb, and whether Charlie lost a life, recovered a life, or was already full.

## Project

Gameplay code is in `Assets/Scripts/BitArena`. The main menu picks a field. `ArenaMaps` builds The Hall, the winter court, or the forest, and the monster steps use those same platform tops. Sprites are drawn in code.
