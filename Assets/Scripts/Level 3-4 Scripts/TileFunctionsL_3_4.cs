using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TileFunctionsL_3_4 : MonoBehaviour
{
	private WorldTile _tile;
	public Tilemap Tilemap;
	[SerializeField] private Grid grid;
	private Vector3Int prevPos;
	[SerializeField] private Text tileTypeText;
	[SerializeField] private Text budgetText;
	[SerializeField] private Text purchaseButtonText;
	[SerializeField] private Button purchButton;
	[SerializeField] private Button resetButton;
	[SerializeField] private Button submitButton;
	Rect position;
	[SerializeField] private GameObject menu;
	[SerializeField] private Text scoreText;
	[SerializeField] private Text priceValText;
	[SerializeField] private Text ecoVal1Text;
	[SerializeField] private Text ecoVal2Text;
	[SerializeField] private Text bonusValText;
	

	void Start()
    {
		position = menu.GetComponent<RectTransform>().rect;
		purchButton.onClick.AddListener(PurchasedClick);
		resetButton.onClick.AddListener(ResetClick);
    }
	// Update is called once per frame
	private void Update()
	{
		scoreText.text = GameTiles.instance.score.ToString();

		//print("Mouse Click: " + Input.GetMouseButtonDown(0));
		var tiles = GameTiles.instance.tiles;
		foreach (var tile in tiles)
		{
			tiles.TryGetValue(tile.Key, out _tile);
			if (_tile != null)
			{
				if (_tile.Purchased && !_tile.Locked)
				{
					_tile.TilemapMember.SetColor(_tile.LocalPlace, Color.magenta);
				}
			}
		}
		Vector2 _lp;
		budgetText.text = GameTiles.instance.budget.ToString();
		RectTransformUtility.ScreenPointToLocalPointInRectangle(menu.GetComponent<RectTransform>(), Input.mousePosition, Camera.main, out _lp);
		if(EventSystem.current.IsPointerOverGameObject())
        {
			print("worked");
        }
		else if (Input.GetMouseButtonDown(0))
		{
			print(_lp);
			print("pos: " + position);
			print(position.Contains(_lp));
			print(position.xMax + " " + position.yMax);
			print(position.xMin + " " + position.yMin);
			print("x: " + _lp.x + " y: " + _lp.y);
			/*print(Input.mousePosition);
			print(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			print("x: " + position.x + " y: " + position.y);*/


			Vector3 point = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			//var worldPoint = new Vector3Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y), 0);
			Vector3Int worldPoint = Tilemap.WorldToCell(point);
			worldPoint.z = 0;
			print("mouse down " + worldPoint);
			tiles = GameTiles.instance.tiles; // This is our Dictionary of tiles
			var tilepoint = new Vector3Int(3, 2, 0);
			//print(tiles.TryGetValue(Tilemap.CellToWorld(worldPoint), out _tile));
			print(tiles.TryGetValue(Tilemap.WorldToCell(worldPoint), out _tile));
			print(tiles.TryGetValue(grid.WorldToCell(worldPoint), out _tile));
			if (tiles.TryGetValue(worldPoint, out _tile))
			{
				print(_tile.TilemapMember.GetColor(_tile.LocalPlace));
				print("Tile " + _tile.Name + " costs: " + _tile.Cost + " purchased: " + _tile.Purchased);
				_tile.TilemapMember.SetTileFlags(_tile.LocalPlace, TileFlags.None);
				tileTypeText.text = _tile.Name;
				priceValText.text = _tile.Cost.ToString();
				ecoVal1Text.text = _tile.ecoVal.ToString();
				ecoVal2Text.text = _tile.ecoVal2.ToString();
				bonusValText.text = _tile.bonus.ToString();
				
				_tile.TilemapMember.SetColor(_tile.LocalPlace, Color.green);
				if(_tile.Locked){
					purchaseButtonText.text = "Locked";
					purchButton.enabled = false;
				}
                else if (_tile.Purchased && !_tile.Locked)
                {
					purchaseButtonText.text = "Sell";
					purchButton.enabled = true;
					
                }
                else
                {
					purchaseButtonText.text = "Purchase";
					purchButton.enabled = true;
                }
				GameTiles.instance.selected = _tile;
			}
			if (prevPos != worldPoint)
				if (tiles.TryGetValue(prevPos, out _tile))
					if (_tile.Purchased && !_tile.Locked)
						_tile.TilemapMember.SetColor(_tile.LocalPlace, Color.magenta);
					else if (_tile.TilemapMember.GetColor(_tile.LocalPlace) == Color.green)
						_tile.TilemapMember.SetColor(_tile.LocalPlace, Color.white);
					else if(!_tile.Purchased)
						_tile.TilemapMember.SetColor(_tile.LocalPlace, Color.white);
			prevPos = worldPoint;
			
		}
	}

	private void PurchasedClick()
    {
		var tile = GameTiles.instance.selected;
		var tiles = GameTiles.instance.tiles;
		Vector3Int cPlace = tile.LocalPlace;
		WorldTile edTile;
		Vector3Int keyCheck = new Vector3Int(0,0,0);
        if ((!tile.Purchased && !tile.Locked) && tile.Cost <= GameTiles.instance.budget)
        {
			tile.TilemapMember.SetColor(tile.LocalPlace, Color.magenta);
			tile.Purchased = true;
			GameTiles.instance.score += (tile.ecoVal + tile.ecoVal2 + tile.bonus);
			tile.lastTotal = (tile.ecoVal + tile.ecoVal2 + tile.bonus);
			GameTiles.instance.budget -= tile.Cost;
			GameTiles.instance.boughtTiles.Add(tile.LocalPlace, tile);
			purchaseButtonText.text = "Sell";
			for(int i = -1 ; i < 2 ; i++){
				for(int j = -1 ; j < 2 ; j++){
					if(cPlace.x+j > -7 && cPlace.x+j < 5 && cPlace.y+i > -6 && cPlace.y+i < 6){
						//Even row vs odd has different offsets
						if(cPlace.y%2 == 0){
							if(!(i==1 && j == 1) && !(i==-1 &&j==1) && !(i == 0 && j == 0)){
								keyCheck.Set(cPlace.x+j,cPlace.y+i,0);
								edTile = tiles[keyCheck];
								if(i == -1 && j == -1 && edTile.Purchased == false){
									tile.applyBonus[4] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == -1 && edTile.Purchased == false){
									tile.applyBonus[5] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == -1 && edTile.Purchased == false){
									tile.applyBonus[0] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == 0 && edTile.Purchased == false){
									tile.applyBonus[1] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == -1 && j == 0 && edTile.Purchased == false){
									tile.applyBonus[3] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == 1 && edTile.Purchased == false){
									tile.applyBonus[2] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
							}			
						}
						else{ 
							if(!(i==1 && j == -1) && !(i==-1 &&j==-1) && !(i == 0 && j == 0)){
								keyCheck.Set(cPlace.x+j,cPlace.y+i,0);
								edTile = tiles[keyCheck];
								if(i == -1 && j == 0 && edTile.Purchased == false){
									tile.applyBonus[4] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == -1 && edTile.Purchased == false){
									tile.applyBonus[5] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == 0 && edTile.Purchased == false){
									tile.applyBonus[0] = true;	
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == 1 && edTile.Purchased == false){
									tile.applyBonus[1] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == -1 && j == 1 && edTile.Purchased == false){
									tile.applyBonus[3] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == 1 && edTile.Purchased == false){
									tile.applyBonus[2] = true;
									edTile.bonus += (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
							}
						}
					}
				}
			}
        }
		else if(tile.Purchased && !tile.Locked){
			tile.TilemapMember.SetColor(tile.LocalPlace, Color.white);
			tile.Purchased = false;
			GameTiles.instance.score -= tile.lastTotal;
			GameTiles.instance.budget += tile.Cost;
			GameTiles.instance.boughtTiles.Remove(tile.LocalPlace);
			purchaseButtonText.text = "Purchase";
			for(int i = -1 ; i < 2 ; i++){
				for(int j = -1 ; j < 2 ; j++){
					if(cPlace.x+j > -7 && cPlace.x+j < 5 && cPlace.y+i > -6 && cPlace.y+i < 6){
						if(cPlace.y%2 == 0){
							if(!(i==1 && j == 1) && !(i==-1 &&j==1) && !(i == 0 && j == 0)){
								keyCheck.Set(cPlace.x+j,cPlace.y+i,0);
								edTile = tiles[keyCheck];
								if(i == -1 && j == -1 && tile.applyBonus[4] == true){
									tile.applyBonus[4] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == -1 && tile.applyBonus[5] == true){
									tile.applyBonus[5] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == -1 && tile.applyBonus[0] == true){
									tile.applyBonus[0] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == 0 && tile.applyBonus[1] == true){
									tile.applyBonus[1] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == -1 && j == 0 && tile.applyBonus[3] == true){
									tile.applyBonus[3] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == 1 && tile.applyBonus[2] == true){
									tile.applyBonus[2] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
							}			
						}
						else{ 
							if(!(i==1 && j == -1) && !(i==-1 &&j==-1) && !(i == 0 && j == 0)){
								keyCheck.Set(cPlace.x+j,cPlace.y+i,0);
								edTile = tiles[keyCheck];
								if(i == -1 && j == 0 && tile.applyBonus[4] == true){
									tile.applyBonus[4] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == -1 && tile.applyBonus[5] == true){
									tile.applyBonus[5] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == 0 && tile.applyBonus[0] == true){
									tile.applyBonus[0] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 1 && j == 1 && tile.applyBonus[1] == true){
									tile.applyBonus[1] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == -1 && j == 1 && tile.applyBonus[3] == true){
									tile.applyBonus[3] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
								else if(i == 0 && j == 1 && tile.applyBonus[2] == true){
									tile.applyBonus[2] = false;
									edTile.bonus -= (int)((edTile.ecoVal + edTile.ecoVal2) * 0.1);
								}
							}
						}
					}
				}
			}
		}
    }
	
	private void ResetClick(){
		var tiles = GameTiles.instance.tiles;
		foreach( var cTile in  GameTiles.instance.tiles.Values){
			cTile.bonus = 0;
			cTile.TilemapMember.SetColor(cTile.LocalPlace, Color.white);
			cTile.Purchased = false;
		}
		GameTiles.instance.score = 0;
		if(SceneManager.GetActiveScene().buildIndex == 3)
			GameTiles.instance.budget = 70000;
		else if(SceneManager.GetActiveScene().buildIndex == 4)
			GameTiles.instance.budget = 40000;
		GameTiles.instance.boughtTiles.Clear();
	}
}