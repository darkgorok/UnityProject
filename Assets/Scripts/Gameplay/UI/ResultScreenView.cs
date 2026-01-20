using UnityEngine;

public class ResultScreenView : MonoBehaviour, IResultScreen
{
    [SerializeField] private GameObject root;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
        Hide();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}
