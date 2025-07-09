using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class newLevel1 : MonoBehaviour
{
    private WorldTile _tile;
	[SerializeField] public Tilemap Overlevel;

    
    // public Tilemap Tilemap;
    // Start is called before the first frame update
    public int[,] costArray = new int[7, 7]{
    {0,8,8,7,7,6,6},
    {0,0,7,6,6,6,5},
    {0,4,5,6,5,4,4},
    {2,2,3,4,4,4,0},
    {2,3,1,2,2,0,0},
    {2,2,0,0,6,5,0},
    {0,0,0,3,4,5,7}
    }; //budget=25
       //numbers are listed between 1 and 10 to make it easy to change and to compare with the other two arrays, but I would recommend multiplying
       //each by something like 10,000 for the actual implementation, to decrease the likelihood of the player mixing up cost and eco value


    public int[,] ecoValArray = new int[7, 7]{
    {0,52,51,58,54,57,81},
    {0,0,52,55,53,58,52},
    {0,44,60,56,59,54,51},
    {55,58,60,55,58,43,0},
    {57,59,46,60,56,0,0},
    {51,56,0,0,59,42,0},
    {0,0,0,49,59,55,94}
    };

    public int budgetVal = 25;
    public int[,] lockedArray = new int[7, 7]{ //this array designates tiles that are preselected at level start, i.e. preexisiting conservation areas
    {1,0,0,0,0,0,0},
    {1,1,0,0,0,0,0},
    {1,0,0,0,0,0,0},
    {0,0,0,0,0,0,1},
    {0,0,0,0,0,1,1},
    {0,0,1,1,0,0,1},
    {1,1,1,0,0,0,0}
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
            var x = _tile.LocalPlace.x+4;
            var y = -(_tile.LocalPlace.y)+3;
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
