#if hNW_BOLT
using System;
using System.Collections;
using System.Collections.Generic;
using UdpKit;
#endif
using System.IO;
using UnityEngine;

namespace Passer.Humanoid {
    using Pawn;

    public static class HumanoidBolt {
        public static bool IsAvailable() {
            string path = Application.dataPath + "/Photon/PhotonBolt/project.json";
            return File.Exists(path);
        }
    }

#if hNW_BOLT
    [BoltGlobalBehaviour]
    public class HumanoidBoltGlobal : Bolt.GlobalEventListener {

        public static UdpChannelName instantiateChannel;
        public static UdpChannelName poseChannel;
        public static UdpChannelName destroyChannel;
        public static UdpChannelName changeAvatarChannel;
        public static UdpChannelName grabChannel;
        public static UdpChannelName letGoChannel;
        public static UdpChannelName syncTrackingChannel;

        public HumanoidPlayer humanoidBolt;

        public override void Connected(BoltConnection connection) {
            connection.SetStreamBandwidth(1024 * 20);
        }

        public override void BoltStartBegin() {
            instantiateChannel = BoltNetwork.CreateStreamChannel("Instantiate", UdpChannelMode.Reliable, 2);
            destroyChannel = BoltNetwork.CreateStreamChannel("Destroy", UdpChannelMode.Reliable, 2);

            poseChannel = BoltNetwork.CreateStreamChannel("Pose", UdpChannelMode.Unreliable, 2);

            changeAvatarChannel = BoltNetwork.CreateStreamChannel("ChangeAvatar", UdpChannelMode.Reliable, 2);

            grabChannel = BoltNetwork.CreateStreamChannel("Grab", UdpChannelMode.Reliable, 2);
            letGoChannel = BoltNetwork.CreateStreamChannel("LetGo", UdpChannelMode.Reliable, 2);

            syncTrackingChannel = BoltNetwork.CreateStreamChannel("SyncTrackingSpace", UdpChannelMode.Reliable, 2);
        }

        public override void StreamDataReceived(BoltConnection connection, UdpStreamData data) {
            if (humanoidBolt == null)
                return;

            if (Equals(data.Channel, poseChannel))
                humanoidBolt.ForwardHumanoidPose(data.Data);

            else if (Equals(data.Channel, instantiateChannel))
                humanoidBolt.ForwardInstantiate(data.Data);

            else if (Equals(data.Channel, destroyChannel))
                humanoidBolt.ForwardDestroy(data.Data);

            else if (Equals(data.Channel, changeAvatarChannel))
                humanoidBolt.ForwardChangeAvatar(data.Data);

            else if (Equals(data.Channel, grabChannel))
                humanoidBolt.ForwardGrab(data.Data);

            else if (Equals(data.Channel, letGoChannel))
                humanoidBolt.ForwardLetGo(data.Data);

            else if (Equals(data.Channel, syncTrackingChannel))
                humanoidBolt.ForwardSyncTrackingSpace(data.Data);
        }
    }

    [RequireComponent(typeof(BoltEntity))]
    public partial class HumanoidPlayer : Bolt.EntityBehaviour, IHumanoidNetworking {

        public ulong nwId {
            get { return entity.NetworkId.PackedValue; }
        }

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
        protected PawnNetworking.Smoothing _smoothing = PawnNetworking.Smoothing.None;
        public PawnNetworking.Smoothing smoothing {
            get { return _smoothing; }
        }

        [SerializeField]
        protected bool _createLocalRemotes = false;
        public bool createLocalRemotes {
            get { return _createLocalRemotes; }
            set { _createLocalRemotes = value; }
        }

        public bool isLocal { get; set; } = false;

        public List<HumanoidControl> humanoids { get; set; } = new List<HumanoidControl>();

        public ulong GetObjectIdentity(GameObject obj) {
            BoltEntity objEntity = obj.GetComponent<BoltEntity>();
            if (objEntity == null)
                return 0;

            return objEntity.NetworkId.PackedValue;
        }

        protected BoltEntity[] boltEntitiesBuffer = new BoltEntity[0];

