using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
#if hPHOTON2
using Photon.Pun;
#endif

namespace Passer.Humanoid {

    [CustomEditor(typeof(NetworkingStarter))]
    public class NetworkingStarter_Editor : Editor {
        NetworkingStarter nwStarter;

        SerializedProperty autoStartProp;

        public virtual void OnEnable() {
            nwStarter = (NetworkingStarter)target;

#if pHUMANOID
#if hUNET
            OnLoadHumanoidPlayerUnet.CheckHumanoidPlayer();
#endif
#if hPHOTON1 || hPHOTON2
            OnLoadHumanoidPlayerPun.CheckHumanoidPlayer();
#endif
#if hBOLT
            OnLoadHumanoidPlayerBolt.CheckHumanoidPlayer();
#endif
#if hMIRROR
            OnLoadHumanoidPlayerMirror.CheckHumanoidPlayer();
#endif
#endif
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

#if !hNW_UNET && !hNW_PHOTON && !hNW_MIRROR && !hNW_BOLT
            EditorGUILayout.HelpBox("Networking Support is disabled. Check Preferences to enable it.", MessageType.Warning);
#else

            Inspector();

#if hNW_UNET || hNW_PHOTON
            NetworkingStarter nwStarter = (NetworkingStarter)target;
            //EditorGUILayout.EnumPopup("Networking Status", nwStarter.networkingStatus);

            if (Application.isPlaying || !nwStarter.gameObject.activeInHierarchy)
                return;
#endif

            //GameObject humanoidPrefab = GetHumanoidPlayerPrefab();

            serializedObject.ApplyModifiedProperties();
#endif
        }

