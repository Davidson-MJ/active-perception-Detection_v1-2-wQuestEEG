//#define RemoteAvatarBundles

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Passer.Humanoid {
    using Pawn;

    /// <summary>
    /// Interface for Humanoid Networking functions
    /// </summary>
    public interface IHumanoidNetworking : IPawnNetworking {

        ulong nwId { get; }

        bool syncFingerSwing { get; }

        bool syncTracking { get; set; }
        bool fuseTracking { get; }

        List<HumanoidControl> humanoids { get; }

        HumanoidNetworking.HumanoidPose lastHumanoidPose { get; set; }

        ulong GetObjectIdentity(GameObject obj);
        GameObject GetGameObject(ulong objIdentity);

        void InstantiateHumanoid(HumanoidControl humanoid);
        void DestroyHumanoid(HumanoidControl humanoid);

        void UpdateHumanoidPose(HumanoidControl humanoid);

        void Grab(HandTarget handTarget, GameObject obj, bool rangeCheck, HandTarget.GrabType grabType = HandTarget.GrabType.HandGrab);
        void LetGo(HandTarget handTarget);

        void ChangeAvatar(HumanoidControl humanoid, string remoteAvatarName);

        void SyncTrackingSpace(HumanoidControl humanoid);

        void DebugLog(string s);
        void DebugWarning(string s);
        void DebugError(string s);

        void ReenableNetworkSync(GameObject obj);
        void DisableNetworkSync(GameObject obj);

    }

    /// <summary>
    /// Humanoid Networking
    /// </summary>
    public static class HumanoidNetworking {
        //public enum Smoothing {
        //    None,
        //    Interpolation,
        //    Extrapolation
        //};

        public class IMessage
#if hNW_MIRROR
            : Mirror.NetworkMessage
#endif
        {
            public IMessage() { }
            public IMessage(byte[] data) {
                Deserialize(data);
            }

            public virtual byte[] Serialize() { return null; }
            public virtual void Deserialize(byte[] data) { }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    //writer.WriteBytesAndSize(data);
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    //byte[] data = reader.ReadBytesAndSize();
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif
        }

        public static void Connected(HumanoidControl humanoid) {
            OnConnectedToNetwork(humanoid);
        }

        #region Instantiate Humanoid

        public class InstantiateHumanoid : IMessage {
            public ulong nwId;
            public byte humanoidId;
            public string name;
            public string avatarPrefabName;
            public bool physics;
#if RemoteAvatarBundles
            public int remoteAvatarBundleSize;
            public byte[] remoteAvatarBundle;
#endif

            public InstantiateHumanoid() { }
            public InstantiateHumanoid(HumanoidControl humanoid) {
                nwId = humanoid.nwId;
                humanoidId = (byte)humanoid.humanoidId;

                int nameLength = humanoid.gameObject.name.Length;
                if (nameLength > 9 && humanoid.gameObject.name.Substring(nameLength - 9, 9) == " (Remote)")
                    name = humanoid.gameObject.name.Substring(0, nameLength - 9);
                else
                    name = humanoid.gameObject.name;

                if (humanoid.remoteAvatar == null)
                    avatarPrefabName = humanoid.avatarRig.name.Substring(0, humanoid.avatarRig.name.Length - 7);
                else
                    avatarPrefabName = humanoid.remoteAvatar.name;

#if RemoteAvatarBundles
                /* Get the remoteAvatarBundle from the streaming assets */
                remoteAvatarBundle = File.ReadAllBytes(Application.streamingAssetsPath + "/RemoteAvatarBundles/" + avatarPrefabName);
                remoteAvatarBundleSize = remoteAvatarBundle.Length;
#endif
                physics = humanoid.physics;
            }
            public InstantiateHumanoid(byte[] data) : base(data) { }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);
                bw.Write(name);
                bw.Write(avatarPrefabName);
                bw.Write(physics);
#if RemoteAvatarBundles
                bw.Write(remoteAvatarBundleSize);
                bw.Write(remoteAvatarBundle);
#endif

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();
                name = br.ReadString();
                avatarPrefabName = br.ReadString();
                physics = br.ReadBoolean();
#if RemoteAvatarBundles
                remoteAvatarBundleSize = br.ReadInt32();
                remoteAvatarBundle = br.ReadBytes(remoteAvatarBundleSize);
#endif
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif        
        }

        public static void ReceiveInstantiate(this IHumanoidNetworking networking, byte[] serializedData) {
            InstantiateHumanoid data = new InstantiateHumanoid(serializedData);

            Receive(networking, data);
        }

        public static void Receive(this IHumanoidNetworking receivingNetworking, InstantiateHumanoid data) {

            GameObject networkingObj = receivingNetworking.GetGameObject(data.nwId);
            if (networkingObj == null) {
                if (receivingNetworking.debug <= PawnNetworking.DebugLevel.Error)
                    receivingNetworking.DebugLog("Could not find Networking for Instantiate Humanoid " + data.nwId + "/" + data.humanoidId);
                return;
            }

            IHumanoidNetworking networking = networkingObj.GetComponent<IHumanoidNetworking>();

            if (networking.isLocal && !networking.createLocalRemotes) {
                if (networking.debug <= PawnNetworking.DebugLevel.Debug)
                    networking.DebugLog("Remote Humanoid " + data.nwId + "/" + data.humanoidId + " is local and local remotes are not created");
                return;
            }

            HumanoidControl remoteHumanoid = FindRemoteHumanoid(networking.humanoids, data.humanoidId);
            if (remoteHumanoid != null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Warning)
                    networking.DebugLog("Remote Humanoid " + data.nwId + "/" + data.humanoidId + " already exists");
                // This remote humanoid already exists
                return;
            }

            if (networking.debug <= PawnNetworking.DebugLevel.Info)
                networking.DebugLog("Receive Instantiate Humanoid " + data.nwId + "/" + data.humanoidId);

            remoteHumanoid = InstantiateRemoteHumanoid(data.name, Vector3.zero, Quaternion.identity); //, position, rotation);
            remoteHumanoid.nwId = data.nwId;
            remoteHumanoid.humanoidId = data.humanoidId;

            if (networking.debug <= PawnNetworking.DebugLevel.Info)
                networking.DebugLog("Remote Humanoid " + remoteHumanoid.nwId + "/" + remoteHumanoid.humanoidId + " Added");

#if RemoteAvatarBundles
            AssetBundle assetBundle = AssetBundle.LoadFromMemory(data.remoteAvatarBundle);
            GameObject remoteAvatar = assetBundle.LoadAsset<GameObject>(data.avatarPrefabName);
            if (remoteAvatar == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Error)
                    networking.DebugError("Could not load remote avatar " + data.avatarPrefabName + ". Asset Bundle is not present");
                return;
            }
#else
            GameObject remoteAvatar = (GameObject)Resources.Load(data.avatarPrefabName);
            if (remoteAvatar == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Error)
                    networking.DebugError("Could not load remote avatar " + data.avatarPrefabName + ". Is it located in a Resources folder?");
                return;
            }
