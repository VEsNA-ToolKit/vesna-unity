using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AutonomousWalking : MovementModel
{
    private NavMeshAgent agent;
    private GameObject[] waypoints;
    private int currentWP = 0;   

    public GameObject[] Waypoints
    {
        set { waypoints = value; }
    }

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();       
    }

    public override void StartWalking()
    {
        StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        while (!isStopped)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    Walk();
                    currentWP++;
                    if(currentWP >= waypoints.Length)
                    {
                        currentWP = 0;
                        System.Array.Reverse(waypoints);
                    }
                }
            }
            yield return null;  // Check every frame
        }
    }

    public void Walk()
    {
        if (!agent.hasPath || agent.velocity.magnitude < 0.1f)
        {            
            // Check if the random position is on the NavMesh                        
            agent.SetDestination(waypoints[currentWP].transform.position);            
        }
    }
}