        public GameObject GetGameObject(ulong objIdentity) {
            Bolt.NetworkId nwId = new Bolt.NetworkId(objIdentity);

            // Look in buffer first
            foreach (BoltEntity entity in boltEntitiesBuffer) {
                if (entity.NetworkId == nwId)
                    return entity.gameObject;
            }

            // Refresh buffer
            boltEntitiesBuffer = FindObjectsOfType<BoltEntity>();
            // And look again.
            foreach (var entity in boltEntitiesBuffer) {
                if (entity.NetworkId == nwId)
                    return entity.gameObject;
            }
            return null;
        }

    #region Pawn stub
        // Needs to be replaced by PawnBolt later
        void IPawnNetworking.InstantiatePawn(PawnControl pawn) { }
        void IPawnNetworking.DestroyPawn(PawnControl pawn) { }
        void IPawnNetworking.Grab(Pawn.PawnHand handTarget, GameObject obj, bool rangeCheck) { }
        void IPawnNetworking.LetGo(Pawn.PawnHand handTarget) { }
    #endregion

    #region Init

        public void Awake() {
            HumanoidBoltGlobal humanoidBoltGlobal = FindObjectOfType<HumanoidBoltGlobal>();
            if (humanoidBoltGlobal != null) {
                humanoidBoltGlobal.humanoidBolt = this;
            }
        }

        public override void Attached() {

            if (entity.IsOwner) {
                isLocal = true;
                name = "HumanoidBolt " + entity.NetworkId.PackedValue + " (Local)";

                humanoids = HumanoidNetworking.FindLocalHumanoids();
                if (debug <= PawnNetworking.DebugLevel.Info)
                    DebugLog("Found " + humanoids.Count + " Humanoids");

                for (int i = 0; i < humanoids.Count; i++) {
                    HumanoidControl humanoid = humanoids[i];
                    if (humanoid.isRemote)
                        continue;

                    humanoid.nwId = GetObjectIdentity(this.gameObject);
                    humanoid.humanoidNetworking = this;

                    if (debug <= PawnNetworking.DebugLevel.Info)
                        DebugLog("Send Start Humanoid " + humanoid.humanoidId);

                    ((IHumanoidNetworking)this).InstantiateHumanoid(humanoid);
                }
            }
            else {
                isLocal = false;

                DebugLog("New Remote Player");

                StartCoroutine(SendInstantiateLocalHumanoids());
                name = "HumanoidBolt " + entity.NetworkId.PackedValue;
            }
        }

        IEnumerator SendInstantiateLocalHumanoids() {
            // We need to delay this, because not all networked player instances may be there yet
            yield return new WaitForSeconds(0.1F);
            IHumanoidNetworking[] nwHumanoids = FindObjectsOfType<HumanoidPlayer>();
            foreach (IHumanoidNetworking nwHumanoid in nwHumanoids) {
                foreach (HumanoidControl humanoid in nwHumanoid.humanoids) {
                    if (humanoid.isRemote)
                        continue;

                    BoltEntity entity = ((HumanoidPlayer)humanoid.humanoidNetworking).gameObject.GetComponent<BoltEntity>();
                    humanoid.nwId = entity.NetworkId.PackedValue;
                    HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(humanoid);
                    nwHumanoid.InstantiateHumanoid(humanoid);
                }
            }
        }

        //public void InstantiateHumanoids() {
        //    if (!entity.IsOwner)
        //        return;

        //    for (int i = 0; i < humanoids.Count; i++) {
        //        HumanoidControl humanoid = humanoids[i];
        //        if (humanoid.isRemote)
        //            continue;

        //        humanoid.nwId = GetObjectIdentity(this.gameObject);
        //        humanoid.humanoidNetworking = this;

        //        if (debug <= PawnNetworking.DebugLevel.Info)
        //            DebugLog("Send Start Humanoid " + humanoid.humanoidId);

        //        ((IHumanoidNetworking)this).InstantiateHumanoid(humanoids[i]);
        //        return;
        //    }
        //}

    #endregion

    #region Update

        float lastSendTime = 0;
        public override void SimulateOwner() {
            if (Time.time > lastSendTime + 1 / sendRate) {
                foreach (HumanoidControl humanoid in humanoids) {
                    if (!humanoid.isRemote) {
                        UpdateHumanoidPose(humanoid);
                        if (syncTracking)
                            SyncTrackingSpace(humanoid);
                    }
                }
                //SendHumanoidPoses();
                lastSendTime = Time.time;
            }
        }

