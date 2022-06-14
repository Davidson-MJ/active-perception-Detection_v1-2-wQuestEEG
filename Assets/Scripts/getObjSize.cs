using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class getObjSize : MonoBehaviour
{
    // is attached to GameObjects, to print their size in the console at runtime.
    void Start()
    {
        
        //Mesh mesh = GetComponent<MeshFilter>().mesh;
        //Vector3 objectSize = Vector3.Scale(transform.localScale, mesh.bounds.size);

        //print("mesh size is : " + mesh.bounds.size);
        //print(gameObject.name + " local scale is: " + transform.localScale);
        //print(gameObject.name + "sacled size is: " + objectSize);

        Vector3 boundsz= GetComponent<Collider>().bounds.size;
        Vector3 rendsz = GetComponent<Renderer>().bounds.size;
        
        print(gameObject.name + " renderer size " + rendsz.ToString("F4")) ; // F4 for 4 pos after the decimal
        print(gameObject.name + " boundary size " + boundsz.ToString("F4")); // F4 for 4 pos after the decimal

    }
}

