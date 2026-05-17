using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        InvestigateSound,
        Chase,
        InvestigateLastSeen
    }

    [Header("References")]
    public Transform player;
    public List<RoomNode> allRooms = new List<RoomNode>();

    [Header("Movement")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6f;

    [Header("Vision")]
    public float viewDistance = 18f;
    public float viewAngle = 70f;
    public float closeSenseDistance = 4f;
    public LayerMask obstacleMask;

    [Header("Chase")]
    public float lostSightDelay = 3f;
    public float catchDistance = 1.5f;

    [Header("Patrol Decisions")]
    public float waitInRoomTime = 1.5f;

    [Header("Suspicion System")]
    public float totalSuspicionBudget = 200f;
    public float minBaseChance = 2f;
    public float maxBaseChance = 60f;
    public float suspicionUpdateInterval = 10f;
    public float hidingRoomIncrease = 2.4f;
    public float otherRoomDecrease = 0.1f;

    private NavMeshAgent agent;
    private EnemyState state = EnemyState.Patrol;

    private RoomNode currentRoom;
    private RoomNode targetRoom;
    private Vector3 lastSeenPosition;
    private float lostSightTimer = 0f;
    private float waitTimer = 0f;
    private float suspicionTimer = 0f;
    private Vector3 currentInvestigationPosition;
    private bool goingToExactInvestigationPosition = false;

    private Queue<RoomNode> investigationQueue = new Queue<RoomNode>();
    private RoomNode currentInvestigationRoom;
    private RoomNode forcedInvestigationRoom;
    private Coroutine activeHidingSpotCheck;

    private bool isCheckingHidingSpot = false;
    private PlayerHiding playerHiding;
    private GameManager gameManager;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerHiding = player.GetComponent<PlayerHiding>();
        gameManager = FindObjectOfType<GameManager>();

        if (allRooms.Count > 0)
        {
            currentRoom = GetClosestRoom(transform.position);
            ChooseNextPatrolRoom();
        }
    }

    void Update()
    {
        UpdateSuspicionSystem();

        if (CanSeePlayer() || CanSensePlayerNearby())
        {
            CancelActiveHidingSpotCheck();
            EnterChase();
        }

        if ((playerHiding == null || !playerHiding.IsHiding) && Vector3.Distance(transform.position, player.position) <= catchDistance)
            {
                Debug.Log("Player caught!");

                if (gameManager != null)
                {
                    gameManager.LoseGame();
                }
            }

        switch (state)
        {
            case EnemyState.Patrol:
                PatrolUpdate();
                break;

            case EnemyState.InvestigateSound:
                InvestigateUpdate();
                break;

            case EnemyState.Chase:
                ChaseUpdate();
                break;

            case EnemyState.InvestigateLastSeen:
                InvestigateUpdate();
                break;
        }
    }

    // -------------------------
    // PATROL
    // -------------------------

    void PatrolUpdate()
    {
        agent.speed = patrolSpeed;

        if (targetRoom == null)
        {
            ChooseNextPatrolRoom();
            return;
        }

        if (ReachedDestination())
        {
            currentRoom = targetRoom;

            if (!isCheckingHidingSpot)
            {
                float roll = Random.Range(0f, 100f);

                if (roll <= currentRoom.FinalCheckChance)
                {
                    StartHidingSpotCheck(CheckHidingSpots(currentRoom));
                    return;
                }
                else
                {
                    ChooseNextPatrolRoom();
                    return;
                }
            }

            waitTimer += Time.deltaTime;

            if (waitTimer >= waitInRoomTime)
            {
                waitTimer = 0f;
                ChooseNextPatrolRoom();
            }
        }
    }

    void ChooseNextPatrolRoom()
    {
        if (currentRoom == null)
        {
            currentRoom = GetClosestRoom(transform.position);
        }

        if (currentRoom == null || currentRoom.connectedRooms.Count == 0)
        {
            return;
        }

        targetRoom = currentRoom.connectedRooms[Random.Range(0, currentRoom.connectedRooms.Count)];
        agent.SetDestination(targetRoom.transform.position);
    }

    // -------------------------
    // CHASE
    // -------------------------

    void EnterChase()
    {
        state = EnemyState.Chase;
        agent.speed = chaseSpeed;
        lostSightTimer = 0f;
        lastSeenPosition = player.position;
    }

    void ChaseUpdate()
    {
        agent.speed = chaseSpeed;

        if (CanSeePlayer() || CanSensePlayerNearby())
        {
            lastSeenPosition = player.position;
            lostSightTimer = 0f;
            agent.SetDestination(player.position);
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            agent.SetDestination(lastSeenPosition);

            if (lostSightTimer >= lostSightDelay)
            {
                EnterInvestigation(lastSeenPosition);
            }
        }
    }

    // -------------------------
    // INVESTIGATION
    // -------------------------

