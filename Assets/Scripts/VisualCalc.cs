using UnityEngine;

public class VisualCalc : MonoBehaviour
{
    public static float degDirection;
    public static float degDistance;
    public static float degDirectionJitter = 1f;
    public static float degDistanceJitter = 1f;
    public static float compX;
    public static float compZ;
    public static float degCentralE;
    public static float degPeripheralE;
    public static float jitter;

    public GameObject objHMD, objHoverscreen;

    public float visMScaling(float degEccentricity)
    {
        return 1f + .33f*degEccentricity*Mathf.Deg2Rad + .00007f*Mathf.Pow(degEccentricity*Mathf.Deg2Rad, 3);
    }
    public float visSize(float degSize, float distVis)
    {
        return 2 * distVis * Mathf.Tan(Mathf.Deg2Rad * degSize/2);
    }
    public static Vector3 visDirection(int LtoR, int TtoB) // Cam has different (fixed) target positions.
    {
        jitter = Random.Range(-degDirectionJitter, degDirectionJitter);
        if (LtoR == 0)
        {
            if (TtoB == 0)
            {
                degDirection = 135f + jitter;

            }
            else if (TtoB == 1)
            {
                degDirection = 180f + jitter;
            }
            else
            {
                degDirection = 225f + jitter;
            }
        }
        else if (LtoR == 1)
        {
            if (TtoB == 0)
            {
                degDirection = 90f + jitter; // 0f is unnecessary
            }
            else
            {
                degDirection = 270f + jitter;
            }
        }
        else
        {
            if (TtoB == 0)
            {
                degDirection = 45f + jitter;
            }
            else if (TtoB == 1)
            {
                degDirection = 0f + jitter;
            }
            else
            {
                degDirection = 315f + jitter;
            }
        }
        compX = Mathf.Sin(Mathf.Deg2Rad * degDirection);
        compZ = Mathf.Cos(Mathf.Deg2Rad * degDirection);
        Vector3 vectDirection = new Vector3(0, compX, compZ);
        return vectDirection;
    }
    public float[] visDistance(int Eccentricity)
    {
        Vector3 vectDiff = objHMD.transform.position - objHoverscreen.transform.position;
        jitter = Random.Range(-degDistanceJitter, degDistanceJitter);
        if (Eccentricity == 0)
        {
            degDistance = degCentralE + jitter;
        }
        else
        {
            degDistance = degPeripheralE + jitter;
        }
        //print(degDistance);
        //print("Distance Difference = " + vectDiff.magnitude);
        //print("Tan Angle = " + Mathf.Tan(Mathf.Deg2Rad * degDistance));
        //print(Mathf.Tan(Mathf.Deg2Rad * degDistance) * vectDiff.magnitude);
        float[] arrReturn = new float[2];
        arrReturn[0] = Mathf.Tan(Mathf.Deg2Rad * degDistance) * vectDiff.magnitude;
        arrReturn[1] = degDistance;
        return arrReturn;
    }

    public float visPracticalAngle(Vector3 vectEyeOrigin, Vector3 vectEyeDirection, Vector3 vectTargetLocation)
    {
        Vector3 vectTargetDirection = vectTargetLocation - vectEyeOrigin;
        return Vector3.Angle(vectEyeDirection, vectTargetDirection);
    }
    public float[,] objectRotationMatrix(Vector3 vectEulerAngle)
    {
        vectEulerAngle = vectEulerAngle * Mathf.Deg2Rad;
        float[,] matrix = new float[3,3];
        matrix[0,0] = Mathf.Cos(vectEulerAngle.y) * Mathf.Cos(vectEulerAngle.z);
        matrix[0,1] = -Mathf.Cos(vectEulerAngle.x)*Mathf.Sin(vectEulerAngle.z) + Mathf.Sin(vectEulerAngle.x)*Mathf.Sin(vectEulerAngle.y)*Mathf.Cos(vectEulerAngle.z);
        matrix[0,2] = Mathf.Sin(vectEulerAngle.x)*Mathf.Sin(vectEulerAngle.z) + Mathf.Cos(vectEulerAngle.x)*Mathf.Sin(vectEulerAngle.y)*Mathf.Cos(vectEulerAngle.z);
        matrix[1,0] = Mathf.Cos(vectEulerAngle.y) * Mathf.Cos(vectEulerAngle.z);
        matrix[1,1] = Mathf.Cos(vectEulerAngle.x)*Mathf.Cos(vectEulerAngle.z) + Mathf.Sin(vectEulerAngle.x)*Mathf.Sin(vectEulerAngle.y)*Mathf.Sin(vectEulerAngle.z);
        matrix[1,2] = -Mathf.Sin(vectEulerAngle.x)*Mathf.Cos(vectEulerAngle.z) + Mathf.Cos(vectEulerAngle.x)*Mathf.Sin(vectEulerAngle.y)*Mathf.Sin(vectEulerAngle.z);
        matrix[2,0] = -Mathf.Sin(vectEulerAngle.y);
        matrix[2,1] = Mathf.Sin(vectEulerAngle.x)*Mathf.Cos(vectEulerAngle.y);
        matrix[2,2] = Mathf.Cos(vectEulerAngle.x)*Mathf.Cos(vectEulerAngle.y);
        return matrix;
    }
}