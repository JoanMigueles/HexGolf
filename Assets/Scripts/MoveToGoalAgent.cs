using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MoveToGoalAgent : Agent
{
    public bool requestDecisions = false;
    public bool coaching = false;
    private Ball ball;
    private HexPathGenerator map;
    

    private float previousDistance;
    private int previousGridDistance;
    private int hits;

    private void FixedUpdate()
    {
        if (ball != null && ball.GetMap() != null && requestDecisions) {
            if (!coaching && !ball.IsMoving()) {
                GiveDistanceReward();
                RequestDecision();
            } else if (transform.GetComponentInChildren<ChargeHitSlider>().GetReleaseDirection().direction != Vector2.zero) {
                GiveDistanceReward();
                RequestDecision();
            }
            
        }
    }

    private void OnDrawGizmos()
    {
        Color originalColor = Gizmos.color;
        Gizmos.color = Color.blue;

        if (map != null) {
            List<Vector3> points = map.GetNextObjectivesWorldPosition(ball, 3);
            Gizmos.DrawLine(transform.localPosition, points[0]);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.localPosition, map.GetGoalWorldPosition());
        }

        Gizmos.color = originalColor;
    }

    public bool MovedForward()
    {
        int gridDistance = ball.GetMap().GetGridDistanceToGoal(ball);
        return previousGridDistance > gridDistance;
    }

    public override void OnEpisodeBegin()
    {
        if (CompletedEpisodes > 0) {
            //map.ResetMap();
        } else {
            ball = GetComponent<Ball>();
            map = ball.GetMap();
        }

        previousDistance = float.MaxValue;
        previousGridDistance = int.MaxValue;
        ball.Reposition();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 goalPosition = map.GetGoalWorldPosition();
        
        // positions
        sensor.AddObservation(transform.localPosition); // observar el vector posicion de la bola, 3 observations
        sensor.AddObservation(goalPosition); // observar meta, 3 observations
        int gridDistanceToGoal = map.GetGridDistanceToGoal(ball);
        sensor.AddObservation(gridDistanceToGoal); // observar distancia a la meta (en path), 1 observations


        // Checkpoint system
        List<Vector3> checkpointPositions = map.GetNextObjectivesWorldPosition(ball, 3);
        sensor.AddObservation(ball.GetNextCheckpointIndex()); // observar index checkpoint
        if (checkpointPositions.Count == 3) {
            foreach (Vector3 position in checkpointPositions) {
                sensor.AddObservation(position); // observar siguiente checkpoint, 3 * 3 = 9 observations
            }
        }
        else {
            for (int i = 0; i < 3; i++) {
                sensor.AddObservation(Vector3.zero);
            }
        }
        int gridDistanceToObjective = map.GetGridDistanceToObjective(ball);
        sensor.AddObservation(gridDistanceToObjective); // observar distancia al checkpoint, 1 observations
        Vector3 directionToObjective = map.GetDirectionToObjective(ball);
        sensor.AddObservation(directionToObjective); // observar direccion al checkpoint, 3 observations
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        Vector2 direction = new Vector2(actions.ContinuousActions[0], actions.ContinuousActions[1]);

        float strength = (actions.ContinuousActions[2] + 1) / 2;
        direction = direction.normalized * strength * ball.speed;
        ball.SetVelocity(new Vector3(direction.x, 0, direction.y));
        hits++;
        AddReward(-0.1f); // para darle prisa a la pelota
        if (hits >= MaxStep) {
            hits = 0;
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Get the direction and strength from the provided function
        (Vector2 direction, float strength) = transform.GetComponentInChildren<ChargeHitSlider>().GetReleaseDirection();
        Debug.Log(direction.ToString() + " " + strength);

        transform.GetComponentInChildren<ChargeHitSlider>().ResetReleaseDirection();

        var continuousActions = actionsOut.ContinuousActions;

        continuousActions[0] = direction.x; // X-component of the direction
        continuousActions[1] = direction.y; // Y-component of the direction
        continuousActions[2] = strength;    // Kick strength
    }

    private void GiveDistanceReward()
    {
        int newGridDistance = map.GetGridDistanceToGoal(ball);
        if (previousGridDistance - newGridDistance < 10) {
            if (previousGridDistance > newGridDistance) {
                AddReward((previousGridDistance - newGridDistance) * 0.1f);
            }
            else {
                AddReward(-0.05f);
            }
        }
        else {
            AddReward(1f);
        }

        float newDistance = map.GetDistanceToObjective(ball);
        int objectiveGridDistance = map.GetGridDistanceToObjective(ball);
        if (newDistance < previousDistance && objectiveGridDistance < 4) {
            AddReward(0.1f); // recompensar acercarse al objetivo
        }
        else if (objectiveGridDistance > 7) {
            AddReward(-0.1f);
        }

        previousDistance = newDistance;
        previousGridDistance = newGridDistance;
    }
}