#endif
            remoteHumanoid.physics = data.physics;
            remoteHumanoid.LocalChangeAvatar(remoteAvatar);

            networking.humanoids.Add(remoteHumanoid);
            if (OnNewRemoteHumanoid != null)
                OnNewRemoteHumanoid(remoteHumanoid);
        }

        public static HumanoidControl FindRemoteHumanoid(List<HumanoidControl> humanoids, ulong nwId, int humanoidId) {
            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid.isRemote && humanoid.nwId == nwId && humanoid.humanoidId == humanoidId)
                    return humanoid;
            }
            return null;
        }

        private static HumanoidControl InstantiateRemoteHumanoid(string name, Vector3 position, Quaternion rotation) {
            GameObject remoteHumanoidPrefab = (GameObject)Resources.Load("RemoteHumanoid");

            GameObject remoteHumanoidObj = UnityEngine.Object.Instantiate(remoteHumanoidPrefab, position, rotation);
            remoteHumanoidObj.name = name + " (Remote)";

            HumanoidControl remoteHumanoid = remoteHumanoidObj.GetComponent<HumanoidControl>();
            remoteHumanoid.isRemote = true;

            return remoteHumanoid;
        }

        public delegate void ConnectedToNetwork(HumanoidControl humanoid);
        public static event ConnectedToNetwork OnConnectedToNetwork;

        public delegate void NewRemoteHumanoidArgs(HumanoidControl humanoid);
        public static event NewRemoteHumanoidArgs OnNewRemoteHumanoid;

        #endregion

        #region Destroy Humanoid

        public class DestroyHumanoid : IMessage {
            public ulong nwId;
            public byte humanoidId;

            public DestroyHumanoid() { }
            public DestroyHumanoid(HumanoidControl humanoid) {
                nwId = humanoid.nwId;
                humanoidId = (byte)humanoid.humanoidId;
            }
            public DestroyHumanoid(byte[] data) : base(data) { }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif        
        }

        public static void ReceiveDestroy(this IHumanoidNetworking networking, byte[] serializedData) {
            DestroyHumanoid data = new DestroyHumanoid(serializedData);
            Receive(networking, data);
        }

        public static void Receive(this IHumanoidNetworking networking, DestroyHumanoid msg) {
            if (networking.isLocal && !networking.createLocalRemotes)
                return;

            HumanoidControl remoteHumanoid = FindRemoteHumanoid(networking.humanoids, msg.humanoidId);
            if (remoteHumanoid == null) {
                // Unknown remote humanoid
                return;
            }

            if (remoteHumanoid.gameObject != null)
                UnityEngine.Object.Destroy(remoteHumanoid.gameObject);
        }

        #endregion

        #region Pose

        [Serializable]
        public struct Vector3S {
            public float x;
            public float y;
            public float z;

            public Vector3S(Vector3 v) {
                x = v.x;
                y = v.y;
                z = v.z;
            }
            public Vector3S(Quaternion q) {
                Vector3 euler = q.eulerAngles;
                x = euler.x;
                y = euler.y;
                z = euler.z;
            }

            public Vector3 vector3 {
                get { return new Vector3(x, y, z); }
            }
            public Quaternion quaternion {
                get { return Quaternion.Euler(x, y, z); }
            }

            public void Write(BinaryWriter bw) {
                bw.Write(x);
                bw.Write(y);
                bw.Write(z);
            }

            public static Vector3S Read(BinaryReader br) {
                float x = br.ReadSingle();
                float y = br.ReadSingle();
                float z = br.ReadSingle();
                Vector3S v = new Vector3S() { x = x, y = y, z = z };
                return v;
            }
        }

        // Rotations can be sent much more efficient: 3 * 16bit (angles) or 4 * 16bit (quatornions)

        [Serializable]
        public class HumanoidAnimatorParameters {
            public class AnimatorParameter {
                public AnimatorControllerParameterType type;
                public bool boolValue;
                public float floatValue;
                public int intValue;
            }

            public AnimatorParameter[] parameters;
            public HumanoidAnimatorParameters() {
                parameters = new AnimatorParameter[0];
            }

            public HumanoidAnimatorParameters(HumanoidControl humanoid) {
                if (humanoid.targetsRig.runtimeAnimatorController == null) {
                    parameters = new AnimatorParameter[0];
                    return;
                }

                parameters = new AnimatorParameter[humanoid.targetsRig.parameterCount];

                for (int i = 0; i < humanoid.targetsRig.parameterCount; i++) {
                    AnimatorControllerParameter parameter = humanoid.targetsRig.parameters[i];
                    parameters[i] = new AnimatorParameter() {
                        type = parameter.type
                    };
                    switch (parameters[i].type) {
                        case AnimatorControllerParameterType.Bool:
                            parameters[i].boolValue = humanoid.targetsRig.GetBool(parameter.name);
                            break;
                        case AnimatorControllerParameterType.Float:
                            parameters[i].floatValue = humanoid.targetsRig.GetFloat(parameter.name);
                            break;
                        case AnimatorControllerParameterType.Int:
                            parameters[i].intValue = humanoid.targetsRig.GetInteger(parameter.name);
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            parameters[i].boolValue = humanoid.targetsRig.GetBool(parameter.name);
                            break;
                    }
                }
            }

            public virtual void Write(BinaryWriter bw) {
                bw.Write(parameters.Length);
                for (int i = 0; i < parameters.Length; i++) {
                    bw.Write((byte)parameters[i].type);
                    switch (parameters[i].type) {
                        case AnimatorControllerParameterType.Bool:
                            bw.Write(parameters[i].boolValue);
                            break;
                        case AnimatorControllerParameterType.Float:
                            bw.Write(parameters[i].floatValue);
                            break;
                        case AnimatorControllerParameterType.Int:
                            bw.Write(parameters[i].intValue);
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            bw.Write(parameters[i].boolValue);
                            break;
                    }
                }
            }
            public static HumanoidAnimatorParameters Read(BinaryReader br) {
                int parameterCount = br.ReadInt32();
                HumanoidAnimatorParameters animatorParameters = new HumanoidAnimatorParameters() {
                    parameters = new AnimatorParameter[parameterCount]
                };

                for (int i = 0; i < parameterCount; i++) {
                    AnimatorControllerParameterType parameterType = (AnimatorControllerParameterType)br.ReadByte();
                    animatorParameters.parameters[i] = new AnimatorParameter() {
                        type = parameterType
                    };
                    switch (parameterType) {
                        case AnimatorControllerParameterType.Bool:
                            animatorParameters.parameters[i].boolValue = br.ReadBoolean();
                            break;
                        case AnimatorControllerParameterType.Float:
                            animatorParameters.parameters[i].floatValue = br.ReadSingle();
                            break;
                        case AnimatorControllerParameterType.Int:
                            animatorParameters.parameters[i].intValue = br.ReadInt32();
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            animatorParameters.parameters[i].boolValue = br.ReadBoolean();
                            break;
                    }
                }
                return animatorParameters;
            }
        }

        [Serializable]
        public class HumanoidHandPose : HumanoidTargetPose {
            public float thumbCurl;
            public float indexCurl;
            public float middleCurl;
            public float ringCurl;
            public float littleCurl;

            public bool syncSwing;

            public float thumbSwing;
            public float indexSwing;
            public float middleSwing;
            public float ringSwing;
            public float littleSwing;

            public HumanoidHandPose() : base(Tracking.Bone.None) { }

            public HumanoidHandPose(Tracking.Bone boneId) : base(boneId) { }

            public HumanoidHandPose(HandTarget handTarget, bool syncSwing) : base(handTarget) {
                this.syncSwing = syncSwing;

                FingersTarget.UpdateCurlValues(handTarget);
                //FingersTarget.UpdateCurlValues(handTarget);
                thumbCurl = handTarget.fingers.thumb.curl;
                indexCurl = handTarget.fingers.index.curl;
                middleCurl = handTarget.fingers.middle.curl;
                ringCurl = handTarget.fingers.ring.curl;
                littleCurl = handTarget.fingers.little.curl;

                thumbSwing = handTarget.fingers.thumb.swing;
                indexSwing = handTarget.fingers.index.swing;
                middleSwing = handTarget.fingers.middle.swing;
                ringSwing = handTarget.fingers.ring.swing;
                littleSwing = handTarget.fingers.little.swing;
            }

            public override void Write(BinaryWriter bw) {
                base.Write(bw);

                if (boneId != Tracking.Bone.None) {
                    bw.Write(thumbCurl);
                    bw.Write(indexCurl);
                    bw.Write(middleCurl);
                    bw.Write(ringCurl);
                    bw.Write(littleCurl);
                }
            }
            public static new HumanoidHandPose Read(BinaryReader br) {
                Tracking.Bone boneId = (Tracking.Bone)br.ReadSByte();
                if (boneId == Tracking.Bone.None)
                    return new HumanoidHandPose();
                else
                    return new HumanoidHandPose(boneId) {
                        localPosition = Vector3S.Read(br),
                        positionConfidenceByte = br.ReadByte(),
                        rotation = Vector3S.Read(br),
                        rotationConfidenceByte = br.ReadByte(),

                        thumbCurl = br.ReadSingle(),
                        indexCurl = br.ReadSingle(),
                        middleCurl = br.ReadSingle(),
                        ringCurl = br.ReadSingle(),
                        littleCurl = br.ReadSingle(),
                    };

            }
        }

        [Serializable]
        public class HumanoidTargetPose {
            public Tracking.Bone boneId;

            public Vector3S localPosition;
            protected byte positionConfidenceByte;
            public float positionConfidence {
                get { return ToFloat(positionConfidenceByte); }
            }
            public Vector3S rotation;
            protected byte rotationConfidenceByte;
            public float rotationConfidence {
                get { return ToFloat(rotationConfidenceByte); }
            }

            public float ToFloat(byte confidenceByte) {
                float value = Convert.ToSingle(confidenceByte) / 255;
                return value;
            }

            public HumanoidTargetPose() {
                this.boneId = Tracking.Bone.None;

                localPosition = new Vector3S();
                rotation = new Vector3S();
            }

            public HumanoidTargetPose(Tracking.Bone boneId) {
                this.boneId = boneId;

                localPosition = new Vector3S();
                rotation = new Vector3S();
            }
            public HumanoidTargetPose(HumanoidTarget target) {
                boneId = GetBoneId(target);
                // Head bone is always synced
                if (boneId != Tracking.Bone.Head && !HumanoidPose.TargetActive(target))
                    boneId = Tracking.Bone.None;

                Vector3 localPosition = target.humanoid.transform.InverseTransformPoint(target.main.target.transform.position);
                this.localPosition = new Vector3S(localPosition);
                positionConfidenceByte = Convert.ToByte(target.main.target.confidence.position * 255);

                rotation = new Vector3S(target.main.target.transform.rotation);
                rotationConfidenceByte = Convert.ToByte(target.main.target.confidence.rotation * 255);
            }

            private Tracking.Bone GetBoneId(HumanoidTarget target) {
                if (target == target.humanoid.hipsTarget)
                    return Tracking.Bone.Hips;
                else if (target == target.humanoid.headTarget)
                    return Tracking.Bone.Head;
                else if (target == target.humanoid.leftHandTarget)
                    return Tracking.Bone.LeftHand;
                else if (target == target.humanoid.rightHandTarget)
                    return Tracking.Bone.RightHand;
                else if (target == target.humanoid.leftFootTarget)
                    return Tracking.Bone.LeftFoot;
                else if (target == target.humanoid.rightFootTarget)
                    return Tracking.Bone.RightFoot;
                else
                    return Tracking.Bone.None;
            }

            public virtual void Write(BinaryWriter bw) {
                bw.Write((sbyte)boneId);
                if (boneId != Tracking.Bone.None) {
                    localPosition.Write(bw);
                    bw.Write(positionConfidenceByte);
                    rotation.Write(bw);
                    bw.Write(rotationConfidenceByte);
                }
            }

            public static HumanoidTargetPose Read(BinaryReader br) {
                Tracking.Bone boneId = (Tracking.Bone)br.ReadSByte();
                if (boneId == Tracking.Bone.None)
                    return new HumanoidTargetPose();
                else {
                    return new HumanoidTargetPose(boneId) {
                        localPosition = Vector3S.Read(br),
                        positionConfidenceByte = br.ReadByte(),
                        rotation = Vector3S.Read(br),
                        rotationConfidenceByte = br.ReadByte()
                    };
                }
            }
        }

        [Serializable]
        public class HumanoidPose : IMessage {
            public ulong nwId;
            public byte humanoidId;

            public float poseTime;
            public float receiveTime;

            public Vector3S position;
            public Vector3S rotation;

            public HumanoidAnimatorParameters animatorParameters;

            //public byte targetMask;
            public HumanoidTargetPose hips;
            public HumanoidTargetPose head;
            public HumanoidHandPose leftHand;
            public HumanoidHandPose rightHand;
            public HumanoidTargetPose leftFoot;
            public HumanoidTargetPose rightFoot;

            public bool syncFace;
#if hFACE
            public HumanoidFacePose faceTarget;
#endif

            public HumanoidPose() {
                animatorParameters = new HumanoidAnimatorParameters();

                hips = new HumanoidTargetPose(Tracking.Bone.None);
                head = new HumanoidTargetPose(Tracking.Bone.None);
                leftHand = new HumanoidHandPose(Tracking.Bone.None);
                rightHand = new HumanoidHandPose(Tracking.Bone.None);
                leftFoot = new HumanoidTargetPose(Tracking.Bone.None);
                rightFoot = new HumanoidTargetPose(Tracking.Bone.None);
#if hFACE
                faceTarget = new HumanoidFacePose();
#endif
            }
            public HumanoidPose(HumanoidControl humanoid, float poseTime,
                bool syncFingerSwing = false, bool syncFace = false) {

                this.syncFace = syncFace;

                nwId = humanoid.nwId;
                humanoidId = (byte)humanoid.humanoidId;

                this.poseTime = poseTime;

                position = new Vector3S(humanoid.transform.position);
                rotation = new Vector3S(humanoid.transform.rotation);

                animatorParameters = new HumanoidAnimatorParameters(humanoid);

                hips = new HumanoidTargetPose(humanoid.hipsTarget);
                head = new HumanoidTargetPose(humanoid.headTarget);
                leftHand = new HumanoidHandPose(humanoid.leftHandTarget, syncFingerSwing);
                rightHand = new HumanoidHandPose(humanoid.rightHandTarget, syncFingerSwing);
                leftFoot = new HumanoidTargetPose(humanoid.leftFootTarget);
                rightFoot = new HumanoidTargetPose(humanoid.rightFootTarget);
#if hFACE
                faceTarget = new HumanoidFacePose(humanoid.headTarget.face);
#endif
            }
            public HumanoidPose(byte[] data) : base(data) { }


            public static bool TargetActive(HumanoidTarget target) {
                //bool active = target.main != null &&
                //    (target.main.target.confidence.position > 0.2F || target.main.target.confidence.rotation > 0.2F);

                //                if (!target.animator.enabled)
                return true;

                //                return active;
            }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);

                bw.Write(poseTime);

                position.Write(bw);
                rotation.Write(bw);

                animatorParameters.Write(bw);

                hips.Write(bw);
                head.Write(bw);
                leftHand.Write(bw);
                rightHand.Write(bw);
                leftFoot.Write(bw);
                rightFoot.Write(bw);
