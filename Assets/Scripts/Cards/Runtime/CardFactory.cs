// Factory helper providing a centralized creation point for runtime card state objects from static card definitions.
public static class CardFactory
{
    // Returns null for null input to keep calling code simple and safe.
    public static CardRuntimeState CreateRuntimeState(CardData cardData)
    {
        if (cardData == null)
        {
            return null;
        }

        return cardData.CreateRuntimeState();
    }
}
