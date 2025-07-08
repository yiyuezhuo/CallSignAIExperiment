# Call Sign AI Experiment

AI Experiment for Sebastian's postcard wargame Call Sign:

https://sdhist.com/sebastian-bae-call-sign-free-game-carrier-combat/

Current there're traditional heuristic seaerch based AI and a sentis (Unity's ONNX runtime) AI which fit the replay data made by search based AI. RL is not implemented yet.

Training related code:

https://github.com/yiyuezhuo/Call-Sign-AI-Experiment-Notebooks/tree/main


## Usage

The game itself operates in an "edit" mode, for example, you would move unit freely without strict "rule enforcement"--similar to most TTS or Vassal module. While rule enforcement could be useful, this project prioritizes AI experiment over a game for fun, allowing scenario editing to observe AI reactions.

- Simple Human vs AI
    - Select playing side from "Playing" dropdown
    - Press "Next phase" to advance to the phase requiring input.
    - Make input while respecting the rule (it's recommended to read the rule firstly)
        - Selecting: Left click to select a stack and then left click unit in the stack (right-top corner) to select unit in the stack.
        - Editing Move: Move unit to location on the map or "not deployed", destroyed area. (Which may represent a deploy, regenerate, move, a part of C2-move or general editing)
        - Adding a engagement record: press "+" to add a engagement record, which may represent an engagement in the engagement phase. Note shooter, target and mode require more assignment.
        - Commit: Commit unit to an engagement record as shooter or target. (Select a unit, press "commit" button and press on an empty box of a engagement record to commit. The left-top is shooter, the right-bottom is the target.)
        - Toggle engagement mode: press medium toggle to toggle between air-to-air combat or air-to-carrier combat.
        - Press "Next phase" to go to next decision point. (or "End Edit & Next phase" if you want to jump to the next phase only (which is not always the phase that require decision).)
    - End Condition: Check "Victory status", if it's not "undermined" then check "Victory side".
- Simple AI vs AI
    - Press "AI Run & Next Phase" until victory side is determined.
- Generate Self replay records
    - Press on the "Replay" tab
    - Set samples
    - set initial condition (random denotes the initial state is randomized).
    - Press "Self-Play And Cache" and wait the self-play to finish.
    - Press "Export Replay" to get the replay xml
- Misc Settings
    - In the "Misc" tab, select agent to select an agent, `BaselineAgentX` is the search based AI while `NNBaseline` is the NN based AI.
    - Export or import the current state.

Source code:

https://github.com/yiyuezhuo/CallSignAIExperiment

## Screenshots

<img src="https://img.itch.zone/aW1hZ2UvMzcwNTAzMy8yMjA0OTIxNy5wbmc=/original/ech5e8.png">