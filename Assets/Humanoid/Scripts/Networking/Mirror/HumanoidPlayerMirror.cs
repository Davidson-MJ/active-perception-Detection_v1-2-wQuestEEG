using System.IO;
using UnityEngine;
#if hNW_MIRROR
using System.Collections.Generic;
using Mirror;
#endif

namespace Passer.Humanoid {
    using Pawn;

    public static class HumanoidMirror {
        public static bool IsAvailable() {
            string path = Application.dataPath + "/Mirror/Runtime/NetworkBehaviour.cs";
            return File.Exists(path);
        }
    }

#if hNW_MIRROR
    public partial class HumanoidPlayer : NetworkBehaviour, IHumanoidNetworking {

        public ulong nwId {
            get { return identity.netId; }
        }

        public bool isLocal { get; set; }

        protected NetworkWriter writer;
        protected NetworkReader reader;

        [SerializeField]
        protected float _sendRate = 25;
        public float sendRate {
            get { return _sendRate; }
        }

        [SerializeField]
        protected PawnNetworking.DebugLevel _debug = PawnNetworking.DebugLevel.Error;
        public PawnNetworking.DebugLevel debug {
            get { return _debug; }
        }

        [SerializeField]
        protected bool _createLocalRemotes = false;
        public bool createLocalRemotes {
            get { return _createLocalRemotes; }
            set { _createLocalRemotes = value; }
        }

        [SerializeField]
        private PawnNetworking.Smoothing _smoothing = PawnNetworking.Smoothing.None;
        public PawnNetworking.Smoothing smoothing {
            get { return _smoothing; }
        }

        struct MirrorMessage : NetworkMessage {
            public byte[] data;
            public MirrorMessage(byte[] data) {
                this.data = data;
            }
            public MirrorMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        public List<HumanoidControl> humanoids { get; set; } = new List<HumanoidControl>();

        protected NetworkIdentity identity;

        public ulong GetObjectIdentity(GameObject obj) {
            NetworkIdentity identity = obj.GetComponent<NetworkIdentity>();
            if (identity == null)
                return 0;

            return identity.netId;
        }

        public GameObject GetGameObject(ulong objIdentity) {
            NetworkIdentity identity = NetworkClient.spawned[(uint)objIdentity];
            if (identity == null)
                return null;

            return identity.gameObject;
        }

    #region Pawn stub
        void IPawnNetworking.InstantiatePawn(PawnControl pawn) { }
        void IPawnNetworking.DestroyPawn(PawnControl pawn) { }
        void IPawnNetworking.Grab(Pawn.PawnHand controllerTarget, GameObject obj, bool rangeCheck) { }
        void IPawnNetworking.LetGo(Pawn.PawnHand controllerTarget) { }
    #endregion

    #region Init

        public void Awake() {
            DontDestroyOnLoad(this);

            identity = GetComponent<NetworkIdentity>();
        }

        public override void OnStartServer() {
            NetworkServer.ReplaceHandler<InstantiateMessage>(ForwardInstantiateHumanoid);
            NetworkServer.ReplaceHandler<DestroyMessage>(ForwardDestroyHumanoid);

            NetworkServer.ReplaceHandler<PoseMessage>(ForwardHumanoidPose);

            NetworkServer.ReplaceHandler<GrabMessage>(ForwardGrab);
            NetworkServer.ReplaceHandler<LetGoMessage>(ForwardLetGo);

            NetworkServer.ReplaceHandler<ChangeAvatarMessage>(ForwardChangeAvatar);

            NetworkServer.ReplaceHandler<TrackingMessage>(ForwardSyncTrackingSpace);
        }

