using UnityEngine;
#if hNW_PHOTON
using System.Collections.Generic;
#endif
#if hPHOTON2
using Photon.Realtime;
using Photon.Pun;
#endif

namespace Passer.Humanoid {
    using Pawn;

#if hNW_PHOTON
#if hPHOTON2
    public partial class HumanoidPlayer : MonoBehaviourPunCallbacks, IHumanoidNetworking, IPunInstantiateMagicCallback, IPunObservable {
#else
    public partial class HumanoidPlayer : Photon.MonoBehaviour, IHumanoidNetworking {
#endif

        public ulong nwId {
#if hPHOTON2
            get { return (ulong)photonView.ViewID; }
#else
            get { return (ulong)photonView.viewID; }
#endif
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

        public bool isLocal { get; set; }

        public List<HumanoidControl> humanoids { get; set; }

        public ulong GetObjectIdentity(GameObject obj) {
            PhotonView photonView = obj.GetComponent<PhotonView>();
            if (photonView == null)
                return 0;

#if hPHOTON2
            return (ulong)photonView.ViewID;
#else
            return (ulong)photonView.viewID;
#endif
        }

        public GameObject GetGameObject(ulong objIdentity) {
            PhotonView objView = PhotonView.Find((int)objIdentity);
            if (objView == null)
                return null;
            return objView.gameObject;
        }

    #region Pawn stub
        void IPawnNetworking.InstantiatePawn(PawnControl pawn) { }
        void IPawnNetworking.DestroyPawn(PawnControl pawn) { }
        void IPawnNetworking.Grab(Pawn.PawnHand controllerTarget, GameObject obj, bool rangeCheck) { }
        void IPawnNetworking.LetGo(Pawn.PawnHand controllerTarget) { }
    #endregion

    #region Init
#if hPHOTON2
        public override void OnEnable() {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public override void OnDisable() {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }
#endif
        public void Awake() {
            DontDestroyOnLoad(this);
        }

    #endregion

    #region Start
        public void OnPhotonInstantiate(PhotonMessageInfo info) {
#if hPHOTON2
            if (photonView.IsMine) {
#else
            if (photonView.isMine) {
#endif
                isLocal = true;
                name = "HumanoidPun(Local)";

                humanoids = HumanoidNetworking.FindLocalHumanoids();
                if (debug <= PawnNetworking.DebugLevel.Info)
                    DebugLog("Found " + humanoids.Count + " Humanoids");

                for (int i = 0; i < humanoids.Count; i++) {
                    HumanoidControl humanoid = humanoids[i];
                    if (humanoid.isRemote)
                        continue;
#if hPHOTON2
                    humanoid.nwId = (ulong)photonView.ViewID;
#else
                    humanoid.nwId = nwId; // photonView.viewID;
#endif
                    humanoid.humanoidNetworking = this;

                    if (debug <= PawnNetworking.DebugLevel.Info)
                        DebugLog("Send Start Humanoid " + humanoid.humanoidId);

                    ((IHumanoidNetworking)this).InstantiateHumanoid(humanoid);

#if hPUNVOICE2
                    InstantiatePlayerVoice(humanoid);
#endif
                }

                NetworkingSpawner spawner = FindObjectOfType<NetworkingSpawner>();
                if (spawner != null)
                    spawner.OnNetworkingStarted();

            } else {
                humanoids = HumanoidNetworking.FindLocalHumanoids();
                if (debug <= PawnNetworking.DebugLevel.Info)
                    DebugLog("Found " + humanoids.Count + " Humanoids");
            }
        }

#if hPHOTON2
        public override void OnPlayerEnteredRoom(Player newPlayer) {
#else
        public void OnPhotonPlayerConnected(PhotonPlayer player) {
#endif
            List<HumanoidControl> humanoids = HumanoidNetworking.FindLocalHumanoids();
            if (humanoids.Count <= 0)
                return;

            foreach (HumanoidControl humanoid in humanoids) {
                if (debug <= PawnNetworking.DebugLevel.Info)
                    Debug.Log(humanoid.nwId + ": (Re)Send Instantiate Humanoid " + humanoid.humanoidId);

                // Notify new player about my humanoid
                ((IHumanoidNetworking)this).InstantiateHumanoid(humanoid);

                if (humanoid.leftHandTarget.grabbedObject != null)
                    humanoid.humanoidNetworking.Grab(humanoid.leftHandTarget, humanoid.leftHandTarget.grabbedObject, false);
                if (humanoid.rightHandTarget.grabbedObject != null)
                    humanoid.humanoidNetworking.Grab(humanoid.rightHandTarget, humanoid.rightHandTarget.grabbedObject, false);

            }
        }
    #endregion

    #region Update
        PhotonStream stream;

        float lastSend;

        // The number of received messages processed in this frame
        private int processedThisFrame = 0;
        private enum MessageType {
            Pose,
            Grab,
            LetGo
        }
        private struct QueuedMessage {
            public MessageType messageType;
            public byte[] data;
        }
        private Queue<QueuedMessage> messageQueue = new Queue<QueuedMessage>();

        protected virtual void LateUpdate() {
            processedThisFrame = 0;
            if (messageQueue.Count > 0) {
                ProcessMessageFromQueue();
                Debug.Log("----Processed messages = " + messageQueue.Count);
            }

            if (!createLocalRemotes)
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

        private float lastPoseTime;
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
            this.stream = stream;
#if hPHOTON2
            if (stream.IsWriting) {
#else
            if (stream.isWriting) {
#endif
                foreach (HumanoidControl humanoid in humanoids) {
                    if (!humanoid.isRemote) {
                        UpdateHumanoidPose(humanoid);
                        if (syncTracking)
                            SyncTrackingSpace(humanoid);
                    }
                }
            }
            else {
                ReceiveAvatarPose(stream);
            }
        }

        private void ProcessMessageFromQueue() {
            QueuedMessage msg = messageQueue.Dequeue();
            switch (msg.messageType) {
                case MessageType.Pose:
                    this.ReceiveHumanoidPose(msg.data);
                    break;
                case MessageType.Grab:
                    Debug.Log("Processing Queued Grab " + Time.time);
                    this.ReceiveGrab(msg.data);
                    break;
                case MessageType.LetGo:
                    Debug.Log("Processsing Queueud Let Go " + Time.time);
                    this.ReceiveLetGo(msg.data);
                    break;
            }
        }
    #endregion

    #region Stop
        private void OnDestroy() {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Destroy Remote Humanoids");

            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid == null)
                    continue;

                if (humanoid.isRemote) {
                    if (humanoid.gameObject != null)
                        Destroy(humanoid.gameObject);
                }
            }
        }
    #endregion

    #region Instantiate Humanoid

        void IHumanoidNetworking.InstantiateHumanoid(HumanoidControl humanoid) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Send Instantiate Humanoid " + humanoid.humanoidId);

            HumanoidNetworking.InstantiateHumanoid instantiateHumanoid = new HumanoidNetworking.InstantiateHumanoid(humanoid);
            if (createLocalRemotes) {
                this.Receive(instantiateHumanoid);
            }

            byte[] data = instantiateHumanoid.Serialize();
#if hPHOTON2
            photonView.RPC("RpcInstantiateHumanoid", RpcTarget.Others, data);
#else
            photonView.RPC("RpcInstantiateHumanoid", PhotonTargets.Others, data);
#endif
        }

        [PunRPC]
        protected virtual void RpcInstantiateHumanoid(byte[] data) {
            this.ReceiveInstantiate(data);
        }

#if hPUNVOICE2
        protected virtual void InstantiatePlayerVoice(HumanoidControl humanoid) {
            GameObject playerVoiceGameObject = PhotonNetwork.Instantiate("HumanoidPlayerVoice", humanoid.headTarget.transform.position, humanoid.headTarget.transform.rotation);
            HumanoidPlayerPunVoice playerVoice = playerVoiceGameObject.GetComponent<HumanoidPlayerPunVoice>();
            if (playerVoice != null)
                playerVoice.humanoid = humanoid;
            Debug.Log("created voice for " + humanoid);
        }
#endif
    #endregion

    #region Destroy Humanoid

        void IHumanoidNetworking.DestroyHumanoid(HumanoidControl humanoid) {
            if (humanoid == null)
                return;

            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Destroy Humanoid " + humanoid.humanoidId);

            HumanoidNetworking.DestroyHumanoid destroyHumanoid = new HumanoidNetworking.DestroyHumanoid(humanoid);
            if (createLocalRemotes)
                this.Receive(destroyHumanoid);

            byte[] data = destroyHumanoid.Serialize();
#if hPHOTON2
            if (PhotonNetwork.IsConnected)
                photonView.RPC("RpcDestroyHumanoid", RpcTarget.Others, data);
#else
            if (PhotonNetwork.connected)
                photonView.RPC("RpcDestroyHumanoid", PhotonTargets.Others, data);
#endif
        }

        [PunRPC]
        public void RpcDestroyHumanoid(byte[] data) {
            this.ReceiveDestroy(data);
        }
    #endregion

    #region Pose

        public HumanoidNetworking.HumanoidPose lastHumanoidPose { get; set; }

        public virtual void UpdateHumanoidPose(HumanoidControl humanoid) {
            if (debug <= PawnNetworking.DebugLevel.Debug)
                DebugLog("Send Pose Humanoid " + humanoid.humanoidId + " nwId: " + humanoid.nwId);

            HumanoidNetworking.HumanoidPose humanoidPose = new HumanoidNetworking.HumanoidPose(humanoid, Time.time);
            if (createLocalRemotes)
                this.Receive(humanoidPose);

            if (stream != null) {
                byte[] data = humanoidPose.Serialize();
                stream.SendNext(data);
            }
        }

        PhotonStream reader;

        private void ReceiveAvatarPose(PhotonStream reader) {
            this.reader = reader;

            byte[] data = (byte[])reader.ReceiveNext();
            this.ReceiveHumanoidPose(data);
        }

    #endregion

    #region Grab

        void IHumanoidNetworking.Grab(HandTarget handTarget, GameObject obj, bool rangeCheck, HandTarget.GrabType grabType) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("Grab " + obj + " " + grabType);

            ulong objIdentity = GetObjectIdentity(obj);
            if (objIdentity == 0) { 
                if (debug <= PawnNetworking.DebugLevel.Warning)
                    Debug.LogError("Photon Grab: Grabbed object does not have a PhotonView");
                return;
            }

            HumanoidNetworking.Grab grab = new HumanoidNetworking.Grab(handTarget, objIdentity, rangeCheck, grabType);
            if (createLocalRemotes)
                // Does this make sense? 
                this.Receive(grab);

            byte[] data = grab.Serialize();

#if hPHOTON2
            photonView.RPC("RpcGrab", RpcTarget.Others, data);
#else
            photonView.RPC("RpcGrab", PhotonTargets.Others, data);
#endif
        }

        [PunRPC]
        public void RpcGrab(byte[] data) {
            if (processedThisFrame > 0 || messageQueue.Count > 0) {
                QueuedMessage msg = new QueuedMessage() {
                    messageType = MessageType.Grab,
                    data = data
                };
                messageQueue.Enqueue(msg);
                Debug.Log("++++Buffered Grab message = " + messageQueue.Count + " " + Time.time);
                return;
            }

            this.ReceiveGrab(data);
            processedThisFrame++;
        }

    #endregion

    #region Let Go

        void IHumanoidNetworking.LetGo(HandTarget handTarget) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                DebugLog("LetGo");

            HumanoidNetworking.LetGo letGo = new HumanoidNetworking.LetGo(handTarget);
            if (createLocalRemotes)
                this.Receive(letGo);

            byte[] data = letGo.Serialize();
#if hPHOTON2
            photonView.RPC("RpcLetGo", RpcTarget.Others, data);
#else
            photonView.RPC("RpcLetGo", PhotonTargets.Others, data);
#endif
        }

