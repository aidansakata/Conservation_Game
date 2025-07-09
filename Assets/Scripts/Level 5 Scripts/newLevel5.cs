using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class newLevel5 : MonoBehaviour
{
    private WorldTile _tile;
	[SerializeField] public Tilemap Overlevel;

    
    // public Tilemap Tilemap;
    // Start is called before the first frame update
   public int[,] costArray=new int[11,11]{
	{3,7,6,4,4,3,4,3,2,1,1},	
	{4,8,6,6,6,5,4,3,3,2,1},
	{7,9,8,7,7,9,6,4,3,3,2},
	{5,5,6,5,6,0,7,0,3,3,2},
	{4,4,4,5,0,0,6,5,4,2,2},
	{5,6,7,6,5,7,9,5,5,3,2},
	{7,0,6,3,4,8,4,6,5,4,2},
	{7,0,6,8,4,6,5,4,5,4,3},
	{6,8,7,9,5,5,5,4,4,3,3},
	{6,7,8,4,4,5,3,4,5,6,5},
	{7,5,6,7,6,7,4,5,8,4,5}
}; //budget: ~80,000

public int[,] ecoValArray=new int[11,11]{
	{38,37,25,24,24,24,23,23,22,22,22},	
	{39,22,22,22,25,24,24,23,23,22,22},
	{22,26,22,37,37,26,22,24,23,22,22},
	{38,22,38,38,38,0,22,22,24,23,22},
	{39,39,38,38,0,0,22,24,24,22,22},
	{39,38,39,38,38,38,37,22,24,23,22},
	{22,0,38,22,28,26,22,24,24,23,22},
	{22,0,38,26,22,25,24,24,23,23,23},
	{22,26,26,24,22,24,24,24,23,24,23},
	{22,24,24,22,22,23,24,24,24,24,22},
	{24,24,24,23,23,23,23,23,24,22,23}
};

public int[,] ecoValArray2=new int[11,11]{
	{26,28,18,16,16,15,14,13,12,12,11},	
	{24,12,12,12,18,17,16,14,14,12,11},
	{12,18,12,28,29,18,12,14,13,12,12},
	{23,12,26,26,27,0,12,12,14,13,12},
	{23,23,24,25,0,0,12,14,14,12,12},
	{23,24,24,25,25,27,29,12,14,13,12},
	{12,0,27,12,16,19,12,16,14,13,12},
	{12,0,27,18,12,18,18,16,14,14,13},
	{12,18,18,19,12,18,18,17,16,14,14},
	{12,18,18,12,12,15,16,18,16,16,12},
	{18,18,16,15,14,15,16,16,18,12,14}
};

public int[,] ecoValArray3=new int[11,11]{
	{4,4,6,8,8,8,8,8,6,2,2},	
	{3,2,2,2,6,7,8,8,8,6,2},
	{2,4,2,5,5,6,2,9,8,6,2},
	{4,2,4,4,4,0,2,2,8,8,6},
	{3,3,3,4,0,0,2,9,8,6,2},
	{3,3,4,4,4,4,4,2,9,8,6},
	{2,0,4,2,4,4,2,8,9,8,7},
	{2,0,3,4,2,5,6,8,9,8,8},
	{2,3,4,4,2,6,7,7,8,9,9},
	{2,4,4,2,2,7,7,6,8,8,2},
	{5,6,7,8,8,8,7,8,6,2,8}
};


    public int budgetVal = 80;
   public int[,] lockedArray=new int[11,11]{ //2 represents towns/cities, and cannot be purchased
	{0,0,0,0,0,0,0,0,0,0,0},	
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,3,0,0,0,0,0},
	{0,0,0,0,3,3,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,3,0,0,0,0,0,0,0,0,0},
	{0,3,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0},
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
			_tile.ecoVal3 = ecoValArray3[y,x];
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