#if hFACE
                if (syncFace)
                    faceTarget.Write(bw);
#endif

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();

                poseTime = br.ReadSingle();

                position = Vector3S.Read(br);
                rotation = Vector3S.Read(br);

                animatorParameters = HumanoidAnimatorParameters.Read(br);

                hips = HumanoidTargetPose.Read(br);
                head = HumanoidTargetPose.Read(br);
                leftHand = HumanoidHandPose.Read(br);
                rightHand = HumanoidHandPose.Read(br);
                leftFoot = HumanoidTargetPose.Read(br);
                rightFoot = HumanoidTargetPose.Read(br);
#if hFACE
                if (syncFace)
                    faceTarget = HumanoidFacePose.Read(br);
#endif
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif

            public HumanoidTargetPose GetTargetPose(Tracking.Bone boneId) {
                switch (boneId) {
                    case Tracking.Bone.Hips:
                        return hips;
                    case Tracking.Bone.Head:
                        return head;
                    case Tracking.Bone.LeftHand:
                        return leftHand;
                    case Tracking.Bone.RightHand:
                        return rightHand;
                    case Tracking.Bone.LeftFoot:
                        return leftFoot;
                    case Tracking.Bone.RightFoot:
                        return rightFoot;
                    default:
                        return null;
                }
            }
        }
