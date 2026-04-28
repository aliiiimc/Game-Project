using System;
using UnityEngine;

public sealed class CardPlayDependencyResolver
{
    private readonly GameManager gameManager;
    private HexGrid boardSource;
    private GameManagerCardStateWriter writer;
    private readonly ICardTargetValidator fallbackValidator = new FallbackTargetValidator();

    public CardPlayDependencyResolver(GameManager gameManager, HexGrid boardSource, GameManagerCardStateWriter writer)
    {
        this.gameManager = gameManager;
        this.boardSource = boardSource;
        this.writer = writer;
    }

    public IBoardStateReader ResolveBoardReader()
    {
        if (boardSource == null)
        {
            boardSource = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        }

        return boardSource != null ? new FortGame.Computer.HexGridBoardStateReader(boardSource) : null;
    }

    public ICardStateWriter ResolveWriter()
    {
        if (writer != null)
        {
            return writer;
        }

        writer = UnityEngine.Object.FindFirstObjectByType<GameManagerCardStateWriter>();
        if (writer == null && gameManager != null)
        {
            writer = gameManager.GetComponent<GameManagerCardStateWriter>();
            if (writer == null)
            {
                writer = gameManager.gameObject.AddComponent<GameManagerCardStateWriter>();
            }
        }

        return writer;
    }

    public ICardTargetValidator ResolveValidator(CardData cardData)
    {
        string validatorId = cardData != null ? cardData.validatorId : string.Empty;
        ICardTargetValidator mapped = ResolveSceneComponentById<ICardTargetValidator>(validatorId, candidate => candidate.ValidatorId);
        return mapped ?? fallbackValidator;
    }

    public ICardEffect ResolveEffect(CardData cardData)
    {
        string effectId = cardData != null ? cardData.effectId : string.Empty;
        ICardEffect mapped = ResolveSceneComponentById<ICardEffect>(effectId, candidate => candidate.EffectId);
        return mapped ?? FallbackEffectFactory.Create(cardData);
    }

    private static T ResolveSceneComponentById<T>(string id, Func<T, string> getId) where T : class
    {
        if (string.IsNullOrWhiteSpace(id) || id.StartsWith("debug.", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (!(behaviours[i] is T candidate))
            {
                continue;
            }

            string candidateId = getId(candidate);
            if (string.IsNullOrWhiteSpace(candidateId))
            {
                continue;
            }

            if (candidateId.StartsWith("debug.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (candidateId == id)
            {
                return candidate;
            }
        }

        return null;
    }
}
