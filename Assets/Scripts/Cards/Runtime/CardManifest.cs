// MonoBehaviour representing a card after it has been manifested on the board, managing sprite rendering and runtime state.
using UnityEngine;

public class CardManifest : MonoBehaviour
{
    // Mutable runtime values associated with this manifested card.
    [SerializeReference] private CardRuntimeState runtimeState;

    // Owner identifier used for team/faction logic.
    [SerializeField] private string ownerId;

    private SpriteRenderer spriteRenderer;

    public CardRuntimeState RuntimeState => runtimeState;
    public string OwnerId => ownerId;
    public string OwnerKey => ownerId;

    // Cache SpriteRenderer once to avoid repeated GetComponent calls.
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Injects runtime data and owner when this object is spawned.
    public void Setup(CardRuntimeState state, string owner)
    {
        runtimeState = state;
        ownerId = owner;
        RefreshVisual();
    }

    // Updates runtime board position using axial hex coordinates.
    public void SetBoardPosition(AxialCoord position)
    {
        if (runtimeState == null)
        {
            return;
        }

        runtimeState.ManifestOnBoard(position);
    }

    // Keeps manifest sprite in sync with the board representation artwork.
    private void RefreshVisual()
    {
        if (runtimeState?.SourceCard == null || spriteRenderer == null)
        {
            return;
        }

        Sprite manifestSprite = runtimeState.SourceCard switch
        {
            CharacterCardData characterCard => characterCard.manifestedSprite,
            WorldEffectCardData worldEffectCard => worldEffectCard.manifestedSprite,
            _ => runtimeState.SourceCard.handDeckSprite
        };
        if (manifestSprite == null)
        {
            // Fallback keeps old/new assets visible even before all data is filled.
            manifestSprite = runtimeState.SourceCard.handDeckSprite;
        }

        spriteRenderer.sprite = manifestSprite;
    }
}
