using UnityEngine;

public sealed class CardPlayService : MonoBehaviour, ICardPlayService
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HexGrid boardSource;
    [SerializeField] private GameManagerCardStateWriter writer;
    [SerializeField] private bool autoResolveDependencies = true;

    private readonly ICardPlayPipeline pipeline = new CardPlayPipeline();
    private PlayerKeyResolver playerKeyResolver;
    private CardPlayDependencyResolver dependencyResolver;

    private void Awake()
    {
        if (!autoResolveDependencies)
        {
            InitializeResolvers();
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }

        if (writer == null)
        {
            writer = FindFirstObjectByType<GameManagerCardStateWriter>();
        }

        InitializeResolvers();
    }

    public CardPlayResult CanPlayCard(CardRuntimeState sourceCard, string actingPlayerId, CardTarget target)
    {
        if (playerKeyResolver == null || dependencyResolver == null)
        {
            InitializeResolvers();
        }

        if (gameManager == null)
        {
            return Fail("NO_GAME_MANAGER", "Game manager is missing.", sourceCard);
        }

        if (gameManager.currentPhase != GamePhase.Play)
        {
            return Fail("WRONG_PHASE", "Cards can only be played during Play phase.", sourceCard);
        }

        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            return Fail("NO_CARD", "Source card is missing.", sourceCard);
        }

        if (!playerKeyResolver.TryResolveActorAndOpponent(actingPlayerId, out string resolvedActor, out string opponentPlayerId))
        {
            return Fail("NO_ACTOR", "Acting player id is missing or invalid.", sourceCard);
        }

        ICardTargetValidator validator = dependencyResolver.ResolveValidator(sourceCard.SourceCard);
        if (validator == null)
        {
            return Fail("NO_VALIDATOR", "Target validator is missing.", sourceCard);
        }

        ICardEffect effect = dependencyResolver.ResolveEffect(sourceCard.SourceCard);
        if (effect == null)
        {
            return Fail("NO_EFFECT", "Card effect is missing.", sourceCard);
        }

        IBoardStateReader boardReader = dependencyResolver.ResolveBoardReader();
        CardValidationContext validationContext = new CardValidationContext
        {
            ActingPlayerKey = resolvedActor,
            OpponentPlayerKey = opponentPlayerId,
            Board = boardReader
        };

        CardValidationResult validationResult = validator.Validate(validationContext, sourceCard, target);
        if (!validationResult.IsValid)
        {
            return CardPlayResult.Failure(
                validationResult.ReasonCode,
                validationResult.Message,
                validationResult,
                CardEffectResult.Success(),
                finalZone: sourceCard.CurrentZone);
        }

        return CardPlayResult.Success(
            validationResult,
            CardEffectResult.Success("Card can be played."),
            finalZone: sourceCard.CurrentZone,
            message: "Card can be played.");
    }

    public CardPlayResult PlayCard(CardRuntimeState sourceCard, string actingPlayerId, CardTarget target)
    {
        if (playerKeyResolver == null || dependencyResolver == null)
        {
            InitializeResolvers();
        }

        CardPlayResult canPlay = CanPlayCard(sourceCard, actingPlayerId, target);
        if (!canPlay.Succeeded)
        {
            return canPlay;
        }

        if (!playerKeyResolver.TryResolveActorAndOpponent(actingPlayerId, out string resolvedActor, out string opponentPlayerId))
        {
            return Fail("NO_ACTOR", "Acting player id is missing or invalid.", sourceCard);
        }

        ICardStateWriter resolvedWriter = dependencyResolver.ResolveWriter();
        if (resolvedWriter == null)
        {
            return Fail("NO_WRITER", "State writer is missing.", sourceCard);
        }

        CardPlayRequest request = new CardPlayRequest
        {
            ActingPlayerId = resolvedActor,
            OpponentPlayerId = opponentPlayerId,
            SourceCard = sourceCard,
            Target = target,
            Board = dependencyResolver.ResolveBoardReader(),
            Writer = resolvedWriter,
            Validator = dependencyResolver.ResolveValidator(sourceCard.SourceCard),
            Effect = dependencyResolver.ResolveEffect(sourceCard.SourceCard)
        };

        CardPlayResult result = pipeline.Play(request);
        if (!result.Succeeded)
        {
            return result;
        }

        PlayerState actingPlayer = playerKeyResolver.ResolveActingPlayerState(resolvedActor);
        if (actingPlayer != null && actingPlayer.handCards != null && actingPlayer.handCards.Remove(sourceCard))
        {
            actingPlayer.handCount = actingPlayer.handCards.Count;
        }

        return result;
    }

    private static CardPlayResult Fail(string reasonCode, string message, CardRuntimeState sourceCard)
    {
        return CardPlayResult.Failure(
            reasonCode,
            message,
            CardValidationResult.Valid(),
            CardEffectResult.Success(),
            finalZone: sourceCard != null ? sourceCard.CurrentZone : CardZone.Hand);
    }

    private void InitializeResolvers()
    {
        playerKeyResolver = new PlayerKeyResolver(gameManager);
        dependencyResolver = new CardPlayDependencyResolver(gameManager, boardSource, writer);
    }
}
