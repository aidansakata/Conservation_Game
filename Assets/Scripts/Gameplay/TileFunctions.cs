using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileFunctions : MonoBehaviour
{
    public Tilemap Tilemap;

    [Header("Global UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text budgetText;

    [Header("Global Buttons")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button hintButton;

    void Start()
    {
        if (resetButton != null) resetButton.onClick.AddListener(ResetClick);

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(() => {
                if (GridManager.instance != null)
                    GridManager.instance.OnSubmitClicked();
            });
        }

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(() => {
                if (GridManager.instance != null)
                    GridManager.instance.OnHintClicked();
            });
        }

        if (Tilemap == null)
        {
            var gt = GameTiles.instance ?? FindObjectOfType<GameTiles>();
            if (gt != null && gt.Tilemap != null)
            {
                Tilemap = gt.Tilemap;
            }
            else
            {
                var tm = FindObjectOfType<Tilemap>();
                if (tm != null) Tilemap = tm;
            }
        }
    }

    void Update()
    {
        if (GameTiles.instance == null) return;

        if (scoreText != null) scoreText.text = GameTiles.instance.score.ToString();
        if (budgetText != null) budgetText.text = GameTiles.instance.budget.ToString();

        // FORCE COLOR UPDATE LOOP
        // We add the Flag Unlock here to ensure existing tiles get unlocked if missed
        foreach (var kv in GameTiles.instance.tiles)
        {
            var t = kv.Value;
            if (t.Purchased && !t.Locked)
            {
                // CRITICAL FIX: Unlock the tile so we can color it
                t.TilemapMember.SetTileFlags(t.LocalPlace, TileFlags.None);
                t.TilemapMember.SetColor(t.LocalPlace, Color.magenta);
            }
        }

        if (Tilemap == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleTileClick();
        }
    }

    private void HandleTileClick()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = Tilemap.WorldToCell(mouseWorldPos);
        cellPos.z = 0;

        if (GameTiles.instance.tiles != null && GameTiles.instance.tiles.TryGetValue(cellPos, out WorldTile tile))
        {
            TogglePurchase(tile);
        }
    }

    private void TogglePurchase(WorldTile tile)
    {
        if (tile.Locked) return;

        List<Vector3Int> neighbors = GetNeighborPositions(tile.LocalPlace);
        var tiles = GameTiles.instance.tiles;

        // CRITICAL FIX: Unlock flags immediately upon interaction
        tile.TilemapMember.SetTileFlags(tile.LocalPlace, TileFlags.None);

        // CASE 1: BUY
        if (!tile.Purchased)
        {
            if (tile.Cost <= GameTiles.instance.budget)
            {
                // Visuals
                tile.TilemapMember.SetColor(tile.LocalPlace, Color.magenta);
                tile.Purchased = true;

                // Math
                int ecoSum = tile.ecoVal + tile.ecoVal2 + tile.ecoVal3;
                int tileValue = ecoSum + tile.bonus;

                GameTiles.instance.score += tileValue;
                tile.lastTotal = tileValue;

                GameTiles.instance.budget -= tile.Cost;

                GameTiles.instance.boughtTiles[tile.LocalPlace] = tile;

                Debug.Log($"Bought {tile.LocalPlace}. Score +{tileValue}. Budget remaining: {GameTiles.instance.budget}");

                // Neighbors
                for (int k = 0; k < neighbors.Count; k++)
                {
                    var nPos = neighbors[k];
                    if (tiles.TryGetValue(nPos, out var nTile) && nTile.Purchased == false)
                    {
                        tile.applyBonus[k] = true;
                        int nEcoSum = nTile.ecoVal + nTile.ecoVal2 + nTile.ecoVal3;
                        nTile.bonus += (int)(nEcoSum * 0.1f);
                    }
                }
            }
            else
            {
                Debug.Log("Not enough budget to purchase tile!");
                var gm = FindObjectOfType<GridManager>();
                if (gm != null) gm.ShowBudgetWarning();
            }
        }
        // CASE 2: SELL
        else
        {
            // Visuals
            tile.TilemapMember.SetColor(tile.LocalPlace, Color.white);
            tile.Purchased = false;

            // Math
            GameTiles.instance.score -= tile.lastTotal;
            GameTiles.instance.budget += tile.Cost;

            GameTiles.instance.boughtTiles.Remove(tile.LocalPlace);

            Debug.Log($"Sold {tile.LocalPlace}. Score -{tile.lastTotal}. Budget restored.");

            // Neighbors
            for (int k = 0; k < neighbors.Count; k++)
            {
                if (!tile.applyBonus[k]) continue;
                var nPos = neighbors[k];
                if (tiles.TryGetValue(nPos, out var nTile))
                {
                    int nEcoSum = nTile.ecoVal + nTile.ecoVal2 + nTile.ecoVal3;
                    nTile.bonus -= (int)(nEcoSum * 0.1f);
                    tile.applyBonus[k] = false;
                }
            }
        }
    }

    private void ResetClick()
    {
        var gm = GameObject.FindObjectOfType<GridManager>();
        if (gm != null) gm.ResetCurrentLevel();
    }

    private List<Vector3Int> GetNeighborPositions(Vector3Int center)
    {
        bool evenRow = center.y % 2 == 0;
        if (evenRow)
        {
            return new List<Vector3Int>
            {
                new Vector3Int(center.x + 1, center.y - 1, 0),
                new Vector3Int(center.x + 1, center.y + 0, 0),
                new Vector3Int(center.x + 0, center.y + 1, 0),
                new Vector3Int(center.x - 1, center.y + 0, 0),
                new Vector3Int(center.x - 1, center.y - 1, 0),
                new Vector3Int(center.x + 0, center.y - 1, 0),
            };
        }
        else
        {
            return new List<Vector3Int>
            {
                new Vector3Int(center.x + 1, center.y + 0, 0),
                new Vector3Int(center.x + 1, center.y + 1, 0),
                new Vector3Int(center.x + 0, center.y + 1, 0),
                new Vector3Int(center.x - 1, center.y + 1, 0),
                new Vector3Int(center.x - 1, center.y + 0, 0),
                new Vector3Int(center.x + 0, center.y - 1, 0),
            };
        }
    }
}