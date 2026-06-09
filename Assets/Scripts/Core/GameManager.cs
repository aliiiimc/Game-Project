using UnityEngine;
using FortGame.Computer;
using FortGame.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;


public class GameManager : MonoBehaviour  //GameManager gère la logique du jeu

{
    public GameConfig gameConfig; // Config du jeu, voir Config/GameConfig.cs
    public CardLibrary cardLibrary;
    public HUDManager hudManager;
    public ComputerPlayer computerPlayer;

    public PlayerState player1;
    public PlayerState player2;
    public GamePhase currentPhase;
    public PlayerState currentPlayer;
    public bool hasBoughtThisTurn;
    public string winnerName;
    public int discardCardsUsedThisTurn;
    public bool mustDiscardAfterBuy;
    public bool isBuyDecisionPending; // State (when the hand is full) in which the player has to choose between confirming a buy and 
                                      //be forced to discard, or simply cancel the buy 
    public int pendingBuyCost = -1; //Ali: garde le prix choisi quand la main est pleine et que le joueur doit confirmer.
    public CardRuntimeState selectedCardToDiscard;
    public int roundNumber = 1;
    public int turnNumber = 1;

    [Header("Buy Cost Menu UI")]
    public bool autoCreateBuyCostMenu = true;
    public string buyButtonName = "TestBuycard";
    public Transform buyCostMenuParent;
    public float buyCostOptionSpacing = 110f;
    private GameObject buyCostOptionsRoot;

    public HandUI handUI; //HandUI gère l’affichage des cartes dans la main
















    private void Start()
    {
        if (gameConfig == null)
        {
            Debug.LogError("GameConfig is missing!");
            return;
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }

        if (computerPlayer == null)
        {
            computerPlayer = FindFirstObjectByType<ComputerPlayer>();
        }

        SetupGame();
        SetupBuyCostMenu();
    }

    private void SetupGame()
    {
        player1 = new PlayerState();
        player2 = new PlayerState();

        player1.playerName = "Player 1";
        player2.playerName = "Player 2";

        player1.handCards.Clear();
        player2.handCards.Clear();



        winnerName = string.Empty;
        roundNumber = 1;
        turnNumber = 1;

        player1.money = gameConfig.startingMoney;
        player2.money = gameConfig.startingMoney;

        player1.fortHp = gameConfig.startingFortHp;
        player2.fortHp = gameConfig.startingFortHp;

        player1.maxHandSize = gameConfig.maxHandSize;
        player2.maxHandSize = gameConfig.maxHandSize;

        FillStartingHand(player1, gameConfig.startingHandSize); // ajoute les cartes dans la logique handCards
        FillStartingHand(player2, gameConfig.startingHandSize);

        currentPlayer = player1;
        RefreshCurrentPlayerHandUI(); //Sync cards 


        currentPhase = GamePhase.Income;

        Debug.Log("Game started");
        LogStateSummary();

        StartTurn();
    }

