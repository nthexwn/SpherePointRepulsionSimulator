using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimNodeRepulsion : MonoBehaviour
{
    public GameObject prefab;
    private TextHandler log;
    private const string logPath = "Assets/Resources/log.txt";

    private const int NODE_COUNT = 1000;
    private const float FORCE_DAMPENING = NODE_COUNT * 3.6f;
    private const float COLLISION_THRESHOLD = 0.0001f;

    // If all nodes have positioned themselves to be at least this far apart from each other then we can accept the
    // solution and end the simulation.
    private const float CONVERGE_ANGLE_ALLOWED = 360f;

    // If all nodes have moved less than this amount on any given update then we consider the simulation to have
    // converged.
    private const float CONVERGE_ANGLE_MOVED = -1f;

    // Variables for node placement algorithms
    private const float INTERVAL_DEGS = 10.0f;
    private const float REQUIRED_SEPARATION_DEGS = 4.1f;

    private float radius;
    private GameObject[] nodes;
    private List<GameObject> nodeList;
    private Quaternion[] rotations;
    private float[,] forces;
    private float minAngleGlobal;
    private float minAngleFound;
    private bool simulationConverged;
    private bool simulationEnded;
    private int nodeCount = 0;

    // Stupid workaround for Unity lumping everything onto the main thread so sleeps don't work
    private int stupidCounter;
    private const int STUPID_TIMER = 0;


    private void Start()
    {
        InitVars();
        PlaceNodes();
    }

    private void InitVars()
    {
        log = new TextHandler(logPath);
        nodes = new GameObject[NODE_COUNT];
        nodeList = new List<GameObject>();
        rotations = new Quaternion[NODE_COUNT];

        // Get sphere radius from attached object.  Assume size won't change size.
        radius = transform.localScale.x / 2f;

        // Pretend we're already halfway to our goal.  This will prevent excessive early log spam when new records are
        // rapidly being set.
        minAngleGlobal = CONVERGE_ANGLE_ALLOWED / 2f;

        // Initialize minimum detected angle between any two nodes to largest possible value.
        minAngleFound = 180f;

        simulationConverged = false;
        simulationEnded = false;

        stupidCounter = 0;
    }

    private void PlaceNodes()
    {
        /*
        for (int i = 0; i < NODE_COUNT; i++)
        {
            PlaceNode(i);
        }
        */

        float latitude = -90.0f;
        while (latitude < 90.0f)
        {
            float longitude = 0.0f;
            while (longitude < 360.0f)
            {
                Vector3 target = new Vector3(latitude * -1.0f, longitude);
                GameObject node = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                node.transform.eulerAngles = target;
                node.transform.Translate(Vector3.forward * radius);
                if (CanPlaceNode(node, radius))
                {
                    nodeList.Add(node);
                    nodeCount++;
                    log.Print(string.Format("node{0}: {1}{2} {3}{4}\n", nodeCount, longitude > 180 ? 360 - longitude : longitude, longitude > 180 ? 'W' : 'E', latitude, latitude < 0 ? 'S' : 'N'));
                } else {
                    Destroy(node);
                }
                longitude += INTERVAL_DEGS;
            }
            latitude += INTERVAL_DEGS;
        }
    }

    private bool CanPlaceNode(GameObject target, float radius)
    {
        bool can = true;
        foreach (GameObject other in nodeList)
        {
            float arcDistance = Mathf.Acos(Vector3.Dot(other.transform.position.normalized, target.transform.position.normalized));
            if (arcDistance < REQUIRED_SEPARATION_DEGS * Mathf.Deg2Rad)
            {
                can = false;
                break;
            }
        }
        return can;
    }

    private void PlaceNode(int i)
    {
        Vector3 random = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        // Keep re-creating the node until it doesn't collide with any other nodes.
        bool collided = true;
        while (collided)
        {
            collided = false;
            random = Random.onUnitSphere;
            rotation = Quaternion.FromToRotation(Vector3.forward, random);

            // Check for collisions with other nodes
            for (int j = 0; j < i; j++)
            {
                if (Quaternion.Angle(rotation, nodes[j].transform.rotation) < COLLISION_THRESHOLD)
                {
                    collided = true;
                    break;
                }
            }
        }
        nodes[i] = Instantiate(prefab, random, rotation) as GameObject;
        nodes[i].name = string.Format("Node{0}", i);
    }

    private void Update()
    {
        if (stupidCounter > STUPID_TIMER)
        {
            //SimNodes();
        }
        stupidCounter++;
    }

    void SimNodes()
    {
        if (simulationEnded)
        {
            goto done;
        }

        CalcRotations();
        if (simulationEnded)
        {
            goto done;
        }

        if (minAngleFound > minAngleGlobal)
        {
            minAngleGlobal = minAngleFound;
            if (SolutionFound())
            {
                log.Print("\nSolution found!");
                PrintNodes();
                simulationEnded = true;
                goto done;
            }
            else {
                log.Print("\nNew record!");
                PrintNodes();
            }
        }

        if (simulationConverged)
        {
            log.Print(string.Format("Solution rejected!  {0} nodes have converged, but are only at least {1} " +
                    "radians apart.", NODE_COUNT, minAngleFound));
            simulationEnded = true;
            goto done;
        }

        MoveNodes();

    done:
        return;
    }

    private void CalcRotations()
    {
        // Reset for this update
        minAngleFound = 180f;

        // We're going to brute force the repulsion simulation by calculating the proximity of every node to every
        // other node (O(n^2)) and applying a product of rotations based on the resulting forces.
        for (int i = 0; i < NODE_COUNT; i++)
        {
            CalcTotalRotation(i);
            if (simulationEnded)
            {
                goto done;
            }
        }

    done:
        return;
    }

    private void CalcTotalRotation(int i)
    {
        rotations[i] = Quaternion.identity;
        for (int j = 0; j < NODE_COUNT; j++)
        {
            CalcRotation(i, j);
            if (simulationEnded)
            {
                goto done;
            }
        }

    done:
        return;
    }

    private void CalcRotation(int i, int j)
    {
        if (i == j)
        {
            // Although you may find yourself repulsive, you have nowhere to run to.
            goto done;
        }

        float angleBetween = Quaternion.Angle(nodes[i].transform.rotation, nodes[j].transform.rotation);

        if (angleBetween < COLLISION_THRESHOLD)
        {
            log.Print(string.Format("Oops, node{0} collided with node{1}.  Time for the universe to explode!", i, j));
            simulationEnded = true;
            goto done;
        }
        if (angleBetween < minAngleFound)
        {
            minAngleFound = angleBetween;
        }

        // Determine force based on inverse squared angle and use it to create a rotation.
        float force = 1f / (FORCE_DAMPENING * Mathf.Pow(angleBetween, 2f));

        // We want to rotate away from the other node
        Quaternion relative = Quaternion.Inverse(nodes[i].transform.rotation) * nodes[j].transform.rotation;
        Quaternion rotation = Quaternion.LerpUnclamped(Quaternion.identity, relative, force / angleBetween * -1f);

        rotations[i] *= rotation;

    done:
        return;
    }

    private bool SolutionFound()
    {
        return minAngleGlobal > CONVERGE_ANGLE_ALLOWED;
    }

    private void PrintNodes()
    {
        log.Print(string.Format("{0} nodes have positioned themselves at least {1} degrees apart:", NODE_COUNT,
                                minAngleGlobal));
        for (int i = 0; i < NODE_COUNT; i++)
        {
            float x = nodes[i].transform.position.x;
            float y = nodes[i].transform.position.y;
            float z = nodes[i].transform.position.z;
            float latDegs = nodes[i].transform.rotation.eulerAngles.y;
            float longDegs = nodes[i].transform.rotation.eulerAngles.x;

            // TODO: Delete this after sanity checking (it should always be zero)
            float rollDegs = nodes[i].transform.rotation.eulerAngles.z;

            string padX = string.Format("x:{0}{1:0.000}", x >= 0f ? " " : "", x);
            string padY = string.Format("y:{0}{1:0.000}", y >= 0f ? " " : "", y);
            string padZ = string.Format("z:{0}{1:0.000}", z >= 0f ? " " : "", z);
            string padLatDegs = string.Format("latDegs:{0}{1:000.00}", latDegs >= 0f ? " " : "", latDegs);
            string padLongDegs = string.Format("longDegs:{0}{1:000.00}", longDegs >= 0f ? " " : "", longDegs);
            string padRollDegs = string.Format("rollDegs:{0}{1:000.00}", rollDegs >= 0f ? " " : "", rollDegs);
            string str = string.Format("{0} {1} {2} {3} {4} {5}", padX, padY, padZ, padLatDegs, padLongDegs,
                                       padRollDegs);
            log.Print(str);
        }
    }

    private void MoveNodes()
    {
        // Reset value.  If any nodes haven't converged we'll set it to false again.
        simulationConverged = true;

        for (int i = 0; i < NODE_COUNT; i++)
        {
            MoveNode(i);
        }
    }

    private void MoveNode(int i)
    {
        // We don't want nodes to run into each other, so we're only going to allow them to move 1/3rd of the smallest
        // detected angle between any two nodes.  This means that even if the two closest nodes are moving directly
        // towards each other there will still be some space left between them afterwards.  If the total rotations
        // applied to the node node would cause it to move further than this then we'll adjust them such that the
        // resulting movement angle is equal to the maximum that we just set.
        float maxAngleAllowed = minAngleFound / 3f;
        float anglePlanned = Quaternion.Angle(Quaternion.identity, rotations[i]);
        float adjustRatio = anglePlanned > maxAngleAllowed ? maxAngleAllowed / anglePlanned : 1f;
        if(adjustRatio < 1f)
        {
            rotations[i] = Quaternion.Lerp(Quaternion.identity, rotations[i], adjustRatio);
        }
        nodes[i].transform.rotation = nodes[i].transform.rotation * rotations[i];
        nodes[i].transform.position = nodes[i].transform.forward;
        //nodes[i].transform.Translate(nodes[i].transform.forward * radius);


        // Did it move a meaningful amount?
        if (anglePlanned > CONVERGE_ANGLE_MOVED)
        {
            simulationConverged = false;
        }
    }

    // Leftover from an earlier attempt to render the nodes as pixels on a canvas applied to the sphere as a material.
    // Abandoned because applying a 2D rendering canvas to a 3D surface led to projection issues which I didn't care
    // to solve.  Keeping this around since I might still find a use for this technique later.
    private void DynamicTexture()
    {
        var tex = new Texture2D(1024, 1024, TextureFormat.RGBA64, false);
        tex.SetPixel(0, 0, Color.red);
        tex.Apply();
        GetComponent<Renderer>().material.mainTexture = tex;
    }
}
