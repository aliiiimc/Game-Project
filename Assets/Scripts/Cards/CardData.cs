using UnityEngine;
using UnityEngine.Serialization;

public abstract class CardData : ScriptableObject
{
    [Header("Core")]
    public string cardName;

    public int cost;

    [FormerlySerializedAs("artwork")]
    public Sprite handDeckSprite;
    [TextArea]
    public string description;

    [Header("Pipeline Mapping")]
    public string validatorId = "target.rules.reusable";
    public string effectId;

    public abstract OptionalInt MovementCapacity { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;

    public virtual CardRuntimeState CreateRuntimeState()
    {
        return new CardRuntimeState(this);
    }
}
