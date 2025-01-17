using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State
{
    Playing,
    Win,
    Lose
}

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    public bool checkpointPassed = false;

    private State state;

    private Rigidbody rb;
    private HexPathGenerator map;
    private int nextCheckpointIndex;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        nextCheckpointIndex = 0;
        state = State.Playing;
    }

    private void Update()
    {
        if (transform.position.y < -10f) {
            MoveToGoalAgent agent = GetComponent<MoveToGoalAgent>();
            if (agent != null) {
                agent.AddReward(-10f);
            }
            Reposition();
        }
    }


    public void Reposition()
    {
        map.EnableFloor();
        rb.velocity = new Vector3(0f, -0.1f, 0);
        nextCheckpointIndex = 0;
        transform.position = map.GetStartWorldPosition() + new Vector3(0f, 0.6f, 0f);
    }

    public void SetVelocity(Vector3 velocity)
    {
        rb.velocity = velocity;
    }

    public bool IsMoving()
    {
        if (rb.velocity.magnitude > 0.05) {
            return true;
        }
        return false;
    }

    public bool IsSlow()
    {
        if (rb.velocity.magnitude > 0.2) {
            return true;
        }
        return false;
    }

    public void SetNextCheckpoint(int nextCheckpointIndex)
    {
        this.nextCheckpointIndex = nextCheckpointIndex;
        checkpointPassed = nextCheckpointIndex > 0;
    }

    public void SetMap(HexPathGenerator generator)
    {
        map = generator;
    }

    public HexPathGenerator GetMap() { return map; }

    public int GetNextCheckpointIndex()
    {
        return nextCheckpointIndex;
    }

    public void SetState(State state)
    {
        this.state = state;
    }

    public State GetState()
    {
        return this.state;
    }
}
