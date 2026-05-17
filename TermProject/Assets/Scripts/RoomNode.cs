using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomNode : MonoBehaviour
{
    [Header("Room Info")]
    public string roomId;

    [Header("Connections")]
    public List<RoomNode> connectedRooms = new List<RoomNode>();

    [Header("Hiding Spots")]
    public List<Transform> hidingSpots = new List<Transform>();

    [Header("Probabilities")]
    [Range(0f, 100f)] public float baseCheckChance = 8f;
    [Range(0f, 100f)] public float investigationChance = 0f;

    public float FinalCheckChance
    {
        get { return Mathf.Clamp(baseCheckChance + investigationChance, 0f, 100f); }
    }

    public Transform GetRandomHidingSpot()
    {
        if (hidingSpots.Count == 0) return null;
        return hidingSpots[Random.Range(0, hidingSpots.Count)];
    }
}