        [PunRPC]
        public void RpcLetGo(byte[] data) {
            if (processedThisFrame > 0 || messageQueue.Count > 0) {
                QueuedMessage msg = new QueuedMessage() {
                    messageType = MessageType.LetGo,
                    data = data
                };
                messageQueue.Enqueue(msg);
                Debug.Log("++++Buffered Let Go message = " + messageQueue.Count + " " + Time.time);
                return;
            }

            this.ReceiveLetGo(data);
            processedThisFrame++;
        }

    #endregion

    #region Change Avatar
        void IHumanoidNetworking.ChangeAvatar(HumanoidControl humanoid, string avatarPrefabName) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log(humanoid.nwId + ": Change Avatar: " + avatarPrefabName);

            HumanoidNetworking.ChangeAvatar changeAvatar = new HumanoidNetworking.ChangeAvatar(humanoid, avatarPrefabName);
            if (createLocalRemotes)
                this.Receive(changeAvatar);

            byte[] data = changeAvatar.Serialize();
#if hPHOTON2
            photonView.RPC("RpcChangeAvatar", RpcTarget.Others, data); // humanoid.humanoidId, avatarPrefabName);
#else
            photonView.RPC("RpcChangeAvatar", PhotonTargets.Others, data); // humanoid.humanoidId, avatarPrefabName);
#endif
        }

