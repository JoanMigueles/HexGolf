using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Vector2Int gridPosition;
    private HexPathGenerator map;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) {
            Ball ball = other.GetComponent<Ball>();
            if (ball != null)
            map.OnCheckpointEnter(this, ball);
        }
    }

    public void Initialize(HexPathGenerator generator, Vector2Int gridPosition)
    {
        map = generator;
        this.gridPosition = gridPosition;
    }
}
