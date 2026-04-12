using UnityEngine;
using TMPro; // Standard Unity text package

namespace FortGame.UI 
{
    /// <summary>
    /// Manages the heads-up display showing the player's Fort HP, Resources/Money, and game messages.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI fortHpText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI turnStatusText;

        /// <summary>
        /// Updates the HUD to reflect the current state of the provided PlayerState.
        /// </summary>
        public void UpdateHUD(PlayerState state)
        {
            if (state == null) return;

            if (playerNameText != null)
                playerNameText.text = state.playerName;

            if (fortHpText != null)
                fortHpText.text = $"Fort HP: {state.fortHp}";

            if (moneyText != null)
                moneyText.text = $"Money: {state.money}";
        }

        /// <summary>
        /// Call this when the turn changes (e.g., "Your Turn!" or "Enemy Turn")
        /// </summary>
        public void SetTurnStatus(string statusMessage)
        {
            if (turnStatusText != null)
            {
                turnStatusText.text = statusMessage;
            }
        }
    }
}
