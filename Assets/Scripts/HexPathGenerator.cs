using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HexPathGenerator : MonoBehaviour
{
    [Header("Agents")]
    public GameObject agents;
    public bool useAgents;

    [Header("Prefab Settings")]
    public GameObject ballPrefab;
    public GameObject hexPrefab;
    public GameObject startHexPrefab;
    public GameObject holeHexPrefab;
    public GameObject wallPrefab;
    public GameObject gapPrefab;
    public GameObject checkpointPrefab;

    public List<GameObject> unavoidableObstaclePrefabs;
    public List<GameObject> obstaclePrefabs;

    [Header("Grid Settings")]
    public int gridWidth = 12; // Ancho del área
    public int gridHeight = 12; // Alto del área
    public int minRoomSize = 3; // Tamaño mínimo de una sala
    public int maxUnavoidableObstacleCount = 5;
    public int obstacleCount = 20; // Número de obstáculos


    [SerializeField] private GameObject floor;

    private List<Checkpoint> checkpoints;
    private bool[,] tiles;
    private int pathCode;
    private List<RectInt> rooms;
    private Vector2Int start;
    private Vector2Int goal;

    // Estructura para representar las regiones del BSP
    private class BSPRegion
    {
        public RectInt area;
        public BSPRegion leftChild, rightChild; // Subregiones
        public RectInt? room; // Habitación generada en esta región (solo para hojas)

        public BSPRegion(RectInt area)
        {
            this.area = area;
            room = null;
        }
    }

    void Start()
    {
        tiles = new bool[gridWidth, gridHeight];
        rooms = new List<RectInt>();
        checkpoints = new List<Checkpoint>();
        if (!useAgents) {
            agents.SetActive(false);
        }
        GenerateMinigolfPath();
    }

    void GenerateMinigolfPath()
    {
        // Genera el BSP
        BSPRegion root = new BSPRegion(new RectInt(0, 0, gridWidth, gridHeight));
        Subdivide(root, minRoomSize);
        GenerateRooms(root);
        GeneratePaths();

        FindLongestPath();

        RenderTiles();
        //PlaceObstacles();
    }

    public void ResetMap()
    {
        // Destroy all existing tiles and obstacles
        foreach (Transform child in transform) {
            if (child != floor.transform) {
                Destroy(child.gameObject);
            }
        }

        // Destroy all existing tiles and obstacles
        foreach (Transform agent in agents.transform) {
            Ball ball = agent.GetComponent<Ball>();
            ball.SetNextCheckpoint(0);
        }

        // Clear the tiles array and other variables
        tiles = new bool[gridWidth, gridHeight];
        rooms.Clear();
        checkpoints.Clear();
        

        // Generate a new map
        GenerateMinigolfPath();
    }

    // ----------------------------------------------------------------------------------------------------------------------
    // SUBDIVISION Y CONEXIONES DEL BSP
    // ----------------------------------------------------------------------------------------------------------------------
    void Subdivide(BSPRegion region, int minSize)
    {
        if (region.area.width < minSize * 2 || region.area.height < minSize * 2)
            return;

        bool divideHorizontally = Random.value > 0.5f;

        if (region.area.width > region.area.height)
            divideHorizontally = false;
        else if (region.area.height > region.area.width)
            divideHorizontally = true;

        int split;
        if (divideHorizontally) {
            split = Random.Range(minSize, region.area.height - minSize);
            region.leftChild = new BSPRegion(new RectInt(region.area.x, region.area.y, region.area.width, split));
            region.rightChild = new BSPRegion(new RectInt(region.area.x, region.area.y + split, region.area.width, region.area.height - split));
        }
        else {
            split = Random.Range(minSize, region.area.width - minSize);
            region.leftChild = new BSPRegion(new RectInt(region.area.x, region.area.y, split, region.area.height));
            region.rightChild = new BSPRegion(new RectInt(region.area.x + split, region.area.y, region.area.width - split, region.area.height));
        }

        Subdivide(region.leftChild, minSize);
        Subdivide(region.rightChild, minSize);
    }

    void GenerateRooms(BSPRegion region)
    {
        if (region.leftChild != null || region.rightChild != null) {
            if (region.leftChild != null)
                GenerateRooms(region.leftChild);
            if (region.rightChild != null)
                GenerateRooms(region.rightChild);
        }
        else {
            int roomWidth = Random.Range(minRoomSize, region.area.width - 1);
            int roomHeight = Random.Range(minRoomSize, region.area.height - 1);

            int roomX = region.area.x + Random.Range(0, region.area.width - roomWidth);
            int roomY = region.area.y + Random.Range(0, region.area.height - roomHeight);

            RectInt room = new RectInt(roomX, roomY, roomWidth, roomHeight);
            region.room = room;
            rooms.Add(room);

            //Añade room tiles
            for (int i = room.xMin; i < room.xMin + room.width; i++) {
                for (int j = room.yMin; j < room.yMin + room.height; j++) {
                    tiles[i, j] = true;
                }
            }
        }
    }

    void GeneratePaths()
    {

        for (int r = 0; r < rooms.Count - 1; r++) {
            ConnectRooms(rooms[r], rooms[r + 1]);
        }
    }

    void ConnectRooms(RectInt room1, RectInt room2)
    {
        // Selecciona un punto aleatorio en ambas habitaciones
        Vector2Int point1 = new Vector2Int(
            Random.Range(room1.xMin, room1.xMax),
            Random.Range(room1.yMin, room1.yMax)
        );

        Vector2Int point2 = new Vector2Int(
            Random.Range(room2.xMin, room2.xMax),
            Random.Range(room2.yMin, room2.yMax)
        );

        // Conecta los puntos con un camino en forma de L
        Vector2Int current = point1;
        while (current.x != point2.x) {
            tiles[current.x, current.y] = true;
            current.x += (point2.x > current.x) ? 1 : -1;
        }
        while (current.y != point2.y) {
            tiles[current.x, current.y] = true;
            current.y += (point2.y > current.y) ? 1 : -1;
        }
    }

    // ----------------------------------------------------------------------------------------------------------------------
    // DEFINICIION DE PUNTO FINAL E INICIAL EN BUSCA DE LAS DOS HABITCIONES MAS SEPARADAS
    // ----------------------------------------------------------------------------------------------------------------------
    Vector2Int GetRandomInnerPoint(RectInt room)
    {
        return new Vector2Int(
            Random.Range(room.xMin + 1, room.xMax - 1),
            Random.Range(room.yMin + 1, room.yMax - 1)
        );
    }

    void FindLongestPath()
    {
        List<Vector2Int> pathToGoal = new List<Vector2Int>();
        Vector2Int bestStart = Vector2Int.zero;
        Vector2Int bestFinish = Vector2Int.zero;
        int longestPathLength = 0;

        for (int i = 0; i < rooms.Count; i++) {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                RectInt roomA = rooms[i];
                RectInt roomB = rooms[j];

                Vector2Int pointA = GetRandomInnerPoint(roomA);
                Vector2Int pointB = GetRandomInnerPoint(roomB);

                (List<Vector2Int> path, int length) p = CalculatePath(pointA, pointB);
                int pathLength = p.length;
                if (pathLength > longestPathLength) {
                    longestPathLength = pathLength;
                    bestStart = pointA;
                    bestFinish = pointB;
                    pathToGoal = p.path;
                }
            }
        }

        start = bestStart;
        goal = bestFinish;

        // 50% de probabilidad de intercambiar inicio con fin (permite más variedad de mapas)
        if (Random.value < 0.5f)
        {
            Vector2Int temp = start;
            start = goal;
            goal = temp;
            pathToGoal.Reverse();
        }

        foreach (Vector2Int tile in pathToGoal) {
            bool inRoom = false;
            foreach (RectInt room in rooms) {
                if (room.Contains(tile)) {
                    inRoom = true;
                    break;
                }
            }
            if (!inRoom) {
                PlaceCheckpoint(tile);
            }
        }
    }

    // Place a checkpoint at a specific position
    void PlaceCheckpoint(Vector2Int position)
    {
        Vector3 worldPosition = GetTileWorldPosition(position.x, position.y);
        GameObject checkpoint = Instantiate(checkpointPrefab, transform);
        checkpoint.transform.localPosition = worldPosition;
        Checkpoint c = checkpoint.GetComponentInChildren<Checkpoint>();
        c.Initialize(this, position);
        checkpoints.Add(c);
    }


    (List<Vector2Int> path, int length) CalculatePath(Vector2Int startPoint, Vector2Int endPoint)
    {
        // BFS to calculate the shortest path and its length
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(startPoint);
        visited.Add(startPoint);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0) {
            Vector2Int current = queue.Dequeue();
            if (current == endPoint) {
                // Construct the path from the endPoint back to the startPoint
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int step = endPoint;

                while (step != startPoint) {
                    path.Add(step);
                    step = cameFrom[step];
                }
                path.Add(startPoint);
                path.Reverse();

                return (path, path.Count - 1);
            }

            foreach (Vector2Int dir in directions) {
                Vector2Int neighbor = current + dir;
                if (IsValidTile(neighbor.x, neighbor.y) && tiles[neighbor.x, neighbor.y] == true && !visited.Contains(neighbor)) {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        // If no path is found, return an empty path and max length
        return (new List<Vector2Int>(), int.MaxValue);
    }

    // ----------------------------------------------------------------------------------------------------------------------
    // RENDER
    // ----------------------------------------------------------------------------------------------------------------------
    void RenderTiles()
    {
        for (int i = 0; i < gridWidth; i++) {
            for (int j = 0; j < gridHeight; j++) {
                Vector3 pos;
                if (j % 2 == 0) {
                    pos = new Vector3(i + 0.5f, 0, j * 0.866f) * 2;
                }
                else {
                    pos = new Vector3(i, 0, j * 0.866f) * 2;
                }
                if (tiles[i, j] == true) {
                    GameObject tile;
                    if (start == new Vector2Int(i, j)) {
                        tile = Instantiate(startHexPrefab, transform);
                        tile.transform.localPosition = pos;
                        if (!useAgents) {
                            GameObject player = Instantiate(ballPrefab, tile.transform.GetChild(0).position, Quaternion.identity, transform);
                            player.GetComponent<Ball>().SetVelocity(new Vector3(0, -0.1f, 0));
                            player.GetComponent<Ball>().SetMap(this);
                        }
                        else {
                            foreach (Transform agent in agents.transform) {
                                agent.position = tile.transform.GetChild(0).position;
                                agent.GetComponent<Ball>().SetVelocity(new Vector3(0, -0.1f, 0));
                                agent.GetComponent<Ball>().SetMap(this);
                            }
                        }
                    } else if (goal == new Vector2Int(i, j)) {
                        tile = Instantiate(holeHexPrefab, transform);
                        tile.transform.localPosition = pos;
                    } else {
                        tile = Instantiate(hexPrefab, transform);
                        tile.transform.localPosition = pos;
                    }
                    PlaceWalls(i, j, tile);
                } else {
                    GameObject gap = Instantiate(gapPrefab, transform);
                    gap.transform.localPosition = pos;
                    
                }
            }
        }
    }

    void PlaceWalls(int x, int y, GameObject tile)
    {
        if (y % 2 == 1) {
            if (!IsValidTile(x - 1, y) || tiles[x - 1, y] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 180, 0), tile.transform);
            }
            if (!IsValidTile(x - 1, y - 1) || tiles[x - 1, y - 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 120, 0), tile.transform);
            }
            if (!IsValidTile(x - 1, y + 1) || tiles[x - 1, y + 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, -120, 0), tile.transform);
            }
            if (!IsValidTile(x + 1, y) || tiles[x + 1, y] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 0, 0), tile.transform);
            }
            if (!IsValidTile(x, y - 1) || tiles[x, y - 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 60, 0), tile.transform);
            }
            if (!IsValidTile(x, y + 1) || tiles[x, y + 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, -60, 0), tile.transform);
            }
        }
        else
        {
            if (!IsValidTile(x - 1, y) || tiles[x - 1, y] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 180, 0), tile.transform);
            }
            if (!IsValidTile(x, y - 1) || tiles[x, y - 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 120, 0), tile.transform);
            }
            if (!IsValidTile(x, y + 1) || tiles[x, y + 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, -120, 0), tile.transform);
            }
            if (!IsValidTile(x + 1, y) || tiles[x + 1, y] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 0, 0), tile.transform);
            }
            if (!IsValidTile(x + 1, y - 1) || tiles[x + 1, y - 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, 60, 0), tile.transform);
            }
            if (!IsValidTile(x + 1, y + 1) || tiles[x + 1, y + 1] == false) {
                Instantiate(wallPrefab, tile.transform.position, Quaternion.Euler(0, -60, 0), tile.transform);
            }
        }
        
    }

    void PlaceObstacles()
    {
        int placedUnavoidableObstacles = 0;
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>
        {
            start,
            goal
        };

        int i = 0;
        while (placedUnavoidableObstacles < maxUnavoidableObstacleCount) {
            Vector2Int randomTile = GetRandomInnerPoint(rooms[Random.Range(0, rooms.Count)]);

            if (tiles[randomTile.x, randomTile.y] == true && !occupiedPositions.Contains(randomTile)) {
                occupiedPositions.Add(randomTile);

                int r = Random.Range(0, unavoidableObstaclePrefabs.Count);
                GameObject randomUnavoidableObstacle = unavoidableObstaclePrefabs[r];
                Vector3 worldPosition = GetTileWorldPosition(randomTile.x, randomTile.y);
                GameObject obs = Instantiate(randomUnavoidableObstacle, transform);
                obs.transform.localPosition = worldPosition;

                placedUnavoidableObstacles++;
            }

            i++;

            if (i >= gridWidth * gridHeight)
            {
                break;
            }
        }

        int placedObstacles = 0;
        while (placedObstacles < obstacleCount) {
            Vector2Int randomTile = new Vector2Int(Random.Range(0, gridWidth), Random.Range(0, gridHeight));

            if (tiles[randomTile.x, randomTile.y] == true && !occupiedPositions.Contains(randomTile)) {
                occupiedPositions.Add(randomTile);

                int r = Random.Range(0, obstaclePrefabs.Count);
                GameObject randomObstacle = obstaclePrefabs[r];
                Vector3 worldPosition = GetTileWorldPosition(randomTile.x, randomTile.y);
                GameObject obs = Instantiate(randomObstacle, transform);
                obs.transform.localPosition = worldPosition;

                placedObstacles++;
            }
        }
    }

    // ----------------------------------------------------------------------------------------------------------------------
    // HELPER Y AGENT FUNCTIONS
    // ----------------------------------------------------------------------------------------------------------------------
    bool IsValidTile(int x, int y)
    {
        return x >= 0 && x < gridWidth &&
               y >= 0 && y < gridHeight;
    }

    public Vector3 GetTileWorldPosition(int x, int y)
    {
        if (y % 2 == 0) {
            return new Vector3(x + 0.5f, 0, y * 0.866f) * 2;
        }
        else {
            return new Vector3(x, 0, y * 0.866f) * 2;
        }
    }

    public Vector2Int GetClosestTileGridPosition(Vector3 position)
    {
        float closestDistance = Mathf.Infinity;
        Vector2Int closestTileCoordinates = Vector2Int.zero;

        for (int y = 0; y < tiles.GetLength(1); y++) {
            for (int x = 0; x < tiles.GetLength(0); x++) {
                if (tiles[x, y] == true) {
                    Vector3 tileWorldPosition = GetTileWorldPosition(x, y);

                    float distance = Vector3.Distance(position, tileWorldPosition);

                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestTileCoordinates = new Vector2Int(x, y);
                    }
                }
            }
        }

        return closestTileCoordinates;
    }

    public Vector3 GetGoalWorldPosition()
    {
        return GetTileWorldPosition(goal.x, goal.y);
    }

    public Vector3 GetStartWorldPosition()
    {
        return GetTileWorldPosition(start.x, start.y);
    }

    public List<Vector3> GetNextObjectivesWorldPosition(Ball ball, int amount)
    {
        List<Vector3> objectivePositions = new List<Vector3>();
        for (int i = 0; i < amount; i++) {
            if (ball.GetNextCheckpointIndex() + i >= checkpoints.Count) {
                objectivePositions.Add(GetGoalWorldPosition());
            }
            else {
                objectivePositions.Add(checkpoints[ball.GetNextCheckpointIndex() + i].transform.parent.localPosition);
            }
        }
        return objectivePositions;
    }
    public Vector3 GetObjectiveWorldPosition(Ball ball)
    {
        if (ball.GetNextCheckpointIndex() >= checkpoints.Count) {
            return (GetGoalWorldPosition());
        }
        else {
            return checkpoints[ball.GetNextCheckpointIndex()].transform.parent.localPosition;
        }
    }

    public Vector3 GetDirectionToObjective(Ball ball)
    {
        if (ball.GetNextCheckpointIndex() >= checkpoints.Count) {
            return (GetGoalWorldPosition() - ball.transform.localPosition);
        } else {
            return checkpoints[ball.GetNextCheckpointIndex()].transform.parent.localPosition - ball.transform.localPosition;
        }
        
    }

    public float GetDistanceToObjective(Ball ball)
    {
        return (GetObjectiveWorldPosition(ball) - ball.transform.localPosition).magnitude;

    }

    public int GetGridDistanceToGoal(Ball ball)
    {
        Vector2Int gridPosition = GetClosestTileGridPosition(ball.transform.localPosition);

        return CalculatePath(gridPosition, goal).length;
    }

    public int GetGridDistanceToObjective(Ball ball)
    {
        Vector2Int gridPosition = GetClosestTileGridPosition(ball.transform.localPosition);

        if (ball.GetNextCheckpointIndex() >= checkpoints.Count) {
            return GetGridDistanceToGoal(ball);
        }
        else {
            return CalculatePath(gridPosition, checkpoints[ball.GetNextCheckpointIndex()].gridPosition).length;
        }

    }

    public void OnCheckpointEnter(Checkpoint checkpoint, Ball ball)
    {
        if (ball == null) {
            Debug.Log("null ball");
            return;
        }

        if (checkpoints.IndexOf(checkpoint) >= ball.GetNextCheckpointIndex()) {
            ball.SetNextCheckpoint(checkpoints.IndexOf(checkpoint) + 1);
            
            if (useAgents) {
                MoveToGoalAgent agent = ball.GetComponent<MoveToGoalAgent>();
                if (agent == null) return;
                agent.AddReward(1f);
            }
        }
    }

    public void DisableFloor()
    {
        floor.SetActive(false);
    }

    public void EnableFloor()
    {
        floor.SetActive(true);
    }
}
