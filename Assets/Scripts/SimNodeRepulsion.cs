using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimNodeRepulsion : MonoBehaviour
{
    public GameObject prefab;

    private const int NODE_COUNT = 100;
    private const float FORCE_DAMPENING = NODE_COUNT * 2;

    // If all nodes have positioned themselves to be at least this far apart from each other then we can accept the
    // solution and end the simulation.
    //private const float CONVERGE_ANGLE_ALLOWED = 0.0717f;
    private const float CONVERGE_ANGLE_ALLOWED = Mathf.PI;

    // If all nodes have moved less than this amount on any given update then we consider the simulation to have
    // converged.
    private const float CONVERGE_ANGLE_MOVED = 0.00001f;

    private GameObject[] nodes;
    private float[,] forces;
    private bool simulationConverged = false;
    private bool simulationEnded = false;

    private void Start()
    {
        PlaceNodes();
        print(string.Format("{0} nodes have been randomly distributed on the sphere.  Repulsion simulation is now " +
                            "in progress...", NODE_COUNT));
    }

    private void PlaceNodes()
    {
        // Get sphere radius from parent object.  Assume size won't change size.
        float radius = transform.localScale.x / 2f;

        nodes = new GameObject[NODE_COUNT];
        for (int i = 0; i < NODE_COUNT; i++)
        {
            PlaceNode(i, radius);
        }
    }

    private void PlaceNode(int i, float radius)
    {
        SphereCoords sphereCoords = null;
        float latitude, longitude;

        // Keep re-creating the node until it doesn't collide with any other nodes.
        bool collided = true;
        while (collided)
        {
            collided = false;

            // Convert random numbers to spherical coordinates such that the points will be evenly distributed on a
            // unit sphere.  Based on formulas from https://mathworld.wolfram.com/SpherePointPicking.html and adapted
            // to Unity's coordinate system.
            latitude = Mathf.Acos(Random.Range(-1f, 1f));
            longitude = Random.Range(0f, 1f) * 2f * Mathf.PI;
            sphereCoords = new SphereCoords(radius, latitude, longitude);

            // Check for collisions with other nodes
            for (int j = 0; j < i; j++)
            {
                if (sphereCoords.AngleBetween(nodes[j].transform.position) == 0f)
                {
                    collided = true;
                    break;
                }
            }
        }

        // Convert spherical coordinates to Cartesian so that Unity understands where to place the node.
        nodes[i] = Instantiate(prefab, sphereCoords.ToCartesian(), Quaternion.identity) as GameObject;
        nodes[i].name = string.Format("Node{0}", i);
    }

    private void Update()
    {
        SimNodes();
    }

    void SimNodes()
    {
        if (simulationEnded)
        {
            goto done;
        }

        float minAngleDetected = CalcForces();
        if (simulationEnded)
        {
            goto done;
        }

        if (SolutionFound(minAngleDetected))
        {
            print(string.Format("Solution found!  {0} nodes have positioned themselves at least {1} radians apart:",
                                NODE_COUNT, minAngleDetected));
            PrintNodes();
            simulationEnded = true;
            goto done;
        }

        if (simulationConverged)
        {
            print(string.Format("Solution rejected!  {0} nodes have converged, but are only at least {1} radians " +
                    "apart.", NODE_COUNT, minAngleDetected));
            simulationEnded = true;
            goto done;
        }

        MoveNodes(minAngleDetected);

    done:
        return;
    }

    private float CalcForces()
    {
        // Initialize minimum detected angle between any two nodes with largest possible value.
        float minAngleDetected = 2f * Mathf.PI;

        // We're going to brute force the repulsion simulation by calculating the proximity of every node to every
        // other node (O(n^2)).
        forces = new float[NODE_COUNT, 3];
        for (int i = 0; i < NODE_COUNT; i++)
        {
            CalcForce(i, ref minAngleDetected);
            if (simulationEnded)
            {
                goto done;
            }
        }

    done:
        return minAngleDetected;
    }

    private void CalcForce(int i, ref float minAngleDetected)
    {
        SphereCoords sphereCoords = new SphereCoords(nodes[i].transform.position);
        float xForceTotal = 0f;
        float yForceTotal = 0f;
        float zForceTotal = 0f;
        for (int j = 0; j < NODE_COUNT; j++)
        {
            CalcForceOther(sphereCoords, i, j, ref minAngleDetected,
                           ref xForceTotal, ref yForceTotal, ref zForceTotal);
            if (simulationEnded)
            {
                goto done;
            }
        }
        forces[i, 0] = xForceTotal;
        forces[i, 1] = yForceTotal;
        forces[i, 2] = zForceTotal;

    done:
        return;
    }

    // We could make this function signature less ugly by giving it a Tuple return type.  Alas Unity still doesn't
    // support C# 7+ so we can't do that here.
    private void CalcForceOther(SphereCoords sphereCoordsI, int i, int j, ref float minAngleDetected,
                                ref float xForceTotal, ref float yForceTotal, ref float zForceTotal)
    {
        if (i == j)
        {
            // Although you may find yourself repulsive, you have nowhere to run to.
            goto done;
        }

        float angleDetected = SphereCoords.AngleBetween(nodes[i].transform.position,
                                                        nodes[j].transform.position, sphereCoordsI.radius);
        if (angleDetected == 0f)
        {
            print(string.Format("Oops, node{0} collided with node{1}.  Time for the universe to explode!", i, j));
            simulationEnded = true;
            goto done;
        }
        if (angleDetected < minAngleDetected)
        {
            minAngleDetected = angleDetected;
        }

        // Determine force based on inverse squared angle and split it into directional components.
        float force = 1f / (FORCE_DAMPENING * Mathf.Pow(angleDetected, 2));
        Vector3 diffs = nodes[i].transform.position - nodes[j].transform.position;
        float diffSum = (Mathf.Abs(diffs.x) + Mathf.Abs(diffs.y) + Mathf.Abs(diffs.z));
        float ratioX = Mathf.Abs(diffs.x) / diffSum;
        float ratioY = Mathf.Abs(diffs.y) / diffSum;
        float ratioZ = Mathf.Abs(diffs.z) / diffSum;
        float directionX = nodes[i].transform.position.x > nodes[j].transform.position.x ? 1 : -1;
        float directionY = nodes[i].transform.position.y > nodes[j].transform.position.y ? 1 : -1;
        float directionZ = nodes[i].transform.position.z > nodes[j].transform.position.z ? 1 : -1;
        float forceX = force * ratioX * directionX;
        float forceY = force * ratioY * directionY;
        float forceZ = force * ratioZ * directionZ;
        xForceTotal += forceX;
        yForceTotal += forceY;
        zForceTotal += forceZ;

    done:
        return;
    }

    private bool SolutionFound(float minAngleDetected)
    {
        return minAngleDetected > CONVERGE_ANGLE_ALLOWED;
    }

    private void PrintNodes()
    {
        for (int i = 0; i < NODE_COUNT; i++)
        {
            SphereCoords sphereCoords = new SphereCoords(nodes[i].transform.position);
            float x = nodes[i].transform.position.x;
            float y = nodes[i].transform.position.y;
            float z = nodes[i].transform.position.z;
            float latRads = sphereCoords.latitude;
            float longRads = sphereCoords.longitude;
            float latDegs = latRads * 180f / Mathf.PI;
            float longDegs = longRads * 180f / Mathf.PI;
            print(string.Format("x:{0:0.####} y:{1:0.####} z:{2:0.####} " +
                                "latRads:{3:0.####} longRads:{4:0.####} " +
                                "latDegs:{5:0.####}° longDegs:{6:0.####}° ",
                                x, y, z, latRads, longRads, latDegs, longDegs));
        }
    }

    private void MoveNodes(float minAngleDetected)
    {
        // Reset value.  If any nodes haven't converged we'll set it to false again.
        simulationConverged = true;

        for (int i = 0; i < NODE_COUNT; i++)
        {
            MoveNode(i, minAngleDetected);
        }
    }

    private void MoveNode(int i, float minAngleDetected)
    {
        // Determine where the given forces will place the node, then normalize the vector so it still gets placed on
        // the sphere's surface.
        SphereCoords currentSpherical = new SphereCoords(nodes[i].transform.position);
        Vector3 currentCartesian = nodes[i].transform.position;
        Vector3 plannedForces = new Vector3(forces[i, 0], forces[i, 1], forces[i, 2]);
        Vector3 plannedCartesian = currentCartesian + plannedForces;
        SphereCoords plannedSpherical = new SphereCoords(plannedCartesian);
        float normalizeRatio = currentSpherical.radius / plannedSpherical.radius;
        plannedCartesian *= normalizeRatio;
        plannedSpherical.radius *= normalizeRatio;

        // We don't want nodes to run into each other, so we're only going to allow them to move 1/3rd of the smallest
        // detected angle between any two nodes.  This means that even if the two closest nodes are moving directly
        // towards each other there will still be some space left between them afterwards.  If the total forces on the
        // node would cause it to move further than this then we'll clamp them such that the resulting movement angle
        // is equal to the maximum that we just set.
        float maxAngleAllowed = minAngleDetected / 3f;
        float anglePlanned = currentSpherical.AngleBetween(plannedSpherical);
        float clampRatio = anglePlanned > maxAngleAllowed ? maxAngleAllowed / anglePlanned : 1f;
        if(clampRatio < 1f)
        {
            // Now we get to do this again with the reduced force.
            plannedForces *= clampRatio;
            plannedCartesian = currentCartesian + plannedForces;
            plannedSpherical = new SphereCoords(plannedCartesian);
            normalizeRatio = currentSpherical.radius / plannedSpherical.radius;
            plannedCartesian *= normalizeRatio;
        }

        // Move it!
        nodes[i].transform.position = plannedCartesian;

        // Did it move a meaningful amount?
        if (anglePlanned > CONVERGE_ANGLE_MOVED * clampRatio)
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
