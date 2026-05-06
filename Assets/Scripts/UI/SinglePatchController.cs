using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SinglePatchController : MonoBehaviour
{
    [Header("Dynamic Content (assign in Inspector after prefab is built)")]
    [SerializeField] private TextMeshProUGUI tileNameText;
    [SerializeField] private TextMeshProUGUI tileDescriptionText;
    [SerializeField] private TextMeshProUGUI tileScoreRangeText;
    [SerializeField] private Image tileImage;

    [Header("Tile Sprites (assign in Inspector)")]
    [SerializeField] private Sprite forestSprite;
    [SerializeField] private Sprite citySprite;
    [SerializeField] private Sprite farmlandSprite;
    [SerializeField] private Sprite grasslandSprite;
    [SerializeField] private Sprite habitatSprite;
    [SerializeField] private Sprite roadSprite;

    private void Start()
    {
        ApplyTileContent(GameState.SelectedTileType);
    }

    private void ApplyTileContent(string tileType)
    {
        if (string.IsNullOrEmpty(tileType))
        {
            Debug.LogWarning("[SinglePatchController] No tile type selected. Defaulting to forest.");
            tileType = "forest";
        }

        switch (tileType.ToLower())
        {
            case "forest":
                SetContent("Forest", forestSprite, "Score: 80–100",
                    "Forests are the Florida panther's home. They give panthers places to hunt for food, hide, and raise their kittens safely.");
                break;
            case "city":
                SetContent("City", citySprite, "Score: 1–20",
                    "Cities and urban areas are difficult for panthers to cross safely. Heavy traffic and loss of natural habitat make these areas dangerous.");
                break;
            case "farmland":
                SetContent("Farmland", farmlandSprite, "Score: 30–60",
                    "Farmlands offer some open space for panthers to move through, but lack the shelter and prey density of natural habitats.");
                break;
            case "grassland":
                SetContent("Grassland", grasslandSprite, "Score: 40–70",
                    "Grasslands provide open corridors for panther movement and support prey species like deer that panthers depend on.");
                break;
            case "habitat":
                SetContent("Habitat", habitatSprite, "Score: N/A",
                    "Protected habitat patches are the starting and ending points of the panther's corridor. These are the areas we are trying to connect.");
                break;
            case "road":
                SetContent("Road", roadSprite, "Score: N/A",
                    "Roads are impassable barriers for panthers and cannot be selected as part of the corridor. Wildlife crossings are needed to help panthers cross safely.");
                break;
            default:
                Debug.LogWarning($"[SinglePatchController] Unknown tile type: {tileType}");
                break;
        }
    }

    private void SetContent(string name, Sprite sprite, string scoreRange, string description)
    {
        if (tileNameText != null) tileNameText.text = name;
        if (tileScoreRangeText != null) tileScoreRangeText.text = scoreRange;
        if (tileDescriptionText != null) tileDescriptionText.text = description;
        if (tileImage != null && sprite != null) tileImage.sprite = sprite;
    }

    public void OnBackClicked()
    {
        SceneLoader.LoadPatches();
    }
}
