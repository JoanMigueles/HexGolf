using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public Material triggeredMaterial;
    public Material floorTriggeredMaterial;

    private MeshRenderer goalRenderer;
    private MeshRenderer floorRenderer;

    void Start()
    {
        goalRenderer = GetComponent<MeshRenderer>();
        floorRenderer = GetComponentInParent<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            goalRenderer.material = triggeredMaterial;
            floorRenderer.material = floorTriggeredMaterial;

            MoveToGoalAgent agent = other.GetComponent<MoveToGoalAgent>();
            if (agent != null) {
                Debug.Log("Reached Goal!!");
                agent.AddReward(5f);

                /*
                foreach (Transform t in agent.transform.parent) {
                    MoveToGoalAgent a = t.GetComponent<MoveToGoalAgent>();
                    a.EndEpisode();
                }*/
            }
            other.GetComponent<Ball>().SetState(State.Win);
            other.gameObject.SetActive(false);
            GameManager.Instance.CheckEndgame();
        }
    }
}