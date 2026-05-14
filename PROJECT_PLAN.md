# Fort & Cards - 4 Member Project Plan

## 1. Project Goal
Make a Unity 2D strategy card game where:

- the player fights the computer
- the map uses hex tiles
- each side protects a Fort
- the game is played locally on one machine
- the main goal is to make the game work well and feel complete

Important decisions:

- do not build online multiplayer or LAN first
- do not build a server first
- first build a strong local game against the computer
- if the game is fully working early, extra features can be added later

## 2. Simple Scope
Keep the first version of the game small and solid.

Start with:

- 2 Forts
- a hex board
- one unit maximum per tile
- 3 card types: World Effect, Character, Spell
- a small card set: about 12 to 15 cards
- this turn order:
  - income
  - optional buy or discard
  - play cards
  - attack
  - end turn

Keep the card ideas simple at first:

- World Effect:
  - defense building
  - money field
  - hazard tile
- Character:
  - soldier
  - tanky unit
  - ranged unit
  - special unit
- Spell:
  - heal
  - direct damage
  - buff
  - debuff

### Card placement and targeting rules for v1

These rules define what counts as a valid target when a player clicks a card.
The UI should only highlight valid targets, but the real validation must also be enforced in code.

- Character cards:
  - spawn only on an empty tile
  - cannot spawn on a Fort
  - cannot spawn on another unit
  - Player 1 can spawn only in the first 2 columns on the blue side
  - Player 2 can spawn only in the last 2 columns on the red side
  - spawned units use the card's board sprite and movement value
  - current v1 implementation can create visible board units from played Character cards
- World Effect cards:
  - building/resource/hazard placement must be on an empty tile
  - v1 buildings and resource fields can be placed anywhere in the owner's half of the board
  - later hazard/weather cards may get special placement rules if needed
- Spell cards:
  - spells do not use empty-tile placement by default
  - heal targets an allied unit or allied Fort
  - buff targets an allied unit
  - direct damage targets an enemy unit or enemy Fort
  - debuff targets an enemy unit
  - utility cards define their target by effect

Current repo note as of `2026-04-30`:

- reusable runtime validation now enforces Character deployment-zone rules and World Effect owner-half rules
- target selection can now build `Tile`, `Ally/EnemyUnit`, and `Ally/EnemyFort` targets before card-play validation
- damage/heal card effects can now route valid Fort targets through the card pipeline
- effect-specific spell restrictions still need follow-up and Unity verification

Movement rule note:

- Character cards already have `unitMovementCapacity` in data.
- Runtime cards already copy movement into `CardRuntimeState.CurrentMovementCapacity`.
- Board movement uses `Unit.moveRange`, so spawned units must copy card movement into `Unit.moveRange`.

## 3. Team Roles

### Ali - Game Logic and Balancing Lead
Ali owns the part that needs the most game sense.

Main coding tasks:

- build the main game flow
- manage turns from start to finish
- manage player money, Fort HP, hand size, buy rules, and discard rules
- control when a card can be played and when a turn can end
- manage win and lose conditions
- balance card costs, unit stats, Fort HP, money gain, and card effects
- decide the final gameplay rules with the team after testing

Ali also owns:

- the starting hand rules
- the buy and discard rules
- the main balance file or game config
- the card values sheet


Ali should not spend most of his time on:

- hex movement details
- UI polish
- animation polish

### Abdo - Hex Board and Combat
Abdo owns everything that happens on the board.

Main coding tasks:

- build the hex grid
- create the tile system
- manage which tile is occupied and which is free
- enforce the rule: one unit maximum per tile
- place Forts on the board
- place units on the board
- code unit movement on hex tiles
- code attack range and damage
- remove dead units from the board
- apply damage to the Fort

Extra coding tasks:

- calculate hex neighbors
- calculate movement range
- calculate attack range
- support hazards or blocked tiles if needed

Why this role matters:

- hex logic is one of the hardest technical parts of the whole project

### Rabie - Computer Opponent and Game UI
Rabie owns the computer player and the main player-facing interface.

Main coding tasks:

- make the computer take a full turn by itself
- make the computer choose which card to play
- make the computer choose where to place units
- make the computer choose who to attack
- make the computer protect its Fort when in danger
- make the computer prefer useful moves over random bad moves
- build the hand UI
- show player money, turn, phase, and Fort HP on screen
- make the player click a card and choose a target
- highlight valid tiles or targets
- show error feedback when a move is invalid
- show game over screen and restart flow

Simple target for the computer:

- first make it play only legal actions
- then make it play reasonable actions
- do not try to make it perfect or too advanced

Good computer behavior examples:

