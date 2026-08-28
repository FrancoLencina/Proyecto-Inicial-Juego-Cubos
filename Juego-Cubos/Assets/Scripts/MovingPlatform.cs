using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField]
    private PlatformPathway path;

    [SerializeField]
    private float _speed;

    private int nextCheckpointIndex;

    private Transform previousCheckpoint;
    private Transform nextCheckpoint;

    private float _timeToWaypoint;
    private float _elapsedTime;

    void Start()
    {
        TargetNextCheckpoint();
    }

    void FixedUpdate()
    {
        _elapsedTime += Time.deltaTime;

        float elapsedPercentage = _elapsedTime / _timeToWaypoint;
        elapsedPercentage = Mathf.SmoothStep(0, 1, elapsedPercentage);
        transform.position = Vector3.Lerp(previousCheckpoint.position, nextCheckpoint.position, elapsedPercentage);
        transform.rotation = Quaternion.Lerp(previousCheckpoint.rotation, nextCheckpoint.rotation, elapsedPercentage);

        if (elapsedPercentage >= 1)
        {
            TargetNextCheckpoint();
        }
    }

    private void TargetNextCheckpoint()
    {
        previousCheckpoint = path.GetWaypoint(nextCheckpointIndex);
        nextCheckpointIndex = path.GetNextWaypointIndex(nextCheckpointIndex);
        nextCheckpoint = path.GetWaypoint(nextCheckpointIndex);

        _elapsedTime = 0;

        float distanceToWaypoint = Vector3.Distance(previousCheckpoint.position, nextCheckpoint.position);
        _timeToWaypoint = distanceToWaypoint / _speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        other.transform.SetParent(this.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        other.transform.SetParent(null);
    }
}