        protected virtual void Update() {
            if (smoothing == PawnNetworking.Smoothing.Interpolation ||
                smoothing == PawnNetworking.Smoothing.Extrapolation) {

                HumanoidNetworking.SmoothUpdate(humanoids);
            }
        }

    #endregion

        protected virtual void SendToServer(UdpChannelName channelName, HumanoidNetworking.IMessage msg) {
            byte[] data = msg.Serialize();
            SendToServer(channelName, data);
        }

        protected virtual void SendToServer(UdpChannelName channelName, byte[] data) {
            if (BoltNetwork.Server != null)
                BoltNetwork.Server.StreamBytes(channelName, data);
        }

        protected virtual void SendToClients(UdpChannelName channelName, HumanoidNetworking.IMessage msg) {
            byte[] data = msg.Serialize();
            SendToClients(channelName, data);
        }

        protected virtual void SendToClients(UdpChannelName channelName, byte[] data) {
            foreach (BoltConnection connection in BoltNetwork.Connections)
                connection.StreamBytes(channelName, data);
        }

    #region Instantiate

        void IHumanoidNetworking.InstantiateHumanoid(HumanoidControl humanoid) {

            HumanoidNetworking.InstantiateHumanoid instantiateHumanoid =
                new HumanoidNetworking.InstantiateHumanoid(humanoid);

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Send Instantiate Humanoid " + instantiateHumanoid.nwId + "/" + instantiateHumanoid.humanoidId);

            if (BoltNetwork.IsServer)
                Forward(instantiateHumanoid);
            else
                SendToServer(HumanoidBoltGlobal.instantiateChannel, instantiateHumanoid);
        }

        public void ForwardInstantiate(byte[] data) {
            HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(data);
            Forward(instantiateHumanoid);
        }

