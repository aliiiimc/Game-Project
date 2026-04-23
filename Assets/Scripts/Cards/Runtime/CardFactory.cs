public static class CardFactory
{
    public static CardRuntimeState CreateRuntimeState(CardData cardData)
    {
        if (cardData == null)
        {
            return null;
        }

        return cardData.CreateRuntimeState();
    }
}
