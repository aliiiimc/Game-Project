# Fort & Cards — Turn-Based Strategy Game

## Game Concept Summary

A **PVP turn-based strategy card game** hosted on a local server. Each player defends a **Fort** using cards drawn from three categories.

---

## Core Mechanics

### The Fort
- Each player has a Fort on the map — lose it, you lose the game
- The Fort has HP and can be defended with Environment cards

### Card System
We start game like this
| Category | Starting Hand | Purpose |
|----------|--------------|---------|
| **Environment** (2) | Build structures, plant money-generating fields, deploy natural disasters (volcano, earthquake, fog) |
| **Character** (2) | Spawn units for offense/defense — soldiers, animals, wizards, mythical beings |
| **Spell** (1) | Heal, accelerate field growth, steal opponent's cards, buffs/debuffs |

- **Starting hand**: 5 cards (2 Environment + 2 Character + 1 Spell), drawn randomly
- **Buy/Discard**: Discard 1 card to buy a random card from the buy stack (chance-based rarity)

### Win Condition
Destroy the opponent's Fort.

---

## How to Build This in Unity — Step by Step

### Phase 0: Learn the Basics (Week 1–2)

Since this is your first Unity/C# project, start here:

1. **Install Unity Hub** → install Unity 2022 LTS (stable, most tutorials target it)
2. **Pick 2D or 3D** — for a card/strategy game, **2D is simpler** to start with
3. **Learn C# basics** — variables, functions, classes, lists, enums
   - Free: [Microsoft C# fundamentals](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/)
4. **Follow Unity's official beginner tutorials** (2–3 hours)
   - Unity Learn: "Create with Code" or "Junior Programmer" pathway
5. **Key Unity concepts to understand first:**
   - GameObjects, Components, Prefabs
   - MonoBehaviour lifecycle (`Start()`, `Update()`)
   - UI system (Canvas, Buttons, Text)
   - ScriptableObjects (perfect for card data)
   - Scene management

### Phase 1: Card Data & Deck System (Week 3)

**Goal**: Define cards as data, build a deck, draw hands.

```
Assets/
  ScriptableObjects/
    Cards/
      EnvironmentCards/
      CharacterCards/
      SpellCards/
  Scripts/
    Cards/
      Card.cs              — base ScriptableObject
      EnvironmentCard.cs   — inherits Card
      CharacterCard.cs     — inherits Card
      SpellCard.cs         — inherits Card
    Deck/
      Deck.cs              — list of cards, shuffle, draw
      DiscardPile.cs       — discarded cards
      BuyStack.cs          — random card purchase logic
    Player/
      PlayerHand.cs        — holds 5 cards, play/discard
      PlayerData.cs        — money, fort HP, stats
```

**How ScriptableObjects work for cards:**
```csharp
// Card.cs — base card
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card")]
public class Card : ScriptableObject
{
    public string cardName;
    public string description;
    public Sprite artwork;
    public int cost;
    public CardCategory category; // enum: Environment, Character, Spell
}

// Enum
public enum CardCategory { Environment, Character, Spell }
```

> ScriptableObjects let you create card assets in the Unity Editor — no code needed to add new cards. Right-click → Create → Cards → Card.

### Phase 2: Game Board & Fort (Week 4)

**Goal**: Create the map, place Forts, basic grid/tile system.

- Use a **Tilemap** (2D) or simple grid for the game board
- Each player's Fort is a GameObject with HP
- Define placement zones where cards can be played

```
Scripts/
  Board/
    GameBoard.cs        — grid setup, tile references
    Tile.cs             — what occupies each tile
  Fort/
    Fort.cs             — HP, defense stats, damage/destroy logic
```

### Phase 3: Turn System (Week 5)

**Goal**: Alternate turns, define what a player can do per turn.

```csharp
public class TurnManager : MonoBehaviour
{
    public enum Phase { Draw, Play, Attack, End }

    public void StartTurn(Player player) { }
    public void EndTurn() { }
    public void NextPhase() { }
}
```

Each turn:
1. **Draw phase** — option to buy/discard
2. **Play phase** — place cards on the board
3. **Attack phase** — units attack
4. **End phase** — field income, effects resolve, pass turn

### Phase 4: Card Effects & Combat (Week 6–7)

**Goal**: Make cards actually do things.

- **Environment**: spawn buildings/fields on tiles, trigger disasters on enemy tiles
- **Character**: spawn units with HP/ATK/movement, basic combat (ATK vs HP)
- **Spell**: apply effects (heal Fort, boost units, steal card)

Use an **interface pattern**:
```csharp
public interface IPlayable
{
    void Play(Player owner, Tile target);
}
```

### Phase 5: Local Multiplayer / Networking (Week 8–9)

**Goal**: Two players on a local network.

**Options (easiest to hardest):**

| Option | Difficulty | Description |
|--------|-----------|-------------|
| **Hot-seat** (same PC) | Easy | Players take turns on the same screen |
| **Unity Netcode for GameObjects** | Medium | Unity's built-in networking, good for LAN |
| **Mirror Networking** | Medium | Popular free networking library, lots of tutorials |

**Recommendation**: Start with **hot-seat** (pass and play) to get the game working first, then add networking with **Mirror** (most beginner-friendly for LAN).

```
Scripts/
  Networking/
    GameServer.cs        — host game, manage connections
    GameClient.cs        — connect to host
    NetworkTurnManager.cs — sync turns over network
```

### Phase 6: UI & Polish (Week 10+)

- Card hand UI (drag and drop cards)
- Board interaction (click to place, highlight valid tiles)
- HP bars, money display, turn indicator
- Card animations (draw, play, discard)
- Sound effects, visual effects for spells/attacks

---

## Recommended Project Structure

```
Assets/
  Art/
    Cards/           — card artwork (can use placeholder squares at first)
    Board/           — tile sprites
    UI/              — buttons, panels
  Audio/
  Prefabs/
    Cards/
    Units/
    Buildings/
  Scenes/
    MainMenu.unity
    GameScene.unity
  ScriptableObjects/
    Cards/           — all card data assets
    GameConfig/      — balance values (starting money, fort HP, etc.)
  Scripts/
    Cards/
    Board/
    Fort/
    Player/
    Deck/
    Combat/
    TurnSystem/
    Networking/
    UI/
```

---

## How to Work as a Team

### Use Git from Day 1
- Create a GitHub/GitLab repo
- Use Unity's `.gitignore` template (critical — Unity generates huge files)
- Each person works on a **branch**, merge via pull requests
- **Never commit the `Library/` folder**


### Development Rules
1. **Prototype ugly first** — use colored squares, not art. Gameplay > visuals
2. **Test constantly** — play the game after every feature, even with placeholder art
3. **Keep a shared doc** (Notion, Google Doc) for card ideas and balance values
4. **Use ScriptableObjects for all data** — makes it easy for non-coders to add cards
5. **Commit often, with clear messages** — "Add card draw logic" not "stuff"

---

## Recommended Learning Resources

- **Unity Learn** (free): learn.unity.com — official beginner tutorials
- **Brackeys** (YouTube): best Unity beginner channel (archived but still relevant)
- **Code Monkey** (YouTube): great for C# + Unity gameplay systems
- **Mirror Networking docs**: mirror-networking.gitbook.io — when you reach networking
- **Game Dev with ScriptableObjects**: search "Unite 2017 ScriptableObjects" on YouTube

---

## Milestone Checklist

- [ ] Everyone installs Unity, creates a test project, runs "Hello World"
- [ ] Card ScriptableObjects created, can define cards in editor
- [ ] Deck shuffle + draw 5 cards working
- [ ] Cards display in player's hand (UI)
- [ ] Game board with tiles rendered
- [ ] Fort placed, has HP, can take damage
- [ ] Turn system working (hot-seat, 2 players same screen)
- [ ] Cards can be played onto the board
- [ ] Character units move and attack
- [ ] Environment effects work (build, fields, disasters)
- [ ] Spell effects work
- [ ] Buy/discard system working
- [ ] Money system (round rewards, field income)
- [ ] Win condition (Fort destroyed → game over screen)
- [ ] LAN multiplayer working
- [ ] UI polish, animations, sound
