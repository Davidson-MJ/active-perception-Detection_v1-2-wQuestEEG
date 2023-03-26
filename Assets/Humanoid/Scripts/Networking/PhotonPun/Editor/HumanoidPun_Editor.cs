using UnityEngine;
#if hPHOTON2
using Photon.Pun;
#if hPUNVOICE2
using Photon.Voice.PUN;
using Photon.Voice.Unity;
#endif
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Passer.Humanoid {

    [InitializeOnLoad]
    public class OnLoadHumanoidPlayerPun {
        static OnLoadHumanoidPlayerPun() {
            CheckHumanoidPlayer();
        }

        protected static void CheckHumanoidPlayerVoice() {
#if hPUNVOICE2
            string prefabPath = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefabPath();
            prefabPath = prefabPath.Substring(0, prefabPath.Length - 21) + "HumanoidPlayerVoice.prefab";
            GameObject playerVoicePrefab = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefab(prefabPath);
            if (playerVoicePrefab == null)
                return;

            PhotonVoiceView photonVoiceView = playerVoicePrefab.GetComponent<PhotonVoiceView>();
            if (photonVoiceView == null) {
                photonVoiceView = playerVoicePrefab.AddComponent<PhotonVoiceView>();
            }
            photonVoiceView.UsePrimaryRecorder = true;
            PhotonTransformView photonTransformView = playerVoicePrefab.GetComponent<PhotonTransformView>();
            if (photonTransformView == null) {
                photonTransformView = playerVoicePrefab.AddComponent<PhotonTransformView>();
            }
            PhotonView photonView = playerVoicePrefab.GetComponent<PhotonView>();
            if (photonView != null) {
                // should always be there because of the photonVoiceView
                photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
                photonView.ObservedComponents.Add(photonTransformView);
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            }

            OnLoadHumanoidPlayer.UpdateHumanoidPrefab(playerVoicePrefab, prefabPath);
            CheckVoiceNetwork();
#endif
        }

#if hPUNVOICE2
        protected static void CheckVoiceNetwork() {
            NetworkingStarter networkingStarter = Object.FindObjectOfType<NetworkingStarter>();
            if (networkingStarter == null)
                return;

            PhotonVoiceNetwork voiceNetwork = Object.FindObjectOfType<PhotonVoiceNetwork>();
            if (voiceNetwork != null)
                return;

            GameObject voiceNetworkObject = new GameObject("Voice Network");
            voiceNetwork = voiceNetworkObject.AddComponent<PhotonVoiceNetwork>();

            Recorder voiceRecorder = voiceNetworkObject.AddComponent<Recorder>();
            voiceRecorder.ReactOnSystemChanges = true;
            voiceRecorder.TransmitEnabled = true;
            voiceRecorder.SamplingRate = POpusCodec.Enums.SamplingRate.Sampling48000;

            voiceNetwork.PrimaryRecorder = voiceRecorder;
        }
#endif

        public static void CheckHumanoidPlayer() {
#if hPHOTON1 || hPHOTON2
            string prefabPath = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefabPath();
            GameObject playerPrefab = OnLoadHumanoidPlayer.GetHumanoidPlayerPrefab(prefabPath);

#if hNW_PHOTON
            if (playerPrefab != null) {
                PhotonView photonView = playerPrefab.GetComponent<PhotonView>();
                if (photonView == null) {
                    photonView = playerPrefab.AddComponent<PhotonView>();
                    photonView.ObservedComponents = new System.Collections.Generic.List<Component>();
#if hPHOTON2
                    photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
#else
                    photonView.synchronization = ViewSynchronization.UnreliableOnChange;
#endif

                    HumanoidPlayer humanoidPun = playerPrefab.GetComponent<HumanoidPlayer>();
                    if (humanoidPun != null)
                        photonView.ObservedComponents.Add(humanoidPun);
                }
            }
#else
            if (playerPrefab != null) {
                PhotonView photonView = playerPrefab.GetComponent<PhotonView>();
                if (photonView != null)
                    Object.DestroyImmediate(photonView, true);
            }
#endif
            OnLoadHumanoidPlayer.UpdateHumanoidPrefab(playerPrefab, prefabPath);
#endif
            CheckHumanoidPlayerVoice();
        }
    }

}