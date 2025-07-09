using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class newLevel3 : MonoBehaviour
{
    private WorldTile _tile;
	[SerializeField] public Tilemap Overlevel;

    
    // public Tilemap Tilemap;
    // Start is called before the first frame update
	public int[,] costArray=new int[11,11]{
	{0,0,0,0,0,0,0,5,5,6,7},
	{0,0,0,7,8,0,0,0,4,5,7},
	{0,8,7,6,7,0,0,6,5,6,6},
	{0,7,5,5,6,7,0,7,8,7,0},
	{0,6,8,8,5,0,0,7,0,0,0},
	{0,5,6,9,6,6,0,0,0,0,0},
	{8,8,8,9,7,0,0,0,4,0,0},
	{13,12,6,8,0,0,0,5,4,4,0},
	{10,11,6,0,0,0,6,5,4,0,0},
	{0,7,6,6,0,0,7,6,9,9,0},
	{0,0,0,0,0,0,0,4,9,0,0}
	}; //budget=70000

	public int[,]ecoValArray=new int[11,11]{
	{0,0,0,0,0,0,0,45,46,39,39},
	{0,0,0,47,48,0,0,0,46,47,38},
	{0,48,49,49,48,0,0,49,45,47,48},
	{0,44,45,58,45,43,0,49,48,46,0},
	{0,53,54,45,42,0,0,49,0,0,0},
	{0,53,52,45,42,43,0,0,0,0,0},
	{92,54,45,44,43,0,0,0,41,0,0},
	{92,84,48,47,0,0,0,41,43,42,0},
	{85,49,49,0,0,0,42,43,43,0,0},
	{0,46,48,49,0,0,43,45,85,94,0},
	{0,0,0,0,0,0,0,48,96,0,0}
	};

	public int[,]ecoValArray2=new int[11,11]{
	{0,0,0,0,0,0,0,31,31,22,24},
	{0,0,0,31,33,0,0,0,32,36,25},
	{0,38,34,32,34,0,0,33,35,38,37},
	{0,38,37,55,36,38,0,33,34,35,0},
	{0,55,56,39,38,0,0,33,0,0,0},
	{0,52,55,38,39,39,0,0,0,0,0},
	{95,56,37,38,38,0,0,0,39,0,0},
	{98,89,36,36,0,0,0,39,35,39,0},
	{89,38,35,0,0,0,39,35,35,0,0},
	{0,38,36,33,0,0,35,35,85,85,0},
	{0,0,0,0,0,0,0,39,99,0,0}
	};

    public int budgetVal = 70;
    public int[,]lockedArray=new int[11,11]{ //2 indicates tiles that cannot be purchased, as they are water
	{2,2,2,2,2,2,2,0,0,0,0},
	{2,2,2,0,0,2,2,2,0,0,0},
	{2,0,0,0,0,2,2,0,0,0,0},
	{2,0,0,0,0,0,2,0,0,0,2},
	{2,0,0,0,0,2,2,0,2,2,2},
	{2,0,0,0,0,0,2,2,2,2,2},
	{0,0,0,0,0,2,2,2,0,2,2},
	{0,0,0,0,2,2,2,0,0,0,2},
	{0,0,0,2,2,2,0,0,0,2,2},
	{2,0,0,0,2,2,0,0,0,0,2},
	{2,2,2,2,2,2,2,0,0,2,2}
	};
	void Start()
    {
        
        var tiles = GameTiles.instance.tiles;
        print("Starting now");
        GameTiles.instance.budget = budgetVal * 1000;
        foreach (var tile in tiles)
        {
            _tile = tile.Value;
            print("Tile : " + _tile.LocalPlace);
            var x = _tile.LocalPlace.x+6;
            var y = -(_tile.LocalPlace.y)+5;
			_tile.Cost = costArray[y,x] * 1000;
			print(_tile.Cost);
			_tile.ecoVal = ecoValArray[y,x];
			_tile.ecoVal2 = ecoValArray2[y,x];
			_tile.Name = "( " + _tile.LocalPlace.x + " , " + _tile.LocalPlace.y + " )";
			if( _tile.ecoVal == 0)
				_tile.Name = "Locked";
			else if( _tile.ecoVal >= 1 && _tile.ecoVal<= 10)
				_tile.Name = "Desert";
			else if( _tile.ecoVal >= 11 && _tile.ecoVal<= 20)
				_tile.Name = "Ocean";
			else if( _tile.ecoVal >= 21 && _tile.ecoVal<= 30)
				_tile.Name = "Tundra";
			else if( _tile.ecoVal >= 31 && _tile.ecoVal<= 40)
				_tile.Name = "Mountain";
			else if( _tile.ecoVal >= 41 && _tile.ecoVal<= 50)
				_tile.Name = "Grassland";
			else if( _tile.ecoVal >= 51 && _tile.ecoVal<= 60)
				_tile.Name = "Forest";
			else if( _tile.ecoVal >= 61 && _tile.ecoVal<= 80)
				_tile.Name = "Swamp";
			else if( _tile.ecoVal >= 81 && _tile.ecoVal<= 100)
				_tile.Name = "Jungle";
			if(lockedArray[y,x] == 1)
			{
				_tile.Purchased = true;
				_tile.Locked = true; 
				GameTiles.instance.boughtTiles.Add(_tile.LocalPlace, _tile);
				GameTiles.instance.score += _tile.ecoVal;
			}
			else if(lockedArray[y,x] == 2)
			{
				_tile.Purchased = true;
				_tile.Locked = true; 
				_tile.Name = "Ocean";
				GameTiles.instance.boughtTiles.Add(_tile.LocalPlace, _tile);
				GameTiles.instance.score += _tile.ecoVal;
			}			
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