        public override void OnStartClient() {
            //name = name + " " + nwId;

            NetworkClient.ReplaceHandler<InstantiateMessage>(ReceiveInstantiateHumanoid);
            NetworkClient.ReplaceHandler<DestroyMessage>(ReceiveDestroyHumanoid);

            NetworkClient.ReplaceHandler<PoseMessage>(ReceiveHumanoidPose);

            NetworkClient.ReplaceHandler<GrabMessage>(ReceiveGrab);
            NetworkClient.ReplaceHandler<LetGoMessage>(ReceiveLetGo);

            NetworkClient.ReplaceHandler<ChangeAvatarMessage>(ReceiveChangeAvatar);

            NetworkClient.ReplaceHandler<TrackingMessage>(ReceiveSyncTrackingSpace);

            if (identity != null && identity.isServer) {
                IHumanoidNetworking[] nwHumanoids = FindObjectsOfType<HumanoidPlayer>();
                foreach (IHumanoidNetworking nwHumanoid in nwHumanoids) {
                    foreach (HumanoidControl humanoid in nwHumanoid.humanoids) {
                        HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(humanoid);
                        InstantiateMessage msg = new InstantiateMessage(instantiateHumanoid);
                        //NetworkServer.SendToClientOfPlayer(identity, msg);
                        identity.connectionToClient.Send(msg);
                    }
                }
            }
        }

    #endregion

    #region Start
        public override void OnStartLocalPlayer() {
            isLocal = true;
            name = "HumanoidPlayer(Local)";

            humanoids = HumanoidNetworking.FindLocalHumanoids();
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Found " + humanoids.Count + " Humanoids");

            for (int i = 0; i < humanoids.Count; i++) {
                HumanoidControl humanoid = humanoids[i];
                if (humanoid.isRemote)
                    continue;

                humanoid.nwId = (ulong)nwId;
                humanoid.humanoidNetworking = this;

                if (debug <= PawnNetworking.DebugLevel.Info)
                    DebugLog("Send Start Humanoid " + humanoid.humanoidId);

                ((IHumanoidNetworking)this).InstantiateHumanoid(humanoid);
            }
            NetworkingSpawner spawner = FindObjectOfType<NetworkingSpawner>();
            if (spawner != null)
                spawner.OnNetworkingStarted();
        }

    #endregion

    #region Update

        float lastSend;