    private void StartTurn()
    {
        if (currentPhase == GamePhase.GameOver)
        {
            return;
        }

        if (currentPhase != GamePhase.Income)
        {
            return;
        }

        //Resets
        hasBoughtThisTurn = false;
        discardCardsUsedThisTurn = 0;
        isBuyDecisionPending = false;
        mustDiscardAfterBuy = false;
        pendingBuyCost = -1;
        HideBuyCostOptions();
        ClearSelectedCardToDiscard();
        currentPlayer.money += gameConfig.moneyPerTurn;
        ApplyWheatFieldIncomeForCurrentPlayer();
        UpdateMineVisibilityForBoardViewer();
        ApplyAutomaticWorldEffectAttacksForCurrentPlayer();


        Debug.Log(currentPlayer.playerName + " receives income.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);

        currentPhase = GamePhase.Buy;
        LogStateSummary();

        if (IsComputerTurn())
        {
            // (abdo :) AI turns now buy/refill before Play instead of skipping straight to action execution.
            HandleComputerBuyPhase();
        }
    }





    private void SyncHandCount(PlayerState player)
    {
        if (player == null) { return; }
        player.handCount = player.handCards.Count;
    }



    private bool IsHandFull(PlayerState player)
    {
        if (player == null)
        { return false; }

        return player.handCount >= player.maxHandSize;
    }



    private void FillStartingHand(PlayerState player, int numberOfCards)
    {
        if (player == null)
        { return; }

        for (int i = 0; i < numberOfCards; i++)
        {
            CardRuntimeState card = CreateRandomCardRuntimeState();

            if (card == null)
            {
                Debug.Log("Could not create a starting card.");
                return;
            }

            AddCardToHand(player, card);
        }
    }

    public void BuyCard()
    {
        int defaultCost = gameConfig != null ? gameConfig.buyCost : -1;
        if (!HasCardsAtBuyCost(defaultCost))
        {
            defaultCost = GetLowestAvailableBuyCost();
        }

        BuyCardForCost(defaultCost);
    }

    public void BuyCardForCost2() { BuyCardForCost(2); }
    public void BuyCardForCost3() { BuyCardForCost(3); }
    public void BuyCardForCost4() { BuyCardForCost(4); }
    public void BuyCardForCost5() { BuyCardForCost(5); }
    public void BuyCardForCost6() { BuyCardForCost(6); }

    public void BuyCardForCost(int selectedCost)
    {
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You cannot buy cards right now.");
            return;
        }

        if (isBuyDecisionPending)
        {
            Debug.Log("Resolve buy decision first: confirm or cancel.");
            return;
        }

        HideBuyCostOptions();

        if (hasBoughtThisTurn)
        {
            Debug.Log(currentPlayer.playerName + " already bought a card this turn.");
            return;
        }

        if (selectedCost <= 0)
        {
            Debug.Log("Choose a valid buy cost.");
            return;
        }

        if (currentPlayer.money < selectedCost)
        {
            Debug.Log(currentPlayer.playerName + " does not have enough money to buy a " + selectedCost + " cost card.");
            return;
        }

        if (!HasCardsAtBuyCost(selectedCost))
        {
            Debug.Log("No cards are available at cost " + selectedCost + ".");
            return;
        }

        if (IsHandFull(currentPlayer) && discardCardsUsedThisTurn >= gameConfig.maxDiscardCardsPerTurn)
        {
            Debug.Log(currentPlayer.playerName + " cannot buy because hand is full and no discard is available this turn.");
            return;
        }

        if (IsHandFull(currentPlayer))
        {
            isBuyDecisionPending = true;
            pendingBuyCost = selectedCost;
            Debug.Log(currentPlayer.playerName + " has a full hand. Confirm buy to buy a " + selectedCost + " cost card and be forced to discard, or cancel.");
            return;
        }


        CardRuntimeState boughtCard = CreateRandomCardRuntimeStateForCost(selectedCost);
        if (boughtCard == null)
        {
            Debug.Log("Could not create a bought card.");
            return;
        }

        currentPlayer.money -= selectedCost;
        AddCardToHand(currentPlayer, boughtCard);

        hasBoughtThisTurn = true;

        string cardName = boughtCard.SourceCard != null ? boughtCard.SourceCard.DisplayName : "Unknown Card";
        Debug.Log(currentPlayer.playerName + " bought " + cardName + " for " + selectedCost + ".");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);
        Debug.Log(currentPlayer.playerName + " hand count is now: " + currentPlayer.handCount);

