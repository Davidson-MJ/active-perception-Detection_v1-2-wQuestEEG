using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Passer.Humanoid {
    using Pawn;

    public static class HumanoidUnet {
        public static bool IsAvailable() {
#if UNITY_2019_1_OR_NEWER
            return false;
#else
            return true;
#endif
        }
    }

#if hNW_UNET
#pragma warning disable 0618
    [RequireComponent(typeof(NetworkIdentity))]
    public partial class HumanoidPlayer : PawnUnet, IHumanoidNetworking {

        public ulong nwId {
            get { return netId.Value; }
        }

        public List<HumanoidControl> humanoids { get; set; }

        protected NetworkIdentity identity;

        public ulong GetObjectIdentity(GameObject obj) {
            NetworkIdentity identity = obj.GetComponent<NetworkIdentity>();
            if (identity == null)
                return 0;

            return identity.netId.Value;
        }

        public GameObject GetGameObject(ulong objIdentity) {
            NetworkInstanceId netId = new NetworkInstanceId((uint)objIdentity);
            GameObject gameObject = ClientScene.FindLocalObject(netId);
            return gameObject;
        }

        #region Init

        override public void Awake() {
            DontDestroyOnLoad(this);

            identity = GetComponent<NetworkIdentity>();
            humanoids = new List<HumanoidControl>();

            lastSend = Time.time;
        }

        public override void OnStartClient() {
            name = name + " " + netId;

            //NetworkManager nwManager = FindObjectOfType<NetworkManager>();
            //short msgType = MsgType.Highest + 2;
            //nwManager.client.RegisterHandler(msgType, ClientProcessAvatarPose);

            if (identity.isServer) {
                IHumanoidNetworking[] nwHumanoids = FindObjectsOfType<HumanoidPlayer>();
                foreach (IHumanoidNetworking nwHumanoid in nwHumanoids) {
                    foreach (HumanoidControl humanoid in nwHumanoid.humanoids) {
                        if (humanoid.isRemote)
                            continue;

                        DebugLog("Server Instantiate " + humanoid.nwId + " " + humanoid.humanoidId);
                        ((IHumanoidNetworking)this).InstantiateHumanoid(humanoid);
                    }
                }
            }
        }

        public override void OnStartServer() {
            //short msgType = MsgType.Highest + 1;
            //NetworkServer.RegisterHandler(msgType, ForwardAvatarPose);
        }

        #endregion

        #region Start

        public override void OnStartLocalPlayer() {
            isLocal = true;
            name = "HumanoidPlayer(Local)";

            humanoids = HumanoidNetworking.FindLocalHumanoids();
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log((int)netId.Value + ": Found " + humanoids.Count + " Humanoids");

            for (int i = 0; i < humanoids.Count; i++) {
                HumanoidControl humanoid = humanoids[i];
                if (humanoid.isRemote)
                    continue;

                humanoid.nwId = netId.Value;
                humanoid.humanoidNetworking = this;

                if (debug <= PawnNetworking.DebugLevel.Info)
                    Debug.Log(humanoid.nwId + ": Send Start Humanoid " + humanoid.humanoidId);

                ((IHumanoidNetworking)this).InstantiateHumanoid(humanoid);
            }
            foreach (HumanoidControl humanoid in humanoids)
                DetectNetworkObjects(humanoid);


            NetworkingSpawner spawner = FindObjectOfType<NetworkingSpawner>();
            if (spawner != null)
                spawner.OnNetworkingStarted();
        }

        #endregion

        #region Update

        protected virtual void FixedUpdate() {
            //if (Time.time > lastSend + 1 / sendRate) {
            //    foreach (HumanoidControl humanoid in humanoids) {
            //        if (!humanoid.isRemote) {
            //            UpdateHumanoidPose(humanoid);
            //            if (syncTracking)
            //                SyncTrackingSpace(humanoid);
            //        }
            //    }
            //    lastSend = Time.time;
            //}
        }

        protected virtual void Update() {
            if (smoothing == PawnNetworking.Smoothing.Interpolation ||
                smoothing == PawnNetworking.Smoothing.Extrapolation) {

                HumanoidNetworking.SmoothUpdate(humanoids);
            }
        }

        public override void LateUpdate() {
            if (Time.time > lastSend + 1 / sendRate) {
                foreach (HumanoidControl humanoid in humanoids) {
                    if (!humanoid.isRemote) {
                        UpdateHumanoidPose(humanoid);
                        if (syncTracking)
                            SyncTrackingSpace(humanoid);
                    }
                }
                lastSend = Time.time;
            }
        }

        #endregion

        #region Stop

        override public void OnDestroy() {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log((int)netId.Value + ": Destroy Remote Humanoid");

            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid == null)
                    continue;

                if (humanoid.isRemote) {
                    if (humanoid.gameObject != null)
                        Destroy(humanoid.gameObject);
                }
                else
                    humanoid.nwId = 0;
            }
        }
        #endregion

        protected virtual void SendToServer(NetworkIdentity identity, HumanoidNetworking.IMessage msg) {
            byte[] data = msg.Serialize();

            short msgType = MsgType.Highest + 1;
            writer = new NetworkWriter();
            writer.StartMessage(msgType);
            writer.WriteBytesAndSize(data, data.Length);
            writer.FinishMessage();
            identity.connectionToServer.SendWriter(writer, Channels.DefaultUnreliable);
        }

        protected virtual void SendToClients(byte[] data) {
            short msgType = MsgType.Highest + 2;
            NetworkWriter sWriter = new NetworkWriter();

            sWriter.StartMessage(msgType);
            sWriter.WriteBytesAndSize(data, data.Length);
            sWriter.FinishMessage();

            NetworkServer.SendWriterToReady(null, sWriter, Channels.DefaultUnreliable);
        }

        #region Instantiate Humanoid

        void IHumanoidNetworking.InstantiateHumanoid(HumanoidControl humanoid) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Instantiate Humanoid " + humanoid.nwId + "/" + humanoid.humanoidId);

            HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(humanoid);
            byte[] data = instantiateHumanoid.Serialize();

            CmdForwardInstantiateHumanoid(data);
        }

        protected HumanoidNetworking.InstantiateHumanoid instantiatedHumanoid;

        [Command] // @ server
        protected virtual void CmdForwardInstantiateHumanoid(byte[] data) {

            instantiatedHumanoid = new HumanoidNetworking.InstantiateHumanoid(data);
            HumanoidPlayer[] nwHumanoids = FindObjectsOfType<HumanoidPlayer>();
            foreach (HumanoidPlayer nwHumanoid in nwHumanoids)
                nwHumanoid.ServerSendInstantiateHumanoid();
        }

        protected virtual void ServerSendInstantiateHumanoid() {
            if (debug <= PawnNetworking.DebugLevel.Info) {
                DebugLog("Server Send InstantiateHumanoid: " + instantiatedHumanoid.nwId + "/" + instantiatedHumanoid.humanoidId);
            }

            byte[] data = instantiatedHumanoid.Serialize();
            RpcReceiveInitiateHumanoid(data);
        }


        [ClientRpc] // @ remote client
        protected virtual void RpcReceiveInitiateHumanoid(byte[] data) {
            HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(data);

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Received Instantiate Humanoid " + instantiateHumanoid.nwId + "/" + instantiateHumanoid.humanoidId);

            if (instantiateHumanoid.nwId != identity.netId.Value) {
                // Get the right HumanoidPlayer for this humanoid
                NetworkInstanceId netId = new NetworkInstanceId((uint)instantiateHumanoid.nwId);
                GameObject gameObject = ClientScene.FindLocalObject(netId);
                HumanoidPlayer humanoidPlayer = gameObject.GetComponent<HumanoidPlayer>();
                if (humanoidPlayer != null)
                    humanoidPlayer.ReceiveInstantiate(data);
                else
                    DebugError("Could not find HumanoidPlayer with id = " + instantiateHumanoid.nwId);
            }
            else
                this.ReceiveInstantiate(data);
        }

        #endregion

        #region Destroy Humanoid

        void IHumanoidNetworking.DestroyHumanoid(HumanoidControl humanoid) {
            if (humanoid == null)
                return;

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Destroy Humanoid " + humanoid.humanoidId);

            HumanoidNetworking.DestroyHumanoid destroyHumanoid = new HumanoidNetworking.DestroyHumanoid(humanoid);
            byte[] data = destroyHumanoid.Serialize();

            CmdForwardDestroyHumanoid(data);
        }

        [Command] // @ server
        private void CmdForwardDestroyHumanoid(byte[] data) {
            if (debug <= PawnNetworking.DebugLevel.Debug)
                DebugLog("Forward DestroyHumanoid");

            RpcReceiveDestroyHumanoid(data);
        }

        [ClientRpc]
        private void RpcReceiveDestroyHumanoid(byte[] data) {
            this.ReceiveDestroy(data);
        }

        #endregion

        #region Pose

        public HumanoidNetworking.HumanoidPose lastHumanoidPose { get; set; }

        public void UpdateHumanoidPose(HumanoidControl humanoid) {
            HumanoidNetworking.HumanoidPose humanoidPose =
                new HumanoidNetworking.HumanoidPose(humanoid, Time.time, syncFingerSwing, syncFace);

            if (debug <= PawnNetworking.DebugLevel.Debug)
                DebugLog("Send Humanoid Pose " + humanoid.nwId + "/" + humanoid.humanoidId);

            byte[] data = humanoidPose.Serialize();
            CmdForwardHumanoidPose(data);
        }

        [Command]
        protected virtual void CmdForwardHumanoidPose(byte[] data) {
            if (debug <= PawnNetworking.DebugLevel.Debug) {
                HumanoidNetworking.HumanoidPose humanoidPose = new HumanoidNetworking.HumanoidPose(data);
                DebugLog("Forward HumanoidPose " + humanoidPose.nwId + "/" + humanoidPose.humanoidId);
            }
            RpcReceiveHumanoidPose(data);
        }

        [ClientRpc]
        protected virtual void RpcReceiveHumanoidPose(byte[] data) {
            this.ReceiveHumanoidPose(data);
        }

        #endregion

        #region Grab

        void IHumanoidNetworking.Grab(HandTarget handTarget, GameObject obj, bool rangeCheck, HandTarget.GrabType grabType) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log(handTarget.humanoid.nwId + ": Grab " + obj);

            ulong objIdentity = GetObjectIdentity(obj);
            if (objIdentity == 0) {
                if (debug <= PawnNetworking.DebugLevel.Error)
                    Debug.LogError("Grabbed object " + obj + " does not have a network identity");
                return;
            }

            HumanoidNetworking.Grab grab = new HumanoidNetworking.Grab(handTarget, objIdentity, rangeCheck, grabType);
            byte[] data = grab.Serialize();
            CmdForwardGrab(data);
        }

        [Command]
        protected virtual void CmdForwardGrab(byte[] data) {
            RpcReceiveGrab(data);
        }

        [ClientRpc]
        protected virtual void RpcReceiveGrab(byte[] data) {
            this.ReceiveGrab(data);
        }

        #endregion

        #region Let Go

        void IHumanoidNetworking.LetGo(HandTarget handTarget) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("LetGo");

            HumanoidNetworking.LetGo letGo = new HumanoidNetworking.LetGo(handTarget);
            byte[] data = letGo.Serialize();
            CmdForwardLetGo(data);
        }

        [Command]
        protected virtual void CmdForwardLetGo(byte[] data) {
            RpcReceiveLetGo(data);
        }

        [ClientRpc]
        protected virtual void RpcReceiveLetGo(byte[] data) {
            this.ReceiveLetGo(data);
        }

        #endregion

        #region ChangeAvatar

        void IHumanoidNetworking.ChangeAvatar(HumanoidControl humanoid, string avatarPrefabName) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Change Avatar: " + avatarPrefabName);

            HumanoidNetworking.ChangeAvatar changeAvatar = new HumanoidNetworking.ChangeAvatar(humanoid, avatarPrefabName);
            byte[] data = changeAvatar.Serialize();
            CmdForwardChangeAvatar(data);
        }

        [Command]
        protected virtual void CmdForwardChangeAvatar(byte[] data) {
            RpcReceiveChangeAvatar(data);
        }

        [ClientRpc]
        protected virtual void RpcReceiveChangeAvatar(byte[] data) {
            this.ReceiveChangeAvatar(data);
        }

        #endregion

        #region Tracking

        public void SyncTrackingSpace(HumanoidControl humanoid) {
            Transform trackingTransform = HumanoidNetworking.GetTrackingTransform(humanoid);
            if (trackingTransform == null)
                return;

            HumanoidNetworking.SyncTrackingSpace syncTracking = new HumanoidNetworking.SyncTrackingSpace(humanoid, trackingTransform.position, trackingTransform.rotation);
            byte[] data = syncTracking.Serialize();
            CmdForwardSyncTracking(data);
        }

        [Command]
        protected virtual void CmdForwardSyncTracking(byte[] data) {
            RpcReceiveSyncTracking(data);
        }

        [ClientRpc]
        protected virtual void RpcReceiveSyncTracking(byte[] data) {
            this.ReceiveSyncTrackingSpace(data);
        }

        #endregion

        #region Network Sync

        void IHumanoidNetworking.ReenableNetworkSync(GameObject obj) {
            ReenableNetworkSync(obj);
        }

        void IHumanoidNetworking.DisableNetworkSync(GameObject obj) {
            DisableNetworkSync(obj);
        }

        public static void ReenableNetworkSync(GameObject obj) {
            NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
            if (networkTransform != null) {
                networkTransform.enabled = true;
            }
        }

        public static void DisableNetworkSync(GameObject obj) {
            NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
            if (networkTransform != null) {
                networkTransform.enabled = false;
            }
        }

        #endregion

        #region Network Object

        protected NetworkObject[] networkObjects;

        protected void DetectNetworkObjects(HumanoidControl humanoid) {
            if (networkObjects != null)
                Debug.LogError("Network Objects currently support only one humanoid");
            networkObjects = humanoid.GetComponentsInChildren<NetworkObject>();
        }

        public int GetNwObjectId(NetworkObject nwObject) {
            for (int i = 0; i < networkObjects.Length; i++) {
                if (networkObjects[i] == nwObject)
                    return i;
            }
            return -1;
        }

        #region Void Event

        public void RPC(FunctionCall functionCall) {
            CmdRPCVoid(functionCall.targetGameObject, functionCall.methodName);
        }

        [Command] // @ server
        public void CmdRPCVoid(GameObject target, string methodName) {
            RpcRPCVoid(target, methodName);
        }

        [ClientRpc] // @ remote client
        public void RpcRPCVoid(GameObject targetGameObject, string methodName) {
            Debug.Log("RPC: " + methodName);
            FunctionCall.Execute(targetGameObject, methodName);
        }

        #endregion

        #region Bool Event

        public void RPC(FunctionCall functionCall, bool value) {
            CmdRPCBool(functionCall.targetGameObject, functionCall.methodName, value);
        }

        [Command] // @ server
        public void CmdRPCBool(GameObject target, string methodName, bool value) {
            RpcRPCBool(target, methodName, value);
        }

        [ClientRpc] // @ remote client
        public void RpcRPCBool(GameObject target, string methodName, bool value) {
            FunctionCall.Execute(target, methodName, value);
        }

        #endregion

        #region String Event

        //public void RPC(FunctionCall functionCall, string value) {

        //    Debug.Log("HC RPC functioncall " + value);
        //    //CmdRPCString(nwObjectId, functionCall.methodName, value);
        //}

        //[Command] // @ server
        //public void CmdRPCString(int nwObjectId, string methodName, string value) {
        //    Debug.Log("HC Cmd functioncall " + value);
        //    RpcRPCString(nwObjectId, methodName, value);
        //}

        //[ClientRpc] // @ remote client
        //public void RpcRPCString(int nwObjectId, string methodName, string value) {
        //    Debug.Log("HC Client RPC functioncall " + value);
        //    GameObject target = networkObjects[nwObjectId].gameObject;
        //    FunctionCall.Execute(target, methodName, value);
        //}

        #endregion
        #endregion

        #region Debug

        public void DebugLog(string s) {
            Debug.Log(netId + ": " + s);
        }

        public void DebugWarning(string s) {
            Debug.LogWarning(netId + ": " + s);
        }

        public void DebugError(string s) {
            Debug.LogError(netId + ": " + s);
        }

        #endregion

#pragma warning restore 0618
    }
#endif
}