#if hFACE
        [Serializable]
        public class HumanoidFacePose {
            public short targetMask;

            public sbyte leftBrowOuterRaise;
            public sbyte leftBrowInnerRaise;

            public sbyte rightBrowOuterRaise;
            public sbyte rightBrowInnerRaise;

            public sbyte leftEyeClosed;
            public sbyte rightEyeClosed;

            public sbyte mouthLeftRaise;
            public sbyte mouthRightRaise;
            public sbyte mouthLeftStretch;
            public sbyte mouthRightStretch;
            public sbyte mouthShiftRight;

            public sbyte jawOpen;
            public sbyte jawShiftRight;

            public HumanoidFacePose() { }
            public HumanoidFacePose(FaceTarget face) {
                leftBrowOuterRaise = (sbyte)(face.leftBrow.outerRaise * 127);
                leftBrowInnerRaise = (sbyte)(face.leftBrow.innerRaise * 127);
                rightBrowOuterRaise = (sbyte)(face.rightBrow.outerRaise * 127);
                rightBrowInnerRaise = (sbyte)(face.rightBrow.innerRaise * 127);
                leftEyeClosed = (sbyte)(face.leftEye.closed * 127);
                rightEyeClosed = (sbyte)(face.rightEye.closed * 127);
                mouthLeftRaise = (sbyte)(face.mouth.leftRaise * 127);
                mouthRightRaise = (sbyte)(face.mouth.rightRaise * 127);
                mouthLeftStretch = (sbyte)(face.mouth.leftStretch * 127);
                mouthRightStretch = (sbyte)(face.mouth.rightStretch * 127);
                mouthShiftRight = (sbyte)(face.mouth.shiftRight * 127);
                jawOpen = (sbyte)(face.jaw.open * 127);
                jawShiftRight = (sbyte)(face.jaw.shiftRight * 127);
            }

            public void Write(BinaryWriter bw) {
                bw.Write(leftBrowOuterRaise);
                bw.Write(leftBrowInnerRaise);
                bw.Write(rightBrowOuterRaise);
                bw.Write(rightBrowInnerRaise);
                bw.Write(leftEyeClosed);
                bw.Write(rightEyeClosed);
                bw.Write(mouthLeftRaise);
                bw.Write(mouthRightRaise);
                bw.Write(mouthLeftStretch);
                bw.Write(mouthRightStretch);
                bw.Write(mouthShiftRight);
                bw.Write(jawOpen);
                bw.Write(jawShiftRight);
            }

            public static HumanoidFacePose Read(BinaryReader br) {
                HumanoidFacePose pose = new HumanoidFacePose();
                pose.leftBrowOuterRaise = br.ReadSByte();
                pose.leftBrowInnerRaise = br.ReadSByte();
                pose.rightBrowOuterRaise = br.ReadSByte();
                pose.rightBrowInnerRaise = br.ReadSByte();
                pose.leftEyeClosed = br.ReadSByte();
                pose.rightEyeClosed = br.ReadSByte();
                pose.mouthLeftRaise = br.ReadSByte();
                pose.mouthRightRaise = br.ReadSByte();
                pose.mouthLeftStretch = br.ReadSByte();
                pose.mouthRightStretch = br.ReadSByte();
                pose.mouthShiftRight = br.ReadSByte();
                pose.jawOpen = br.ReadSByte();
                pose.jawShiftRight = br.ReadSByte();
                return pose;
            }
        }
