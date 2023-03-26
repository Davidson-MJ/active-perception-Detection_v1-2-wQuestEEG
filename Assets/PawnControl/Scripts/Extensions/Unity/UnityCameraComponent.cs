using UnityEngine;
#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
using UnityEngine.VR;
#endif

namespace Passer.Tracking {

    public class UnityCameraComponent : SensorComponent {

#if !pUNITYXR && !UNITY_2020_1_OR_NEWER
        public UnityTrackerComponent tracker;

        new public Camera camera;

        public void Show(bool showModel) {
        }

        /// <summary>Create a new Unity Camera</summary>
        public static UnityCameraComponent Get(UnityTrackerComponent tracker) {
            Transform cameraTransform = tracker.transform.Find("Camera");
            if (cameraTransform == null) {
                GameObject trackerObject = new GameObject("Camera");
                cameraTransform = trackerObject.transform;

                cameraTransform.parent = tracker.transform;
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
            }

            UnityCameraComponent unityCamera = cameraTransform.GetComponent<UnityCameraComponent>();
            if (unityCamera == null) {
                unityCamera = cameraTransform.gameObject.AddComponent<UnityCameraComponent>();
                unityCamera.tracker = tracker;
            }

            unityCamera.camera = cameraTransform.GetComponent<Camera>();
            if (unityCamera.camera == null) {
                unityCamera.camera = cameraTransform.gameObject.AddComponent<Camera>();
                unityCamera.camera.nearClipPlane = 0.05F;

                cameraTransform.gameObject.AddComponent<AudioListener>();
            }

            // Need to add an AudioListener 

            return unityCamera;
        }

        public override void StartComponent(Transform trackerTransform) {
            base.StartComponent(trackerTransform);
            InitFader();
        }

        #region Fader
        protected Material fadeMaterial;

        protected void InitFader() {
            //Transform planeTransform = camera.transform.Find("Fader");
            //if (planeTransform != null)
            //    return;

            //GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            //plane.transform.name = "Fader";
            //plane.transform.parent = camera.transform;
            //plane.transform.localEulerAngles = new Vector3(-90, 0, 0);
            //plane.transform.localPosition = new Vector3(0, 0, camera.nearClipPlane + 0.01F);

            //Renderer renderer = plane.GetComponent<Renderer>();
            //if (renderer != null) {
            //    Shader fadeShader = Shader.Find("Standard");
            //    fadeMaterial = new Material(fadeShader);
            //    fadeMaterial.name = "FadeMaterial";
            //    fadeMaterial.SetFloat("_Mode", 2);
            //    fadeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            //    fadeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            //    fadeMaterial.SetInt("_ZWrite", 0);
            //    fadeMaterial.DisableKeyword("_ALPHATEST_ON");
            //    fadeMaterial.EnableKeyword("_ALPHABLEND_ON");
            //    fadeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            //    fadeMaterial.renderQueue = 3000;
            //    Color color = Color.black;
            //    color.a = 0.0F;
            //    fadeMaterial.SetColor("_Color", new Color(0, 0, 0, 0));
            //    renderer.material = fadeMaterial;
            //    renderer.enabled = true;
            //}

            //Collider c = plane.GetComponent<Collider>();
            //Object.DestroyImmediate(c);
        }

        //public void Fader(float f) {
        //    Color color = Color.black;
        //    color.a = Mathf.Clamp01(f);
        //    fadeMaterial.color = color;
        //}
        #endregion
#endif
    }

}