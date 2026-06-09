using System.Collections.Generic;
using FortGame.UI;
using UnityEngine;
using UnityEngine.UI;

public sealed class RevivalManager : MonoBehaviour
{
    public static RevivalManager Instance { get; private set; }

    private readonly Revival revival = new Revival();
    private readonly List<CardUI> choiceCards = new List<CardUI>();

    private GameManager gameManager;
    private HandUI handUI;
    private HUDManager hudManager;
    private CardPlayService cardPlayService;
    private CardSelectionManager cardSelectionManager;
    private TargetSelectionManager targetSelectionManager;

    private CardRuntimeState pendingSpellCard;
    private CardRuntimeState pendingRevivedCard;
    private CardUI placementCardUi;
    private string pendingActingPlayerKey;

    public bool HasPendingSession => pendingSpellCard != null || pendingRevivedCard != null || choiceCards.Count > 0 || placementCardUi != null;

    public static RevivalManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RevivalManager manager = FindFirstObjectByType<RevivalManager>();
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("RevivalManager");
        return managerObject.AddComponent<RevivalManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureReferences();
    }

    private void Update()
    {
        if (!HasPendingSession)
        {
            return;
        }

        EnsureReferences();
        if (gameManager == null || gameManager.currentPhase != GamePhase.Play)
        {
            CancelSession(showMessage: false);
        }
    }

    public bool TryBeginFromHand(CardRuntimeState spellCard)
    {
        if (!revival.IsMatch(spellCard))
        {
            return false;
        }

        EnsureReferences();
        if (gameManager == null || handUI == null || cardPlayService == null)
        {
            hudManager?.ShowError("Revival is not ready.");
            return true;
        }

        PlayerState actingPlayer = gameManager.currentPlayer;
        if (actingPlayer == null || actingPlayer.handCards == null || !actingPlayer.handCards.Contains(spellCard))
        {
            hudManager?.ShowError("Revival must be played from the current player's hand.");
            return true;
        }

        pendingActingPlayerKey = ResolveCurrentPlayerKey();
        if (string.IsNullOrWhiteSpace(pendingActingPlayerKey))
        {
            hudManager?.ShowError("Revival could not resolve the current player.");
            return true;
        }

        List<CharacterCardData> choices = DeathHistoryManager.GetOrCreate().GetRecentCharacterChoices(revival.GetLookbackTurns(spellCard));
        CardEffectResult validation = revival.ValidateChoiceWindow(spellCard, choices);
        if (!validation.Succeeded)
        {
            hudManager?.ShowError(validation.Message);
            return true;
        }

        CancelSession(showMessage: false);
        pendingSpellCard = spellCard;

        cardSelectionManager?.ClearSelection();
        targetSelectionManager?.OnSelectionCancelled();

        CreateChoiceCards(choices);
        hudManager?.ShowInfo("Choose one recently defeated character to revive.");
        return true;
    }

    public bool IsAwaitingPlacementFor(CardRuntimeState runtimeCard)
    {
        return pendingRevivedCard != null && ReferenceEquals(pendingRevivedCard, runtimeCard);
    }

    public bool BlocksCardSelection(CardUI cardUi)
    {
        if (!HasPendingSession || cardUi == null)
        {
            return false;
        }

        if (choiceCards.Contains(cardUi))
        {
            return false;
        }

        return cardUi != placementCardUi;
    }

    public bool TryResolvePlacement(CardTarget target)
    {
        if (pendingRevivedCard == null || string.IsNullOrWhiteSpace(pendingActingPlayerKey))
        {
            return false;
        }

        EnsureReferences();
        CardPlayResult result = cardPlayService.PlayCard(pendingRevivedCard, pendingActingPlayerKey, target);
        if (!result.Succeeded)
        {
            hudManager?.ShowError(result.Message);
            return true;
        }

        CharacterCardData revivedCharacter = pendingRevivedCard.SourceCard as CharacterCardData;
        ConsumeRevivalSpell();
        cardSelectionManager?.ClearSelection();
        targetSelectionManager?.OnSelectionCancelled();
        ClearTemporaryCards();
        hudManager?.ClearFeedback();
        gameManager?.RefreshHUD();
        hudManager?.ShowSpellAnnouncement($"Revival cast: {revivedCharacter?.DisplayName ?? "Character"} revived!");
        pendingRevivedCard = null;
        pendingActingPlayerKey = string.Empty;
        pendingSpellCard = null;
        return true;
    }

    public void CancelSession(bool showMessage = true)
    {
        if (!HasPendingSession)
        {
            return;
        }

        cardSelectionManager?.ClearSelection();
        targetSelectionManager?.OnSelectionCancelled();
        ClearTemporaryCards();
        pendingSpellCard = null;
        pendingRevivedCard = null;
        pendingActingPlayerKey = string.Empty;

        if (showMessage)
        {
            hudManager?.ShowInfo("Revival cancelled.");
        }
    }

    private void CreateChoiceCards(List<CharacterCardData> choices)
    {
        if (choices == null || handUI == null)
        {
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            CharacterCardData choice = choices[i];
            if (choice == null)
            {
                continue;
            }

            CardUI choiceCardUi = CreatePreviewCard(CardFactory.CreateRuntimeState(choice));
            if (choiceCardUi == null)
            {
                continue;
            }

            choiceCardUi.clickOverride = _ => SelectRevivalChoice(choice);
            choiceCards.Add(choiceCardUi);
        }
    }

    private void SelectRevivalChoice(CharacterCardData chosenCard)
    {
        if (chosenCard == null)
        {
            return;
        }

        ClearChoiceCards();
        pendingRevivedCard = CardFactory.CreateRuntimeState(chosenCard);
        placementCardUi = CreatePreviewCard(pendingRevivedCard);
        if (placementCardUi == null)
        {
            hudManager?.ShowError("Could not prepare the revived character preview.");
            CancelSession(showMessage: false);
            return;
        }

        cardSelectionManager?.TrySelectCard(placementCardUi);
        targetSelectionManager?.ShowValidTargets(placementCardUi);
        hudManager?.ShowInfo($"Choose where to place {chosenCard.DisplayName}.");
    }

    private void ConsumeRevivalSpell()
    {
        if (pendingSpellCard == null || gameManager == null)
        {
            return;
        }

        pendingSpellCard.MoveToZone(CardZone.Discard);

        PlayerState actingPlayer = gameManager.currentPlayer;
        if (actingPlayer != null && actingPlayer.handCards != null && actingPlayer.handCards.Remove(pendingSpellCard))
        {
            actingPlayer.handCount = actingPlayer.handCards.Count;
        }

        handUI?.RemoveCardFromHand(pendingSpellCard);
    }

    private void ClearChoiceCards()
    {
        for (int i = 0; i < choiceCards.Count; i++)
        {
            CardUI choiceCard = choiceCards[i];
            if (choiceCard != null)
            {
                Destroy(choiceCard.gameObject);
            }
        }

        choiceCards.Clear();
    }

    private void ClearTemporaryCards()
    {
        ClearChoiceCards();

        if (placementCardUi != null)
        {
            Destroy(placementCardUi.gameObject);
            placementCardUi = null;
        }
    }

    private CardUI CreatePreviewCard(CardRuntimeState runtimeCard)
    {
        if (runtimeCard == null || handUI == null || handUI.cardPrefab == null || handUI.handContainer == null)
        {
            return null;
        }

        GameObject previewObject = Instantiate(handUI.cardPrefab, handUI.handContainer);
        CardUI cardUi = previewObject.GetComponent<CardUI>();
        if (cardUi == null)
        {
            Destroy(previewObject);
            return null;
        }

        cardUi.runtimeCard = runtimeCard;
        if (cardUi.cardNameText != null)
        {
            cardUi.cardNameText.text = runtimeCard.SourceCard.DisplayName;
        }

        if (cardUi.costText != null)
        {
            cardUi.costText.text = string.Empty;
        }

        Image image = cardUi.GetComponent<Image>();
        if (image != null && runtimeCard.SourceCard.handDeckSprite != null)
        {
            image.sprite = runtimeCard.SourceCard.handDeckSprite;
        }

        return cardUi;
    }

    private string ResolveCurrentPlayerKey()
    {
        if (gameManager == null || gameManager.currentPlayer == null)
        {
            return string.Empty;
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player1))
        {
            return PlayerKeyResolver.PlayerOneKey;
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
        {
            return PlayerKeyResolver.PlayerTwoKey;
        }

        return gameManager.currentPlayer.playerName;
    }

    private void EnsureReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (handUI == null && gameManager != null)
        {
            handUI = gameManager.handUI;
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }

        if (cardPlayService == null)
        {
            cardPlayService = FindFirstObjectByType<CardPlayService>();
        }

        if (cardSelectionManager == null)
        {
            cardSelectionManager = CardSelectionManager.Instance ?? FindFirstObjectByType<CardSelectionManager>();
        }

        if (targetSelectionManager == null)
        {
            targetSelectionManager = TargetSelectionManager.Instance ?? FindFirstObjectByType<TargetSelectionManager>();
        }
    }
}
