using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Singleton Instance
    public static GameManager Instance { get; private set; }
    public TMP_Text hitsDisplay;

    private bool isPaused = false;
    private int hits = 0;

    private void Awake()
    {
        // Ensure Singleton
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        hits = 0;
    }

    public void AddHit()
    {
        hits++;
        if (hitsDisplay != null) {
            hitsDisplay.text = $"Hits: {hits}";
        }
    }

    // Method to Pause the Game
    public void PauseGame()
    {
        if (!isPaused) {
            isPaused = true;
            Time.timeScale = 0f; // Stops the game time
            Debug.Log("Game Paused");
        }
    }

    // Method to Resume the Game
    public void ResumeGame()
    {
        if (isPaused) {
            isPaused = false;
            Time.timeScale = 1f; // Resumes the game time
            Debug.Log("Game Resumed");
        }
    }
}