        [PunRPC]
        protected virtual void RpcChangeAvatar(byte[] data) {
            this.ReceiveChangeAvatar(data);
        }

    #endregion

    #region Tracking

        private Transform GetTrackingTransform(HumanoidControl humanoid) {
#if hANTILATENCY
            if (humanoid.antilatency != null)
                return humanoid.antilatency.trackerTransform;
#endif
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
                DebugLog("Send Sync Tracking Space " + humanoid.humanoidId + " " + trackingTransform.position + " " + trackingTransform.rotation);

            HumanoidNetworking.SyncTrackingSpace syncTrackingSpace =
                new HumanoidNetworking.SyncTrackingSpace(humanoid, trackingTransform.position, trackingTransform.rotation);

            if (createLocalRemotes)
                // Does this make sense?
                this.Receive(syncTrackingSpace);

            byte[] data = syncTrackingSpace.Serialize();
#if hPHOTON2
            photonView.RPC("RpcSyncTracking", RpcTarget.Others, data);
#else
            photonView.RPC("RpcSyncTracking", PhotonTargets.Others, data);
#endif

        }

        [PunRPC]
        protected virtual void RpcSyncTracking(byte[] data) {
            this.ReceiveSyncTrackingSpace(data);
        }

    #endregion

    #region Network Sync

