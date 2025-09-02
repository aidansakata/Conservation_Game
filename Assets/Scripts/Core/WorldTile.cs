/*
* Purpose: Container class for tile information. 
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldTile
{
    public Vector3Int LocalPlace { get; set; }

    public Vector3 WorldLocation { get; set; }

    public TileBase TileBase { get; set; }

    public Tilemap TilemapMember { get; set; }

    public string Name { get; set; }

    // Below is needed for Breadth First Searching
	
    public bool IsExplored { get; set; }

    public WorldTile ExploredFrom { get; set; }

    public int Cost { get; set; }

    public int ecoVal { get; set; }
	
	public int ecoVal2 { get; set; }
	
	public int ecoVal3 { get; set; }
	
	public int bonus { get; set; }
	
	public int lastTotal { get; set; }
	
	public bool[] applyBonus { get; set;}

    public bool Purchased { get; set; }
	
	public bool Locked { get; set; } 
}