public void HearNoise(Vector3 noisePosition)
{
    if (state == EnemyState.Chase)
        return;

    RoomNode sourceRoom = GetClosestRoom(noisePosition);

    if (sourceRoom == null)
    {
        Debug.LogWarning("Noise heard, but no closest room found.");
        return;
    }

    CancelActiveHidingSpotCheck();
    BuildInvestigationQueue(sourceRoom);

    state = EnemyState.InvestigateSound;
    agent.speed = patrolSpeed;

    currentInvestigationPosition = noisePosition;
    currentInvestigationRoom = sourceRoom;
    forcedInvestigationRoom = sourceRoom;
    goingToExactInvestigationPosition = true;

    agent.SetDestination(noisePosition);

    Debug.Log("Enemy heard noise. Going to exact noise position.");
}
private void BuildInvestigationQueue(RoomNode sourceRoom)
{
    investigationQueue.Clear();
    ClearInvestigationValues();

    // 1. Source room is checked first at the exact investigation position.
    sourceRoom.investigationChance = 100f;

    // 2. Adjacent rooms
    foreach (RoomNode neighbor in sourceRoom.connectedRooms)
    {
        if (neighbor == null) continue;

        neighbor.investigationChance = 60f;

        if (!investigationQueue.Contains(neighbor))
            investigationQueue.Enqueue(neighbor);
    }

    // 3. Second-level adjacent rooms
    foreach (RoomNode neighbor in sourceRoom.connectedRooms)
    {
        if (neighbor == null) continue;

        foreach (RoomNode secondNeighbor in neighbor.connectedRooms)
        {
            if (secondNeighbor == null) continue;
            if (secondNeighbor == sourceRoom) continue;

            secondNeighbor.investigationChance = Mathf.Max(secondNeighbor.investigationChance, 20f);

            if (!investigationQueue.Contains(secondNeighbor))
                investigationQueue.Enqueue(secondNeighbor);
        }
    }
}
private void GoToNextInvestigationRoom()
{
    if (investigationQueue.Count == 0)
    {
        ClearInvestigationValues();
        forcedInvestigationRoom = null;
        activeHidingSpotCheck = null;
        state = EnemyState.Patrol;
        currentRoom = GetClosestRoom(transform.position);
        ChooseNextPatrolRoom();
        return;
    }

    currentInvestigationRoom = investigationQueue.Dequeue();

    if (currentInvestigationRoom != null)
    {
        agent.SetDestination(currentInvestigationRoom.transform.position);
        Debug.Log("Investigating room: " + currentInvestigationRoom.roomId);
    }
}
    void EnterInvestigation(Vector3 position)
    {
        lastSeenPosition = position;
        RoomNode sourceRoom = GetClosestRoom(position);

        if (sourceRoom == null)
        {
            state = EnemyState.Patrol;
            currentRoom = GetClosestRoom(transform.position);
            ChooseNextPatrolRoom();
            return;
        }

        CancelActiveHidingSpotCheck();
        BuildInvestigationQueue(sourceRoom);

        state = EnemyState.InvestigateLastSeen;
        agent.speed = patrolSpeed;
        currentInvestigationPosition = position;
        currentInvestigationRoom = sourceRoom;
        forcedInvestigationRoom = sourceRoom;
        goingToExactInvestigationPosition = true;
        agent.SetDestination(position);
        isCheckingHidingSpot = false;
    }

    void InvestigateUpdate()
{
    if (!ReachedDestination()) return;

    // First go to exact noise / last seen position.
    if (goingToExactInvestigationPosition)
    {
        goingToExactInvestigationPosition = false;

        RoomNode room = currentInvestigationRoom;

        if (room == null)
            room = GetClosestRoom(transform.position);

        if (room != null && !isCheckingHidingSpot)
        {
            if (room == forcedInvestigationRoom || room.FinalCheckChance >= 100f)
            {
                StartHidingSpotCheck(CheckInvestigationRoom(room));
                return;
            }

            float roll = Random.Range(0f, 100f);

            if (roll <= room.FinalCheckChance)
            {
                StartHidingSpotCheck(CheckInvestigationRoom(room));
                return;
            }
        }

        GoToNextInvestigationRoom();
        return;
    }

    // Then continue investigation queue.
    RoomNode queueRoom = currentInvestigationRoom;

    if (queueRoom == null)
        queueRoom = GetClosestRoom(transform.position);

    if (queueRoom != null && !isCheckingHidingSpot)
    {
        float roll = Random.Range(0f, 100f);

        if (roll <= queueRoom.FinalCheckChance)
        {
            StartHidingSpotCheck(CheckInvestigationRoom(queueRoom));
            return;
        }
    }

    GoToNextInvestigationRoom();
}
    void ClearInvestigationValues()
    {
        foreach (RoomNode room in allRooms)
        {
            room.investigationChance = 0f;
        }
    }

    void StartHidingSpotCheck(System.Collections.IEnumerator checkRoutine)
    {
        CancelActiveHidingSpotCheck();
        activeHidingSpotCheck = StartCoroutine(checkRoutine);
    }

    void CancelActiveHidingSpotCheck()
    {
        if (activeHidingSpotCheck == null)
            return;

        StopCoroutine(activeHidingSpotCheck);
        activeHidingSpotCheck = null;
        isCheckingHidingSpot = false;
    }

    System.Collections.IEnumerator CheckInvestigationRoom(RoomNode room)
{
    isCheckingHidingSpot = true;

    int spotsToCheck = room.FinalCheckChance >= 100f ? room.hidingSpots.Count : DecideHidingSpotCount(room.FinalCheckChance);

    List<Transform> uncheckedSpots = new List<Transform>(room.hidingSpots);

    for (int i = 0; i < spotsToCheck; i++)
    {
        if (uncheckedSpots.Count == 0)
            break;

        int randomIndex = Random.Range(0, uncheckedSpots.Count);
        Transform selectedSpot = uncheckedSpots[randomIndex];
        uncheckedSpots.RemoveAt(randomIndex);

        if (selectedSpot == null)
            continue;

        if (TryCatchHiddenPlayerAtSpot(selectedSpot))
            yield break;

        agent.SetDestination(selectedSpot.position);

        while (!ReachedDestination())
        {
            if (CanSeePlayer())
            {
                isCheckingHidingSpot = false;
                activeHidingSpotCheck = null;
                EnterChase();
                yield break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.7f);

        if (TryCatchHiddenPlayerAtSpot(selectedSpot))
            yield break;
    }

    isCheckingHidingSpot = false;
    activeHidingSpotCheck = null;

    GoToNextInvestigationRoom();
}

    // -------------------------
    // HIDING SPOT CHECKING
    // -------------------------

    System.Collections.IEnumerator CheckHidingSpots(RoomNode room)
{
    isCheckingHidingSpot = true;

    int spotsToCheck = DecideHidingSpotCount(room.FinalCheckChance);

    List<Transform> uncheckedSpots = new List<Transform>(room.hidingSpots);

    for (int i = 0; i < spotsToCheck; i++)
    {
        if (uncheckedSpots.Count == 0)
            break;

        int randomIndex = Random.Range(0, uncheckedSpots.Count);
        Transform selectedSpot = uncheckedSpots[randomIndex];

        // Remove it immediately so it cannot be selected again
        uncheckedSpots.RemoveAt(randomIndex);

        if (selectedSpot == null)
            continue;

        if (TryCatchHiddenPlayerAtSpot(selectedSpot))
            yield break;

        agent.SetDestination(selectedSpot.position);

        while (!ReachedDestination())
        {
            if (CanSeePlayer())
            {
                isCheckingHidingSpot = false;
                activeHidingSpotCheck = null;
                EnterChase();
                yield break;
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.7f);

        if (TryCatchHiddenPlayerAtSpot(selectedSpot))
            yield break;
    }

    isCheckingHidingSpot = false;
    activeHidingSpotCheck = null;
    waitTimer = 0f;

    // Important: after checking, force enemy to leave the room
    ChooseNextPatrolRoom();
}
    int DecideHidingSpotCount(float chance)
    {
        if (chance < 30f)
        {
            return Random.Range(0, 2); // 0 or 1
        }
        else if (chance < 70f)
        {
            return Random.Range(1, 3); // 1 or 2
        }
        else
        {
            return Random.Range(2, 4); // 2 or 3
        }
    }

    bool TryCatchHiddenPlayerAtSpot(Transform selectedSpot)
    {
        HidingCabin checkedCabin = GetCheckedCabin(selectedSpot);

        if (checkedCabin == null || !checkedCabin.PlayerIsHidingHere)
            return false;

        Debug.Log("Enemy found player hiding in checked cabin!");
        isCheckingHidingSpot = false;
        activeHidingSpotCheck = null;

        if (agent != null)
            agent.ResetPath();

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null)
            gameManager.LoseGame();

        return true;
    }

    HidingCabin GetCheckedCabin(Transform selectedSpot)
    {
        if (selectedSpot == null)
            return null;

        HidingCabin checkedCabin = selectedSpot.GetComponent<HidingCabin>();

        if (checkedCabin != null)
            return checkedCabin;

        checkedCabin = selectedSpot.GetComponentInParent<HidingCabin>();

        if (checkedCabin != null)
            return checkedCabin;

        checkedCabin = selectedSpot.GetComponentInChildren<HidingCabin>();

        if (checkedCabin != null)
            return checkedCabin;

        HidingCabin[] allCabins = FindObjectsOfType<HidingCabin>();

        foreach (HidingCabin cabin in allCabins)
        {
            if (selectedSpot == cabin.transform || selectedSpot == cabin.hidePosition || selectedSpot == cabin.exitPosition)
                return cabin;
        }

        return null;
    }

    // -------------------------
    // SUSPICION UPDATE
    // -------------------------

    void UpdateSuspicionSystem()
    {
        suspicionTimer += Time.deltaTime;

        if (suspicionTimer < suspicionUpdateInterval) return;

        suspicionTimer = 0f;

        if (playerHiding == null) return;
        if (!playerHiding.IsHiding) return;
        if (playerHiding.CurrentRoom == null) return;

        RoomNode hiddenRoom = playerHiding.CurrentRoom;

        foreach (RoomNode room in allRooms)
        {
            if (room == hiddenRoom)
            {
                room.baseCheckChance += hidingRoomIncrease;
            }
            else
            {
                room.baseCheckChance -= otherRoomDecrease;
            }

            room.baseCheckChance = Mathf.Clamp(room.baseCheckChance, minBaseChance, maxBaseChance);
        }

        NormalizeBaseSuspicion();
    }

    void NormalizeBaseSuspicion()
    {
        float currentTotal = 0f;

        foreach (RoomNode room in allRooms)
        {
            currentTotal += room.baseCheckChance;
        }

        if (currentTotal <= 0f) return;

        float difference = totalSuspicionBudget - currentTotal;
        float perRoomAdjustment = difference / allRooms.Count;

        foreach (RoomNode room in allRooms)
        {
            room.baseCheckChance += perRoomAdjustment;
            room.baseCheckChance = Mathf.Clamp(room.baseCheckChance, minBaseChance, maxBaseChance);
        }
    }

    // -------------------------
    // VISION
    // -------------------------

    bool CanSeePlayer()
    {
        if (playerHiding != null && playerHiding.IsHiding)
        {
            return false;
        }

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);

        if (angle > viewAngle / 2f)
            return false;

        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized, distanceToPlayer, obstacleMask))
            return false;

        return true;
    }

    bool CanSensePlayerNearby()
    {
        if (playerHiding != null && playerHiding.IsHiding)
            return false;

        return Vector3.Distance(transform.position, player.position) <= closeSenseDistance;
    }

    // -------------------------
    // HELPERS
    // -------------------------

    bool ReachedDestination()
    {
        if (agent.pathPending) return false;

        return agent.remainingDistance <= agent.stoppingDistance + 0.3f;
    }

    RoomNode GetClosestRoom(Vector3 position)
    {
        RoomNode closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (RoomNode room in allRooms)
        {
            float distance = Vector3.Distance(position, room.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = room;
            }
        }

        return closest;
    }
}
