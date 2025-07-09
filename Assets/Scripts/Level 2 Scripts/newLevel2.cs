using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class newLevel2 : MonoBehaviour
{
    private WorldTile _tile;
	[SerializeField] public Tilemap Overlevel;

    
    // public Tilemap Tilemap;
    // Start is called before the first frame update
    public int[,] costArray=new int[9,9]{
	{6,0,5,5,4,0,0,4,5},
	{6,5,3,3,0,0,4,5,6},
	{0,0,4,2,3,2,4,4,6},
	{5,4,3,1,2,2,3,4,4},
	{5,3,3,2,1,1,3,3,3},
	{6,4,4,2,1,1,2,0,2},
	{8,5,5,3,2,2,2,0,4},
	{8,6,6,4,3,4,0,5,0},
	{9,7,6,4,4,3,3,4,0}
};		//budget=30
		//numbers are listed between 1 and 10 to make it easy to change and to compare with the other two arrays, but I would recommend multiplying
		//each by something like 10,000 for the actual implementation, to decrease the likelihood of the player mixing up cost and eco value


    public int[,] ecoValArray=new int[9,9]{
	{53,0,59,53,54,0,0,53,51},
	{54,56,64,75,0,0,78,72,51},
	{0,0,74,65,77,56,54,73,52},
	{54,53,78,75,56,55,65,74,51},
	{51,53,59,69,75,56,75,62,58},
	{52,53,78,65,78,57,55,0,53},
	{51,2,63,75,69,59,54,0,55},
	{51,62,63,54,75,54,0,57,0},
	{51,51,51,52,52,54,55,56,0}
};

    public int budgetVal = 30;
    public int[,] lockedArray = new int[9, 9]{ //this array designates tiles that are preselected at level start, i.e. preexisiting conservation areas
    {0,1,0,0,0,1,1,0,0},
	{0,0,0,0,1,1,0,0,0},
	{1,1,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0},
	{0,0,0,0,0,0,0,1,0},
	{0,0,0,0,0,0,0,1,0},
	{0,0,0,0,0,0,1,0,1},
	{0,0,0,0,0,0,0,0,1}
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
            var x = _tile.LocalPlace.x+5;
            var y = -(_tile.LocalPlace.y)+4;
			_tile.Cost = costArray[y,x] * 1000;
			print(_tile.Cost);
			_tile.ecoVal = ecoValArray[y,x];
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
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
