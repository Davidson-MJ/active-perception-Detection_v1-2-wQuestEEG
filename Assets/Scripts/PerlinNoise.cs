
using UnityEngine;

public class PerlinNoise : MonoBehaviour
{

    /// <summary>
    ///  create pseudo random noise texture for the sphere.
    ///  Rotates it about an axis within a trial.
    /// </summary>
    /// 


    //resolution
    public int width = 256;
    public int height = 256;

    public float minContrast = 0; //adjust the greyscale of our noise map.
    public float scale = 50f; // zoom applied to Perlin noise. Heigher numbers greater resolution (higher SF).

    // includ offset to randomize noise gen
    private float offsetX = 1f;
    private float offsetY = 1f;
    public float tjitter;
    private float tremainingRotation;
    private float tremainingMask;
    private Vector3 myDir1;
    private Vector3 myDir2;
    private float rotAngle;
    private float flipInterval;
    private float expansionSpeed = .3f;
    Renderer renderer;
    runExperiment runExperiment; // used for listeners. (set texture at trial start).

     void Start()
    {
        renderer = GetComponent<Renderer>();
        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();
        offsetX = 1;// Random.Range(0f, 100f);
        offsetY = 1;// Random.Range(0f, 100f); // randomize noise.
        transform.Rotate(Vector3.up, 180f);
        tjitter =1f;
        rotAngle = 2f;
        myDir1 = Vector3.up;
        myDir2 = Vector3.left;
        // create texture on Start:
        renderer.material.mainTexture = GenerateTexture(offsetX, offsetY);

        flipInterval =1/20f; //  update?
    }
     void LateUpdate()
    {


        // random rotation, 
        //Update rotation direction, and texture after a short interval, to avoid jitter.

        //changeSphereRotation();


        // update Perlin noise mask
        //updatePerlinatFreq(flipInterval);
       


    }



    // fucntion to return 2D texture
    Texture2D GenerateTexture(float offsX, float offsY)
    {
        Texture2D texture = new Texture2D(width, height);
        //generate a perlin noise map for the texture:
       
        for (int x =0; x<width; x++)
        {
            for (int y=0; y<height; y++)
            {

                //set pixel
                Color color = CalculateColor(x, y, offsX, offsY);

               

                texture.SetPixel(x, y, color);
            }
        }

        // we may want to scale the texture (greyscale), to match trial conds.

        //apply the new tcolor data
        texture.Apply();
        return texture;

    }

    Color CalculateColor(int x, int y, float offsX, float offsY)
    {
        float xCoord = (float)x / width * scale + offsX; // bigger wiuth coords, more texture. offset randomizes each call.
        float yCoord =  (float)y / height * scale + offsY; ;

        float sample = Mathf.PerlinNoise(xCoord, yCoord);         // input in scaled coords [0 1]
       
        return new Color(sample, sample, sample); // keep grey scale.
    }

    private void changeSphereRotation()
    {

        if (tremainingRotation <= 0) // update direction of rotation.
        {


            if (Random.value < 0.5f)
            {
                myDir1 = Vector3.down;

            }
            else
            {
                myDir1 = Vector3.up;
            }
            // random rotation
            if (Random.value < 0.5f)
            {
                myDir2 = Vector3.left;
            }
            else
            {
                myDir2 = Vector3.right;
            }

            // reset 
            tremainingRotation = Random.Range(1.0f, 2f);
        }
        else
        {

            tremainingRotation -= Time.deltaTime;
            // use previous.
            transform.Rotate(myDir1, rotAngle);
            transform.Rotate(myDir2, rotAngle);
        }

    }

    void updatePerlinatFreq(float flipInterval)
    { // if the correct frames have passed for our update freq, then update!

        if (tremainingMask <= 0)
        {
            offsetX += expansionSpeed;
            offsetY +=  expansionSpeed;
           
            renderer.material.mainTexture = GenerateTexture(offsetX, offsetY);

            // reset 
            tremainingMask =flipInterval;
        } else
        {
            tremainingMask -= Time.deltaTime;
            
        }
        

    }
}
