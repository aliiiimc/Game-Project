using UnityEngine;

public class GameManager : MonoBehaviour // This class will manage the overall game state, 
// including player states and game phases
{
    public GameConfig gameConfig;

    public PlayerState player1;
    public PlayerState player2;
    public GamePhase currentPhase;
    public PlayerState currentPlayer;
    public bool hasBoughtThisTurn;


    private void Start(){ //fonction spéciale Unity, se lance automatiquement quand on appuie sur Play
        if (gameConfig == null){
            Debug.LogError("GameConfig is missing!"); return;}

        SetupGame();}
    
    private void SetupGame(){
        player1 = new PlayerState();
        player2 = new PlayerState();

        player1.playerName = "Player 1";
        player2.playerName = "Player 2";

        currentPlayer = player1;

        player1.money = gameConfig.startingMoney;
        player2.money = gameConfig.startingMoney;

        player1.fortHp = gameConfig.startingFortHp;
        player2.fortHp = gameConfig.startingFortHp;

        player1.maxHandSize = gameConfig.maxHandSize;
        player2.maxHandSize = gameConfig.maxHandSize;

        player1.handCount = gameConfig.startingHandSize;
        player2.handCount = gameConfig.startingHandSize;

        Debug.Log("Game started");
        Debug.Log("Current player: " + currentPlayer.playerName);
        Debug.Log(player1.playerName + " money: " + player1.money);
        Debug.Log(player2.playerName + " money: " + player2.money);
        Debug.Log(player1.playerName + " fort HP: " + player1.fortHp);
        Debug.Log(player2.playerName + " fort HP: " + player2.fortHp);
        Debug.Log(player1.playerName + " hand count: " + player1.handCount);
        Debug.Log(player2.playerName + " hand count: " + player2.handCount);
        currentPhase = GamePhase.Income;
        Debug.Log("Current phase: " + currentPhase);

        StartTurn();

    }
   private void StartTurn(){
    if (currentPhase == GamePhase.Income){   
        hasBoughtThisTurn = false;
        currentPlayer.money += gameConfig.moneyPerTurn;

        Debug.Log(currentPlayer.playerName + " receives income.");
        Debug.Log(currentPlayer.playerName + " money is now: " + currentPlayer.money);

        currentPhase = GamePhase.Buy;
        Debug.Log("Current phase: " + currentPhase);
    }
}
    public void EndTurn(){
        CheckGameOver();
        if (currentPhase == GamePhase.GameOver){return;}

        currentPhase = GamePhase.End;
        Debug.Log("Ending turn for " + currentPlayer.playerName);

        if (currentPlayer == player1){currentPlayer = player2;}
        else{currentPlayer = player1;}

        Debug.Log("New current player: " + currentPlayer.playerName);
        currentPhase = GamePhase.Income;
        Debug.Log("Current phase: " + currentPhase);

        StartTurn();
}


    public void TestEndTurn(){
    EndTurn();
}


    public void BuyCard(){
    if (currentPhase != GamePhase.Buy){
        Debug.Log("You cannot buy cards right now.");
        return;
    }

    if (hasBoughtThisTurn){
        Debug.Log(currentPlayer.playerName + " already bought a card this turn.");
        return;
    }

    if (currentPlayer.money < gameConfig.buyCost){
        Debug.Log(currentPlayer.playerName + " does not have enough money to buy a card.");
        return;
    }

    if (currentPlayer.handCount >= currentPlayer.maxHandSize){
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
    Debug.Log("Current phase: " + currentPhase);

}

    public void TestBuyCard(){
    BuyCard();
}


    public void GoToPlayPhase(){
    if (currentPhase != GamePhase.Buy){
        Debug.Log("You can only go to Play phase from Buy phase.");
        return;
    }

    currentPhase = GamePhase.Play;
    Debug.Log("Current phase: " + currentPhase);
}
    public void GoToAttackPhase(){
    if (currentPhase != GamePhase.Play){
        Debug.Log("You can only go to Attack phase from Play phase.");
        return;
    }

    currentPhase = GamePhase.Attack;
    Debug.Log("Current phase: " + currentPhase);
}
    public void FinishAttackPhase(){
    if (currentPhase != GamePhase.Attack)
    {
        Debug.Log("You can only finish attack during Attack phase.");
        return;
    }

    EndTurn();
}
    private void CheckGameOver(){// Gère condtition de défaite.
    if (player1.fortHp <= 0){
        currentPhase = GamePhase.GameOver;
        Debug.Log(player2.playerName + " wins the game!");
        return;
    }

    if (player2.fortHp <= 0){
        currentPhase = GamePhase.GameOver;
        Debug.Log(player1.playerName + " wins the game!");
    }
}

    public void DamagePlayer1Fort(int damage){
        player1.fortHp -= damage;
        Debug.Log(player1.playerName + " fort HP is now: " + player1.fortHp);
}

    public void DamagePlayer2Fort(int damage){
    player2.fortHp -= damage;
    Debug.Log(player2.playerName + " fort HP is now: " + player2.fortHp);
}

    public void TestPlayer2Lose(){
    player2.fortHp = 0;
    Debug.Log(player2.playerName + " fort HP is now: " + player2.fortHp);
}

}



