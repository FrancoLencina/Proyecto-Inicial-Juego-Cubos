using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformPathway : MonoBehaviour
{
    public Transform GetWaypoint(int checkpointIndex)
    {
        return transform.GetChild(checkpointIndex);
    }

    public int GetNextWaypointIndex(int currentCheckpointIndex)
    {
        int nextCheckpoint = currentCheckpointIndex + 1;

        if (nextCheckpoint == transform.childCount)
        {
            nextCheckpoint = 0;
        }

        return nextCheckpoint;
    }
}