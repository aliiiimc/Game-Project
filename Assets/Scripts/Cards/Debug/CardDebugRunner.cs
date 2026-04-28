using System.Collections.Generic;
using UnityEngine;
public sealed class CardDebugRunner : MonoBehaviour
{
    [Header("Card Under Test")]
    [SerializeField] private CardData sourceCard;

    [Header("Debug Implementations")]
    [SerializeField] private MonoBehaviour validatorBehaviour;
    [SerializeField] private MonoBehaviour effectBehaviour;

    [Header("Runtime Integration")]
    [SerializeField] private MonoBehaviour writerBehaviour;
    [SerializeField] private HexGrid boardSource;
    [SerializeField] private bool useCardPlayPipeline = true;

    [Header("Context")]
    [SerializeField] private string actingPlayerKey;
    [SerializeField] private string opponentPlayerKey;
    [SerializeField] private int startingMoney = 10;

    [Header("Target")]
    [SerializeField] private CardTargetType targetType = CardTargetType.Tile;
    [SerializeField] private int targetQ;
    [SerializeField] private int targetR;
    [SerializeField] private string targetPlayerId;
    [SerializeField] private string targetEntityId;

    [Header("Fake Board")]
    [SerializeField] private int boardWidth = 8;
    [SerializeField] private int boardHeight = 6;
    [SerializeField] private bool markTargetTileOccupied;

    [Header("Execution")]
    [SerializeField] private bool runValidation = true;
    [SerializeField] private bool runEffect = true;

    [ContextMenu("Run Debug Card Flow")]
    public void RunDebugCardFlow()
    {
        if (sourceCard == null)
        {
            Debug.LogWarning("[CardDebugRunner] No source card assigned.");
            return;
        }

        ICardTargetValidator validator = validatorBehaviour as ICardTargetValidator;
        ICardEffect effect = effectBehaviour as ICardEffect;

        if (runValidation && validator == null)
        {
            Debug.LogWarning("[CardDebugRunner] Validation enabled but validatorBehaviour does not implement ICardTargetValidator.");
            return;
        }

        if (runEffect && effect == null)
        {
            Debug.LogWarning("[CardDebugRunner] Effect execution enabled but effectBehaviour does not implement ICardEffect.");
            return;
        }

        CardRuntimeState runtimeCard = CardFactory.CreateRuntimeState(sourceCard);
        if (runtimeCard == null)
        {
            Debug.LogWarning("[CardDebugRunner] Failed to create runtime state.");
            return;
        }

        AxialCoord tile = new AxialCoord(targetQ, targetR);
        CardTarget target = new CardTarget
        {
            type = targetType,
            tile = tile,
            targetPlayerId = targetPlayerId,
            targetEntityId = targetEntityId,
            targetCard = null
        };

        IBoardStateReader board = CreateBoardReader(runtimeCard, tile);
        ICardStateWriter writer = CreateWriter(actingPlayerKey, startingMoney);

        CardValidationContext validationContext = new CardValidationContext
        {
            ActingPlayerKey = actingPlayerKey,
            OpponentPlayerKey = opponentPlayerKey,
            Board = board
        };

        CardEffectContext effectContext = new CardEffectContext
        {
            ActingPlayerKey = actingPlayerKey,
            OpponentPlayerKey = opponentPlayerKey,
            Board = board,
            Writer = writer
        };

        Debug.Log($"[CardDebugRunner] Starting debug flow for card '{sourceCard.DisplayName}' with target {target.type} at {target.tile}.");

        if (useCardPlayPipeline)
        {
            CardPlayPipeline pipeline = new CardPlayPipeline();
            CardPlayRequest request = new CardPlayRequest
            {
                ActingPlayerId = actingPlayerKey,
                OpponentPlayerId = opponentPlayerKey,
                SourceCard = runtimeCard,
                Target = target,
                Board = board,
                Writer = writer,
                Validator = validator,
                Effect = effect
            };

            CardPlayResult playResult = pipeline.Play(request);
            if (!playResult.Succeeded)
            {
                Debug.LogWarning($"[CardDebugRunner] Play FAILED | code={playResult.ReasonCode} | message={playResult.Message}");
            }
            else
            {
                Debug.Log($"[CardDebugRunner] Play PASSED | zone={playResult.FinalZone} | damage={playResult.EffectResult.DamageDealt} | heal={playResult.EffectResult.HealApplied} | revenue={playResult.EffectResult.RevenueGained}");
            }

            DumpWriterState(writer, runtimeCard);
            return;
        }

        if (runValidation)
        {
            CardValidationResult validation = validator.Validate(validationContext, runtimeCard, target);
            if (!validation.IsValid)
            {
                Debug.LogWarning($"[CardDebugRunner] Validation FAILED | code={validation.ReasonCode} | message={validation.Message}");
                DumpWriterState(writer, runtimeCard);
                return;
            }

            Debug.Log("[CardDebugRunner] Validation PASSED.");
        }

        if (runEffect)
        {
            CardEffectResult effectResult = effect.Apply(effectContext, runtimeCard, target);
            if (!effectResult.Succeeded)
            {
                Debug.LogWarning($"[CardDebugRunner] Effect FAILED | code={effectResult.ReasonCode} | message={effectResult.Message}");
                DumpWriterState(writer, runtimeCard);
                return;
            }

            Debug.Log($"[CardDebugRunner] Effect PASSED | damage={effectResult.DamageDealt} | heal={effectResult.HealApplied} | revenue={effectResult.RevenueGained} | message={effectResult.Message}");
        }

        DumpWriterState(writer, runtimeCard);
    }

