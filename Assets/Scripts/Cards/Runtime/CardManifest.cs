using UnityEngine;

public class CardManifest : MonoBehaviour
{
    [SerializeReference] private CardRuntimeState runtimeState;

    [SerializeField] private string ownerId;

    private SpriteRenderer spriteRenderer;

    public CardRuntimeState RuntimeState => runtimeState;
    public string OwnerId => ownerId;
    public string OwnerKey => ownerId;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(CardRuntimeState state, string owner)
    {
        runtimeState = state;
        ownerId = owner;
        RefreshVisual();
    }

    public void SetBoardPosition(AxialCoord position)
    {
        if (runtimeState == null)
        {
            return;
        }

        runtimeState.ManifestOnBoard(position);
    }

    private void RefreshVisual()
    {
        if (runtimeState?.SourceCard == null || spriteRenderer == null)
        {
            return;
        }

        Sprite manifestSprite = null;
        if (runtimeState.SourceCard is CharacterCardData)
        {
            CharacterCardData characterCard = (CharacterCardData)runtimeState.SourceCard;
            manifestSprite = characterCard.manifestedSprite;
        }
        else if (runtimeState.SourceCard is WorldEffectCardData)
        {
            WorldEffectCardData worldEffectCard = (WorldEffectCardData)runtimeState.SourceCard;
            manifestSprite = worldEffectCard.manifestedSprite;
        }

        if (manifestSprite == null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.sprite = manifestSprite;
        spriteRenderer.enabled = true;
    }
}