        currentPhase = GamePhase.Play;
        LogStateSummary();
    }
    public void DiscardCard()
    {
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You can only discard during Buy phase.");
            return;
        }

        if (isBuyDecisionPending)
        {
            Debug.Log("Resolve buy decision first: confirm or cancel.");
            return;
        }


        if (discardCardsUsedThisTurn >= gameConfig.maxDiscardCardsPerTurn)
        {
            Debug.Log(currentPlayer.playerName + " already used the maximum number of discards this turn.");
            return;
        }

        if (currentPlayer.handCount <= 0)
        {
            Debug.Log(currentPlayer.playerName + " has no cards to discard.");
            return;
        }

        if (selectedCardToDiscard == null)  // To make sure the player selected a card to discard 
        {
            Debug.Log("Select a card to discard first.");
            return;
        }

        if (!currentPlayer.handCards.Contains(selectedCardToDiscard)) // To make sure the player selected a card he has to discard
        {
            Debug.Log("The selected card is not in the current player's hand.");
            return;
        }


        RemoveCardFromHand(currentPlayer, selectedCardToDiscard);

        if (handUI != null)//nettoie la logique
        {
            handUI.RemoveCardFromHand(selectedCardToDiscard);
        }
        if (handUI != null)//nettoie le visuel
        {
            handUI.ClearVisualSelection();
        }


        currentPlayer.discardCount += 1;
        currentPlayer.money += gameConfig.discardMoneyReward;
        discardCardsUsedThisTurn += 1;
        ClearSelectedCardToDiscard();

        Debug.Log(currentPlayer.playerName + " discarded a card and gained " + gameConfig.discardMoneyReward + " money.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);
        Debug.Log(currentPlayer.playerName + " hand count is now: " + currentPlayer.handCount);


        if (mustDiscardAfterBuy && currentPlayer.handCount <= currentPlayer.maxHandSize)
        {
            mustDiscardAfterBuy = false;
            currentPhase = GamePhase.Play;
            Debug.Log(currentPlayer.playerName + " finished the required discard after buying and can now go to Play phase. ");
        }

        LogStateSummary();
    }



    public void SelectCardToDiscard(CardRuntimeState card)
    {
        if (card == null)
        {
            Debug.Log("No card was selected for discard.");
            return;
        }
        if (currentPlayer == null)
        {
            Debug.Log("There is no current player.");
            return;
        }

        if (!currentPlayer.handCards.Contains(card))
        {
            Debug.Log("You can only select a card from the current player's hand.");
            return;
        }

        selectedCardToDiscard = card;
        Debug.Log("Selected a card to discard.");
    }


    public void ClearSelectedCardToDiscard()
    {
        selectedCardToDiscard = null;
    }


    public void ConfirmBuyWithFullHand()
    {
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You can only confirm a full-hand buy during Buy phase.");
            return;
        }

        if (!isBuyDecisionPending)
        {
            Debug.Log("No buy decision is pending.");
            return;
        }

        if (discardCardsUsedThisTurn >= gameConfig.maxDiscardCardsPerTurn)
        {
            Debug.Log(currentPlayer.playerName + " cannot confirm buy because no discard is available this turn, you are forced to cancel the buy");
            return;
        }

        if (pendingBuyCost <= 0)
        {
            Debug.Log("No buy cost was selected.");
            isBuyDecisionPending = false;
            return;
        }

        if (currentPlayer.money < pendingBuyCost)
        {
            Debug.Log(currentPlayer.playerName + " cannot buy because insufficient funds.");
            isBuyDecisionPending = false;
            pendingBuyCost = -1;
            return;
        }

        CardRuntimeState boughtCard = CreateRandomCardRuntimeStateForCost(pendingBuyCost);

        if (boughtCard == null)
        {
            Debug.Log("Could not create a bought card.");
            isBuyDecisionPending = false;
            pendingBuyCost = -1;
            return;
        }

        currentPlayer.money -= pendingBuyCost;
        AddCardToHand(currentPlayer, boughtCard);

        hasBoughtThisTurn = true;
        isBuyDecisionPending = false;
        pendingBuyCost = -1;
        mustDiscardAfterBuy = true;

        Debug.Log(currentPlayer.playerName + " confirmed the buy with a full hand and must now discard one card.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);
        Debug.Log(currentPlayer.playerName + " hand count is now: " + currentPlayer.handCount);

        LogStateSummary();
    }

    public void CancelBuyDecision()
    {
        if (!isBuyDecisionPending)
        {
            Debug.Log("No buy decision is pending.");
            return;
        }
        isBuyDecisionPending = false;
        pendingBuyCost = -1;
        Debug.Log(currentPlayer.playerName + " canceled the buy and stayed in Buy phase.");
        LogStateSummary();

    }


    private void RefreshCurrentPlayerHandUI() // Sync le visuel des cartes quand changement de joueur 
    {
        if (handUI == null || currentPlayer == null)
        {
            return;
        }

        bool showHand = !IsComputerTurn();
        handUI.gameObject.SetActive(showHand);
        handUI.ClearHand();

        if (!showHand)
        {
            return;
        }

        for (int i = 0; i < currentPlayer.handCards.Count; i++)
        {
            handUI.AddCardToHand(currentPlayer.handCards[i]);
        }
    }











    private void AddCardToHand(PlayerState player, CardRuntimeState card)
    {
        if (player == null || card == null)
        { return; }

        player.handCards.Add(card);//Logique 
        SyncHandCount(player);
        if (player == currentPlayer && handUI != null && !IsComputerTurn())
        {
            handUI.AddCardToHand(card);//Visuel 
        }

    }

    private void RemoveCardFromHand(PlayerState player, CardRuntimeState card)
    {
        if (player == null || card == null)
        { return; }

        player.handCards.Remove(card);
        SyncHandCount(player);
    }

    private CardData GetRandomCardFromLibrary()
    {
        if (cardLibrary == null)
        {
            Debug.Log("Card Library is missing.");
            return null;
        }

        if (cardLibrary.cards == null || cardLibrary.cards.Count == 0)
        {
            Debug.Log("Card Library is empty.");
            return null;
        }

        int randomIndex = Random.Range(0, cardLibrary.cards.Count);
        return cardLibrary.cards[randomIndex];
    }


    private CardRuntimeState CreateRandomCardRuntimeState()
    {
        CardData randomCard = GetRandomCardFromLibrary();

        if (randomCard == null)
        {
            return null;
        }

        return CardFactory.CreateRuntimeState(randomCard);
    }

    private CardRuntimeState CreateRandomCardRuntimeStateForCost(int selectedCost)
    {
        //Ali: ici le random reste, mais seulement parmi les cartes qui ont exactement ce prix.
        CardData randomCard = GetRandomCardFromLibraryByCost(selectedCost, false);

        if (randomCard == null)
        {
            return null;
        }

        return CardFactory.CreateRuntimeState(randomCard);
    }

    private CardRuntimeState CreateRandomCharacterCardRuntimeState()
    {
        // (abdo :) Used by AI buying when it needs real character cards to keep board pressure.
        if (cardLibrary == null || cardLibrary.cards == null || cardLibrary.cards.Count == 0)
        {
            return null;
        }

        List<CardData> characterCards = new List<CardData>();
        for (int i = 0; i < cardLibrary.cards.Count; i++)
        {
            CardData card = cardLibrary.cards[i];
            if (card is CharacterCardData)
            {
                characterCards.Add(card);
            }
        }

        if (characterCards.Count == 0)
        {
            return null;
        }

        CardData randomCharacterCard = characterCards[Random.Range(0, characterCards.Count)];
        return CardFactory.CreateRuntimeState(randomCharacterCard);
    }

    private CardRuntimeState CreateRandomCharacterCardRuntimeStateForCost(int selectedCost)
    {
        CardData randomCharacterCard = GetRandomCardFromLibraryByCost(selectedCost, true);

        if (randomCharacterCard == null)
        {
            return null;
        }

        return CardFactory.CreateRuntimeState(randomCharacterCard);
    }

    private CardData GetRandomCardFromLibraryByCost(int selectedCost, bool characterOnly)
    {
        if (cardLibrary == null || cardLibrary.cards == null || cardLibrary.cards.Count == 0)
        {
            Debug.Log("Card Library is missing or empty.");
            return null;
        }

        List<CardData> matchingCards = new List<CardData>();
        for (int i = 0; i < cardLibrary.cards.Count; i++)
        {
            CardData card = cardLibrary.cards[i];
            if (card == null || card.cost != selectedCost)
            {
                continue;
            }

            if (characterOnly && !(card is CharacterCardData))
            {
                continue;
            }

            matchingCards.Add(card);
        }

        if (matchingCards.Count == 0)
        {
            return null;
        }

        return matchingCards[Random.Range(0, matchingCards.Count)];
    }

    private bool HasCardsAtBuyCost(int selectedCost)
    {
        return GetRandomCardFromLibraryByCost(selectedCost, false) != null;
    }

    private List<int> GetAvailableBuyCosts(bool characterOnly)
    {
        List<int> costs = new List<int>();

        if (cardLibrary == null || cardLibrary.cards == null)
        {
            return costs;
        }

        for (int i = 0; i < cardLibrary.cards.Count; i++)
        {
            CardData card = cardLibrary.cards[i];
            if (card == null || card.cost <= 0)
            {
                continue;
            }

            if (characterOnly && !(card is CharacterCardData))
            {
                continue;
            }

            if (!costs.Contains(card.cost))
            {
                costs.Add(card.cost);
            }
        }

        costs.Sort();
        return costs;
    }

    private int ChooseComputerBuyCost(bool preferCharacter)
    {
        int characterCost = preferCharacter ? ChooseHighestAffordableBuyCost(true) : -1;
        if (characterCost > 0)
        {
            return characterCost;
        }

        return ChooseHighestAffordableBuyCost(false);
    }

    private int ChooseHighestAffordableBuyCost(bool characterOnly)
    {
        //Ali: l'IA prend le meilleur tier qu'elle peut payer et qui contient au moins une carte.
        List<int> costs = GetAvailableBuyCosts(characterOnly);
        for (int i = costs.Count - 1; i >= 0; i--)
        {
            if (currentPlayer != null && currentPlayer.money >= costs[i])
            {
                return costs[i];
            }
        }

        return -1;
    }

    private int GetLowestAvailableBuyCost()
    {
        List<int> costs = GetAvailableBuyCosts(false);
        return costs.Count > 0 ? costs[0] : -1;
    }

    private void SetupBuyCostMenu()
    {
        if (!autoCreateBuyCostMenu)
        {
            return;
        }

        Button buyButton = FindButtonByObjectName(buyButtonName);
        if (buyButton == null)
        {
            Debug.Log("Buy button was not found: " + buyButtonName);
            return;
        }

        SetButtonText(buyButton, "Buy");
        CreateBuyCostOptions(buyButton);
        HideBuyCostOptions();
    }

    private void CreateBuyCostOptions(Button buyButton)
    {
        List<int> costs = GetAvailableBuyCosts(false);
        if (costs.Count == 0)
        {
            return;
        }

        RectTransform buyButtonRect = buyButton.GetComponent<RectTransform>();
        Transform parent = buyCostMenuParent != null ? buyCostMenuParent : buyButton.transform.parent;

        buyCostOptionsRoot = new GameObject("BuyCostOptions");
        RectTransform rootRect = buyCostOptionsRoot.AddComponent<RectTransform>();
        buyCostOptionsRoot.transform.SetParent(parent, false);

        if (buyButtonRect != null)
        {
            rootRect.anchorMin = buyButtonRect.anchorMin;
            rootRect.anchorMax = buyButtonRect.anchorMax;
            rootRect.pivot = buyButtonRect.pivot;
            rootRect.anchoredPosition = buyButtonRect.anchoredPosition + new Vector2(0f, 90f);
            rootRect.sizeDelta = new Vector2(costs.Count * buyCostOptionSpacing, 50f);
        }

        for (int i = 0; i < costs.Count; i++)
        {
            CreateBuyCostOptionButton(buyButton, costs[i], i);
        }
    }

    private void CreateBuyCostOptionButton(Button buyButton, int selectedCost, int index)
    {
        //Ali: ces boutons sont neufs, donc ils ne gardent pas l'ancien OnClick Unity du bouton Buy.
        GameObject buttonObject = new GameObject("BuyCost" + selectedCost + "Option");
        buttonObject.transform.SetParent(buyCostOptionsRoot.transform, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(95f, 44f);
        buttonRect.anchoredPosition = new Vector2(index * buyCostOptionSpacing, 0f);

        Image image = buttonObject.AddComponent<Image>();
        Image buyButtonImage = buyButton.GetComponent<Image>();
        if (buyButtonImage != null)
        {
            image.sprite = buyButtonImage.sprite;
            image.type = buyButtonImage.type;
            image.color = buyButtonImage.color;
        }

        Button optionButton = buttonObject.AddComponent<Button>();
        optionButton.targetGraphic = image;
        optionButton.onClick.AddListener(() => BuyCardForCost(selectedCost));

        GameObject labelObject = new GameObject("Text");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        TextMeshProUGUI templateLabel = buyButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (templateLabel != null)
        {
            label.font = templateLabel.font;
            label.fontMaterial = templateLabel.fontMaterial;
            label.color = templateLabel.color;
            label.fontSize = templateLabel.fontSize;
        }

        label.alignment = TextAlignmentOptions.Center;
        label.text = "$" + selectedCost;
    }

    public void ToggleBuyCostOptions()
    {
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You can only choose a buy cost during Buy phase.");
            return;
        }

        if (isBuyDecisionPending)
        {
            Debug.Log("Resolve buy decision first: confirm or cancel.");
            return;
        }

        if (buyCostOptionsRoot == null)
        {
            Debug.Log("Buy cost options are not ready.");
            return;
        }

        buyCostOptionsRoot.SetActive(!buyCostOptionsRoot.activeSelf);
    }

    private void HideBuyCostOptions()
    {
        if (buyCostOptionsRoot != null)
        {
            buyCostOptionsRoot.SetActive(false);
        }
    }

    private Button FindButtonByObjectName(string objectName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == objectName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void SetButtonText(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

    private void HandleComputerBuyPhase()
    {
        // (abdo :) Keep the computer turn automatic, but give it the same card-refill chance before it starts playing.
        TryComputerBuyCard();

        // Rabie: when player2 becomes current player, start the local computer opponent.
        currentPhase = GamePhase.Play;
        LogStateSummary();
        computerPlayer.StartTurn();
    }

    private bool TryComputerBuyCard()
    {
        // (abdo :) AI buys once per turn and prefers a character if it has low units or no character in hand.
        if (currentPlayer == null || gameConfig == null || hasBoughtThisTurn)
        {
            return false;
        }

        if (IsHandFull(currentPlayer))
        {
            Debug.Log(currentPlayer.playerName + " skipped buy: hand is full.");
            return false;
        }

        string ownerKey = ResolveCurrentOwnerKey();
        int ownedUnitCount = CountUnitsForOwner(ownerKey);
        bool shouldBuyCharacter = ownedUnitCount < 3 || !HasCharacterCardInHand(currentPlayer);
        int selectedCost = ChooseComputerBuyCost(shouldBuyCharacter);

        if (selectedCost <= 0)
        {
            Debug.Log(currentPlayer.playerName + " skipped buy: no affordable cost tier has cards.");
            return false;
        }

        CardRuntimeState boughtCard = shouldBuyCharacter
            ? CreateRandomCharacterCardRuntimeStateForCost(selectedCost)
            : CreateRandomCardRuntimeStateForCost(selectedCost);

        if (boughtCard == null && shouldBuyCharacter)
        {
            boughtCard = CreateRandomCardRuntimeStateForCost(selectedCost);
        }

        if (boughtCard == null)
        {
            Debug.Log(currentPlayer.playerName + " could not buy a card.");
            return false;
        }

        currentPlayer.money -= selectedCost;
        AddCardToHand(currentPlayer, boughtCard);
        hasBoughtThisTurn = true;

        string cardName = boughtCard.SourceCard != null ? boughtCard.SourceCard.DisplayName : "Unknown Card";
        Debug.Log(currentPlayer.playerName + " bought " + cardName + " for " + selectedCost + ".");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);
        Debug.Log(currentPlayer.playerName + " hand count is now: " + currentPlayer.handCount);

        return true;
    }

    private bool HasCharacterCardInHand(PlayerState player)
    {
        if (player == null || player.handCards == null)
        {
            return false;
        }

        for (int i = 0; i < player.handCards.Count; i++)
        {
            CardRuntimeState card = player.handCards[i];
            if (card != null && card.SourceCard is CharacterCardData)
            {
                return true;
            }
        }

        return false;
    }

    private int CountUnitsForOwner(string ownerKey)
    {
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            return 0;
        }

        int unitCount = 0;
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit != null && unit.owner == ownerKey)
            {
                unitCount++;
            }
        }

        return unitCount;
    }

    public void GoToPlayPhase()
    {
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You can only go to Play phase from Buy phase.");
            return;
        }
        if (isBuyDecisionPending == true) { Debug.Log("Resolve buy decision first, then you can play"); return; }
        if (mustDiscardAfterBuy == true) { Debug.Log("You must Discard before leaving Buy phase."); return; }

        currentPhase = GamePhase.Play;
        LogStateSummary();
    }

    public void GoToAttackPhase()
    {
        Debug.Log("Attack phase has been removed. Units can attack during Play phase.");
    }

    public void FinishAttackPhase()
    {
        if (currentPhase != GamePhase.Play)
        {
            Debug.Log("You can only end combat actions during Play phase.");
            return;
        }

        EndTurn();
    }



    public void EndTurn()
    {
        bool canEndFromBuy = currentPhase == GamePhase.Buy && !isBuyDecisionPending && !mustDiscardAfterBuy;
        if (currentPhase != GamePhase.Play && !canEndFromBuy)
        {
            Debug.Log("You can only end turn from Play phase, or from Buy phase if no buy decision is pending.");
            return;
        }

        if (isBuyDecisionPending)
        {
            Debug.Log("Resolve the buy decision first before ending turn.");
            return;
        }

        if (mustDiscardAfterBuy)
        {
            Debug.Log("You must discard a card before ending turn.");
            return;
        }

        if (currentPhase == GamePhase.Buy)
        {
            currentPhase = GamePhase.Play;
        }

        CheckGameOver();
        if (currentPhase == GamePhase.GameOver)
        {
            return;
        }

        currentPhase = GamePhase.End;
        Debug.Log("Ending turn for " + currentPlayer.playerName);
        string endingOwnerKey = ResolveCurrentOwnerKey();
        ConsumeUnitTimedEffectsForOwner(endingOwnerKey);
        SpellManager.GetOrCreate().ConsumePersistentDurations(endingOwnerKey);

        PlayerState previousPlayer = currentPlayer;
        turnNumber += 1;
        currentPlayer = currentPlayer == player1 ? player2 : player1;
        if (ReferenceEquals(previousPlayer, player2) && ReferenceEquals(currentPlayer, player1))
        {
            roundNumber += 1;
        }

        RefreshCurrentPlayerHandUI(); //Sync cards


        Debug.Log("New current player: " + currentPlayer.playerName);
        currentPhase = GamePhase.Income;
        LogStateSummary();

        StartTurn();
    }



    private void CheckGameOver()
    {
        if (player1.fortHp <= 0)
        {
            currentPhase = GamePhase.GameOver;
            winnerName = player2.playerName;
            Debug.Log(winnerName + " wins the game!");
            RefreshHUD();
            return;
        }

        if (player2.fortHp <= 0)
        {
            currentPhase = GamePhase.GameOver;
            winnerName = player1.playerName;
            Debug.Log(winnerName + " wins the game!");
            RefreshHUD();
        }
    }

    public void DamagePlayer1Fort(int damage)
    {
        ApplyFortDamage(player1, damage);
    }

    public void DamagePlayer2Fort(int damage)
    {
        ApplyFortDamage(player2, damage);
    }

    public void HealPlayer1Fort(int amount)
    {
        ApplyFortHeal(player1, amount);
    }

    public void HealPlayer2Fort(int amount)
    {
        ApplyFortHeal(player2, amount);
    }



    public string GetStateSummary()
    {
        string winnerText = string.IsNullOrEmpty(winnerName) ? "None" : winnerName;

        return "Phase=" + currentPhase
            + " | CurrentPlayer=" + currentPlayer.playerName
            + " | HasBought=" + hasBoughtThisTurn
            + " | DiscardCardsUsedThisTurn=" + discardCardsUsedThisTurn
            + " | isBuyDecisionPending=" + isBuyDecisionPending
            + " | pendingBuyCost=" + pendingBuyCost
            + " | mustDiscardAfterBuy=" + mustDiscardAfterBuy
            + " | Winner=" + winnerText
            + " | P1(Money=" + player1.money + ", Hand=" + player1.handCount + ", Discard=" + player1.discardCount + ", Fort=" + player1.fortHp + ")"
            + " | P2(Money=" + player2.money + ", Hand=" + player2.handCount + ", Discard=" + player2.discardCount + ", Fort=" + player2.fortHp + ")";
    }

    public void LogStateSummary()
    {
        RefreshHUD();

        Debug.Log(GetStateSummary());
    }

    public void RefreshHUD()
    {
        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }

        if (hudManager == null || currentPlayer == null || player1 == null || player2 == null)
        {
            return;
        }

        int maxFortHp = gameConfig != null ? gameConfig.startingFortHp : Mathf.Max(player1.fortHp, player2.fortHp);
        hudManager.UpdateHUD(player1, player2, currentPlayer, currentPhase, maxFortHp, roundNumber, winnerName);
    }

    private bool IsComputerTurn()
    {
        return computerPlayer != null && currentPlayer != null && ReferenceEquals(currentPlayer, player2);
    }

    private void ApplyWheatFieldIncomeForCurrentPlayer()
    {
        if (currentPlayer == null)
        {
            return;
        }

        string ownerKey = ResolveCurrentOwnerKey();
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            return;
        }

        HexTile[] allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        if (allTiles == null || allTiles.Length == 0)
        {
            return;
        }

        Dictionary<string, int> ownedFieldClusterBonusById = new Dictionary<string, int>();
        Dictionary<string, int> redirectedFieldClusterBonusByRecipient = new Dictionary<string, int>();
        int unclusteredBonusTotal = 0;
        int redirectedUnclusteredBonusTotal = 0;
        SpellManager spellManager = SpellManager.GetOrCreate();
        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile tile = allTiles[i];
            if (tile == null
                || !tile.HasWorldEffect()
                || !tile.isFieldTile
                || tile.worldEffectOwner != ownerKey)
            {
                continue;
            }

            int tileBonus = Mathf.Max(0, tile.fieldBonusMoneyPerTurn);
            string redirectedRecipientKey = string.Empty;
            bool isRedirected = spellManager != null
                && spellManager.TryGetFieldIncomeRecipient(ownerKey, tile, out redirectedRecipientKey);

            if (!string.IsNullOrWhiteSpace(tile.fieldClusterId))
            {
                if (ownedFieldClusterBonusById.ContainsKey(tile.fieldClusterId))
                {
                    continue;
                }

                ownedFieldClusterBonusById[tile.fieldClusterId] = tileBonus;
                if (isRedirected)
                {
                    if (!redirectedFieldClusterBonusByRecipient.ContainsKey(redirectedRecipientKey))
                    {
                        redirectedFieldClusterBonusByRecipient[redirectedRecipientKey] = 0;
                    }

                    redirectedFieldClusterBonusByRecipient[redirectedRecipientKey] += tileBonus;
                }
            }
            else
            {
                if (isRedirected)
                {
                    redirectedUnclusteredBonusTotal += tileBonus;
                    AddMoneyToOwner(redirectedRecipientKey, tileBonus);
                }
                else
                {
                    unclusteredBonusTotal += tileBonus;
                }
            }
        }

        int clusterBonusTotal = 0;
        foreach (KeyValuePair<string, int> pair in ownedFieldClusterBonusById)
        {
            clusterBonusTotal += Mathf.Max(0, pair.Value);
        }

        foreach (KeyValuePair<string, int> pair in redirectedFieldClusterBonusByRecipient)
        {
            AddMoneyToOwner(pair.Key, pair.Value);
            clusterBonusTotal -= Mathf.Max(0, pair.Value);
        }

        int bonusMoney = Mathf.Max(0, clusterBonusTotal + unclusteredBonusTotal);
        if (bonusMoney <= 0)
        {
            if (redirectedFieldClusterBonusByRecipient.Count > 0 || redirectedUnclusteredBonusTotal > 0)
            {
                Debug.Log($"{currentPlayer.playerName} loses field income to Tax collection.");
            }

            return;
        }

        currentPlayer.money += bonusMoney;
        Debug.Log($"{currentPlayer.playerName} gains +{bonusMoney} from Wheat field.");
    }

    public void UpdateMineVisibilityForBoardViewer()
    {
        string visibleOwnerKey = ResolveVisibleMineOwnerKey();
        if (string.IsNullOrWhiteSpace(visibleOwnerKey))
        {
            return;
        }

        HexTile[] allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        if (allTiles == null || allTiles.Length == 0)
        {
            return;
        }

        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile tile = allTiles[i];
            if (tile == null || !tile.HasWorldEffect() || !tile.isMineTile)
            {
                continue;
            }

            bool isVisibleToViewer = tile.worldEffectOwner == visibleOwnerKey;
            tile.SetMineVisibility(isVisibleToViewer);
        }
    }

    public void NotifyEnemyMinefieldPlaced(int placedMineCount)
    {
        if (placedMineCount <= 0 || computerPlayer == null)
        {
            return;
        }

        Debug.LogWarning("[SpecialTrigger][Mines] Warning: enemy mines may have been placed in their territory.");
    }

    private void ApplyAutomaticWorldEffectAttacksForCurrentPlayer()
    {
        string ownerKey = ResolveCurrentOwnerKey();
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            return;
        }

        HexGrid grid = FindFirstObjectByType<HexGrid>();
        WorldEffectManager worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        if (grid == null || worldEffectManager == null)
        {
            return;
        }

        Hospital hospital = new Hospital();
        int totalHealing = hospital.ApplyAutomaticHealing(ownerKey);
        if (totalHealing > 0)
        {
            Debug.Log($"[SpecialTrigger][Hospital] Applied {totalHealing} total healing for '{ownerKey}'.");
        }

        WatchTower watchTower = new WatchTower();
        int hits = watchTower.ApplyAutomaticAttacks(ownerKey, grid, worldEffectManager);
        if (hits > 0)
        {
            Debug.Log($"[SpecialTrigger][WatchTower] Applied {hits} automatic watch-tower hit(s) against '{ownerKey}'.");
        }

        AntiAirTower antiAirTower = new AntiAirTower();
        int antiAirHits = antiAirTower.ApplyAutomaticAttacks(ownerKey, grid, worldEffectManager);
        if (antiAirHits > 0)
        {
            Debug.Log($"[SpecialTrigger][AntiAirTower] Applied {antiAirHits} automatic anti-air hit(s) against '{ownerKey}'.");
        }
    }

    private string ResolveCurrentOwnerKey()
    {
        if (currentPlayer == null)
        {
            return string.Empty;
        }

        if (ReferenceEquals(currentPlayer, player2))
        {
            return PlayerKeyResolver.PlayerTwoKey;
        }

        if (ReferenceEquals(currentPlayer, player1))
        {
            return PlayerKeyResolver.PlayerOneKey;
        }

        return string.Empty;
    }

    private string ResolveVisibleMineOwnerKey()
    {
        if (computerPlayer != null)
        {
            return PlayerKeyResolver.PlayerOneKey;
        }

        return ResolveCurrentOwnerKey();
    }

    private void AddMoneyToOwner(string ownerKey, int amount)
    {
        if (string.IsNullOrWhiteSpace(ownerKey) || amount <= 0)
        {
            return;
        }

        if (ownerKey == PlayerKeyResolver.PlayerOneKey)
        {
            player1.money += amount;
            return;
        }

        if (ownerKey == PlayerKeyResolver.PlayerTwoKey)
        {
            player2.money += amount;
        }
    }

    private void ApplyFortDamage(PlayerState targetPlayer, int damage)
    {
        if (damage <= 0)
        {
            Debug.Log("Damage must be greater than zero.");
            return;
        }

        if (currentPhase == GamePhase.GameOver)
        {
            Debug.Log("Game is already over. Fort damage ignored.");
            return;
        }

        targetPlayer.fortHp = Mathf.Max(0, targetPlayer.fortHp - damage);
        Debug.Log(targetPlayer.playerName + " fort HP is now: " + targetPlayer.fortHp);

        CheckGameOver();
        LogStateSummary();
    }

    private void ApplyFortHeal(PlayerState targetPlayer, int amount)
    {
        if (amount <= 0)
        {
            Debug.Log("Heal amount must be greater than zero.");
            return;
        }

        if (currentPhase == GamePhase.GameOver)
        {
            Debug.Log("Game is already over. Fort heal ignored.");
            return;
        }

        targetPlayer.fortHp += amount;
        Debug.Log(targetPlayer.playerName + " fort HP is now: " + targetPlayer.fortHp);

        LogStateSummary();
    }

    private void ConsumeUnitTimedEffectsForOwner(string ownerKey)
    {
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            return;
        }

        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        if (allUnits == null || allUnits.Length == 0)
        {
            return;
        }

        for (int i = 0; i < allUnits.Length; i++)
        {
            Unit unit = allUnits[i];
            if (unit == null || unit.owner != ownerKey)
            {
                continue;
            }

            unit.ConsumeTimedEffectsOnOwnerTurnEnd();
        }
    }



























    public void TestDiscardCard()
    {
        DiscardCard();
    }
    public void TestEndTurn()
    { EndTurn(); }

    public void TestPlayer2Lose()
    {
        player2.fortHp = 0;
        Debug.Log(player2.playerName + " fort HP is now: " + player2.fortHp);
        CheckGameOver();
        LogStateSummary();
    }

    public void TestBuyCard()
    { ToggleBuyCostOptions(); }
}
