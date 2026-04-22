using System.Collections.Generic;

[System.Serializable]
public class PlayerState
{
    public string playerName;
    public int money;
    public int fortHp;
    public int discardCount;
    public int handCount;
    public int deckCount;
    public int maxHandSize;
    public List<CardRuntimeState> handCards = new List<CardRuntimeState>();

}