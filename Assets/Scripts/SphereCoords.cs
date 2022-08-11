using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Convenience class for converting between Cartesian and spherical coordinates.  Based on formulas from
// https://www.cpp.edu/~ajm/materials/delsph.pdf and adapted to Unity's coordinate system.
public class SphereCoords
{
    public float radius; // AKA: ρ (rho)
    public float latitude; // AKA: θ (theta)
    public float longitude; // AKA: φ (phi)

    public SphereCoords(float radius, float latitude, float longitude)
    {
        this.radius = radius;
        this.latitude = latitude;
        this.longitude = longitude;
    }

    public SphereCoords(Vector3 cartesian)
    {
        radius = Mathf.Sqrt(Mathf.Pow(cartesian.x, 2f) + Mathf.Pow(cartesian.z, 2f) + Mathf.Pow(cartesian.y, 2f));
        latitude = Mathf.Atan2(Mathf.Sqrt(Mathf.Pow(cartesian.x, 2f) + Mathf.Pow(cartesian.z, 2f)), cartesian.y * -1f);
        longitude = Mathf.Atan2(cartesian.z, cartesian.x);
    }

    public Vector3 ToCartesian()
    {
        float x = radius * Mathf.Sin(latitude) * Mathf.Cos(longitude);
        float y = radius * Mathf.Cos(latitude) * -1f;
        float z = radius * Mathf.Sin(latitude) * Mathf.Sin(longitude);
        return new Vector3(x, y, z);
    }

    public static float AngleBetween(Vector3 self, Vector3 other, float radius)
    {
        // Find the Cartesian distance between the two nodes.
        float distance = Vector3.Distance(other, self);

        // Use the Cartesian distance to calculate the angle between these two points and the center of the sphere
        // using the law of cosines.  We can take a shortcut here since we know that a and b are both equal to the
        // radius of the sphere.  We're throwing in a clamp here to prevent floating point imprecision from
        // causing impossible triangles to form when the nodes are on opposite ends of the sphere from each other.
        return Mathf.Acos(1f - Mathf.Clamp(Mathf.Pow(distance, 2f) / (2f * Mathf.Pow(radius, 2f)), 0f, 2f));
    }

    public float AngleBetween(Vector3 other)
    {
        return AngleBetween(ToCartesian(), other, radius);
    }

    public float AngleBetween(SphereCoords other)
    {
        return AngleBetween(other.ToCartesian());
    }
}
