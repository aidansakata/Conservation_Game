using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class newLevel4 : MonoBehaviour
{
    private WorldTile _tile;
	[SerializeField] public Tilemap Overlevel;

    
    // public Tilemap Tilemap;
    // Start is called before the first frame update
   public int[,] costArray=new int[11,11]{
	{2,3,4,7,6,4,3,2,1,3,4},
	{3,3,4,7,4,6,3,2,3,7,6},
	{2,2,3,8,4,6,4,0,8,4,5},
	{2,3,0,9,5,4,8,0,3,7,4},
	{4,2,4,7,4,6,5,2,3,6,6},
	{7,6,3,3,7,6,3,1,3,3,4},
	{5,7,2,3,5,3,2,0,8,7,8},
	{4,6,8,4,0,2,2,8,9,6,5},
	{5,7,9,0,0,2,7,8,8,0,8},
	{4,5,6,7,4,2,7,6,8,0,5},
	{5,5,8,3,3,4,6,7,8,5,4}
}; //budget: 40,000

public int[,] ecoValArray=new int[11,11]{ //mountains
	{1,1,2,5,5,2,2,2,2,2,2},
	{2,2,2,35,9,4,3,2,3,5,5},
	{2,2,3,35,8,5,3,0,5,39,38},
	{2,2,0,35,38,9,5,0,3,5,39},
	{3,2,2,35,9,5,3,2,3,5,5},
	{5,5,3,3,5,5,3,2,2,2,3},
	{38,6,3,3,3,2,2,0,5,5,6},
	{39,38,5,2,0,2,3,5,6,39,38},
	{39,38,6,0,0,3,5,38,39,0,5},
	{39,39,38,6,2,2,5,38,38,0,3},
	{39,38,5,3,1,2,5,6,5,2,2}
}; //

public int[,] ecoValArray2=new int[11,11]{ //valley/desert
	{9,9,5,3,2,4,6,9,9,5,3},
	{7,9,7,24,1,3,5,9,5,5,3},
	{6,9,5,23,1,3,5,0,4,21,21},
	{5,9,0,23,21,1,3,0,5,3,21},
	{5,9,6,25,1,3,5,9,5,3,2},
	{3,6,9,8,5,6,7,9,6,4,3},
	{21,5,8,9,5,9,9,0,4,3,3},
	{21,21,5,7,0,9,7,5,4,21,21},
	{21,21,3,0,0,9,6,21,21,0,3},
	{21,21,21,3,7,9,5,21,21,0,3},
	{21,21,3,5,9,5,4,4,3,3,4}
}; //


    public int budgetVal = 40;
   public int[,] lockedArray=new int[11,11]{
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,3,0,0,0},
	{0,0,3,0,0,0,0,3,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,3,0,0,0},
	{0,0,0,0,3,0,0,0,0,0,0},
	{0,0,0,3,3,0,0,0,0,3,0},
	{0,0,0,0,0,0,0,0,0,3,0},
	{0,0,0,0,0,0,0,0,0,0,0}
}; //tiles with a '3' value here represent towns/cities, and cannot be purchased
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
			if(lockedArray[y,x] == 3)
			{
				_tile.Name = "Inhabited";
				_tile.Purchased = true;
				_tile.Locked = true; 
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
