using UnityEngine;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#elif UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Passer {

    [InitializeOnLoad]
    public class NetworkedTransform_Check {
        /// <summary>Check if this gameObject has the right network transform code</summary>
        static NetworkedTransform_Check() {
            NetworkedTransform[] networkedTransforms = Object.FindObjectsOfType<NetworkedTransform>();

            foreach (NetworkedTransform networkedTransform in networkedTransforms)
                CheckForNetworkTransform(networkedTransform.gameObject);
        }

        public static void CheckForNetworkTransform(GameObject gameObject) {
            CheckNetworkTransformNone(gameObject);
            CheckNetworkTransformUnet(gameObject);
            CheckNetworkTransformPun(gameObject);
            CheckNetworkTransformBolt(gameObject);
            CheckNetworkTransformMirror(gameObject);
        }

#region None

        protected static void CheckNetworkTransformNone(GameObject gameObject) {
#if !hNW_UNET && !hNW_PHOTON && !hNW_MIRROR
            CleanupNetworkTransformUnet(gameObject);
            CleanupNetworkTransformPun(gameObject);
            CleanupNetworkTransformMirror(gameObject);
#endif
        }

#endregion

#region Unet

        protected static void CheckNetworkTransformUnet(GameObject gameObject) {
#if hNW_UNET && !UNITY_2019_1_OR_NEWER
#pragma warning disable 0618
            UnityEngine.Networking.NetworkIdentity networkIdentity = gameObject.GetComponent<UnityEngine.Networking.NetworkIdentity>();
            if (networkIdentity == null)
                networkIdentity = gameObject.AddComponent<UnityEngine.Networking.NetworkIdentity>();

            UnityEngine.Networking.NetworkTransform networkTransform = gameObject.GetComponent<UnityEngine.Networking.NetworkTransform>();
            if (networkTransform == null)
                networkTransform = gameObject.AddComponent<UnityEngine.Networking.NetworkTransform>();

            CleanupNetworkTransformPun(gameObject);
            CleanupNetworkTransformMirror(gameObject);
#pragma warning restore 0618
#endif
        }

        protected static void CleanupNetworkTransformUnet(GameObject gameObject) {
#if !UNITY_2019_1_OR_NEWER
#pragma warning disable 0618
            UnityEngine.Networking.NetworkTransform networkTransform = gameObject.GetComponent<UnityEngine.Networking.NetworkTransform>();
            if (networkTransform != null)
                DestroyComponent(networkTransform);

            UnityEngine.Networking.NetworkIdentity networkIdentity = gameObject.GetComponent<UnityEngine.Networking.NetworkIdentity>();
            if (networkIdentity != null)
                DestroyComponent(networkIdentity);
#pragma warning restore 0618
#endif
        }

#endregion

#region Photon PUN

        protected static void CheckNetworkTransformPun(GameObject gameObject) {
#if hNW_PHOTON
#if hPHOTON1
            PhotonView photonView = gameObject.GetComponent<PhotonView>();
            if (photonView == null) 
                photonView = gameObject.AddComponent<PhotonView>();                            

            PhotonTransformView transformView = gameObject.GetComponent<PhotonTransformView>();
            if (transformView == null) {
                transformView = gameObject.AddComponent<PhotonTransformView>();
                transformView.m_PositionModel.SynchronizeEnabled = true;
                transformView.m_RotationModel.SynchronizeEnabled = true;
                if (photonView.ObservedComponents == null)
                    photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
                photonView.ObservedComponents.Add(transformView);
            }

            CleanupNetworkTransformUnet(gameObject);
            CleanupNetworkTransformMirror(gameObject);
#elif hPHOTON2
            Photon.Pun.PhotonView photonView = gameObject.GetComponent<Photon.Pun.PhotonView>();
            if (photonView == null) {
                photonView = gameObject.AddComponent<Photon.Pun.PhotonView>();
                photonView.OwnershipTransfer = Photon.Pun.OwnershipOption.Takeover;
            }

            Photon.Pun.PhotonTransformView transformView = gameObject.GetComponent<Photon.Pun.PhotonTransformView>();
            if (transformView == null) {
                transformView = gameObject.AddComponent<Photon.Pun.PhotonTransformView>();
                transformView.m_SynchronizePosition = true;
                transformView.m_SynchronizeRotation = true;
                if (photonView.ObservedComponents == null)
                    photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
                photonView.ObservedComponents.Add(transformView);
            }

            CleanupNetworkTransformUnet(gameObject);
            CleanupNetworkTransformMirror(gameObject);
#endif
#endif
        }

        protected static void CleanupNetworkTransformPun(GameObject gameObject) {
#if hPHOTON1
            PhotonTransformView transformView = gameObject.GetComponent<PhotonTransformView>();
            if (transformView != null) 
                DestroyComponent(transformView);

            PhotonView photonView = gameObject.GetComponent<PhotonView>();
            if (photonView != null)
                DestroyComponent(photonView);
#elif hPHOTON2
            Photon.Pun.PhotonTransformView transformView = gameObject.GetComponent<Photon.Pun.PhotonTransformView>();
            if (transformView != null)
                DestroyComponent(transformView);

            Photon.Pun.PhotonView photonView = gameObject.GetComponent<Photon.Pun.PhotonView>();
            if (photonView != null)
                DestroyComponent(photonView);
#endif
        }

#endregion

#region Photon Bolt

        protected static void CheckNetworkTransformBolt(GameObject gameObject) {
#if hNW_BOLT && hBOLT
            CleanupNetworkTransformUnet(gameObject);
            CleanupNetworkTransformPun(gameObject);
            CleanupNetworkTransformMirror(gameObject);
#endif
        }

#endregion

#region Mirror

        protected static void CheckNetworkTransformMirror(GameObject gameObject) {
#if hNW_MIRROR && hMIRROR
            Mirror.NetworkIdentity networkIdentity = gameObject.GetComponent<Mirror.NetworkIdentity>();
            if (networkIdentity == null)
                networkIdentity = gameObject.AddComponent<Mirror.NetworkIdentity>();

            Mirror.NetworkTransform networkTransform = gameObject.GetComponent<Mirror.NetworkTransform>();
            if (networkTransform == null)
                networkTransform = gameObject.AddComponent<Mirror.NetworkTransform>();

            CleanupNetworkTransformUnet(gameObject);
            CleanupNetworkTransformPun(gameObject);
#endif
        }

        protected static void CleanupNetworkTransformMirror(GameObject gameObject) {
#if hMIRROR
            Mirror.NetworkTransform networkTransform = gameObject.GetComponent<Mirror.NetworkTransform>();
            if (networkTransform != null)
                DestroyComponent(networkTransform);

            Mirror.NetworkIdentity networkIdentity = gameObject.GetComponent<Mirror.NetworkIdentity>();
            if (networkIdentity != null)
                DestroyComponent(networkIdentity);
#endif
        }

#endregion

        protected static void DestroyComponent(Component component) {
            if (IsPrefab(component.gameObject))
                Object.DestroyImmediate(component, true);
            else
                Object.DestroyImmediate(component, true);
        }

        protected static bool IsPrefab(GameObject gameObject) {
#if UNITY_2018_3_OR_NEWER
            PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (prefabStage == null)
                return false;
            else
                return true;
#else
            PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
            if (prefabType == PrefabType.Prefab)
                return true;
            else
                return false;
#endif
        }

    }

    [CustomEditor(typeof(NetworkedTransform))]
    public class NetworkedTransform_Editor : Editor {

#region Enable

        public void OnEnable() {
            NetworkedTransform networkedTransform = (NetworkedTransform)target;

            NetworkedTransform_Check.CheckForNetworkTransform(networkedTransform.gameObject);
        }

        public override void OnInspectorGUI() {
#if hNW_BOLT && hBOLT
            NetworkedTransform transform = (NetworkedTransform)target;
            BoltEntity boltEntity = transform.GetComponent<BoltEntity>();
            if (boltEntity == null)
                EditorGUILayout.HelpBox("Object needs to have a Bolt Entity component for network grabbing", MessageType.Error);            
#endif
            base.OnInspectorGUI();
        }

#endregion

    }
}