#endif
        public static void ReceiveHumanoidPose(this IHumanoidNetworking networking, byte[] data) {
            if (data == null)
                return;


            HumanoidPose humanoidPose = new HumanoidPose(data);
            Receive(networking, humanoidPose);
        }

        public static void Receive(this IHumanoidNetworking receivingNetworking, HumanoidPose humanoidPose) {
            //DebugHumanoidPose(humanoidPose);

            humanoidPose.receiveTime = Time.time;

            IHumanoidNetworking networking = GetHumanoidNetworking(receivingNetworking, humanoidPose.nwId);
            if (networking == null) {
                if (receivingNetworking.debug <= PawnNetworking.DebugLevel.Error)
                    receivingNetworking.DebugLog("Could not find Networking for Humanoid Pose " + humanoidPose.nwId + "/" + humanoidPose.humanoidId);
                return;
            }

            if (networking.isLocal && !networking.createLocalRemotes)
                return;

            if (networking.humanoids == null) {
                networking.DebugLog("No humanoids found");
                return;
            }

            HumanoidControl remoteHumanoid = FindRemoteHumanoid(networking.humanoids, humanoidPose.humanoidId);
            if (remoteHumanoid == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Warning)
                    networking.DebugWarning("Could not find humanoid: " + humanoidPose.nwId + "/" + humanoidPose.humanoidId);
                return;
            }

            if (networking.debug <= PawnNetworking.DebugLevel.Debug)
                networking.DebugLog("Receive HumanoidPose " + humanoidPose.nwId + "/" + humanoidPose.humanoidId);

            ReceiveHumanoidPose(remoteHumanoid, humanoidPose, networking.lastHumanoidPose, networking.smoothing);

            if (networking.createLocalRemotes)
                remoteHumanoid.transform.Translate(0, 0, 1, Space.Self);

            networking.lastHumanoidPose = humanoidPose;
        }

        private static void DebugHumanoidPose(HumanoidPose pose) {
            string s = "";
            if (pose.hips.boneId == Tracking.Bone.Hips)
                s += " HI_" + pose.hips.positionConfidence + "/" + pose.hips.rotationConfidence;
            if (pose.head.boneId == Tracking.Bone.Head)
                s += " HE_" + pose.head.positionConfidence + "/" + pose.head.rotationConfidence;
            if (pose.leftHand.boneId == Tracking.Bone.LeftHand)
                s += " LH_" + pose.leftHand.positionConfidence + "/" + pose.leftHand.rotationConfidence;
            if (pose.rightHand.boneId == Tracking.Bone.RightHand)
                s += " RH_" + pose.rightHand.positionConfidence + "/" + pose.rightHand.rotationConfidence;
            if (pose.leftFoot.boneId == Tracking.Bone.LeftFoot)
                s += " LH_" + pose.leftFoot.positionConfidence + "/" + pose.leftFoot.rotationConfidence;
            if (pose.rightFoot.boneId == Tracking.Bone.RightFoot)
                s += " RH_" + pose.rightFoot.positionConfidence + "/" + pose.rightFoot.rotationConfidence;
            Debug.Log(s);
        }

        public static void ReceiveHumanoidPose(
            HumanoidControl remoteHumanoid,
            HumanoidPose humanoidPose, HumanoidPose lastHumanoidPose,
            PawnNetworking.Smoothing smoothing) {

            remoteHumanoid.transform.position = humanoidPose.position.vector3;
            remoteHumanoid.transform.rotation = humanoidPose.rotation.quaternion;

            if (lastHumanoidPose != null)
                CalculateVelocity(remoteHumanoid, humanoidPose.position.vector3, lastHumanoidPose.position.vector3,
                    humanoidPose.poseTime, humanoidPose.receiveTime, lastHumanoidPose.poseTime);

            ReceiveAnimationParameters(remoteHumanoid, humanoidPose);

            remoteHumanoid.headTarget.animator.enabled = true;
            remoteHumanoid.leftHandTarget.animator.enabled = true;
            remoteHumanoid.rightHandTarget.animator.enabled = true;
            remoteHumanoid.hipsTarget.animator.enabled = true;
            remoteHumanoid.leftFootTarget.animator.enabled = true;
            remoteHumanoid.rightFootTarget.animator.enabled = true;

            ReceiveTargetPose(remoteHumanoid, humanoidPose, humanoidPose.hips, lastHumanoidPose, smoothing);
            ReceiveTargetPose(remoteHumanoid, humanoidPose, humanoidPose.head, lastHumanoidPose, smoothing);
            HeadAnimator.UpdateNeckTargetFromHead(remoteHumanoid.headTarget);
            ReceiveTargetPose(remoteHumanoid, humanoidPose, humanoidPose.leftHand, lastHumanoidPose, smoothing);
            ReceiveTargetPose(remoteHumanoid, humanoidPose, humanoidPose.rightHand, lastHumanoidPose, smoothing);
            ReceiveTargetPose(remoteHumanoid, humanoidPose, humanoidPose.leftFoot, lastHumanoidPose, smoothing);
            ReceiveTargetPose(remoteHumanoid, humanoidPose, humanoidPose.rightFoot, lastHumanoidPose, smoothing);

            remoteHumanoid.CopyRigToTargets();
        }

        private static bool ReceiveAnimationParameters(HumanoidControl humanoid, HumanoidPose pose) {
            if (humanoid.targetsRig.runtimeAnimatorController == null) {
                if (pose.animatorParameters.parameters.Length > 0)
                    Debug.LogWarning("Animation Controller is missing on remote Humanoid.\nAnitaion will not be synchronized");
                return false;
            }

            for (int i = 0; i < humanoid.targetsRig.parameterCount; i++) {
                AnimatorControllerParameter parameter = humanoid.targetsRig.parameters[i];
                switch (parameter.type) {
                    case AnimatorControllerParameterType.Bool:
                        humanoid.targetsRig.SetBool(parameter.name, pose.animatorParameters.parameters[i].boolValue);
                        break;
                    case AnimatorControllerParameterType.Float:
                        humanoid.targetsRig.SetFloat(parameter.name, pose.animatorParameters.parameters[i].floatValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        humanoid.targetsRig.SetInteger(parameter.name, pose.animatorParameters.parameters[i].intValue);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        humanoid.targetsRig.SetBool(parameter.name, pose.animatorParameters.parameters[i].boolValue);
                        break;
                }
            }
            return true;
        }

        private static void ReceiveTargetPose(
            HumanoidControl humanoid,
            HumanoidPose humanoidPose,
            HumanoidTargetPose targetPose,
            HumanoidPose lastPose,
            PawnNetworking.Smoothing smoothing) {

            if (targetPose == null) {
                return;
            }

            HumanoidTarget target = GetTarget(humanoid, targetPose.boneId);
            if (target != null)
                target.animator.enabled = false;
            //target.EnableAnimator(false);

            if (targetPose.boneId == Tracking.Bone.LeftHand || targetPose.boneId == Tracking.Bone.RightHand)
                ReceiveHand((HandTarget)target, (HumanoidHandPose)targetPose);
            if (lastPose != null) {
                HumanoidTargetPose lastTargetPose = lastPose.GetTargetPose(targetPose.boneId);
                if (lastTargetPose != null) {
                    ReceiveTarget(target, targetPose, lastTargetPose, humanoidPose.poseTime, humanoidPose.receiveTime, lastPose.poseTime, lastPose.receiveTime, smoothing);
                    return;
                }
            }
            ReceiveTarget(target, targetPose);
        }

        private static HumanoidTarget GetTarget(HumanoidControl humanoid, Tracking.Bone boneId) {
            switch (boneId) {
                case Tracking.Bone.Hips:
                    return humanoid.hipsTarget;
                case Tracking.Bone.Head:
                    return humanoid.headTarget;
                case Tracking.Bone.LeftHand:
                    return humanoid.leftHandTarget;
                case Tracking.Bone.RightHand:
                    return humanoid.rightHandTarget;
                case Tracking.Bone.LeftFoot:
                    return humanoid.leftFootTarget;
                case Tracking.Bone.RightFoot:
                    return humanoid.rightFootTarget;
                default:
                    return null;
            }
        }


        #region Receive Target

        private static void ReceiveTarget(HumanoidTarget target, HumanoidTargetPose targetPose) {
            if (target == null || targetPose == null)
                return;

            HumanoidTarget.TargetTransform targetTransform = target.main.target;
            if (targetPose.positionConfidence >= targetTransform.confidence.position) {
                targetTransform.transform.position = target.humanoid.transform.TransformPoint(targetPose.localPosition.vector3);
                targetTransform.confidence.position = targetPose.positionConfidence;
            }
            if (targetPose.rotationConfidence >= targetTransform.confidence.rotation) {
                targetTransform.transform.rotation = targetPose.rotation.quaternion;
                targetTransform.confidence.rotation = targetPose.rotationConfidence;
            }
        }

        private static void ReceiveTarget(
            HumanoidTarget target,
            HumanoidTargetPose targetPose, HumanoidTargetPose lastTargetPose,
            float poseTime, float receiveTime, float lastPoseTime, float lastReceiveTime,
            PawnNetworking.Smoothing smoothing) {

            if (target == null || targetPose == null)
                return;

            HumanoidTarget.TargetTransform targetTransform = target.main.target;
            if (targetPose.positionConfidence >= targetTransform.confidence.position) {
                // This code results in wrong hand positions:
                //Vector3 oldLocalPosition = target.humanoid.transform.InverseTransformPoint(target.transform.position);
                //targetTransform.transform.position =
                //    CorrectedLocalPosition(oldLocalPosition, target.humanoid.transform, targetPose.localPosition.vector3, lastTargetPose.localPosition.vector3, poseTime, receiveTime, lastPoseTime, lastReceiveTime, smoothing);
                targetTransform.transform.position = target.humanoid.transform.TransformPoint(targetPose.localPosition.vector3);
                targetTransform.confidence.position = targetPose.positionConfidence;
            }
            if (targetPose.rotationConfidence >= targetTransform.confidence.rotation) {
                targetTransform.transform.rotation = targetPose.rotation.quaternion;
                targetTransform.confidence.rotation = targetPose.rotationConfidence;
            }
        }

        ///<summary>Update the transform position with correction for transport jitter</summary>
        private static Vector3 CorrectedPosition(
            Transform targetTransform,
            Vector3 receivedPosition, Vector3 lastReceivedPosition,
            float poseTime, float receiveTime,
            float lastPoseTime, float lastReceiveTime,
            PawnNetworking.Smoothing smoothing) {

            Vector3 newTargetPosition = receivedPosition;

            float deltaPoseTime = poseTime - lastPoseTime;
            float deltaReceiveTime = receiveTime - lastReceiveTime;

            if (deltaPoseTime > 0 && deltaReceiveTime > 0 && lastReceivedPosition != Vector3.zero) {
                Vector3 receivedTranslation = receivedPosition - lastReceivedPosition;
                Vector3 translation = receivedTranslation * (deltaReceiveTime / deltaPoseTime);

                if (smoothing == PawnNetworking.Smoothing.None)
                    newTargetPosition = targetTransform.position + translation;
                else if (smoothing == PawnNetworking.Smoothing.Extrapolation)
                    newTargetPosition = receivedPosition;
                else if (smoothing == PawnNetworking.Smoothing.Interpolation)
                    newTargetPosition = lastReceivedPosition;
            }

            lastReceiveTime = receiveTime;
            lastPoseTime = poseTime;

            return newTargetPosition;
        }

        ///<summary>Update the transform position with correction for transport jitter</summary>
        private static Vector3 CorrectedLocalPosition(
            Vector3 oldLocalPosition, Transform parentTransform,
            Vector3 receivedPosition, Vector3 lastReceivedPosition,
            float poseTime, float receiveTime,
            float lastPoseTime, float lastReceiveTime,
            PawnNetworking.Smoothing smoothing) {

            Vector3 newLocalPosition = receivedPosition;

            float deltaPoseTime = poseTime - lastPoseTime;
            float deltaReceiveTime = receiveTime - lastReceiveTime;

            if (deltaPoseTime > 0 && deltaReceiveTime > 0 && lastReceivedPosition != Vector3.zero) {
                Vector3 receivedTranslation = receivedPosition - lastReceivedPosition;
                Vector3 translation = receivedTranslation * (deltaReceiveTime / deltaPoseTime);
                if (smoothing != PawnNetworking.Smoothing.Interpolation)
                    newLocalPosition = oldLocalPosition + translation;
            }

            lastReceiveTime = receiveTime;
            lastPoseTime = poseTime;

            return parentTransform.TransformPoint(newLocalPosition);
        }

        private static void CalculateVelocity(
            HumanoidControl humanoid,
            Vector3 receivedPosition, Vector3 lastReceivedPosition,
            float poseTime, float receiveTime, float lastPoseTime) {

            float deltaTime = poseTime - lastPoseTime;
            if (deltaTime > 0) {
                Vector3 translation = receivedPosition - lastReceivedPosition;
                humanoid.velocity = translation / deltaTime;
            }
        }

        #endregion

        private static void ReceiveHand(HandTarget handTarget, HumanoidHandPose handPose) {
            if (handPose == null)
                return;

            FingersTarget fingersTarget = handTarget.fingers;

            fingersTarget.thumb.curl = handPose.thumbCurl;
            fingersTarget.index.curl = handPose.indexCurl;
            fingersTarget.middle.curl = handPose.middleCurl;
            fingersTarget.ring.curl = handPose.ringCurl;
            fingersTarget.little.curl = handPose.littleCurl;

            if (handPose.syncSwing) {
                fingersTarget.thumb.swing = handPose.thumbSwing;
                fingersTarget.index.swing = handPose.indexSwing;
                fingersTarget.middle.swing = handPose.middleSwing;
                fingersTarget.ring.swing = handPose.ringSwing;
                fingersTarget.little.swing = handPose.littleSwing;
            }
        }

#if hFACE
        private static void ReceiveFace(FaceTarget faceTarget, HumanoidFacePose facePose) {
            if (facePose == null)
                return;

            faceTarget.leftBrow.outerRaise = ((float)facePose.leftBrowOuterRaise) / 127;
            faceTarget.leftBrow.innerRaise = ((float)facePose.leftBrowInnerRaise) / 127;

            faceTarget.rightBrow.outerRaise = ((float)facePose.rightBrowOuterRaise) / 127;
            faceTarget.rightBrow.innerRaise = ((float)facePose.rightBrowInnerRaise) / 127;

            faceTarget.leftEye.closed = ((float)facePose.leftEyeClosed) / 127;
            faceTarget.rightEye.closed = ((float)facePose.rightEyeClosed) / 127;

            faceTarget.mouth.leftRaise = ((float)facePose.mouthLeftRaise) / 127;
            faceTarget.mouth.rightRaise = ((float)facePose.mouthRightRaise) / 127;
            faceTarget.mouth.leftStretch = ((float)facePose.mouthLeftStretch) / 127;
            faceTarget.mouth.rightStretch = ((float)facePose.mouthRightStretch) / 127;
            faceTarget.mouth.shiftRight = ((float)facePose.mouthShiftRight) / 127;

            faceTarget.jaw.open = ((float)facePose.jawOpen) / 127;
            faceTarget.jaw.shiftRight = ((float)facePose.jawShiftRight) / 127;

            faceTarget.UpdateMorphTargets();
        }
#endif

        public static bool IsTargetActive(byte targetMask, HumanoidControl.TargetId targetIndex) {
            int bitset = targetMask & (byte)(1 << ((int)targetIndex + 1));
            return (bitset != 0);
        }

        #endregion

        #region Grab

        public class Grab : IMessage {
            public ulong nwId;
            public byte humanoidId;
            public bool isLeft;
            public ulong nwId_grabbedObject;
            public bool rangeCheck;
            public HandTarget.GrabType grabType;

            public Grab() { }
            public Grab(HandTarget handTarget, ulong nwId_grabbedObject, bool rangeCheck, HandTarget.GrabType grabType) {
                nwId = handTarget.humanoid.nwId;
                humanoidId = (byte)handTarget.humanoid.humanoidId;
                isLeft = handTarget.isLeft;
                this.nwId_grabbedObject = nwId_grabbedObject;
                this.rangeCheck = rangeCheck;
                this.grabType = grabType;
            }
            public Grab(byte[] data) : base(data) { }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);
                bw.Write(isLeft);
                bw.Write(nwId_grabbedObject);
                bw.Write(rangeCheck);
                bw.Write((int)grabType);

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();
                isLeft = br.ReadBoolean();
                nwId_grabbedObject = br.ReadUInt64();
                rangeCheck = br.ReadBoolean();
                grabType = (HandTarget.GrabType)br.ReadInt32();
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif        
        }

        public static void ReceiveGrab(this IHumanoidNetworking networking, byte[] serializedData) {
            Grab data = new Grab(serializedData);
            Receive(networking, data);
        }

        public static void Receive(this IHumanoidNetworking networking, Grab msg) {
            GameObject obj = networking.GetGameObject(msg.nwId_grabbedObject);

            if (networking.debug <= PawnNetworking.DebugLevel.Info)
                networking.DebugLog("GrabEvent " + obj);

            if (networking.isLocal && !networking.createLocalRemotes)
                return;

            HumanoidControl humanoid = HumanoidNetworking.FindRemoteHumanoid(networking.humanoids, msg.humanoidId);
            if (humanoid == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Warning)
                    networking.DebugWarning("Could not find humanoid: " + msg.humanoidId);
                return;
            }

            HandTarget handTarget = msg.isLeft ? humanoid.leftHandTarget : humanoid.rightHandTarget;
            handTarget.Grab(obj, msg.rangeCheck);
        }

        #endregion

        #region LetGo

        public class LetGo : IMessage {
            public ulong nwId;
            public byte humanoidId;
            public bool isLeft;

            public LetGo() { }
            public LetGo(HandTarget handTarget) {
                nwId = handTarget.humanoid.nwId;
                humanoidId = (byte)handTarget.humanoid.humanoidId;
                isLeft = handTarget.isLeft;
            }
            public LetGo(byte[] data) : base(data) { }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);
                bw.Write(isLeft);

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();
                isLeft = br.ReadBoolean();
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif        
        }

        public static void ReceiveLetGo(this IHumanoidNetworking networking, byte[] serializedData) {
            LetGo data = new LetGo(serializedData);

            Receive(networking, data);
        }

        public static void Receive(this IHumanoidNetworking networking, LetGo msg) {
            if (networking.isLocal && !networking.createLocalRemotes)
                return;

            HumanoidControl humanoid = FindRemoteHumanoid(networking.humanoids, msg.humanoidId);
            if (humanoid == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Warning)
                    Debug.LogWarning("Could not find humanoid: " + msg.humanoidId);
                return;
            }

            HandTarget handTarget = msg.isLeft ? humanoid.leftHandTarget : humanoid.rightHandTarget;
            handTarget.LetGo();
        }

        #endregion

        #region Change Avatar

        public class ChangeAvatar : IMessage {
            public ulong nwId;
            public byte humanoidId;
            public string avatarPrefabName;

            public ChangeAvatar() { }
            public ChangeAvatar(HumanoidControl humanoid, string avatarPrefabName) {
                nwId = humanoid.nwId;
                humanoidId = (byte)humanoid.humanoidId;
                this.avatarPrefabName = avatarPrefabName;
            }
            public ChangeAvatar(byte[] data) : base(data) { }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);
                bw.Write(avatarPrefabName);

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();
                avatarPrefabName = br.ReadString();
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif        
        }

        public static void ReceiveChangeAvatar(this IHumanoidNetworking networking, byte[] serializedData) {
            ChangeAvatar data = new ChangeAvatar(serializedData);

            Receive(networking, data);
        }

        public static void Receive(this IHumanoidNetworking receivingNetworking, ChangeAvatar msg) {
            receivingNetworking.DebugLog("Receive Change Avatar");
            IHumanoidNetworking networking = GetHumanoidNetworking(receivingNetworking, msg.nwId);
            if (networking == null) {
                if (receivingNetworking.debug <= PawnNetworking.DebugLevel.Error)
                    receivingNetworking.DebugLog("Could not find Networking for Humanoid Pose " + msg.nwId + "/" + msg.humanoidId);
                return;
            }

            if (networking.isLocal && !networking.createLocalRemotes)
                return;

            HumanoidControl remoteHumanoid = FindRemoteHumanoid(networking.humanoids, msg.humanoidId);
            if (remoteHumanoid == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Warning)
                    Debug.LogWarning("Could not find humanoid: " + msg.humanoidId);
                return;
            }

            GameObject remoteAvatar = (GameObject)Resources.Load(msg.avatarPrefabName);
            if (remoteAvatar == null) {
                if (networking.debug <= PawnNetworking.DebugLevel.Error)
                    Debug.LogError("Could not load remote avatar " + msg.avatarPrefabName + ". Is it located in a Resources folder?");
                return;
            }

            if (networking.debug <= PawnNetworking.DebugLevel.Info)
                networking.DebugLog("Receive Change Avatar " + msg.nwId + "/" + msg.humanoidId + " " + msg.avatarPrefabName);

            remoteHumanoid.LocalChangeAvatar(remoteAvatar);
        }


        #endregion

        #region Sync Tracking Space

        public class SyncTrackingSpace : IMessage {
            public ulong nwId;
            public byte humanoidId;
            public Vector3S position;
            public Vector3S rotation;

            public SyncTrackingSpace() { }
            public SyncTrackingSpace(HumanoidControl humanoid, Vector3 position, Quaternion rotation) {
                nwId = humanoid.nwId;
                humanoidId = (byte)humanoid.humanoidId;

                this.position = new Vector3S(position);
                this.rotation = new Vector3S(rotation);
            }
            public SyncTrackingSpace(byte[] data) : base(data) { }

            public override byte[] Serialize() {
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);

                bw.Write(nwId);
                bw.Write(humanoidId);

                position.Write(bw);
                rotation.Write(bw);

                byte[] data = ms.ToArray();
                return data;
            }

            public override void Deserialize(byte[] data) {
                MemoryStream ms = new MemoryStream(data);
                BinaryReader br = new BinaryReader(ms);

                nwId = br.ReadUInt64();
                humanoidId = br.ReadByte();

                position = Vector3S.Read(br);
                rotation = Vector3S.Read(br);
            }

            // This is the same code as in the base class IMessage
            // It is needed because the base class implementation only
            // results in compiler errors.
