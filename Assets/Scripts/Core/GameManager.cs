using UnityEngine;
using FortGame.UI;


public class GameManager : MonoBehaviour  //GameManager gère la logique du jeu

{
    public GameConfig gameConfig; // Config du jeu, voir Config/GameConfig.cs
    public CardLibrary cardLibrary;

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
    public CardRuntimeState selectedCardToDiscard;

    public HandUI handUI; //HandUI gère l’affichage des cartes dans la main
















    private void Start()
    {
        if (gameConfig == null)
        {
            Debug.LogError("GameConfig is missing!");
            return;
        }

        SetupGame();
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
        ClearSelectedCardToDiscard();
        currentPlayer.money += gameConfig.moneyPerTurn;


        Debug.Log(currentPlayer.playerName + " receives income.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);

        currentPhase = GamePhase.Buy;
        LogStateSummary();
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
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You cannot buy cards right now.");
            return;
        }

        if (hasBoughtThisTurn)
        {
            Debug.Log(currentPlayer.playerName + " already bought a card this turn.");
            return;
        }

        if (currentPlayer.money < gameConfig.buyCost)
        {
            Debug.Log(currentPlayer.playerName + " does not have enough money to buy a card.");
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
            Debug.Log(currentPlayer.playerName + " has a full hand. Confirm buy to buy anyway and be forced to discard, or cancel.");
            return;
        }


        currentPlayer.money -= gameConfig.buyCost;


        CardRuntimeState boughtCard = CreateRandomCardRuntimeState();
        if (boughtCard == null)
        {
            Debug.Log("Could not create a bought card.");
            return;
        }

        AddCardToHand(currentPlayer, boughtCard);

        hasBoughtThisTurn = true;

        Debug.Log(currentPlayer.playerName + " bought a card.");
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

        if (currentPlayer.money < gameConfig.buyCost)
        {
            Debug.Log(currentPlayer.playerName + " cannot buy because insufficient funds.");
            isBuyDecisionPending = false;
            return;
        }

        currentPlayer.money -= gameConfig.buyCost;
        CardRuntimeState boughtCard = CreateRandomCardRuntimeState();

        if (boughtCard == null)
        {
            Debug.Log("Could not create a bought card.");
            isBuyDecisionPending = false;
            return;
        }

        AddCardToHand(currentPlayer, boughtCard);

        hasBoughtThisTurn = true;
        isBuyDecisionPending = false;
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
        Debug.Log(currentPlayer.playerName + " canceled the buy and stayed in Buy phase.");
        LogStateSummary();

    }


    private void RefreshCurrentPlayerHandUI() // Sync le visuel des cartes quand changement de joueur 
    {
        if (handUI == null || currentPlayer == null)
        {
            return;
        }

        handUI.ClearHand();

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
        if (player == currentPlayer && handUI != null)
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
        if (currentPhase != GamePhase.Play)
        {
            Debug.Log("You can only go to Attack phase from Play phase.");
            return;
        }

        currentPhase = GamePhase.Attack;
        LogStateSummary();
    }

    public void FinishAttackPhase()
    {
        if (currentPhase != GamePhase.Attack)
        {
            Debug.Log("You can only finish attack during Attack phase.");
            return;
        }

        EndTurn();
    }



    public void EndTurn()
    {
        if (currentPhase != GamePhase.Play && currentPhase != GamePhase.Attack)
        {
            Debug.Log("You can only end turn from Play or Attack phase.");
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

        CheckGameOver();
        if (currentPhase == GamePhase.GameOver)
        {
            return;
        }

        currentPhase = GamePhase.End;
        Debug.Log("Ending turn for " + currentPlayer.playerName);

        currentPlayer = currentPlayer == player1 ? player2 : player1;
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
            return;
        }

        if (player2.fortHp <= 0)
        {
            currentPhase = GamePhase.GameOver;
            winnerName = player1.playerName;
            Debug.Log(winnerName + " wins the game!");
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

    public string GetStateSummary()
    {
        string winnerText = string.IsNullOrEmpty(winnerName) ? "None" : winnerName;

        return "Phase=" + currentPhase
            + " | CurrentPlayer=" + currentPlayer.playerName
            + " | HasBought=" + hasBoughtThisTurn
            + " | DiscardCardsUsedThisTurn=" + discardCardsUsedThisTurn
            + " | isBuyDecisionPending=" + isBuyDecisionPending
            + " | mustDiscardAfterBuy=" + mustDiscardAfterBuy
            + " | Winner=" + winnerText
            + " | P1(Money=" + player1.money + ", Hand=" + player1.handCount + ", Discard=" + player1.discardCount + ", Fort=" + player1.fortHp + ")"
            + " | P2(Money=" + player2.money + ", Hand=" + player2.handCount + ", Discard=" + player2.discardCount + ", Fort=" + player2.fortHp + ")";
    }

    public void LogStateSummary()
    {
        Debug.Log(GetStateSummary());
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

        targetPlayer.fortHp -= damage;
        Debug.Log(targetPlayer.playerName + " fort HP is now: " + targetPlayer.fortHp);

        CheckGameOver();
        LogStateSummary();
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
    { BuyCard(); }
}
