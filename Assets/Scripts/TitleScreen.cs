using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour, IInputClickHandler
{
    void Start()
    {
        // Catching All Jestures
        InputManager.Instance.AddGlobalListener(gameObject);
    }

    /// Air Tap Event Handler
    public void OnInputClicked(InputClickedEventData eventData)
    {
        // Working When no Gaze
        if (!GazeManager.Instance.HitObject)
        {

            // Loading the Scene for "main"
            SceneManager.LoadScene("main");

        }
    }
}