    private IBoardStateReader CreateBoardReader(CardRuntimeState runtimeCard, AxialCoord tile)
    {
        if (boardSource != null)
        {
            return new FortGame.Computer.HexGridBoardStateReader(boardSource);
        }

        FakeBoardStateReader board = new FakeBoardStateReader(boardWidth, boardHeight);
        if (markTargetTileOccupied && board.IsTileValid(tile))
        {
            board.SetOccupied(tile, runtimeCard);
        }

        return board;
    }

    private ICardStateWriter CreateWriter(string actingPlayerId, int initialMoney)
    {
        ICardStateWriter runtimeWriter = writerBehaviour as ICardStateWriter;
        if (runtimeWriter != null)
        {
            return runtimeWriter;
        }

        return new FakeCardStateWriter(actingPlayerId, initialMoney);
    }

    private static void DumpWriterState(ICardStateWriter writer, CardRuntimeState runtimeCard)
    {
        string moneyPlayerId = string.Empty;
        if (writer is FakeCardStateWriter fakeWriter)
        {
            moneyPlayerId = fakeWriter.LastActingPlayerId;
        }
        else if (writer is GameManagerCardStateWriter gameWriter)
        {
            moneyPlayerId = gameWriter.LastActingPlayerId;
        }

        int money = 0;
        if (writer is FakeCardStateWriter fakeStateWriter)
        {
            money = fakeStateWriter.GetMoney(moneyPlayerId);
        }
        else if (writer is GameManagerCardStateWriter gameStateWriter)
        {
            money = gameStateWriter.GetMoney(moneyPlayerId);
        }

        Debug.Log($"[CardDebugRunner] Final state | zone={runtimeCard.CurrentZone} | manifested={runtimeCard.IsManifestedOnBoard} | pos={runtimeCard.BoardPosition} | hp={(runtimeCard.CurrentHp.HasValue ? runtimeCard.CurrentHp.Value.ToString() : "n/a")} | money={money}");

        if (writer is FakeCardStateWriter fakeLogWriter)
        {
            IReadOnlyList<string> log = fakeLogWriter.ActionLog;
            for (int i = 0; i < log.Count; i++)
            {
                Debug.Log($"[CardDebugRunner] Log[{i}] {log[i]}");
            }
        }
    }

    private sealed class FakeBoardStateReader : IBoardStateReader
    {
        private readonly int width;
        private readonly int height;
        private readonly Dictionary<AxialCoord, CardRuntimeState> occupiedByTile = new Dictionary<AxialCoord, CardRuntimeState>();

        public FakeBoardStateReader(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
        }

        public bool IsTileValid(AxialCoord tile)
        {
            return tile.q >= 0 && tile.q < width && tile.r >= 0 && tile.r < height;
        }

        public bool IsTileOccupied(AxialCoord tile)
        {
            return occupiedByTile.ContainsKey(tile);
        }

        public CardRuntimeState GetCardAt(AxialCoord tile)
        {
            occupiedByTile.TryGetValue(tile, out CardRuntimeState card);
            return card;
        }

        public void SetOccupied(AxialCoord tile, CardRuntimeState card)
        {
            if (!IsTileValid(tile))
            {
                return;
            }

            occupiedByTile[tile] = card;
        }

        public void ClearOccupied(AxialCoord tile)
        {
            occupiedByTile.Remove(tile);
        }
    }

    private sealed class FakeCardStateWriter : CardStateWriterBase
    {
        private readonly Dictionary<string, int> moneyByPlayer = new Dictionary<string, int>();
        private readonly List<string> actionLog = new List<string>();

        public FakeCardStateWriter(string actingPlayerKey, int startingMoney)
        {
            moneyByPlayer[actingPlayerKey] = Mathf.Max(0, startingMoney);
            LastActingPlayerId = actingPlayerKey;
        }

        public IReadOnlyList<string> ActionLog => actionLog;
        public string LastActingPlayerId { get; private set; }

        public override bool TrySpendCost(string playerId, int amount)
        {
            LastActingPlayerId = playerId;

            if (string.IsNullOrWhiteSpace(playerId) || !moneyByPlayer.ContainsKey(playerId))
            {
                actionLog.Add($"TrySpendCost failed: unknown player '{playerId}'.");
                return false;
            }

            int current = moneyByPlayer[playerId];
            int cost = Mathf.Max(0, amount);
            if (current < cost)
            {
                actionLog.Add($"TrySpendCost failed: player '{playerId}' has {current}, needs {cost}.");
                return false;
            }

            moneyByPlayer[playerId] = current - cost;
            actionLog.Add($"TrySpendCost success: player '{playerId}' paid {cost}. Remaining={moneyByPlayer[playerId]}.");
            return true;
        }

        public override void AddRevenue(string playerId, int amount)
        {
            LastActingPlayerId = playerId;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            if (!moneyByPlayer.TryGetValue(playerId, out int currentMoney))
            {
                currentMoney = 0;
            }

            int add = Mathf.Max(0, amount);
            moneyByPlayer[playerId] = currentMoney + add;
            actionLog.Add($"AddRevenue: player='{playerId}' amount={add} total={moneyByPlayer[playerId]}.");
        }

        protected override void LogTransaction(string message)
        {
            actionLog.Add(message);
        }

        public int GetMoney(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return 0;
            }

            return moneyByPlayer.TryGetValue(playerId, out int value) ? value : 0;
        }
    }

}
