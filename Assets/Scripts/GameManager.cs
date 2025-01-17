using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Security.Cryptography;

public class GameManager : MonoBehaviour
{
    // Singleton Instance
    public static GameManager Instance { get; private set; }
    public TMP_Text hitsDisplay;
    public TMP_Text agentHitsDisplay;
    public int maxHits = 25;
    public GameObject player;
    public GameObject agents;

    private bool isPaused = false;
    private int playerHits;
    private List<int> agentHits;

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

        playerHits = 0;
        agentHits = new List<int>();
    }

    private void Start()
    {
        int n = 1;
        foreach (Transform t in agents.transform) {
            MoveToGoalAgent agent = t.GetComponent<MoveToGoalAgent>();
            if (agent != null) {
                agent.agentNumber = n;
                agentHits.Add(0);
                n++;
            }
        }
    }

    public void AddHit(int playerNumber)
    {
        if (playerNumber == 0) {
            playerHits++;
            if (playerHits > maxHits) {
                player.GetComponent<Ball>().SetState(State.Lose);
                player.transform.GetChild(0).gameObject.SetActive(false);
                CheckEndgame();
            }
        } else {
            agentHits[playerNumber - 1] = agentHits[playerNumber - 1] + 1;
            if (agentHits[playerNumber - 1] > maxHits) {
                agents.transform.GetChild(playerNumber - 1).GetComponent<Ball>().SetState(State.Lose);
                agents.transform.GetChild(playerNumber - 1).gameObject.SetActive(false);
                CheckEndgame();
            }
        }

        UpdateDisplays();
    }

    private void UpdateDisplays()
    {
        if (hitsDisplay != null) {
            hitsDisplay.text = $"Hits: {playerHits}";
        }

        if (agentHitsDisplay != null) {
            string hits = "";
            for (int i = 1; i <= agentHits.Count; i++) {
                if (agentHits[i - 1] <= maxHits) {
                    hits += $"IA{i}: {agentHits[i - 1]}\n";
                }
                else {
                    hits += $"IA{i}: F\n";
                }

            }
            agentHitsDisplay.text = hits;
        }
    }

    public void CheckEndgame()
    {
        if (player.GetComponent<Ball>().GetState() == State.Playing) {
            return;
        }

        foreach (Transform t in agents.transform) {
            if (t.GetComponent<Ball>().GetState() == State.Playing) {
                return;
            }
        }

        HexPathGenerator map = player.GetComponent<Ball>().GetMap();
        CSVExporter csv = new CSVExporter();

        State s = player.GetComponent<Ball>().GetState();
        csv.ExportToCSV(0, playerHits, s == State.Win, map.GetPathLength());

        for(int i = 0; i < agents.transform.childCount; i++) {
            s = agents.transform.GetChild(i).GetComponent<Ball>().GetState();
            csv.ExportToCSV(i + 1, agentHits[i], s == State.Win, map.GetPathLength());
        }

        // End game
        EndGame();
    }

    public void EndGame()
    {
        playerHits = 0;
        for (int i = 0; i < agents.transform.childCount; i++) {
            agentHits[i] = 0;
        }
        UpdateDisplays();

        player.GetComponent<Ball>().SetState(State.Playing);

        foreach (Transform t in agents.transform) {
            t.GetComponent<Ball>().SetState(State.Playing);
        }

        HexPathGenerator map = player.GetComponent<Ball>().GetMap();
        map.ResetMap();
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
