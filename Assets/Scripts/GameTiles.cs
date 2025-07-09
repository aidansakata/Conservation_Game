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

		GetWorldTiles();
	}

	// Use this for initialization
	private void GetWorldTiles()
	{
		print("getting tiles");
		boughtTiles = new Dictionary<Vector3Int, WorldTile>();
		tiles = new Dictionary<Vector3Int, WorldTile>();
		score = 0;
		foreach (Vector3Int pos in Tilemap.cellBounds.allPositionsWithin)
		{
			var localPlace = new Vector3Int(pos.x, pos.y, 0);
			
			print("has tile: " + Tilemap.HasTile(localPlace) + " " + localPlace);
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

			Debug.Log($"Tile Base {Tilemap.GetTile(localPlace)}");
			
			print("Location : " + tile.WorldLocation);
			tiles.Add(tile.LocalPlace, tile);
		}
	}
}
