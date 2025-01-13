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
                agent.AddReward(3.5f);
                transform.GetComponentInParent<HexPathGenerator>().ResetMap();
                agent.EndEpisode();

            } else {
                transform.GetComponentInParent<HexPathGenerator>().ResetMap();
            }
        }
    }
}