        private void Inspector() {
            autoStartProp = serializedObject.FindProperty("autoStart");
            autoStartProp.boolValue = EditorGUILayout.Toggle("Auto Start", autoStartProp.boolValue);
#if hNW_UNET
            NetworkingPrefabInspector();
            ServerTypeInspector();
#elif hNW_PHOTON
            NetworkingPrefabInspector();
            CloudServerInspector();
#elif hNW_BOLT
            NetworkingPrefabInspector();
            OwnServerInspector();
#elif hNW_MIRROR
            OwnServerInspector();
#endif
            SendRateInspector();

            if (Application.isPlaying) {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Start Host"))
                    nwStarter.StartHost();
                if (GUILayout.Button("Start Client"))
                    nwStarter.StartClient();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ServerTypeInspector() {
            SerializedProperty serverTypeProp = serializedObject.FindProperty("serverType");
            serverTypeProp.intValue = (int)(NetworkingStarter.ServerType)EditorGUILayout.EnumPopup("Server Type", (NetworkingStarter.ServerType)serverTypeProp.intValue);
            if ((NetworkingStarter.ServerType)serverTypeProp.intValue == NetworkingStarter.ServerType.CloudServer)
                CloudServerInspector();
            else
                OwnServerInspector();
        }

        private void CloudServerInspector() {
            SerializedProperty roomNameProp = serializedObject.FindProperty("roomName");
            SerializedProperty gameVersionProp = serializedObject.FindProperty("gameVersion");

            roomNameProp.stringValue = EditorGUILayout.TextField("Room Name", roomNameProp.stringValue);
            gameVersionProp.intValue = EditorGUILayout.IntField("Game Version", gameVersionProp.intValue);
        }

        private void OwnServerInspector() {
            SerializedProperty serverIpAddressProp = serializedObject.FindProperty("serverIpAddress");
            SerializedProperty useRoleFileProp = serializedObject.FindProperty("useRoleFile");
            SerializedProperty roleFileName = serializedObject.FindProperty("roleFileName");
            SerializedProperty roleProp = serializedObject.FindProperty("role");

            if (autoStartProp.boolValue) {
                useRoleFileProp.boolValue = EditorGUILayout.Toggle("Use Role File", useRoleFileProp.boolValue);
                if (useRoleFileProp.boolValue) {
                    roleFileName.stringValue = EditorGUILayout.TextField("Role File Name", roleFileName.stringValue);
                    EditorGUI.indentLevel++;
#if hNW_UNET || hMIRROR
                    serverIpAddressProp.stringValue = EditorGUILayout.TextField("Server IP Address", serverIpAddressProp.stringValue);
#endif
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Set Role in File");
                    if (GUILayout.Button("Host"))
                        WriteToRoleFile(roleFileName.stringValue, "Host", serverIpAddressProp.stringValue);
                    if (GUILayout.Button("Client"))
                        WriteToRoleFile(roleFileName.stringValue, "Client", serverIpAddressProp.stringValue);

                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }
                else {
                    roleProp.intValue = (int)(NetworkingStarter.Role)EditorGUILayout.EnumPopup("Role", (NetworkingStarter.Role)roleProp.intValue);
#if hNW_UNET || hMIRROR
                    serverIpAddressProp.stringValue = EditorGUILayout.TextField("Server IP Address", serverIpAddressProp.stringValue);
#endif
                }
            }
#if hNW_UNET || hMIRROR
            else
                serverIpAddressProp.stringValue = EditorGUILayout.TextField("Server IP Address", serverIpAddressProp.stringValue);
#endif
#if hMIRROR
            Mirror.NetworkManager nwManager = nwStarter.GetComponent<Mirror.NetworkManager>();
            nwManager.networkAddress = serverIpAddressProp.stringValue;
#endif
        }

        private void WriteToRoleFile(string roleFileName, string roleText, string ipAddress) {
            string filename = Application.streamingAssetsPath + "/" + roleFileName;
            Debug.Log(filename);
            if (!Directory.Exists(Application.streamingAssetsPath))
                Directory.CreateDirectory(Application.streamingAssetsPath);
            StreamWriter file = File.CreateText(filename);
            file.WriteLine(roleText);
            file.WriteLine(ipAddress);
            file.Close();
        }

        private void SendRateInspector() {
            SerializedProperty sendRateProp = serializedObject.FindProperty("sendRate");
            sendRateProp.intValue = EditorGUILayout.IntField("Send Rate", sendRateProp.intValue);
        }

        private void NetworkingPrefabInspector() {
            SerializedProperty networkingPrefabProp = serializedObject.FindProperty("playerPrefab");
            if (networkingPrefabProp.objectReferenceValue == null)
                networkingPrefabProp.objectReferenceValue = GetHumanoidPlayerPrefab();

            networkingPrefabProp.objectReferenceValue = (GameObject)EditorGUILayout.ObjectField("Player Prefab", (GameObject)networkingPrefabProp.objectReferenceValue, typeof(GameObject), true);
        }

        private GameObject GetHumanoidPlayerPrefab() {
            GameObject humanoidPrefab = Resources.Load<GameObject>("HumanoidPlayer");
            return humanoidPrefab;
        }

        public void OnDisable() {
            CleanupStuff();
        }

#if !UNITY_2019_1_OR_NEWER //&& hNW_UNET
#pragma warning disable 0618
        private NetworkManager cleanupNetworkManager;
#pragma warning restore 0618
#endif
#if hPHOTON1 || hPHOTON2
        private PhotonView cleanupPhotonView;
#endif
#if hMIRROR
        private Mirror.NetworkManager cleanupMirror;
#endif

        private void CleanupStuff() {
#if !UNITY_2019_1_OR_NEWER //&& hNW_UNET
            if (cleanupNetworkManager) {
                DestroyImmediate(cleanupNetworkManager, true);
                cleanupNetworkManager = null;
            }
#endif
#if hPHOTON1 || hPHOTON2
            if (cleanupPhotonView) {
                DestroyImmediate(cleanupPhotonView, true);
                cleanupPhotonView = null;
            }
#endif
#if hMIRROR
            if (cleanupMirror) {
                DestroyImmediate(cleanupMirror, true);
                cleanupMirror = null;
                Mirror.Transport transport = FindObjectOfType<Mirror.Transport>();
                if (transport != null)
                    DestroyImmediate(transport, true);
            }
#endif
        }
    }
}