        public void LateUpdate() {
            if (!isLocalPlayer)
                return;

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

        public void OnDestroy() {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log((int)netId + ": Destroy Remote Humanoid");

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

    #region Instantiate Humanoid

        public struct InstantiateMessage : NetworkMessage {
            public byte[] data;
            public InstantiateMessage(byte[] data) {
                this.data = data;
            }
            public InstantiateMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        public void InstantiateHumanoid(HumanoidControl humanoid) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Instantiate Humanoid " + humanoid.humanoidId);

            HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(humanoid);

            NetworkIdentity identity = GetComponent<NetworkIdentity>();
            if (identity.isServer)
                this.Receive(instantiateHumanoid);
            else {
                InstantiateMessage msg = new InstantiateMessage(instantiateHumanoid);
                identity.connectionToServer.Send(msg, Channels.Reliable);
            }
        }

        public void ForwardInstantiateHumanoid(NetworkConnection nwConnection, InstantiateMessage msg) {
            Debug.Log("forward instantiate humanoid");
            NetworkServer.SendToAll(msg, Channels.Reliable);
        }

        protected void ReceiveInstantiateHumanoid(NetworkConnection nwConnection, InstantiateMessage msg) {
            this.ReceiveInstantiate(msg.data);
        }


    #endregion

    #region Destroy Humanoid

        public struct DestroyMessage : NetworkMessage {
            public byte[] data;
            public DestroyMessage(byte[] data) {
                this.data = data;
            }
            public DestroyMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }


        public void DestroyHumanoid(HumanoidControl humanoid) {
            if (humanoid == null)
                return;

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog(nwId + ": Destroy Humanoid " + humanoid.humanoidId);

            HumanoidNetworking.DestroyHumanoid destroyHumanoid = new HumanoidNetworking.DestroyHumanoid(humanoid);

            if (identity.isServer)
                this.Receive(destroyHumanoid);
            else {
                DestroyMessage msg = new DestroyMessage(destroyHumanoid);
                identity.connectionToServer.Send(msg, Channels.Reliable);
            }
        }

        public void ForwardDestroyHumanoid(NetworkConnection nwConnection, DestroyMessage destroyHumanoid) {
            NetworkServer.SendToAll(destroyHumanoid, Channels.Reliable);
        }

        protected void ReceiveDestroyHumanoid(NetworkConnection nwConnection, DestroyMessage destroyHumanoid) {
            this.ReceiveDestroy(destroyHumanoid.data);
        }

    #endregion

    #region Pose

        public struct PoseMessage : NetworkMessage {
            public byte[] data;
            public PoseMessage(byte[] data) {
                this.data = data;
            }
            public PoseMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        public HumanoidNetworking.HumanoidPose lastHumanoidPose { get; set; }

        public virtual void UpdateHumanoidPose(HumanoidControl humanoid) {
            if (humanoid == null)
                return;

            HumanoidNetworking.HumanoidPose humanoidPose = new HumanoidNetworking.HumanoidPose(humanoid, Time.time, true);

            humanoidPose.Serialize();

            if (debug <= PawnNetworking.DebugLevel.Debug)
                DebugLog("Send Pose Humanoid " + humanoidPose.humanoidId + " nwId: " + humanoidPose.nwId);

            if (identity.isServer) {
                this.Receive(humanoidPose);
                PoseMessage msg = new PoseMessage(humanoidPose);
                NetworkServer.SendToAll(msg, Channels.Unreliable);
            }
            else {
                PoseMessage msg = new PoseMessage(humanoidPose);
                identity.connectionToServer.Send(msg, Channels.Unreliable);
            }
        }

        protected virtual void ForwardHumanoidPose(NetworkConnection nwConnection, PoseMessage humanoidPose) {
            //if (debug <= PawnNetworking.DebugLevel.Debug)
            //    DebugLog("Forward Pose Humanoid: " + humanoidPose.humanoidId + " nwId: " + humanoidPose.nwId);

            NetworkServer.SendToAll(humanoidPose, Channels.Unreliable);
        }

        protected virtual void ReceiveHumanoidPose(NetworkConnection nwConnection, PoseMessage msg) {
            HumanoidNetworking.HumanoidPose humanoidPose = new HumanoidNetworking.HumanoidPose(msg.data);
            HumanoidPlayer player = FindHumanoidPlayerForMessage(humanoidPose.nwId);
            player.Receive(humanoidPose);
        }

        private HumanoidPlayer FindHumanoidPlayerForMessage(ulong ReceivedNwId) {
            NetworkIdentity nwId = NetworkClient.spawned[(uint)ReceivedNwId];
            if (nwId != null) {
                GameObject nwObject = nwId.gameObject;
                if (nwObject != null) {
                    HumanoidPlayer humanoidPlayer = nwObject.GetComponent<HumanoidPlayer>();
                    return humanoidPlayer;
                }
                else {
                    if (debug <= PawnNetworking.DebugLevel.Warning)
                        DebugWarning("Could not find HumanoidNetworking object");
                }
            }
            return null;
        }

    #endregion

    #region Grab

        public struct GrabMessage : NetworkMessage {
            public byte[] data;
            public GrabMessage(byte[] data) {
                this.data = data;
            }
            public GrabMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        void IHumanoidNetworking.Grab(HandTarget handTarget, GameObject obj, bool rangeCheck, HandTarget.GrabType grabType) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Grab " + obj);

            ulong objIdentity = GetObjectIdentity(obj);
            if (objIdentity == 0) {
                if (debug <= PawnNetworking.DebugLevel.Error)
                    Debug.LogError("Grabbed object " + obj + " does not have a network identity");
                return;
            }

            HumanoidNetworking.Grab grab = new HumanoidNetworking.Grab(handTarget, objIdentity, rangeCheck, grabType);

            GrabMessage msg = new GrabMessage(grab);
            if (identity.isServer)
                ForwardGrab(identity.connectionToClient, msg);
            else														
                identity.connectionToServer.Send(msg, Channels.Reliable);			 
        }

        public void ForwardGrab(NetworkConnection nwConnection, GrabMessage msg) {
            // Transfer authortity to the grabbing client
            // This has to be done on the server
            HumanoidNetworking.Grab grabData = new HumanoidNetworking.Grab(msg.data);
            GameObject obj = GetGameObject(grabData.nwId_grabbedObject);
            Debug.Log("Client authorization for " + obj);
            NetworkIdentity nwIdentity = obj.GetComponent<NetworkIdentity>();
            nwIdentity.AssignClientAuthority(nwConnection);

            NetworkServer.SendToReady(msg, Channels.Reliable);
        }

        protected void ReceiveGrab(NetworkConnection nwConnection, GrabMessage msg) {
            HumanoidNetworking.Grab grabData = new HumanoidNetworking.Grab(msg.data);
            HumanoidPlayer player = FindHumanoidPlayerForMessage(grabData.nwId);
            player.Receive(grabData);
        }
    #endregion

    #region Let Go

        public struct LetGoMessage : NetworkMessage {
            public byte[] data;
            public LetGoMessage(byte[] data) {
                this.data = data;
            }
            public LetGoMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        void IHumanoidNetworking.LetGo(HandTarget handTarget) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("LetGo");

            HumanoidNetworking.LetGo letGo = new HumanoidNetworking.LetGo(handTarget);

            LetGoMessage msg = new LetGoMessage(letGo);
            if (identity.isServer)
                ForwardLetGo(identity.connectionToClient, msg);

            else
                identity.connectionToServer.Send(msg, Channels.Reliable);			 
        }

        public void ForwardLetGo(NetworkConnection nwConnection, LetGoMessage msg) {
            // Remote authortity from the letting go client
            // This has to be done on the server

            HumanoidNetworking.LetGo letGoData = new HumanoidNetworking.LetGo(msg.data);

            HumanoidControl humanoid = HumanoidNetworking.FindHumanoid(this.humanoids, letGoData.humanoidId);
            if (humanoid == null) {
                if (this.debug <= PawnNetworking.DebugLevel.Warning)
                    Debug.LogWarning("Could not find humanoid: " + letGoData.humanoidId);

                NetworkServer.SendToReady(msg, Channels.Reliable);
                return;
            }

            HandTarget handTarget = letGoData.isLeft ? humanoid.leftHandTarget : humanoid.rightHandTarget;

            GameObject obj = handTarget.grabbedObject;
            if (obj != null) {
                Debug.Log("Server authorization for " + obj);
                NetworkIdentity nwIdentity = obj.GetComponent<NetworkIdentity>();
                nwIdentity.RemoveClientAuthority();
            }
            else
                Debug.Log("Letting go empty hand " + handTarget.isLeft);

            NetworkServer.SendToReady(msg, Channels.Reliable);
        }

        protected void ReceiveLetGo(NetworkConnection nwConnection, LetGoMessage msg) {
            HumanoidNetworking.LetGo letGoData = new HumanoidNetworking.LetGo(msg.data);
            HumanoidPlayer player = FindHumanoidPlayerForMessage(letGoData.nwId);
            player.Receive(letGoData);
        }
    #endregion

    #region ChangeAvatar

        public struct ChangeAvatarMessage : NetworkMessage {
            public byte[] data;
            public ChangeAvatarMessage(byte[] data) {
                this.data = data;
            }
            public ChangeAvatarMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        void IHumanoidNetworking.ChangeAvatar(HumanoidControl humanoid, string avatarPrefabName) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log("Change Avatar: " + avatarPrefabName);

            HumanoidNetworking.ChangeAvatar changeAvatar = new HumanoidNetworking.ChangeAvatar(humanoid, avatarPrefabName);

            ChangeAvatarMessage msg = new ChangeAvatarMessage(changeAvatar);
            if (identity.isServer)
                ForwardChangeAvatar(identity.connectionToClient, msg);
            else
																				
                identity.connectionToServer.Send(msg, Channels.Reliable);
			 
        }

        public void ForwardChangeAvatar(NetworkConnection nwConnection, ChangeAvatarMessage msg) {
            NetworkServer.SendToAll(msg, Channels.Reliable);
        }

        protected void ReceiveChangeAvatar(NetworkConnection nwConnection, ChangeAvatarMessage msg) {
            HumanoidNetworking.ChangeAvatar changeAvatarData = new HumanoidNetworking.ChangeAvatar(msg.data);
            HumanoidPlayer player = FindHumanoidPlayerForMessage(changeAvatarData.nwId);
            player.Receive(changeAvatarData);
        }

    #endregion

    #region Tracking

        public struct TrackingMessage : NetworkMessage {
            public byte[] data;
            public TrackingMessage(byte[] data) {
                this.data = data;
            }
            public TrackingMessage(HumanoidNetworking.IMessage msg) {
                this.data = msg.Serialize();
            }
        }

        private Transform GetTrackingTransform(HumanoidControl humanoid) {
#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
            if (humanoid.openVR != null)
                return humanoid.openVR.GetTrackingTransform();
#endif
            return null;
        }

        public void SyncTrackingSpace(HumanoidControl humanoid) {
            if (humanoid == null)
                return;

            Transform trackingTransform = GetTrackingTransform(humanoid);
            if (trackingTransform == null)
                return;

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Sync Tracking Space " + humanoid.humanoidId + " " + trackingTransform.position + " " + trackingTransform.rotation);

            HumanoidNetworking.SyncTrackingSpace syncTrackingSpace =
                new HumanoidNetworking.SyncTrackingSpace(humanoid, trackingTransform.position, trackingTransform.rotation);

            TrackingMessage msg = new TrackingMessage(syncTrackingSpace);
            if (identity.isServer)
                ForwardSyncTrackingSpace(identity.connectionToClient, msg);
            else
																		 
                identity.connectionToServer.Send(msg, Channels.Reliable);
			 
        }

        public void ForwardSyncTrackingSpace(NetworkConnection nwConnection, TrackingMessage msg) {
            NetworkServer.SendToAll(msg, Channels.Reliable);
        }

        protected void ReceiveSyncTrackingSpace(NetworkConnection nwConnection, TrackingMessage msg) {
            HumanoidNetworking.SyncTrackingSpace syncTrackingData = new HumanoidNetworking.SyncTrackingSpace(msg.data);
            HumanoidPlayer player = FindHumanoidPlayerForMessage(syncTrackingData.nwId);
            player.Receive(syncTrackingData);
        }

    #endregion

    #region Network Sync

        void IHumanoidNetworking.ReenableNetworkSync(GameObject obj) {
            ReenableNetworkSync(obj);
        }

        void IHumanoidNetworking.DisableNetworkSync(GameObject obj) {
            DisableNetworkSync(obj);
        }


        public static void DisableNetworkSync(GameObject obj) {
            NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
            if (networkTransform != null) {
                networkTransform.enabled = false;
                networkTransform.syncInterval = 0;
            }
        }

        public static void ReenableNetworkSync(GameObject obj) {
            NetworkTransform networkTransform = obj.GetComponent<NetworkTransform>();
            if (networkTransform != null) {
                networkTransform.enabled = true;
                networkTransform.syncInterval = 0.1F;
            }
        }

    #endregion

    #region Network Object

    #region Void Event

        public void RPC(FunctionCall functionCall) {
            Debug.LogWarning("Network Objects are not supported on Mirror Networking yet.");
        }

    #endregion

    #region Bool Event

        public void RPC(FunctionCall functionCall, bool value) {
            Debug.LogWarning("Network Objects are not supported on Mirror Networking yet.");
        }

    #endregion

    #region String Event

        public void RPC(FunctionCall functionCall, string value) {
            Debug.LogWarning("Network Objects are not supported on Mirror Networking yet.");
        }

    #endregion

    #endregion

    #region Send
        public void Send(bool b) { writer.WriteBool(b); }
        public void Send(byte b) { writer.WriteByte(b); }
        public void Send(int x) { writer.WriteInt(x); }
        public void Send(float f) { writer.WriteFloat(f); }
        public void Send(Vector3 v) { writer.WriteVector3(v); }
        public void Send(Quaternion q) { writer.WriteQuaternion(q); }
    #endregion

    #region Receive
        public bool ReceiveBool() { return reader.ReadBool(); }
        public byte ReceiveByte() { return reader.ReadByte(); }
        public int ReceiveInt() { return reader.ReadInt(); }
        public float ReceiveFloat() { return reader.ReadFloat(); }
        public Vector3 ReceiveVector3() { return reader.ReadVector3(); }
        public Quaternion ReceiveQuaternion() { return reader.ReadQuaternion(); }
    #endregion

    #region Debug

        public void DebugLog(string s) {
            Debug.Log(nwId + ": " + s);
        }

        public void DebugWarning(string s) {
            Debug.LogWarning(nwId + ": " + s);
        }

        public void DebugError(string s) {
            Debug.LogError(nwId + ": " + s);
        }

    #endregion
    }
#endif
}