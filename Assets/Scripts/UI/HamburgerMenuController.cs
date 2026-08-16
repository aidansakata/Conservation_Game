using UnityEngine;

public class HamburgerMenuController : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public void Open()  { if (panel != null) panel.SetActive(true); }
    public void Close() { if (panel != null) panel.SetActive(false); }

    public void Toggle()
    {
        if (panel == null)
        {
            Debug.Log("[Hamburger] Clicked - menu overlay not yet implemented");
            return;
        }
        panel.SetActive(!panel.activeSelf);
    }

    // Overlay "Home" item. SceneLoader is a static class, so a UnityEvent cannot bind to
    // it directly; this controller already sits on the overlay prefab root and is the
    // natural owner, which also lets all four instances inherit one prefab-level binding.
    public void GoHome() => SceneLoader.LoadMainMenu();
}
