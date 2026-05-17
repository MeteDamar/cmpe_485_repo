using System.Collections.Generic;
using UnityEngine;

public class GridMapWithHallsGenerator : MonoBehaviour
{
    public enum GridDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    [Header("Room Prefabs")]
    public GameObject middleRoomPrefab;
    public GameObject edgeRoomPrefab;
    public GameObject cornerRoomPrefab;
    public GameObject corridorRoomPrefab;
    public GridDirection corridorPrefabOpenDirection = GridDirection.Up;
    public GameObject oneHallObjectiveRoomPrefab;
    public GridDirection oneHallPrefabOpenDirection = GridDirection.Up;

    [Header("Hall Prefab")]
    public GameObject hallPrefab;

    [Header("Objective Prefabs")]
    public GameObject keycardPrefab;
    public GameObject exitDoorPrefab;

    [Header("Objective Settings")]
    public int keyRoomCount = 3;
    public int startRow = 0;
    public int startColumn = 0;
    public bool avoidStartRoom = true;
    public Vector3 keycardSpawnOffset = new Vector3(0f, 1f, 0f);
    public Vector3 exitSpawnOffset = new Vector3(0f, 0f, 0f);
    public bool placeExitDoorOnClosedWall = true;
    public float exitDoorWallInset = 1f;

    [Header("Grid Settings")]
    public int rows = 5;
    public int columns = 5;

    [Header("Room Settings")]
    public float roomSpacing = 36f;
    public float roomOuterSize = 22f;



    [Header("Generated Parent")]
    public Transform generatedParent;

    private GameObject[,] rooms;
    private readonly List<Vector2Int> keyRoomCells = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, Vector2Int> objectiveRoomConnections = new Dictionary<Vector2Int, Vector2Int>();
    private Vector2Int exitRoomCell;
    private bool hasExitRoomCell = false;

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ClearMap();

        if (middleRoomPrefab == null)
        {
            Debug.LogError("Middle room prefab is not assigned.");
            return;
        }

        if (edgeRoomPrefab == null)
        {
            Debug.LogError("Edge room prefab is not assigned.");
            return;
        }

        if (cornerRoomPrefab == null)
        {
            Debug.LogError("Corner room prefab is not assigned.");
            return;
        }

        if (corridorRoomPrefab == null)
        {
            Debug.LogError("Corridor room prefab is not assigned.");
            return;
        }

        if (hallPrefab == null)
        {
            Debug.LogError("Hall prefab is not assigned.");
            return;
        }

        if (oneHallObjectiveRoomPrefab == null)
        {
            Debug.LogError("One hall objective room prefab is not assigned.");
            return;
        }

        if (generatedParent == null)
        {
            GameObject parentObject = new GameObject("GeneratedMap");
            generatedParent = parentObject.transform;
        }

        rooms = new GameObject[rows, columns];

        SelectObjectiveRoomCells();
        GenerateRooms();
        GenerateHalls();
        ConnectRoomNodes();
        GenerateObjectives();
        AssignRoomsToEnemy();

