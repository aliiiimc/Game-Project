using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameConfig gameConfig;

    public PlayerState player1;
    public PlayerState player2;
    public GamePhase currentPhase;
    public PlayerState currentPlayer;
    public bool hasBoughtThisTurn;
    public string winnerName;

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

        currentPlayer = player1;
        winnerName = string.Empty;

        player1.money = gameConfig.startingMoney;
        player2.money = gameConfig.startingMoney;

        player1.fortHp = gameConfig.startingFortHp;
        player2.fortHp = gameConfig.startingFortHp;

        player1.maxHandSize = gameConfig.maxHandSize;
        player2.maxHandSize = gameConfig.maxHandSize;

        player1.handCount = gameConfig.startingHandSize;
        player2.handCount = gameConfig.startingHandSize;

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

        hasBoughtThisTurn = false;
        currentPlayer.money += gameConfig.moneyPerTurn;

        Debug.Log(currentPlayer.playerName + " receives income.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);

        currentPhase = GamePhase.Buy;
        LogStateSummary();
    }

    public void EndTurn()
    {
        CheckGameOver();
        if (currentPhase == GamePhase.GameOver)
        {
            return;
        }

        currentPhase = GamePhase.End;
        Debug.Log("Ending turn for " + currentPlayer.playerName);

        currentPlayer = currentPlayer == player1 ? player2 : player1;

        Debug.Log("New current player: " + currentPlayer.playerName);
        currentPhase = GamePhase.Income;
        LogStateSummary();

        StartTurn();
    }

    public void TestEndTurn()
    {
        EndTurn();
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

        if (currentPlayer.handCount >= currentPlayer.maxHandSize)
        {
            Debug.Log(currentPlayer.playerName + " cannot buy because hand is full.");
            return;
        }

        currentPlayer.money -= gameConfig.buyCost;
        currentPlayer.handCount += 1;
        hasBoughtThisTurn = true;

        Debug.Log(currentPlayer.playerName + " bought a card.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);
        Debug.Log(currentPlayer.playerName + " hand count is now: " + currentPlayer.handCount);

        currentPhase = GamePhase.Play;
        LogStateSummary();
    }

    public void TestBuyCard()
    {
        BuyCard();
    }

    public void GoToPlayPhase()
    {
        if (currentPhase != GamePhase.Buy)
        {
            Debug.Log("You can only go to Play phase from Buy phase.");
            return;
        }

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

    public void TestPlayer2Lose()
    {
        player2.fortHp = 0;
        Debug.Log(player2.playerName + " fort HP is now: " + player2.fortHp);
        CheckGameOver();
        LogStateSummary();
    }

    public string GetStateSummary()
    {
        string winnerText = string.IsNullOrEmpty(winnerName) ? "None" : winnerName;

        return "Phase=" + currentPhase
            + " | CurrentPlayer=" + currentPlayer.playerName
            + " | HasBought=" + hasBoughtThisTurn
            + " | Winner=" + winnerText
            + " | P1(Money=" + player1.money + ", Hand=" + player1.handCount + ", Fort=" + player1.fortHp + ")"
            + " | P2(Money=" + player2.money + ", Hand=" + player2.handCount + ", Fort=" + player2.fortHp + ")";
    }

    public void LogStateSummary()
    {
        Debug.Log(GetStateSummary());
    }

    private void ApplyFortDamage(PlayerState targetPlayer, int damage)
    {
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
}
