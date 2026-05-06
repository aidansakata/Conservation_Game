using UnityEngine;

public class PatchesController : MonoBehaviour
{
    public void OnForestClicked()
    {
        GameState.SelectedTileType = "forest";
        SceneLoader.LoadSinglePatch();
    }

    public void OnCityClicked()
    {
        GameState.SelectedTileType = "city";
        SceneLoader.LoadSinglePatch();
    }

    public void OnFarmlandClicked()
    {
        GameState.SelectedTileType = "farmland";
        SceneLoader.LoadSinglePatch();
    }

    public void OnGrasslandClicked()
    {
        GameState.SelectedTileType = "grassland";
        SceneLoader.LoadSinglePatch();
    }

    public void OnHabitatClicked()
    {
        GameState.SelectedTileType = "habitat";
        SceneLoader.LoadSinglePatch();
    }

    public void OnRoadClicked()
    {
        GameState.SelectedTileType = "road";
        SceneLoader.LoadSinglePatch();
    }

    public void OnBackClicked()
    {
        SceneLoader.LoadMainMenu();
    }
}
