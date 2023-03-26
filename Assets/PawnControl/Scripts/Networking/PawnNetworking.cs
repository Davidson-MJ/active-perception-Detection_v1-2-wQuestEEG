using System.Collections.Generic;
using UnityEngine;

namespace Passer.Pawn {

    /// <summary>
    /// Interface for Pawn Networking functions
    /// </summary>

    public interface IPawnNetworking {
        void Send(bool b);
        void Send(byte b);
        void Send(int x);
        void Send(float f);
        void Send(Vector3 v);
        void Send(Quaternion q);

        bool ReceiveBool();
        byte ReceiveByte();
        int ReceiveInt();
        float ReceiveFloat();
        Vector3 ReceiveVector3();
        Quaternion ReceiveQuaternion();

        float sendRate { get; }
        PawnNetworking.DebugLevel debug { get; }
        PawnNetworking.Smoothing smoothing { get; }
        bool createLocalRemotes { get; set; }


        bool isLocal { get; }

        void InstantiatePawn(PawnControl pawn);
        void DestroyPawn(PawnControl pawn);

        void Grab(PawnHand controllerTarget, GameObject obj, bool rangeCheck);
        void LetGo(PawnHand controllerTarget);
    }

    public static class PawnNetworking {

        public enum Smoothing {
            None,
            Interpolation,
            Extrapolation
        };

        public enum DebugLevel {
            Debug,
            Info,
            Warning,
            Error,
            None,
        }
        public static DebugLevel debug = DebugLevel.Error;

        private static GameObject remotePawnPrefab;

        public static List<PawnControl> FindLocalPawns() {
            List<PawnControl> pawns = new List<PawnControl>();
            PawnControl[] foundPawns = Object.FindObjectsOfType<PawnControl>();
            for (int i = 0; i < foundPawns.Length; i++) {
                if (!foundPawns[i].isRemote) {
                    pawns.Add(foundPawns[i]);
                }
            }
            return pawns;
        }

        public static PawnControl FindRemotePawn(List<PawnControl> pawns, int pawnId) {
            foreach (PawnControl pawn in pawns) {
                if (pawn.isRemote && pawn.id == pawnId)
                    return pawn;
            }
            return null;
        }

        public static PawnControl StartPawn(ulong nwId, int pawnId, string name, Vector3 position, Quaternion rotation) {
            if (debug <= DebugLevel.Info)
                Debug.Log(nwId + ": Receive StartPawn " + pawnId);

            PawnControl remotePawn = InstantiateRemotePawn(remotePawnPrefab, name, position, rotation);
            remotePawn.nwId = nwId;
            remotePawn.id = pawnId;

            if (debug <= DebugLevel.Info)
                Debug.Log(remotePawn.nwId + ": Remote Pawn " + remotePawn.id + " Added");

            return remotePawn;
        }

        private static PawnControl InstantiateRemotePawn(GameObject remotePawnPrefab, string name, Vector3 position, Quaternion rotation) {
            GameObject remotePawnObj = Object.Instantiate(remotePawnPrefab, position, rotation);
            remotePawnObj.name = name + " (Remote)";

            PawnControl remotePawn = remotePawnObj.GetComponent<PawnControl>();
            remotePawn.isRemote = true;

            return remotePawn;
        }

        #region Start
        public static void Start(DebugLevel debug) {
            PawnNetworking.debug = debug;

            remotePawnPrefab = (GameObject)Resources.Load("RemotePawn");
        }
        #endregion

        #region Send

        public static void SendPawn(this IPawnNetworking networking, PawnControl pawn) {
            networking.Send(pawn.nwId);
            networking.Send(pawn.id);
            networking.Send(Time.time); // Pose Time

            byte targetMask = DetermineActiveTargets(pawn);
            networking.Send(targetMask);

            if (debug <= DebugLevel.Info)
                Debug.Log(pawn.nwId + ": Send Pawn " + pawn.id + ", targetMask = " + targetMask);

            // Pawn Transform is always sent
            networking.SendTarget(pawn.transform);

            SendTargets(networking, pawn, targetMask);
        }

        private static void SendTargets(IPawnNetworking networking, PawnControl pawn, byte targetMask) {
            if (IsTargetActive(targetMask, PawnControl.TargetId.HeadTarget))
                networking.SendTarget(pawn.headTarget.transform);
            if (IsTargetActive(targetMask, PawnControl.TargetId.LeftHandTarget))
                networking.SendTarget(pawn.leftHandTarget.transform);
            if (IsTargetActive(targetMask, PawnControl.TargetId.RightHandTarget))
                networking.SendTarget(pawn.rightHandTarget.transform);
        }

        public static void SendTarget(this IPawnNetworking networking, Transform transform) {
            networking.Send(transform.position);
            networking.Send(transform.rotation);
        }

        public static byte DetermineActiveTargets(PawnControl pawn) {
            byte targetMask = 0;

            Target[] targets = {
                pawn.headTarget,
                pawn.leftHandTarget,
                pawn.rightHandTarget,
            };

            for (int i = 0; i < targets.Length; i++) {
                if (targets[i] != null || i == 1) {
                    // for now, we always send the head to match the avatar's position well
                    targetMask |= (byte)(1 << (i + 1));
                }
            }
            return targetMask;
        }

        public static byte DetermineActiveTargets(PawnControl pawn, out int activeTargetCount) {
            byte targetMask = 0;

            Target[] targets = {
                pawn.headTarget,
                pawn.leftHandTarget,
                pawn.rightHandTarget,
            };

            activeTargetCount = 0;
            for (int i = 0; i < targets.Length; i++) {
                if (targets[i] != null || i == 1) {
                    // for now, we always send the head to match the avatar's position well
                    targetMask |= (byte)(1 << (i + 1));
                    activeTargetCount++;
                }
            }
            return targetMask;
        }

        #endregion

        #region Receive
        public static float ReceivePawn(this IPawnNetworking networking, PawnControl remotePawn, float lastTime) {

            float poseTime = networking.ReceiveFloat();
            //float deltaTime = poseTime - lastTime;

            byte targetMask = networking.ReceiveByte();
            if (debug <= DebugLevel.Info)
                Debug.Log(remotePawn.nwId + ": Receive Pawn " + remotePawn.id + ", targetMask = " + targetMask);

            // Pawn Transform is always received
            ReceiveTransform(networking, remotePawn.transform);

            ReceiveTargets(networking, remotePawn, targetMask);

            return poseTime;
        }

        private static void ReceiveTargets(IPawnNetworking networking, PawnControl pawn, byte targetMask) {
            ReceiveTarget(networking, targetMask, PawnControl.TargetId.HeadTarget, pawn.headTarget);
            ReceiveTarget(networking, targetMask, PawnControl.TargetId.LeftHandTarget, pawn.leftHandTarget);
            ReceiveTarget(networking, targetMask, PawnControl.TargetId.RightHandTarget, pawn.rightHandTarget);
        }

        private static void ReceiveTarget(this IPawnNetworking networking, byte targetMask, PawnControl.TargetId targetId, Target target) {
            if (IsTargetActive(targetMask, targetId)) {
                ReceiveTransform(networking, target.transform);
            }
        }

        private static void ReceiveTransform(IPawnNetworking networking, Transform transform) {
            transform.position = networking.ReceiveVector3();
            transform.rotation = networking.ReceiveQuaternion();
        }
        #endregion

        public static bool IsTargetActive(byte targetMask, PawnControl.TargetId targetIndex) {
            int bitset = targetMask & (byte)(1 << ((int)targetIndex + 1));
            return (bitset != 0);
        }

    }
}