        void IHumanoidNetworking.ReenableNetworkSync(GameObject obj) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log("ReenableNetworkSync " + obj);

            ReenableNetworkSync(obj);
        }

        void IHumanoidNetworking.DisableNetworkSync(GameObject obj) {
            if (debug <= PawnNetworking.DebugLevel.Info)
                Debug.Log("DisableNetworkSync " + obj);
            DisableNetworkSync(obj);
        }

        public static void ReenableNetworkSync(GameObject obj) {
#if hPHOTON2
            PhotonView photonView = obj.GetComponent<PhotonView>();
            if (photonView != null) {
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            }
#endif

            PhotonTransformView transformView = obj.GetComponent<PhotonTransformView>();
            if (transformView != null) {
#if hPHOTON2
                transformView.m_SynchronizePosition = true;
                transformView.m_SynchronizeRotation = true;
                transformView.enabled = true;

#else
                transformView.m_PositionModel.SynchronizeEnabled = true;
                transformView.m_RotationModel.SynchronizeEnabled = true;
#endif
            }
        }

        public static void TakeOwnership(GameObject obj) {
#if hPHOTON2
            PhotonView photonView = obj.GetComponent<PhotonView>();
            if (photonView != null)
                photonView.RequestOwnership();
#endif
        }

        public static void DisableNetworkSync(GameObject obj) {
            PhotonTransformView transformView = obj.GetComponent<PhotonTransformView>();
            if (transformView != null) {
#if hPHOTON2
                transformView.m_SynchronizePosition = false;
                transformView.m_SynchronizeRotation = false;
                transformView.enabled = false;
#else
                transformView.m_PositionModel.SynchronizeEnabled = false;
                transformView.m_RotationModel.SynchronizeEnabled = false;
#endif
            }
        }

    #endregion

    #region Send
        public void Send(bool b) { stream.SendNext(b); }
        public void Send(byte b) { stream.SendNext(b); }
        public void Send(int x) { stream.SendNext(x); }
        public void Send(float f) { stream.SendNext(f); }
        public void Send(Vector3 v) { stream.SendNext(v); }
        public void Send(Quaternion q) { stream.SendNext(q); }
    #endregion

    #region Receive
        public bool ReceiveBool() { return (bool)reader.ReceiveNext(); }
        public byte ReceiveByte() { return (byte)reader.ReceiveNext(); }
        public int ReceiveInt() { return (int)reader.ReceiveNext(); }
        public float ReceiveFloat() { return (float)reader.ReceiveNext(); }
        public Vector3 ReceiveVector3() { return (Vector3)reader.ReceiveNext(); }
        public Quaternion ReceiveQuaternion() { return (Quaternion)reader.ReceiveNext(); }
    #endregion

    #region Debug

        public void DebugLog(string message) {
#if hPHOTON2
            Debug.Log(photonView.ViewID + ": " + message);
#else
            Debug.Log(photonView.viewID + ": " + message);
#endif
        }

        public void DebugWarning(string message) {
#if hPHOTON2
            Debug.LogWarning(photonView.ViewID + ": " + message);
#else
            Debug.LogWarning(photonView.viewID + ": " + message);
#endif
        }

        public void DebugError(string message) {
#if hPHOTON2
            Debug.LogError(photonView.ViewID + ": " + message);
#else
            Debug.LogError(photonView.viewID + ": " + message);
#endif
        }

    #endregion
    }
#endif
}