#if hNW_MIRROR
            //public override void Serialize(Mirror.NetworkWriter writer) {
            //    byte[] data = Serialize();
            //    Mirror.NetworkWriterExtensions.WriteBytesAndSize(writer, data);
            //}

            //public override void Deserialize(Mirror.NetworkReader reader) {
            //    byte[] data = Mirror.NetworkReaderExtensions.ReadBytesAndSize(reader);
            //    Deserialize(data);
            //}
#endif        
        }

        public static Transform GetTrackingTransform(HumanoidControl humanoid) {
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

        public static void ReceiveSyncTrackingSpace(this IHumanoidNetworking networking, byte[] serializedData) {
            SyncTrackingSpace data = new SyncTrackingSpace(serializedData);

            Receive(networking, data);
        }

        public static void Receive(this IHumanoidNetworking networking, SyncTrackingSpace msg) {
            foreach (HumanoidControl humanoid in HumanoidControl.allHumanoids) {
                if (humanoid.isRemote || humanoid.nwId == msg.nwId)
                    continue;

                // The lowest (= earliest) nwId is the boss
                if (msg.nwId > humanoid.nwId)
                    return;

                Debug.Log("receive tracking sync");

#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                // The lowest (= earliest) nwId is the boss
                // NOT ATM FOR TESTING
                //if (msg.nwId > humanoid.nwId)
                if (humanoid.openVR != null) {
                    humanoid.openVR.SyncTracking(msg.position.vector3, msg.rotation.quaternion);
                }
#endif
#if hANTILATENCY
                //if (msg.nwId > humanoid.nwId) {
                if (humanoid.antilatency != null) {
                    humanoid.antilatency.SyncTracking(msg.position.vector3, msg.rotation.quaternion);
                }
                //}
#endif
            }
        }

        #endregion

        public static IHumanoidNetworking GetHumanoidNetworking(IHumanoidNetworking networking, ulong nwId) {
            if (networking.nwId == nwId)
                return networking;

            GameObject networkingObj = networking.GetGameObject(nwId);
            if (networkingObj == null)
                return null;

            return networkingObj.GetComponent<IHumanoidNetworking>();
        }

        public static List<HumanoidControl> FindLocalHumanoids() {
            List<HumanoidControl> humanoidList = new List<HumanoidControl>();
            HumanoidControl[] foundHumanoids = UnityEngine.Object.FindObjectsOfType<HumanoidControl>();
            for (int i = 0; i < foundHumanoids.Length; i++) {
                if (!foundHumanoids[i].isRemote) {
                    humanoidList.Add(foundHumanoids[i]);
                }
            }
            return humanoidList;
        }

        public static IHumanoidNetworking GetLocalHumanoidNetworking() {
#if hNW_UNET || hNW_PHOTON || hNW_BOLT || hNW_MIRROR
            HumanoidPlayer[] humanoidNetworkings = UnityEngine.Object.FindObjectsOfType<HumanoidPlayer>();
            foreach (IHumanoidNetworking humanoidNetworking in humanoidNetworkings) {
                if (humanoidNetworking.isLocal)
                    return humanoidNetworking;
            }
            //#elif hNW_PHOTON
            //            IHumanoidNetworking[] humanoidNetworkings = UnityEngine.Object.FindObjectsOfType<HumanoidPun>();
            //            foreach (IHumanoidNetworking humanoidNetworking in humanoidNetworkings) {
            //                if (humanoidNetworking.isLocal)
            //                    return humanoidNetworking;
            //            }
            //#elif hNW_BOLT
            //            IHumanoidNetworking[] humanoidNetworkings = UnityEngine.Object.FindObjectsOfType<HumanoidBolt>();
            //            foreach (IHumanoidNetworking humanoidNetworking in humanoidNetworkings) {
            //                if (humanoidNetworking.isLocal)
            //                    return humanoidNetworking;
            //            }
            //#elif hNW_MIRROR
            //            IHumanoidNetworking[] humanoidNetworkings = UnityEngine.Object.FindObjectsOfType<HumanoidMirror>();
            //            foreach (IHumanoidNetworking humanoidNetworking in humanoidNetworkings) {
            //                if (humanoidNetworking.isLocal)
            //                    return humanoidNetworking;
            //            }
#endif
            return null;
        }

        public static void DisableNetworkSync(GameObject obj) {
#if hNW_UNET || hNW_PHOTON || hNW_BOLT || hNW_MIRROR
            HumanoidPlayer.DisableNetworkSync(obj);
#endif
        }

        public static void ReenableNetworkSync(GameObject obj) {
#if hNW_UNET || hNW_PHOTON || hNW_BOLT || hNW_MIRROR
            HumanoidPlayer.ReenableNetworkSync(obj);
#endif
        }

        public static void TakeOwnership(GameObject obj) {
#if hNW_PHOTON
            HumanoidPlayer.TakeOwnership(obj);
#endif
        }

        public static HumanoidControl FindHumanoid(List<HumanoidControl> humanoids, int humanoidId) {
            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid == null)
                    continue;
                if (humanoid.humanoidId == humanoidId)
                    return humanoid;
            }
            return null;
        }

        public static HumanoidControl FindLocalHumanoid(List<HumanoidControl> humanoids, int humanoidId) {
            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid == null)
                    continue;
                if (!humanoid.isRemote && humanoid.humanoidId == humanoidId)
                    return humanoid;
            }
            return null;
        }

        public static HumanoidControl FindRemoteHumanoid(List<HumanoidControl> humanoids, int humanoidId) {
            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid == null)
                    continue;
                if (humanoid.isRemote && humanoid.humanoidId == humanoidId)
                    return humanoid;
            }
            return null;
        }
        #region Start

        //public static void Start(PawnNetworking.DebugLevel debug, bool syncFingerSwing) {
        //    HumanoidNetworking.debug = debug;
        //    HumanoidNetworking.syncFingerSwing = syncFingerSwing;
        //}

        #endregion

        #region Start Humanoid
        //public static HumanoidControl StartHumanoid(
        //    ulong nwId,
        //    int humanoidId,
        //    string name,
        //    string avatarPrefabName,
        //    Vector3 position, Quaternion rotation,
        //    bool physics) {

        //    if (debug <= PawnNetworking.DebugLevel.Info)
        //        UnityEngine.Debug.Log(nwId + ": Receive StartHumanoid " + humanoidId);

        //    HumanoidControl remoteHumanoid = InstantiateRemoteHumanoid(remoteHumanoidPrefab, name, position, rotation);
        //    remoteHumanoid.nwId = nwId;
        //    remoteHumanoid.humanoidId = humanoidId;

        //    if (debug <= PawnNetworking.DebugLevel.Info)
        //        UnityEngine.Debug.Log(remoteHumanoid.nwId + ": Remote Humanoid " + remoteHumanoid.humanoidId + " Added");

        //    GameObject remoteAvatar = (GameObject)Resources.Load(avatarPrefabName);
        //    if (remoteAvatar == null) {
        //        if (debug <= PawnNetworking.DebugLevel.Error)
        //            UnityEngine.Debug.LogError("Could not load remote avatar " + avatarPrefabName + ". Is it located in a Resources folder?");
        //        return remoteHumanoid;
        //    }
        //    remoteHumanoid.physics = physics;
        //    remoteHumanoid.LocalChangeAvatar(remoteAvatar);

        //    return remoteHumanoid;
        //}




        #endregion

        #region Pose

        #region Smoothing

        public static void SmoothUpdate(List<HumanoidControl> humanoids) {
            foreach (HumanoidControl humanoid in humanoids) {
                if (humanoid.isRemote)
                    SmoothUpdate(humanoid);
            }
        }

        public static void SmoothUpdate(HumanoidControl humanoid) {
            //Debug.Log("smoothing");
            humanoid.transform.position += humanoid.velocity * Time.deltaTime;
        }

        #endregion

        #endregion
    }
}
