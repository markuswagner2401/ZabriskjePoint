using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] GameObject[] quitterGos;

    public void QuitGame(bool value)
    {
        if (value)
        {
            Debug.Log("Quit Game Requested");
            // Check if we are running in the Unity editor
            if (Application.isEditor)
            {
                // We can't actually quit the application while in the editor, 
                // so we'll just log a message
                Debug.Log("Attempted to quit game, but we are in the Unity editor.");
            }
            else
            {
                // We are in a build, quit the application
                Application.Quit();
            }
        }

        else
        {
            foreach (var item in quitterGos)
            {
                item.SetActive(false);
            }
        }

    }

    public void ShowQuitUI(bool value)
    {
        foreach (var item in quitterGos)
            {
                item.SetActive(value);
            }
    }
}
