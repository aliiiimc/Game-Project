using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Adapter that exposes HexGrid state through the cards board-reader contract.
    /// </summary>
    public sealed class HexGridBoardStateReader : IBoardStateReader
    {
        private readonly HexGrid _hexGrid;

        public HexGridBoardStateReader(HexGrid hexGrid)
        {
            _hexGrid = hexGrid;
        }

        public bool IsTileValid(AxialCoord tile)
        {
            if (_hexGrid == null)
            {
                return false;
            }

            return _hexGrid.GetTile(tile) != null;
        }

        public bool IsTileOccupied(AxialCoord tile)
        {
            if (_hexGrid == null)
            {
                return false;
            }

            HexTile foundTile = _hexGrid.GetTile(tile);
            return foundTile != null && !foundTile.IsEmpty();
        }

        public CardRuntimeState GetCardAt(AxialCoord tile)
        {
            if (_hexGrid == null)
            {
                return null;
            }

            CardManifest[] manifests = Object.FindObjectsByType<CardManifest>(FindObjectsSortMode.None);
            for (int i = 0; i < manifests.Length; i++)
            {
                CardManifest manifest = manifests[i];
                if (manifest == null || manifest.RuntimeState == null)
                {
                    continue;
                }

                if (!manifest.RuntimeState.IsManifestedOnBoard)
                {
                    continue;
                }

                AxialCoord boardPos = manifest.RuntimeState.BoardPosition;
                if (boardPos.q == tile.q && boardPos.r == tile.r)
                {
                    return manifest.RuntimeState;
                }
            }

            return null;
        }
    }
}