        Debug.Log("Map generated successfully.");
    }

    private void GenerateRooms()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 position = new Vector3(
                    col * roomSpacing,
                    0f,
                    row * roomSpacing
                );

                GameObject prefabToUse;
                Quaternion rotation;

                GetRoomPrefabAndRotation(row, col, out prefabToUse, out rotation);

                GameObject room = Instantiate(prefabToUse, position, rotation, generatedParent);
                room.name = $"Room_{row}_{col}";
                rooms[row, col] = room;
            }
        }
    }

    private void GenerateHalls()
{
    float hallLength = roomSpacing - roomOuterSize;

    for (int row = 0; row < rows; row++)
    {
        for (int col = 0; col < columns; col++)
        {
            // Create hall to the right room.
            if (col < columns - 1 && ShouldConnectCells(new Vector2Int(col, row), new Vector2Int(col + 1, row)))
            {
                CreateHorizontalHall(row, col, hallLength);
            }

            // Create hall to the forward/up room.
            if (row < rows - 1 && ShouldConnectCells(new Vector2Int(col, row), new Vector2Int(col, row + 1)))
            {
                CreateVerticalHall(row, col, hallLength);
            }
        }
    }
}

    private void CreateHorizontalHall(int row, int col, float hallLength)
    {
        Vector3 roomA = rooms[row, col].transform.position;
        Vector3 roomB = rooms[row, col + 1].transform.position;

        Vector3 hallPosition = (roomA + roomB) / 2f;

        // Horizontal hall uses default rotation.
        Quaternion rotation = Quaternion.identity;

        GameObject hall = Instantiate(hallPrefab, hallPosition, rotation, generatedParent);
        hall.name = $"Hall_H_{row}_{col}_to_{row}_{col + 1}";

        // Your hall prefab length axis is X.
        Vector3 scale = hall.transform.localScale;
        scale.x = hallLength;
        hall.transform.localScale = scale;
    }

    private void CreateVerticalHall(int row, int col, float hallLength)
    {
        Vector3 roomA = rooms[row, col].transform.position;
        Vector3 roomB = rooms[row + 1, col].transform.position;

        Vector3 hallPosition = (roomA + roomB) / 2f;

        // Rotate vertical halls, but still scale X because your prefab length axis is X.
        Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject hall = Instantiate(hallPrefab, hallPosition, rotation, generatedParent);
        hall.name = $"Hall_V_{row}_{col}_to_{row + 1}_{col}";

        Vector3 scale = hall.transform.localScale;
        scale.x = hallLength;
        hall.transform.localScale = scale;
    }

