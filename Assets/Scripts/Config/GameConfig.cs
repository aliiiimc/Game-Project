using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")] // This attribute allows us to 
// create an instance of this ScriptableObject from the Unity Editor
public class GameConfig : ScriptableObject
{
    public int startingMoney = 4;
    public int startingFortHp = 60;
    public int startingHandSize = 4;
    public int maxHandSize = 7;
    public int moneyPerTurn = 3;
    public int buyCost = 2;
    public int discardMoneyReward = 1;
    public int maxDiscardCardsPerTurn = 1;

}
