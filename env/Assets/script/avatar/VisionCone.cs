using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class VisionCone : MonoBehaviour
{
    // Avatar game object
    private GameObject avatar;
    private NavMeshAgent agent;
    public float distance = 10;
    public float angle = 30;
    public float height = 1.0f;
    public Color meshColor = Color.red;
    // layers that the sensors is interested in
    public LayerMask layers;
    // Layers that occlude the object
    public LayerMask occlusionLayers;
    bool reachedArtifact = false;
    public bool ReachedArtifact
    {
        get { return reachedArtifact; } 
        set { reachedArtifact = value; }
    }
    // List of game objects that are inside the sensor's bound
    public List<GameObject> foundArtifacts = new List<GameObject>();
    public List<GameObject> FoundArtifacts
    {
        get { return foundArtifacts; }
    }
    // Buffer to store the colliders information that the sensor scans
    Collider[] colliders = new Collider[50];   
    int count;
    // instead of updating the sensor at each frame, use scanFrequency to control how frequently the sensor scans the environment
    public int scanFrequency = 30;
    float scanInterval;
    float scanTimer;

    Mesh mesh;

    void Start()
    {
        // Retrieve the parent object -> the avatar which sensor belongs
        avatar = transform.parent.gameObject;
        agent = avatar.GetComponent<NavMeshAgent>();
        scanInterval = 1.0f / scanFrequency;
    }

    void Update()
    {
        scanTimer -= Time.deltaTime;
        if (scanTimer < 0)
        {
            scanTimer += scanInterval;
            Scan(); 
        }

    }

    private void Scan()
    {
        // Physics.OverlapSphereNonAlloc Computes and stores colliders touching or inside the sphere into the provided buffer
        // Use the center of the sphere, the main point of the cone
        // Specify also the layers the cone can collide
        count = Physics.OverlapSphereNonAlloc(transform.position, distance, colliders, 
            layers, QueryTriggerInteraction.Collide);
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj= colliders[i].gameObject;
            if (IsInSight(obj))
            {                
                CheckLayer(obj);                
            }
        }
    }

    private void CheckLayer(GameObject obj)
    {
        // Discovered a shop
        if(obj.layer == LayerMask.NameToLayer("shops"))
        {
            reachedArtifact = true;
            // Stop the agent
            agent.isStopped = true;
        } 
        if (obj.layer == LayerMask.NameToLayer("shops") && !foundArtifacts.Contains(obj))
        {
            foundArtifacts.Add(obj);
            print("Agent " + avatar.name + " found new shop: " + obj.name);
        }
        else if (obj.layer == LayerMask.NameToLayer("pippo"))
        {
            obj.GetComponent<Renderer>().material.color = Color.blue;
        }        
    }

    // Check if the object is inside the angle radius of the vision cone
    public bool IsInSight(GameObject obj)
    {
        Vector3 origin = transform.position;
        Vector3 dest = obj.transform.position;
        Vector3 direction = dest - origin;
        if(direction.y < 0 || direction.y > height)
        {
            return false;
        }

        // check if is inside angle radius of the vision cone
        direction.y = 0;
        float deltaAngle = Vector3.Angle(direction, transform.forward);
        if(deltaAngle > angle)
        {
            return false;  
        }

        origin.y += height / 2;
        dest.y = origin.y;
        if(Physics.Linecast(origin, dest, occlusionLayers))
        {
            return false; 
        }

        return true;
    }

    private void OnValidate()
    {
        mesh = CreateWedgeMesh();
        scanInterval = 1.0f / scanFrequency;
    }

    private void OnDrawGizmos()
    {
        if (mesh)
        {
            Gizmos.color = meshColor;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
        }

        // Draws a wireframe sphere with center and radius
        Gizmos.DrawWireSphere(transform.position, distance);

        // For each object that collides with the cone, draw a little sphere on it
        Gizmos.color = Color.red;
        for (int i = 0; i < count; i++)
        {            
            Gizmos.DrawSphere(colliders[i].transform.position, 0.2f);
        }

        Gizmos.color = Color.green;
        foreach (var obj in foundArtifacts)
        {
            Gizmos.DrawSphere(obj.transform.position, 0.2f);
        }
    }

    /* Creating a triangle mesh each triangle has 3 vertices
     * Num triangles = 8 -> Num vertices = 8 * 3    
     */
    Mesh CreateWedgeMesh()
    {
        Mesh mesh = new Mesh();

        /* Split the mesh in segments
         * Each segment is like a piece of slice. Each segment has 4 triangles:
         * 2 triangles for the far side and 1 for bottom and top
         */
        int segments = 10;
        // 2 + 2 for left right side
        int numTriangles = (segments * 4) + 2 + 2;
        int numVertices = numTriangles * 3;

        // Define array of vertices to specify their position
        Vector3[] vertices = new Vector3[numVertices];

        int[] triangles = new int[numVertices];

        // Define main vertices for the bottom part
        Vector3 bottomCenter = Vector3.zero;
        Vector3 bottomLeft = Quaternion.Euler(0, -angle, 0) * Vector3.forward * distance;
        Vector3 bottomRight = Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;

        // Define main vertices for the top part
        Vector3 topCenter = bottomCenter + Vector3.up * height;
        Vector3 topLeft = bottomLeft + Vector3.up * height;
        Vector3 topRight = bottomRight + Vector3.up * height;

        // Keep track where we are in the vertices array so define integer
        int vert = 0;

        // Draw Left side has two triangles
        vertices[vert++] = bottomCenter;
        vertices[vert++] = bottomLeft;
        vertices[vert++] = topLeft;

        vertices[vert++] = topLeft;
        vertices[vert++] = topCenter;
        vertices[vert++] = bottomCenter;

        // Draw Right side has two triangles
        vertices[vert++] = bottomCenter;
        vertices[vert++] = topCenter;
        vertices[vert++] = topRight;

        vertices[vert++] = topRight;
        vertices[vert++] = bottomRight;
        vertices[vert++] = bottomCenter;

        float currentAngle = -angle;
        float deltaAngle = (angle * 2) / segments;

        for (int i = 0; i < segments; i++)
        {            
            bottomLeft = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * distance;
            bottomRight = Quaternion.Euler(0, currentAngle + deltaAngle, 0) * Vector3.forward * distance;
         
            topLeft = bottomLeft + Vector3.up * height; 
            topRight = bottomRight + Vector3.up * height;
 
            // Draw Far side two triangles
            vertices[vert++] = bottomLeft;
            vertices[vert++] = bottomRight;
            vertices[vert++] = topRight;

            vertices[vert++] = topRight;
            vertices[vert++] = topLeft;
            vertices[vert++] = bottomLeft;

            // Top has one triangle
            vertices[vert++] = topCenter;
            vertices[vert++] = topLeft;
            vertices[vert++] = topRight;

            // Bottom has one triangle        
            vertices[vert++] = bottomCenter;
            vertices[vert++] = bottomLeft;
            vertices[vert++] = bottomRight;


            currentAngle += deltaAngle;
        }

        // gir triangles numbers
        for (int i = 0; i < numVertices; i++)
        {
            triangles[i] = i;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

}