private void GetRoomPrefabAndRotation(
    int row,
    int col,
    out GameObject prefabToUse,
    out Quaternion rotation)
{
    Vector2Int cell = new Vector2Int(col, row);

    if (IsObjectiveRoomCell(cell))
    {
        prefabToUse = oneHallObjectiveRoomPrefab;
        rotation = GetObjectiveRoomRotation(cell);
        return;
    }

    List<GridDirection> openDirections = GetOpenDirections(cell);
    List<GridDirection> closedDirections = GetClosedDirections(openDirections);

    if (openDirections.Count == 4)
    {
        prefabToUse = middleRoomPrefab;
        rotation = Quaternion.identity;
        return;
    }

    if (openDirections.Count == 3)
    {
        prefabToUse = edgeRoomPrefab;
        rotation = GetEdgeRoomRotation(closedDirections[0]);
        return;
    }

    if (openDirections.Count == 2 && AreAdjacentDirections(closedDirections[0], closedDirections[1]))
    {
        prefabToUse = cornerRoomPrefab;
        rotation = GetCornerRoomRotation(closedDirections[0], closedDirections[1]);
        return;
    }

    if (openDirections.Count == 2)
    {
        prefabToUse = corridorRoomPrefab;
        rotation = GetCorridorRoomRotation(openDirections[0]);
        return;
    }

    if (openDirections.Count == 1)
    {
        prefabToUse = oneHallObjectiveRoomPrefab;
        rotation = GetOneHallRoomRotation(openDirections[0]);
        return;
    }

    Debug.LogWarning($"Room_{row}_{col} needs a room shape that is not assigned. Falling back to middle room.");
    prefabToUse = middleRoomPrefab;
    rotation = Quaternion.identity;
}

    private void ConnectRoomNodes()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                RoomNode current = rooms[row, col].GetComponent<RoomNode>();

                if (current == null)
                {
                    Debug.LogWarning($"Room_{row}_{col} has no RoomNode.");
                    continue;
                }

                current.roomId = $"Room_{row}_{col}";
                current.connectedRooms.Clear();

                Vector2Int currentCell = new Vector2Int(col, row);

                // Right neighbor
                if (col < columns - 1 && ShouldConnectCells(currentCell, new Vector2Int(col + 1, row)))
                {
                    RoomNode rightRoom = rooms[row, col + 1].GetComponent<RoomNode>();
                    if (rightRoom != null)
                    {
                        current.connectedRooms.Add(rightRoom);
                    }
                }

                // Left neighbor
                if (col > 0 && ShouldConnectCells(currentCell, new Vector2Int(col - 1, row)))
                {
                    RoomNode leftRoom = rooms[row, col - 1].GetComponent<RoomNode>();
                    if (leftRoom != null)
                    {
                        current.connectedRooms.Add(leftRoom);
                    }
                }

                // Forward / upper neighbor
                if (row < rows - 1 && ShouldConnectCells(currentCell, new Vector2Int(col, row + 1)))
                {
                    RoomNode upRoom = rooms[row + 1, col].GetComponent<RoomNode>();
                    if (upRoom != null)
                    {
                        current.connectedRooms.Add(upRoom);
                    }
                }

                // Back / lower neighbor
                if (row > 0 && ShouldConnectCells(currentCell, new Vector2Int(col, row - 1)))
                {
                    RoomNode downRoom = rooms[row - 1, col].GetComponent<RoomNode>();
                    if (downRoom != null)
                    {
                        current.connectedRooms.Add(downRoom);
                    }
                }
            }
        }
    }

    private void AssignRoomsToEnemy()
    {
        EnemyAI enemyAI = FindObjectOfType<EnemyAI>();

        if (enemyAI == null)
        {
            Debug.LogWarning("EnemyAI not found in scene. Rooms were generated, but not assigned to enemy.");
            return;
        }

        enemyAI.allRooms.Clear();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                RoomNode roomNode = rooms[row, col].GetComponent<RoomNode>();

                if (roomNode != null)
                {
                    enemyAI.allRooms.Add(roomNode);
                }
            }
        }

        Debug.Log("Rooms assigned to EnemyAI: " + enemyAI.allRooms.Count);
    }

    private void SelectObjectiveRoomCells()
    {
        keyRoomCells.Clear();
        objectiveRoomConnections.Clear();
        hasExitRoomCell = false;

        List<Vector2Int> availableCells = GetAvailableObjectiveCells();
        int neededRooms = keyRoomCount + 1;

        if (availableCells.Count < neededRooms)
        {
            Debug.LogError("Not enough cells to place key rooms and exit room.");
            return;
        }

        List<Vector2Int> selectedCells = PickUniqueCells(availableCells, neededRooms);

        for (int i = 0; i < keyRoomCount; i++)
        {
            keyRoomCells.Add(selectedCells[i]);
        }

        exitRoomCell = selectedCells[keyRoomCount];
        hasExitRoomCell = true;

        List<Vector2Int> oneHallCells = new List<Vector2Int>(keyRoomCells);
        oneHallCells.Add(exitRoomCell);

        HashSet<Vector2Int> oneHallCellSet = new HashSet<Vector2Int>(oneHallCells);

        foreach (Vector2Int objectiveCell in oneHallCells)
        {
            objectiveRoomConnections[objectiveCell] = PickObjectiveConnection(objectiveCell, oneHallCellSet);
        }
    }

    private List<Vector2Int> GetAvailableObjectiveCells()
    {
        List<Vector2Int> availableCells = new List<Vector2Int>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (avoidStartRoom && row == startRow && col == startColumn)
                    continue;

                availableCells.Add(new Vector2Int(col, row));
            }
        }

        return availableCells;
    }

    private List<Vector2Int> PickUniqueCells(List<Vector2Int> availableCells, int count)
    {
        List<Vector2Int> cellPool = new List<Vector2Int>(availableCells);
        List<Vector2Int> selectedCells = new List<Vector2Int>();

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, cellPool.Count);
            selectedCells.Add(cellPool[randomIndex]);
            cellPool.RemoveAt(randomIndex);
        }

        return selectedCells;
    }

    private Vector2Int PickObjectiveConnection(Vector2Int objectiveCell, HashSet<Vector2Int> objectiveCells)
    {
        List<Vector2Int> neighbors = GetValidNeighborCells(objectiveCell);
        List<Vector2Int> nonObjectiveNeighbors = new List<Vector2Int>();

        foreach (Vector2Int neighbor in neighbors)
        {
            if (!objectiveCells.Contains(neighbor))
                nonObjectiveNeighbors.Add(neighbor);
        }

        List<Vector2Int> candidates = nonObjectiveNeighbors.Count > 0 ? nonObjectiveNeighbors : neighbors;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private List<Vector2Int> GetValidNeighborCells(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        if (cell.x < columns - 1)
            neighbors.Add(new Vector2Int(cell.x + 1, cell.y));

        if (cell.x > 0)
            neighbors.Add(new Vector2Int(cell.x - 1, cell.y));

        if (cell.y < rows - 1)
            neighbors.Add(new Vector2Int(cell.x, cell.y + 1));

        if (cell.y > 0)
            neighbors.Add(new Vector2Int(cell.x, cell.y - 1));

        return neighbors;
    }

    private bool ShouldConnectCells(Vector2Int cellA, Vector2Int cellB)
    {
        bool aIsObjective = IsObjectiveRoomCell(cellA);
        bool bIsObjective = IsObjectiveRoomCell(cellB);

        if (!aIsObjective && !bIsObjective)
            return true;

        if (aIsObjective && objectiveRoomConnections.ContainsKey(cellA))
            return objectiveRoomConnections[cellA] == cellB;

        if (bIsObjective && objectiveRoomConnections.ContainsKey(cellB))
            return objectiveRoomConnections[cellB] == cellA;

        return false;
    }

    private List<GridDirection> GetOpenDirections(Vector2Int cell)
    {
        List<GridDirection> openDirections = new List<GridDirection>();

        AddOpenDirectionIfConnected(openDirections, cell, new Vector2Int(cell.x, cell.y + 1), GridDirection.Up);
        AddOpenDirectionIfConnected(openDirections, cell, new Vector2Int(cell.x + 1, cell.y), GridDirection.Right);
        AddOpenDirectionIfConnected(openDirections, cell, new Vector2Int(cell.x, cell.y - 1), GridDirection.Down);
        AddOpenDirectionIfConnected(openDirections, cell, new Vector2Int(cell.x - 1, cell.y), GridDirection.Left);

        return openDirections;
    }

    private void AddOpenDirectionIfConnected(
        List<GridDirection> openDirections,
        Vector2Int cell,
        Vector2Int neighbor,
        GridDirection direction)
    {
        if (!IsCellInBounds(neighbor))
            return;

        if (ShouldConnectCells(cell, neighbor))
            openDirections.Add(direction);
    }

    private List<GridDirection> GetClosedDirections(List<GridDirection> openDirections)
    {
        List<GridDirection> closedDirections = new List<GridDirection>();

        AddIfClosed(closedDirections, openDirections, GridDirection.Up);
        AddIfClosed(closedDirections, openDirections, GridDirection.Right);
        AddIfClosed(closedDirections, openDirections, GridDirection.Down);
        AddIfClosed(closedDirections, openDirections, GridDirection.Left);

        return closedDirections;
    }

    private void AddIfClosed(
        List<GridDirection> closedDirections,
        List<GridDirection> openDirections,
        GridDirection direction)
    {
        if (!openDirections.Contains(direction))
            closedDirections.Add(direction);
    }

    private bool IsCellInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
    }

    private bool AreAdjacentDirections(GridDirection first, GridDirection second)
    {
        int difference = Mathf.Abs((int)first - (int)second);
        return difference == 1 || difference == 3;
    }

    private Quaternion GetEdgeRoomRotation(GridDirection closedDirection)
    {
        float yRotation = GetDirectionAngle(closedDirection) - GetDirectionAngle(GridDirection.Down);
        return Quaternion.Euler(0f, yRotation, 0f);
    }

    private Quaternion GetCornerRoomRotation(GridDirection firstClosedDirection, GridDirection secondClosedDirection)
    {
        if (HasClosedDirections(firstClosedDirection, secondClosedDirection, GridDirection.Down, GridDirection.Left))
            return Quaternion.Euler(0f, 0f, 0f);

        if (HasClosedDirections(firstClosedDirection, secondClosedDirection, GridDirection.Down, GridDirection.Right))
            return Quaternion.Euler(0f, 270f, 0f);

        if (HasClosedDirections(firstClosedDirection, secondClosedDirection, GridDirection.Up, GridDirection.Right))
            return Quaternion.Euler(0f, 180f, 0f);

        if (HasClosedDirections(firstClosedDirection, secondClosedDirection, GridDirection.Up, GridDirection.Left))
            return Quaternion.Euler(0f, 90f, 0f);

        return Quaternion.identity;
    }

    private bool HasClosedDirections(
        GridDirection firstClosedDirection,
        GridDirection secondClosedDirection,
        GridDirection expectedFirst,
        GridDirection expectedSecond)
    {
        return (firstClosedDirection == expectedFirst && secondClosedDirection == expectedSecond)
            || (firstClosedDirection == expectedSecond && secondClosedDirection == expectedFirst);
    }

    private Quaternion GetOneHallRoomRotation(GridDirection openDirection)
    {
        float yRotation = GetDirectionAngle(openDirection) - GetDirectionAngle(oneHallPrefabOpenDirection);
        return Quaternion.Euler(0f, yRotation, 0f);
    }

    private Quaternion GetCorridorRoomRotation(GridDirection openDirection)
    {
        float yRotation = GetDirectionAngle(openDirection) - GetDirectionAngle(corridorPrefabOpenDirection);
        return Quaternion.Euler(0f, yRotation, 0f);
    }

    private bool IsObjectiveRoomCell(Vector2Int cell)
    {
        return keyRoomCells.Contains(cell) || (hasExitRoomCell && exitRoomCell == cell);
    }

    private Quaternion GetObjectiveRoomRotation(Vector2Int objectiveCell)
    {
        if (!objectiveRoomConnections.ContainsKey(objectiveCell))
            return Quaternion.identity;

        GridDirection targetDirection = GetDirectionToNeighbor(objectiveCell, objectiveRoomConnections[objectiveCell]);
        float yRotation = GetDirectionAngle(targetDirection) - GetDirectionAngle(oneHallPrefabOpenDirection);

        return Quaternion.Euler(0f, yRotation, 0f);
    }

    private GridDirection GetDirectionToNeighbor(Vector2Int fromCell, Vector2Int toCell)
    {
        if (toCell.x > fromCell.x)
            return GridDirection.Right;

        if (toCell.x < fromCell.x)
            return GridDirection.Left;

        if (toCell.y > fromCell.y)
            return GridDirection.Up;

        return GridDirection.Down;
    }

    private GridDirection GetOppositeDirection(GridDirection direction)
    {
        switch (direction)
        {
            case GridDirection.Up:
                return GridDirection.Down;
            case GridDirection.Right:
                return GridDirection.Left;
            case GridDirection.Down:
                return GridDirection.Up;
            default:
                return GridDirection.Right;
        }
    }

    private Vector3 GetDirectionVector(GridDirection direction)
    {
        switch (direction)
        {
            case GridDirection.Up:
                return Vector3.forward;
            case GridDirection.Right:
                return Vector3.right;
            case GridDirection.Down:
                return Vector3.back;
            default:
                return Vector3.left;
        }
    }

    private float GetDirectionAngle(GridDirection direction)
    {
        switch (direction)
        {
            case GridDirection.Right:
                return 90f;
            case GridDirection.Down:
                return 180f;
            case GridDirection.Left:
                return 270f;
            default:
                return 0f;
        }
    }

    private void GenerateObjectives()
    {
        if (keycardPrefab == null)
        {
            Debug.LogWarning("Keycard prefab is not assigned. Map generated without keycards.");
        }

        if (exitDoorPrefab == null)
        {
            Debug.LogWarning("Exit door prefab is not assigned. Map generated without win room.");
        }

        ObjectiveManager objectiveManager = FindObjectOfType<ObjectiveManager>();

        if (objectiveManager != null)
        {
            objectiveManager.totalKeycardsNeeded = keyRoomCount;
            objectiveManager.collectedKeycards = 0;
        }

        if (keyRoomCells.Count < keyRoomCount || !hasExitRoomCell)
        {
            Debug.LogError("Objective room cells were not selected.");
            return;
        }

        for (int i = 0; i < keyRoomCount; i++)
        {
            RoomNode keyRoom = GetRoomNodeAtCell(keyRoomCells[i]);
            SpawnKeycardInRoom(keyRoom, objectiveManager, i + 1);
        }

        SpawnExitInRoom(GetRoomNodeAtCell(exitRoomCell), objectiveManager, exitRoomCell);
    }

    private RoomNode GetRoomNodeAtCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= columns || cell.y < 0 || cell.y >= rows)
            return null;

        return rooms[cell.y, cell.x].GetComponent<RoomNode>();
    }

    private void SpawnKeycardInRoom(RoomNode room, ObjectiveManager objectiveManager, int keycardNumber)
    {
        if (keycardPrefab == null || room == null)
            return;

        GameObject keycardObject = Instantiate(
            keycardPrefab,
            room.transform.position + keycardSpawnOffset,
            Quaternion.identity,
            generatedParent
        );

        keycardObject.name = $"Keycard_{keycardNumber}_{room.roomId}";

        Keycard keycard = keycardObject.GetComponent<Keycard>();

        if (keycard != null)
        {
            keycard.objectiveManager = objectiveManager;
            keycard.room = room;
        }
        else
        {
            Debug.LogWarning(keycardObject.name + " has no Keycard component.");
        }
    }

    private void SpawnExitInRoom(RoomNode room, ObjectiveManager objectiveManager, Vector2Int roomCell)
    {
        if (exitDoorPrefab == null || room == null)
            return;

        Vector3 spawnPosition = room.transform.position + exitSpawnOffset;
        Quaternion spawnRotation = Quaternion.identity;

        if (placeExitDoorOnClosedWall && objectiveRoomConnections.ContainsKey(roomCell))
        {
            GridDirection openDirection = GetDirectionToNeighbor(roomCell, objectiveRoomConnections[roomCell]);
            GridDirection wallDirection = GetOppositeDirection(openDirection);
            float wallDistance = Mathf.Max(0f, (roomOuterSize * 0.5f) - exitDoorWallInset);

            spawnPosition = room.transform.position + GetDirectionVector(wallDirection) * wallDistance + exitSpawnOffset;
            spawnRotation = Quaternion.Euler(0f, GetDirectionAngle(wallDirection), 0f);
        }

        GameObject exitObject = Instantiate(
            exitDoorPrefab,
            spawnPosition,
            spawnRotation,
            generatedParent
        );

        exitObject.name = $"ExitDoor_{room.roomId}";

        ExitDoor exitDoor = exitObject.GetComponent<ExitDoor>();

        if (exitDoor != null)
        {
            exitDoor.objectiveManager = objectiveManager;
        }
        else
        {
            Debug.LogWarning(exitObject.name + " has no ExitDoor component.");
        }
    }

    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        if (generatedParent == null)
        {
            return;
        }

        for (int i = generatedParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(generatedParent.GetChild(i).gameObject);
        }

        Debug.Log("Generated map cleared.");
    }
}
