/*
* Author: Luke Robinson
* Purpose: Class that keeps track of the game tiles. 
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameTiles : MonoBehaviour
{
	public static GameTiles instance;
	public Tilemap Tilemap;

	public Dictionary<Vector3Int, WorldTile> tiles;

	public int budget { get; set; }

	public Dictionary<Vector3Int, WorldTile> boughtTiles;

	public WorldTile selected;

	public int score;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}

		// Defer population until a Tilemap is assigned (GridManager will call RebuildFromTilemap)
		if (boughtTiles == null) boughtTiles = new Dictionary<Vector3Int, WorldTile>();
		if (tiles == null) tiles = new Dictionary<Vector3Int, WorldTile>();
		score = 0;
	}

	// Use this for initialization
	private void GetWorldTiles()
	{
		// Build tiles dictionary from current Tilemap contents
		if (Tilemap == null)
		{
			// Try to auto-find a Tilemap to prevent startup null exceptions
			Tilemap = GetComponent<Tilemap>();
			if (Tilemap == null)
			{
				var anyTilemap = FindObjectOfType<Tilemap>();
				if (anyTilemap == null) return; // wait until GridManager assigns
				Tilemap = anyTilemap;
			}
		}
		boughtTiles = new Dictionary<Vector3Int, WorldTile>();
		tiles = new Dictionary<Vector3Int, WorldTile>();
		score = 0;
		foreach (Vector3Int pos in Tilemap.cellBounds.allPositionsWithin)
		{
			var localPlace = new Vector3Int(pos.x, pos.y, 0);
            
            // Skip empty cells
			if (!Tilemap.HasTile(localPlace)) continue;
			var tile = new WorldTile
			{
				LocalPlace = localPlace,
				WorldLocation = Tilemap.CellToWorld(localPlace),
				TileBase = Tilemap.GetTile(localPlace),
				TilemapMember = Tilemap,
				Name = localPlace.x + "," + localPlace.y,
				applyBonus = new bool[6]{false,false,false,false,false,false},
				Cost = -1 // TODO: Change this with the proper cost from ruletile
			};

            // Initial diagnostic logs removed for performance
			tiles.Add(tile.LocalPlace, tile);
		}
	}

    /// <summary>
    /// Clears and re-populates the internal tiles dictionary from the given Tilemap.
    /// </summary>
    public void RebuildFromTilemap(Tilemap newTilemap)
    {
        Tilemap = newTilemap;
        // simply re-run your existing GetWorldTiles logic:
        GetWorldTiles();
    }
}
