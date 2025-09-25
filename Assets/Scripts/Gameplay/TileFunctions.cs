using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileFunctions : MonoBehaviour
{
    private WorldTile _tile;
    public Tilemap Tilemap;
    [SerializeField] private Grid grid;
    private Vector3Int prevPos;

    [Header("UI References")]
    [SerializeField] private Text tileTypeText;
    [SerializeField] private Text budgetText;
    [SerializeField] private Text purchaseButtonText;
    [SerializeField] private Button purchButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject menu;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text priceValText;
    [SerializeField] private Text ecoVal1Text;
    [SerializeField] private Text ecoVal2Text; // optional
    [SerializeField] private Text ecoVal3Text; // optional
    [SerializeField] private Text bonusValText;

    private Rect menuRect;

    void Start()
    {
        // Cache the menu Rect for click-zone testing
        menuRect = menu.GetComponent<RectTransform>().rect;
        purchButton.onClick.AddListener(PurchasedClick);
        resetButton.onClick.AddListener(ResetClick);
        submitButton.onClick.AddListener(() => {
            // Placeholder for submit logic (end level, validation, etc.)
        });

        // Auto-assign Tilemap if not set in Inspector
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
        // Defensive: ensure GameTiles is present before reading runtime values
        if (GameTiles.instance == null) return;

        // Update score & budget display
        if (scoreText != null) scoreText.text = GameTiles.instance.score.ToString();
        if (budgetText != null) budgetText.text = GameTiles.instance.budget.ToString();

        // Highlight purchased tiles
        foreach (var kv in GameTiles.instance.tiles)
        {
            var t = kv.Value;
            if (t.Purchased && !t.Locked)
                t.TilemapMember.SetColor(t.LocalPlace, Color.magenta);
        }

        // Handle mouse clicks outside the UI panel
        if (Tilemap == null) return; // cannot select without a Tilemap reference
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                menu.GetComponent<RectTransform>(),
                Input.mousePosition,
                Camera.main,
                out localPoint
            );

            // If click was inside the menu, bail out
            if (menuRect.Contains(localPoint)) return;

            // Convert click to cell coords
            var worldPoint = Tilemap.WorldToCell(
                Camera.main.ScreenToWorldPoint(Input.mousePosition)
            );
            worldPoint.z = 0;

            // Look up the tile
            if (GameTiles.instance.tiles != null && GameTiles.instance.tiles.TryGetValue(worldPoint, out _tile))
            {
                // Show details in UI
                _tile.TilemapMember.SetTileFlags(_tile.LocalPlace, TileFlags.None);
                tileTypeText.text = _tile.Name;
                priceValText.text = _tile.Cost.ToString();
                ecoVal1Text.text = _tile.ecoVal.ToString();
                if (ecoVal2Text != null) ecoVal2Text.text = _tile.ecoVal2.ToString();
                if (ecoVal3Text != null) ecoVal3Text.text = _tile.ecoVal3.ToString();
                bonusValText.text = _tile.bonus.ToString();

                // Color feedback
                _tile.TilemapMember.SetColor(_tile.LocalPlace, Color.green);
                if (_tile.Locked)
                {
                    purchaseButtonText.text = "Locked";
                    purchButton.enabled = false;
                }
                else if (_tile.Purchased)
                {
                    purchaseButtonText.text = "Sell";
                    purchButton.enabled = true;
                }
                else
                {
                    purchaseButtonText.text = "Purchase";
                    purchButton.enabled = true;
                }

                // Mark as the selected tile
                GameTiles.instance.selected = _tile;
            }

            // Restore previous tile's color if needed
            if (prevPos != worldPoint && GameTiles.instance.tiles != null && GameTiles.instance.tiles.TryGetValue(prevPos, out var prevTile))
            {
                if (prevTile.Purchased && !prevTile.Locked)
                    prevTile.TilemapMember.SetColor(prevTile.LocalPlace, Color.magenta);
                else
                    prevTile.TilemapMember.SetColor(prevTile.LocalPlace, Color.white);
            }

            prevPos = worldPoint;
        }
    }

    private void PurchasedClick()
    {
        if (GameTiles.instance == null || GameTiles.instance.tiles == null || GameTiles.instance.tiles.Count == 0)
            return;

        var tile = GameTiles.instance.selected;
        if (tile == null) return;
        var tiles = GameTiles.instance.tiles;

        // Compute neighbor list according to hex parity
        List<Vector3Int> neighbors = GetNeighborPositions(tile.LocalPlace);

        // Purchase
        if ((!tile.Purchased && !tile.Locked) && tile.Cost >= 0 && tile.Cost <= GameTiles.instance.budget)
        {
            tile.TilemapMember.SetColor(tile.LocalPlace, Color.magenta);
            tile.Purchased = true;
            int ecoSum = tile.ecoVal + tile.ecoVal2 + tile.ecoVal3;
            int tileValue = ecoSum + tile.bonus;
            GameTiles.instance.score += tileValue;
            tile.lastTotal = tileValue;
            GameTiles.instance.budget -= tile.Cost;
            GameTiles.instance.boughtTiles[tile.LocalPlace] = tile;
            purchaseButtonText.text = "Sell";

            // Apply adjacency bonuses to neighbors (10% of neighbor eco sum)
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
        // Sell
        else if (tile.Purchased && !tile.Locked)
        {
            tile.TilemapMember.SetColor(tile.LocalPlace, Color.white);
            tile.Purchased = false;
            GameTiles.instance.score -= tile.lastTotal;
            GameTiles.instance.budget += tile.Cost;
            GameTiles.instance.boughtTiles.Remove(tile.LocalPlace);
            purchaseButtonText.text = "Purchase";

            // Remove adjacency bonuses that were applied by this tile
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
        if (gm != null) gm.ReloadCurrent();
    }

    private List<Vector3Int> GetNeighborPositions(Vector3Int center)
    {
        bool evenRow = center.y % 2 == 0;
        if (evenRow)
        {
            return new List<Vector3Int>
            {
                new Vector3Int(center.x + 1, center.y - 1, 0), // idx 0
                new Vector3Int(center.x + 1, center.y + 0, 0), // idx 1
                new Vector3Int(center.x + 0, center.y + 1, 0), // idx 2
                new Vector3Int(center.x - 1, center.y + 0, 0), // idx 3
                new Vector3Int(center.x - 1, center.y - 1, 0), // idx 4
                new Vector3Int(center.x + 0, center.y - 1, 0), // idx 5
            };
        }
        else
        {
            return new List<Vector3Int>
            {
                new Vector3Int(center.x + 1, center.y + 0, 0), // idx 0
                new Vector3Int(center.x + 1, center.y + 1, 0), // idx 1
                new Vector3Int(center.x + 0, center.y + 1, 0), // idx 2
                new Vector3Int(center.x - 1, center.y + 1, 0), // idx 3
                new Vector3Int(center.x - 1, center.y + 0, 0), // idx 4
                new Vector3Int(center.x + 0, center.y - 1, 0), // idx 5
            };
        }
    }
}