        public void Forward(HumanoidNetworking.InstantiateHumanoid instantiateHumanoid) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Info) {
                    DebugLog("Forward Instantiate Humanoid " + instantiateHumanoid.nwId + "/" + instantiateHumanoid.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.instantiateChannel, instantiateHumanoid);
            }
            ((IHumanoidNetworking)this).Receive(instantiateHumanoid);
        }

    #endregion

    #region Destroy

        void IHumanoidNetworking.DestroyHumanoid(HumanoidControl humanoid) {

            HumanoidNetworking.DestroyHumanoid destroyHumanoid = new HumanoidNetworking.DestroyHumanoid(humanoid);

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Destroy Humanoid " + destroyHumanoid.nwId + "/" + destroyHumanoid.humanoidId);

            if (BoltNetwork.IsServer)
                Forward(destroyHumanoid);
            else
                SendToServer(HumanoidBoltGlobal.destroyChannel, destroyHumanoid);
        }

        public void ForwardDestroy(byte[] data) {
            HumanoidNetworking.DestroyHumanoid destroyHumanoid = new HumanoidNetworking.DestroyHumanoid(data);
            Forward(destroyHumanoid);
        }

        public void Forward(HumanoidNetworking.DestroyHumanoid destroyHumanoid) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Info) {
                    DebugLog("Forward Destroy Humanoid " + destroyHumanoid.nwId + "/" + destroyHumanoid.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.destroyChannel, destroyHumanoid);
            }
            ((IHumanoidNetworking)this).Receive(destroyHumanoid);
        }

    #endregion

    #region Pose

        public HumanoidNetworking.HumanoidPose lastHumanoidPose { get; set; }

        public virtual void UpdateHumanoidPose(HumanoidControl humanoid) {

            HumanoidNetworking.HumanoidPose humanoidPose = new HumanoidNetworking.HumanoidPose(humanoid, Time.time);

            if (debug <= PawnNetworking.DebugLevel.Debug)
                DebugLog("Send HumanoidPose " + humanoidPose.nwId + "/" + humanoidPose.humanoidId);

            if (BoltNetwork.IsServer)
                Forward(humanoidPose);
            else
                SendToServer(HumanoidBoltGlobal.poseChannel, humanoidPose);
        }

        public void ForwardHumanoidPose(byte[] data) {
            HumanoidNetworking.HumanoidPose humanoidPose = new HumanoidNetworking.HumanoidPose(data);
            Forward(humanoidPose);
        }

        public void Forward(HumanoidNetworking.HumanoidPose humanoidPose) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Debug) {
                    DebugLog("Forward HumanoidPose " + humanoidPose.nwId + "/" + humanoidPose.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.poseChannel, humanoidPose);
            }
            ((IHumanoidNetworking)this).Receive(humanoidPose);
        }

    #endregion

    #region Grab

        void IHumanoidNetworking.Grab(HandTarget handTarget, GameObject obj, bool rangeCheck, HandTarget.GrabType grabType) {

            ulong objIdentity = GetObjectIdentity(obj);
            if (objIdentity == 0) {
                if (debug <= PawnNetworking.DebugLevel.Error)
                    DebugError("Grabbed object " + obj + " does not have a Bolt Entity");
                return;
            }

            HumanoidNetworking.Grab grab = new HumanoidNetworking.Grab(handTarget, objIdentity, rangeCheck, grabType);

            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log(handTarget.humanoid.nwId + ": Grab " + obj + " " + grabType);

            if (BoltNetwork.IsServer)
                Forward(grab);
            else
                SendToServer(HumanoidBoltGlobal.grabChannel, grab);
        }

        public void ForwardGrab(byte[] data) {
            HumanoidNetworking.Grab grab = new HumanoidNetworking.Grab(data);
            Forward(grab);
        }

        protected void Forward(HumanoidNetworking.Grab grab) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Info) {
                    DebugLog("Forward Grab " + grab.nwId + "/" + grab.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.grabChannel, grab);
            }
            ((IHumanoidNetworking)this).Receive(grab);
        }

    #endregion

    #region Let Go

        void IHumanoidNetworking.LetGo(HandTarget handTarget) {

            HumanoidNetworking.LetGo letGo = new HumanoidNetworking.LetGo(handTarget);

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("LetGo");

            if (BoltNetwork.IsServer)
                Forward(letGo);
            else
                SendToServer(HumanoidBoltGlobal.letGoChannel, letGo);
        }

        public void ForwardLetGo(byte[] data) {
            HumanoidNetworking.LetGo letGo = new HumanoidNetworking.LetGo(data);
        }

        protected void Forward(HumanoidNetworking.LetGo letGo) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Info) {
                    DebugLog("Forward LetGo " + letGo.nwId + "/" + letGo.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.letGoChannel, letGo);
            }
            ((IHumanoidNetworking)this).Receive(letGo);
        }

    #endregion

    #region Change Avatar

        void IHumanoidNetworking.ChangeAvatar(HumanoidControl humanoid, string avatarPrefabName) {
            HumanoidNetworking.ChangeAvatar changeAvatar =
                new HumanoidNetworking.ChangeAvatar(humanoid, avatarPrefabName);

            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log(humanoid.nwId + ": Change Avatar: " + avatarPrefabName);

            if (BoltNetwork.IsServer)
                Forward(changeAvatar);
            else
                SendToServer(HumanoidBoltGlobal.changeAvatarChannel, changeAvatar);
        }

        public void ForwardChangeAvatar(byte[] data) {
            HumanoidNetworking.ChangeAvatar changeAvatar = new HumanoidNetworking.ChangeAvatar(data);
            Forward(changeAvatar);
        }

        protected void Forward(HumanoidNetworking.ChangeAvatar changeAvatar) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Info) {
                    DebugLog("Forward Change Avatar " + changeAvatar.nwId + "/" + changeAvatar.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.changeAvatarChannel, changeAvatar);
            }
            ((IHumanoidNetworking)this).Receive(changeAvatar);
        }

    #endregion

    #region Tracking

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

            HumanoidNetworking.SyncTrackingSpace syncTrackingSpace =
                new HumanoidNetworking.SyncTrackingSpace(humanoid, trackingTransform.position, trackingTransform.rotation);

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Sync Tracking Space " + humanoid.humanoidId + " " + trackingTransform.position + " " + trackingTransform.rotation);

            if (BoltNetwork.IsServer)
                Forward(syncTrackingSpace);
            else
                SendToServer(HumanoidBoltGlobal.syncTrackingChannel, syncTrackingSpace);
        }

        public void ForwardSyncTrackingSpace(byte[] data) {
            HumanoidNetworking.SyncTrackingSpace syncTrackingSpace = new HumanoidNetworking.SyncTrackingSpace(data);
            Forward(syncTrackingSpace);
        }

        protected void Forward(HumanoidNetworking.SyncTrackingSpace syncTrackingSpace) {
            if (BoltNetwork.IsServer) {
                if (debug <= PawnNetworking.DebugLevel.Info) {
                    DebugLog("Forward Sync Tracking " + syncTrackingSpace.nwId + "/" + syncTrackingSpace.humanoidId);
                }
                SendToClients(HumanoidBoltGlobal.syncTrackingChannel, syncTrackingSpace);
            }
            ((IHumanoidNetworking)this).Receive(syncTrackingSpace);
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
            // Empty at the moment, because I don't know what (generic) transform sync will be used
        }

        public static void ReenableNetworkSync(GameObject obj) {
            // Empty at the moment, because I don't know what (generic) transform sync will be used
        }

    #endregion

    #region Network Object

    #region Void Event

        public void RPC(FunctionCall functionCall)
        {
            Debug.LogWarning("Network Objects are not supported on Photon Bolt yet.");
        }

    #endregion

    #region Bool Event

        public void RPC(FunctionCall functionCall, bool value)
        {
            Debug.LogWarning("Network Objects are not supported on Photon Bolt yet.");
        }

    #endregion

    #region String Event

        public void RPC(FunctionCall functionCall, string value)
        {
            Debug.LogWarning("Network Objects are not supported on Photon Bolt yet.");
        }

    #endregion

    #endregion

    #region Send

        public void Send(bool b) {
            byte[] data = { Convert.ToByte(b) };
            BoltNetwork.Server.StreamBytes(HumanoidBoltGlobal.poseChannel, data);
        }

        public void Send(byte b) {
            byte[] data = { b };
            BoltNetwork.Server.StreamBytes(HumanoidBoltGlobal.poseChannel, data);
        }

        public void Send(int x) {
            byte[] data = BitConverter.GetBytes(x);
            BoltNetwork.Server.StreamBytes(HumanoidBoltGlobal.poseChannel, data);
        }

        public void Send(float f) {
            byte[] data = BitConverter.GetBytes(f);
            BoltNetwork.Server.StreamBytes(HumanoidBoltGlobal.poseChannel, data);
        }

        public void Send(Vector3 v) {
            Send(v.x);
            Send(v.y);
            Send(v.z);
        }

        public void Send(Quaternion q) {
            Send(q.x);
            Send(q.y);
            Send(q.z);
            Send(q.w);
        }

    #endregion

    #region Receive

        public bool ReceiveBool() {
            BoltNetwork.Server.ReceiveData(out byte[] data);
            bool b = Convert.ToBoolean(data);
            return b;
        }

        public byte ReceiveByte() {
            BoltNetwork.Server.ReceiveData(out byte[] data);
            byte b = data[0];
            return b;
        }

        public int ReceiveInt() {
            BoltNetwork.Server.ReceiveData(out byte[] data);
            int x = Convert.ToInt32(data);
            return x;
        }

        public float ReceiveFloat() {
            BoltNetwork.Server.ReceiveData(out byte[] data);
            float f = Convert.ToSingle(data);
            return f;
        }

        public Vector3 ReceiveVector3() {
            float x = ReceiveFloat();
            float y = ReceiveFloat();
            float z = ReceiveFloat();
            Vector3 v = new Vector3(x, y, z);
            return v;
        }

        public Quaternion ReceiveQuaternion() {
            float x = ReceiveFloat();
            float y = ReceiveFloat();
            float z = ReceiveFloat();
            float w = ReceiveFloat();
            Quaternion q = new Quaternion(x, y, z, w);
            return q;
        }

    #endregion

    #region Debug

        public void DebugLog(string message) {
            Debug.Log(this.name + ": " + message);
        }

        public void DebugWarning(string message) {
            Debug.LogWarning(this.name + ": " + message);
        }

        public void DebugError(string message) {
            Debug.LogError(this.name + ": " + message);
        }

    #endregion
    }
#endif

}