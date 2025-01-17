
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MoveToGoalAgent : Agent
{
    public bool requestDecisions = false;
    public bool coaching = false;
    public int agentNumber = 1;
    private Ball ball;
    private HexPathGenerator map;
    
    private int previousGridDistance;
    private int previousGridDistanceToGoal;

    private void FixedUpdate()
    {
        if (ball != null && ball.GetMap() != null && requestDecisions) {
            if (!coaching && !ball.IsMoving()) {
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
            Gizmos.color = Color.cyan; 
            Gizmos.DrawLine(transform.localPosition, map.GetObjectiveWorldPosition(ball));
            Vector2Int ballTile = map.GetClosestTileGridPosition(transform.localPosition);
        }

        Gizmos.color = originalColor;
    }

    public override void OnEpisodeBegin()
    {
        if (CompletedEpisodes <= 0) {
            ball = GetComponent<Ball>();
            map = ball.GetMap();
        }

        previousGridDistance = int.MaxValue;
        previousGridDistanceToGoal = int.MaxValue;
        ball.Reposition();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Local position
        sensor.AddObservation(transform.localPosition); // observar el vector posicion de la bola, 3 observations

        Vector3 goalPosition = map.GetGoalWorldPosition();
        sensor.AddObservation(goalPosition); // observar el vector posicion de la meta, 3 observations

        Vector3 objectivePosition = map.GetObjectiveWorldPosition(ball);
        sensor.AddObservation(objectivePosition); // observar el vector posicion del objetivo, 3 observations

        Vector3 direction = objectivePosition - transform.localPosition;
        sensor.AddObservation(direction); // observar el vector distancia del objetivo, 3 observations

        // (TOTAL = 12)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int angle = actions.DiscreteActions[0] * 10; // Ángulo (36 direcciones correspondiendo con las 90 direciones de los rayscasts)
        float strength = (actions.DiscreteActions[1] + 1) * 0.1f; // Fuerza del tiro (escala del 1 al 10)

        Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        direction = direction.normalized * strength * ball.speed;
        ball.SetVelocity(new Vector3(direction.x, 0, direction.y));
        GameManager.Instance.AddHit(agentNumber);
        AddReward(-1f / MaxStep); // Para darle prisa a la pelota
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        
    }

    private void GiveDistanceReward()
    {
        int newGridDistance = map.GetGridDistanceToObjective(ball);
        int newGridDistanceToGoal = map.GetGridDistanceToGoal(ball);
        if (!ball.checkpointPassed) {
            if (previousGridDistance - newGridDistance < 10) {
                if (previousGridDistance > newGridDistance) { // Si se acerca a la meta
                    float reward = (previousGridDistance - newGridDistance) * 0.1f; // Recompensar en base a cuanto se acerca a la meta
                    if (reward > 0.3) reward = reward * 2; // Recompensar tiros largos (que acercan más a la meta) duplicando la recompensa
                    AddReward(reward);
                }
                else {
                    AddReward(-0.1f); // Penalizar alejarse de la meta
                }
            }
            else {
                AddReward(2f); // Máxima recompensa de acercarse
            }
        } else {
            ball.checkpointPassed = false;
            if (newGridDistanceToGoal > previousGridDistanceToGoal) {
                AddReward(-1f); //Penalizar entrar y salir de un checkpoint
                int next = ball.GetNextCheckpointIndex() - 1;
                if (next < 0) { next = 0; }
                ball.SetNextCheckpoint(next);
            }
        }

        previousGridDistance = newGridDistance;
        previousGridDistanceToGoal = newGridDistanceToGoal;
    }
}
