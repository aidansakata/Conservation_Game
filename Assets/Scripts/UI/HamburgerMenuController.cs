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
}
