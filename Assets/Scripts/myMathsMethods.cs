using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class myMathsMethods : MonoBehaviour
{
    // Container for useful methods that can be called from other functions.



    public static float Round(float value, int digit)
    {
        float multi = Mathf.Pow(10.0f, (float)digit);
        return Mathf.Round(value * multi) / multi;
    }
    public static float[] Linspace(float StartValue, float EndValue, int numberofpoints)
    {

        float[] parameterVals = new float[numberofpoints];
        float increment = Mathf.Abs(StartValue - EndValue) / (float)(numberofpoints);
        int j = 0; //will keep a track of the numbers 
        float nextValue = StartValue;
        for (int i = 0; i < numberofpoints; i++)
        {


            parameterVals[i] = nextValue;
            j++;
            if (j > numberofpoints)
            {
                //throw new IndexOutOfRangeException();
            }
            nextValue = nextValue + increment;
        }
        return parameterVals;



    }

}