- attack if it can destroy an enemy unit
- defend its Fort if needed
- play income cards early if useful
- avoid wasting strong cards on bad targets

Why this role matters:

- this is serious coding work and it combines game AI with real gameplay interface code

### Fatine - Card System and Effects
Fatine owns how cards are built and how their effects work in code.

Main coding tasks:

- create the base card system
- set up card data using ScriptableObjects
- connect each card to its real effect in code
- make sure each card checks valid targets before playing
- create the effect pipeline:
  - select card
  - validate target
  - apply effect
  - send the card to the correct pile

Extra coding tasks:

- build reusable card effects such as:
  - heal
  - damage
  - summon
  - buff
  - hazard
  - income boost
- connect UI to the real game state so what the player sees is always correct
- create the small first card set in data files so the team can test the real game

Why this role matters:

- this role is fully technical because it owns the full card architecture and effect logic

## 4. Work Rules So Nobody Blocks Anyone
Use these rules from the start.

- Ali owns main rules and balance
- Abdo owns board and combat code
- Rabie owns computer-opponent code and game UI code
- Fatine owns card system and card effect code
- no one should directly change another person's main files without agreement
- keep shared files small and clear
- merge work often
- test after every important feature
- use placeholder art first
- focus on gameplay before beauty

## 5. Suggested Work Order

### Week 1
Goal: build the base structure.

- Ali:
  - game flow
  - player data
  - Fort data
  - turn order
- Abdo:
  - hex tile system
  - board setup
- Rabie:
  - prepare computer-player structure
  - prepare action scoring idea
  - build HUD and hand UI prototype
- Fatine:
  - base card classes
  - card data
  - effect system prototype

### Week 2
Goal: get the board and cards connected.

- Ali:
  - buy rules
  - discard rules
  - money rules
- Abdo:
  - placement rules
  - occupied tile rules
  - card placement zones for Characters and World Effects
- Rabie:
  - read legal actions from the game state
  - player input
  - target selection
  - highlight valid targets
- Fatine:
- apply effect pipeline
- target validation
- first real card effects
- connect card movement values to spawned units

### Week 3
Goal: make units work.

- Ali:
  - turn state cleanup
  - rule checks
- Abdo:
  - movement
  - attack
  - unit death
  - Fort damage
- Rabie:
  - make the computer place units
  - make the computer attack
  - improve the game interface during real matches
- Fatine:
  - character cards fully playable
  - spell cards fully playable
  - verify spawned Character cards copy HP, attack, movement, owner, and chibi sprite correctly

### Week 4
Goal: make full matches possible.

- Ali:
  - win and lose system
  - balancing pass
- Abdo:
  - board bug fixing
  - combat cleanup
- Rabie:
  - improve computer decisions so it stops making obvious mistakes
  - game over screen
  - restart flow
  - error messages
- Fatine:
  - world effect cards
  - final card effect cleanup

### Week 5 and After
Goal: improve quality.

- rebalance cards
- fix bugs
- improve UI clarity
- improve computer decisions
- add simple effects and polish
- prepare demo and explanation

## 6. Minimum Features That Must Work
Before adding extra ideas, these must work:

- game starts correctly
- each side gets the correct starting hand
- player can play cards legally
- units can move and attack
- Fort can take damage
- the computer can finish its turn without breaking the game
- card effects work correctly
- game ends correctly when a Fort is destroyed

## 7. Testing Checklist
Use this checklist often.

- starting hand always has the right card types
- bought cards are added to hand correctly
- buying and discarding work correctly
- cards cannot be played on wrong targets
- Character cards only spawn in the owner's deployment zone
- World Effect buildings only place in the owner's half of the board
- Spell cards only target valid units or Forts for their effect type
- spawned units use the Character card's movement value
- spawned Character units use the Character card's board sprite
- newly spawned Character units follow the team's summon attack readiness rule
- units cannot move to illegal hexes
- attacks only happen in valid range
- dead units are removed correctly
- Fort HP updates correctly
- computer never makes illegal moves
- restart gives a clean new game

## 8. Final Notes
This is the best structure if you want:

- fair coding work for all 4 members
- clear ownership
- less interference
- a project that still feels technical and serious


## 9. Balance Check for the Team
This role split is more balanced than the previous one.

Why the old split was not balanced:

- Fatine had both the card system and almost all player interaction code
- Rabie only had the computer player, which starts smaller than a full card plus UI system

Why this new split is better:

- Ali keeps the game rules and balancing work
- Abdo keeps the difficult hex board and combat work
- Rabie now has two real systems: computer logic and game UI
- Fatine now focuses deeply on one large technical area: cards and effects

This should feel more fair for a computer engineering project because every member owns a clear coding-heavy subsystem.
