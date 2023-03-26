#if hOPENVR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Passer.Tracking;

namespace Passer.Humanoid {
    using Tracking;

    [System.Serializable]
    public class OpenVRHumanoidTracker : HumanoidTracker {
#if hVIVEHAND
        public bool handTracking = false;
#endif
        public OpenVRHumanoidTracker() {
            deviceView = new DeviceView();
        }

        /// <summary>The name of this tracker</summary>
        public override string name {
            get { return OpenVRDevice.name; }
        }

        #region Tracking

#if UNITY_2020_1_OR_NEWER
        public TrackerComponent tracker;
#else
        public Passer.Tracking.OpenVR tracker;
#endif

        public override HeadSensor headSensor {
            get { return humanoid.headTarget.openVR; }
        }
        public override ArmSensor leftHandSensor {
            get { return humanoid.leftHandTarget.openVR; }
        }
        public override ArmSensor rightHandSensor {
            get { return humanoid.rightHandTarget.openVR; }
        }

        #endregion Tracking

        #region Manage

        public override void CheckTracker(HumanoidControl _humanoid) {
            base.CheckTracker(_humanoid);
            if (humanoid == null)
                return;

            if (enabled) {
                if (tracker == null) {
                    Transform realWorld = humanoid.realWorld;

                    Vector3 position = realWorld.position;
                    Quaternion rotation = realWorld.rotation;
                    tracker = Passer.Tracking.OpenVR.Get(realWorld, position, rotation);
                }
                if (tracker == null)
                    return;

                trackerTransform = tracker.transform;
#if hVIVEHAND
                ViveHandSkeleton.CheckGestureProvider(this);
#endif
            }
            else {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    if (tracker != null)
                        UnityEngine.Object.DestroyImmediate(tracker.gameObject, true);
                }
#endif
                tracker = null;
                trackerTransform = null;
            }
        }

        public override bool AddTracker(HumanoidControl humanoid, string resourceName) {
            return false;
        }

        public override void ShowSkeleton(bool shown) {
            if (tracker != null)
                tracker.ShowSkeleton(shown);
        }

        #endregion Manage

        [System.NonSerialized]
        private UnitySensor[] _sensors;
        public override UnitySensor[] sensors {
            get {
                if (_sensors == null)
                    _sensors = new UnitySensor[] {
                        headSensor,
                        leftHandSensor,
                        rightHandSensor
                    };

                return _sensors;
            }
        }

        public OpenVRHmd hmd = null;
        public List<OpenVRController> controllers = new List<OpenVRController>();

#if hVIVETRACKER
        public HeadSensor headSensorVive {
            get { return humanoid.headTarget.viveTracker; }
        }
        public ArmSensor leftHandSensorVive {
            get { return humanoid.leftHandTarget.viveTracker; }
        }
        public ArmSensor rightHandSensorVive {
            get { return humanoid.rightHandTarget.viveTracker; }
        }
        public TorsoSensor hipsSensorVive {
            get { return humanoid.hipsTarget.viveTracker; }
        }
        public LegSensor leftFootSensorVive {
            get { return humanoid.leftFootTarget.viveTracker; }
        }
        public LegSensor rightFootSensorVive {
            get { return humanoid.rightFootTarget.viveTracker; }
        }

        public List<ViveTrackerComponent> viveTrackers = new List<ViveTrackerComponent>();
#endif

        ulong pHandle;

        [System.NonSerialized]
        protected Passer.VRActiveActionSet_t actionSet = new Passer.VRActiveActionSet_t();
        [System.NonSerialized]
        protected static uint activeActionSetSize;
        [System.NonSerialized]
        protected Passer.VRActiveActionSet_t[] activeActionSets;

        #region Start

        public override void StartTracker(HumanoidControl _humanoid) {
            humanoid = _humanoid;

#if hLEGACYXR
            if (humanoid.headTarget.unity != null && humanoid.headTarget.unity.enabled && UnityVRDevice.xrDevice == UnityVRDevice.XRDeviceType.OpenVR)
                enabled = true;
#endif
#if UNITY_2020_1_OR_NEWER && pUNITYXR
            if (tracker == null)
                tracker = humanoid.unityXR.tracker;

            Passer.Tracking.UnityVRDevice.Start();
            if (tracker == null || !enabled || UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.OpenVR)
                return;
#else
            if (tracker == null || !enabled || UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.OpenVR)
                return;
#endif

            TraditionalDevice.gameControllerEnabled = false;
            // Game controllers interfere with OpenVR Controller Input ... :-(

            OpenVRDevice.Start();

            AddTracker(humanoid, "OpenVR");

#if !UNITY_2020_1_OR_NEWER
            OpenVRController leftController = tracker.FindController(true);
            if (leftController != null)
                controllers.Add(leftController);
            OpenVRController rightController = tracker.FindController(false);
            if (rightController != null)
                controllers.Add(rightController);
#endif

            string path = Application.streamingAssetsPath + "/SteamVR/actions.json";

            if (Passer.OpenVR.Input != null) {
                Passer.EVRInputError err = Passer.OpenVR.Input.SetActionManifestPath(path);
                if (err != Passer.EVRInputError.None)
                    Debug.LogError("OpenVR.Input.SetActionManifestPath error (" + path + "): " + err.ToString());
                //else
                {
                    err = Passer.OpenVR.Input.GetActionSetHandle("/actions/default", ref pHandle);
                    if (err != Passer.EVRInputError.None)
                        Debug.LogError("OpenVR.Input.GetActionSetHandle error (): " + err.ToString());
                    else
                        actionSet.ulActionSet = pHandle;
                }
                activeActionSets = new Passer.VRActiveActionSet_t[] { actionSet };
                activeActionSetSize = (uint)(Marshal.SizeOf(typeof(Passer.VRActiveActionSet_t)));
            }

            OpenVRDevice.onNewSensor = OnNewSensor;
#if hVIVETRACKER
            Debug.Log("Detecting Vive Tracker positions.\nMake sure the Vive HMD is looking in the same direction as the user!");
            ViveTrackerComponent[] viveTrackerComponents = tracker.GetComponentsInChildren<ViveTrackerComponent>();
            foreach (ViveTrackerComponent viveTrackerComponent in viveTrackerComponents)
                viveTrackers.Add(viveTrackerComponent);
#endif
#if hVIVEHAND
            this.trackerTransform = tracker.transform;
            ViveHandSkeleton.CheckGestureProvider(this);
#endif
        }

        public void StartTracker() {
            if (!enabled || UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.OpenVR)
                return;

            TraditionalDevice.gameControllerEnabled = false;
            // Game controllers interfere with OpenVR Controller Input ... :-(

            OpenVRDevice.Start();
        }

        protected virtual void OnNewSensor(uint sensorId) {
            Passer.ETrackedDeviceClass deviceClass = Passer.OpenVR.System.GetTrackedDeviceClass(sensorId);
            switch (deviceClass) {
                case Passer.ETrackedDeviceClass.HMD:
#if !UNITY_2020_1_OR_NEWER
                    hmd = FindHmd(sensorId);
                    if (hmd == null)
                        hmd = OpenVRHmd.NewHmd(humanoid, (int)sensorId);
                    break;
                case Passer.ETrackedDeviceClass.TrackingReference:
                    SubTracker subTracker = FindLighthouse(sensorId);
                    if (subTracker == null)
                        subTrackers.Add(NewLighthouse(humanoid, sensorId));
                    break;
                case Passer.ETrackedDeviceClass.Controller:
                    if (!OpenVRController.IsValidController(sensorId))
                        break;

                    OpenVRController controller = FindController(sensorId);
                    if (controller == null)
                        controllers.Add(OpenVRController.NewController(humanoid, (int)sensorId));
#endif
                    break;
#if hVIVETRACKER
                case Passer.ETrackedDeviceClass.GenericTracker:
                    ViveTrackerComponent viveTracker = FindViveTracker(sensorId);
                    if (viveTracker == null)
                        viveTrackers.Add(ViveTracker.NewViveTracker(tracker, sensorId));
                    else {
                        viveTracker.trackerId = (int)sensorId;
                        viveTracker.StartComponent(humanoid.openVR.trackerTransform);
                    }
                    break;
#endif
                default:
                    break;
            }
        }

        protected SubTracker NewLighthouse(HumanoidControl humanoid, uint sensorId) {
            SubTracker subTracker = OpenVRBaseStation.Create(this);
            subTracker.subTrackerId = (int)sensorId;
            return subTracker;
        }

        protected OpenVRHmd FindHmd(uint sensorId) {
            if (hmd != null && hmd.trackerId == sensorId)
                return hmd;

            OpenVRHead openVRHead = humanoid.headTarget.openVR;
            if (openVRHead.hmd != null) {
                openVRHead.hmd.trackerId = (int)sensorId;
                hmd = openVRHead.hmd;
                return hmd;
            }

            // See if a HMD already exists in the Real World
            OpenVRHmd openVRHmd = tracker.GetComponentInChildren<OpenVRHmd>();
            if (openVRHmd != null) {
                openVRHmd.trackerId = (int)sensorId;
                hmd = openVRHmd;
                openVRHead.hmd = openVRHmd;
                return hmd;
            }

            return null;
        }

        protected OpenVRController FindController(uint sensorId) {
            foreach (OpenVRController controller in controllers) {
                if (controller != null && controller.trackerId == sensorId)
                    return controller;
            }
            if (OpenVRController.IsLeftController(sensorId)) {
                OpenVRHand leftHand = humanoid.leftHandTarget.openVR;
                if (leftHand != null && leftHand.openVRController != null) {
                    leftHand.openVRController.trackerId = (int)sensorId;
                    // leftHand Controller was not in controller list yet
                    controllers.Add(leftHand.openVRController);
                    return leftHand.openVRController;
                }
            }
            else
            if (OpenVRController.IsRightController(sensorId)) {
                OpenVRHand rightHand = humanoid.rightHandTarget.openVR;
                if (rightHand != null && rightHand.openVRController != null) {
                    rightHand.openVRController.trackerId = (int)sensorId;
                    // rightHand Controller was not in controller list yet
                    controllers.Add(rightHand.openVRController);
                    return rightHand.openVRController;
                }
            }
            else {
                return null;
            }

            // See if the controller already exists in the Real World
            OpenVRController[] openVRControllers = tracker.GetComponentsInChildren<OpenVRController>();
            foreach (OpenVRController controller in openVRControllers) {
                if (controller.trackerId == sensorId) {
                    // controller was not in controller list yet
                    controllers.Add(controller);
                    return controller;
                }
            }

            return null;
        }

#if hVIVETRACKER
        protected ViveTrackerComponent FindViveTracker(uint sensorId) {
            string hardwareId = OpenVRDevice.GetHardwareId(sensorId);
            foreach (ViveTrackerComponent viveTracker in viveTrackers) {
                if (viveTracker != null && (viveTracker.trackerId == sensorId || viveTracker.hardwareId == hardwareId))
                    return viveTracker;
            }
            return null;
        }
#endif

        #endregion

        public override void ShowTracker(bool shown) {
            if (!enabled)
                return;
#if hVIVETRACKER
            if (humanoid == null)
                return;

            ViveTracker.ShowTracker(humanoid, shown);
#endif
        }


        #region Update

        public override void UpdateTracker() {
            if (tracker == null || !enabled || UnityVRDevice.xrDevice != UnityVRDevice.XRDeviceType.OpenVR) {
#if pUNITYXR
                if (humanoid.unity != null && humanoid.unity.enabled)
                    enabled = true;

                if (tracker != null)
                    status = tracker.status;
                else
#endif
                status = Tracker.Status.Unavailable;
                return;
            }

            status = OpenVRDevice.status;

            OpenVRDevice.Update();
            if (Passer.OpenVR.Input != null)
                Passer.OpenVR.Input.UpdateActionState(activeActionSets, activeActionSetSize);

            deviceView.position = HumanoidTarget.ToVector(tracker.transform.position);
            deviceView.orientation = HumanoidTarget.ToRotation(tracker.transform.rotation);

            bool showRealObjects = humanoid == null ? true : humanoid.showRealObjects;

            if (hmd != null)
                hmd.UpdateComponent();
            foreach (SubTracker subTracker in subTrackers)
                subTracker.UpdateTracker(showRealObjects);
            foreach (OpenVRController controller in controllers)
                controller.UpdateComponent();
#if hVIVETRACKER
            foreach (ViveTrackerComponent viveTracker in viveTrackers)
                viveTracker.UpdateComponent();
#endif
        }

        protected SubTracker FindLighthouse(uint sensorId) {
            foreach (SubTracker subTracker in subTrackers) {
                if (subTracker != null && subTracker.subTrackerId == sensorId)
                    return subTracker;
            }
            return null;
        }

        private bool IsTracking() {
            if (!humanoid.leftHandTarget.openVR.enabled || humanoid.leftHandTarget.openVR.status == Status.Tracking ||
#if hVIVETRACKER
                humanoid.headTarget.viveTracker.status == Status.Tracking ||
                humanoid.leftHandTarget.viveTracker.status == Status.Tracking ||
                humanoid.rightHandTarget.viveTracker.status == Status.Tracking ||
                humanoid.leftHandTarget.viveTracker.status == Status.Tracking ||
                humanoid.hipsTarget.viveTracker.status == Status.Tracking ||
                humanoid.leftFootTarget.viveTracker.status == Status.Tracking ||
                humanoid.rightFootTarget.viveTracker.status == Status.Tracking ||
#endif
                !humanoid.rightHandTarget.openVR.enabled || humanoid.rightHandTarget.openVR.status == Status.Tracking)
                return true;
            else
                return false;
        }

        #endregion

        public static GameObject CreateControllerObject() {
            GameObject trackerPrefab = Resources.Load("Vive Controller") as GameObject;
            if (trackerPrefab == null)
                return null;

            GameObject trackerObject = UnityEngine.Object.Instantiate(trackerPrefab);
            trackerObject.name = "Vive Controller";

            return trackerObject;
        }

        public override void Calibrate() {
            if (!enabled || trackerTransform == null)
                return;

            OpenVRDevice.ResetSensors();
#if hVIVETRACKER
            humanoid.hipsTarget.viveTracker.ResetSensor();
            humanoid.leftFootTarget.viveTracker.ResetSensor();
            humanoid.rightFootTarget.viveTracker.ResetSensor();
#endif
        }

        public override void AdjustTracking(Vector3 v, Quaternion q) {
            if (trackerTransform != null) {
                trackerTransform.position += v;
                trackerTransform.rotation *= q;
            }
        }

        public Transform GetTrackingTransform() {
            if (!enabled ||
                subTrackers == null ||
                subTrackers.Count <= 0 ||
                subTrackers[0] == null ||
                subTrackers[0].subTrackerId == -1) {

                return null;
            }

            return subTrackers[0].transform;
        }

        public void SyncTracking(Vector3 position, Quaternion rotation) {
            if (!enabled)
                return;


            // rotation

            // Not stable
            //Quaternion deltaRotation = Quaternion.Inverse(lighthouses[0].transform.rotation) * rotation;
            //unityVRroot.rotation *= deltaRotation;

            // stable
            float angle = (-subTrackers[0].transform.eulerAngles.y) + rotation.eulerAngles.y;
            trackerTransform.Rotate(Vector3.up, angle, Space.World);

            // position
            Vector3 deltaPosition = position - subTrackers[0].transform.position;

            trackerTransform.Translate(deltaPosition, Space.World);
        }
    }

    public class OpenVRDevice {
        public const string name = "OpenVR";

        public static bool present = true;
        public static Tracker.Status status;

        private struct SensorState {
            public Passer.ETrackedDeviceClass deviceClass;
            public Vector position;
            public Rotation rotation;
            public float confidence;
            public bool present;
            public string hardwareId;
        }

        private static SensorState[] sensorStates = new SensorState[Passer.OpenVR.k_unMaxTrackedDeviceCount];

        public delegate void OnNewSensor(uint i);
        public static OnNewSensor onNewSensor;

        public static void Start() {
            status = Tracker.Status.Unavailable;
        }

        static Passer.CVRCompositor compositor = Passer.OpenVR.Compositor;
        static Passer.CVRSystem system = Passer.OpenVR.System;
        static Passer.TrackedDevicePose_t[] renderPoseArray = new Passer.TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        static Passer.TrackedDevicePose_t[] gamePoseArray = new Passer.TrackedDevicePose_t[0];

        public static void Update() {
            if (Passer.OpenVR.System == null) {
                status = Tracker.Status.Unavailable;
                return;
            }


            if (compositor != null) {
                status = Tracker.Status.Present;
                compositor.GetLastPoses(renderPoseArray, gamePoseArray);

                for (uint i = 0; i < renderPoseArray.Length; i++) {
                    if (!sensorStates[i].present && renderPoseArray[i].bDeviceIsConnected) {
                        // Detected new sensor
                        if (onNewSensor != null)
                            onNewSensor(i);
                    }
                    sensorStates[i].present = renderPoseArray[i].bDeviceIsConnected;
                    if (renderPoseArray[i].bPoseIsValid) {
                        sensorStates[i].confidence = (renderPoseArray[i].eTrackingResult == Passer.ETrackingResult.Running_OK) ? 1 : 0;
                        StorePose(system, renderPoseArray[i].mDeviceToAbsoluteTracking, i);
                        status = Tracker.Status.Tracking;
                        if (sensorStates[i].hardwareId == null)
                            sensorStates[i].hardwareId = GetSensorHardwareId(i);
                    }
                    else {
                        sensorStates[i].confidence = 0;
                    }
                }
            }
        }

        private static string GetSensorHardwareId(uint index) {
            ETrackedPropertyError error = new ETrackedPropertyError();

            uint capacity = OpenVR.System.GetStringTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_SerialNumber_String, null, 0, ref error);
            StringBuilder sb = new StringBuilder((int)capacity);
            if (capacity > 1) {
                OpenVR.System.GetStringTrackedDeviceProperty(index, ETrackedDeviceProperty.Prop_SerialNumber_String, sb, capacity, ref error);
                string probablyUniqueDeviceSerial = sb.ToString();
                Debug.Log(probablyUniqueDeviceSerial);
                return probablyUniqueDeviceSerial;
            }
            Debug.Log("Unknown device");
            return "Unknown";
        }

        public static string GetHardwareId(uint sensorId) {
            if (sensorStates[sensorId].hardwareId == null)
                sensorStates[sensorId].hardwareId = GetSensorHardwareId(sensorId);
            return sensorStates[sensorId].hardwareId;
        }

        private static ISteamSensor[] sensors = new ISteamSensor[OpenVR.k_unMaxTrackedDeviceCount]; // SteamVR limits # sensors to 16

        public static void ResetSensors() {
            for (int i = 0; i < sensors.Length; i++) {
                if (sensors[i] != null) {
                    sensors[i].trackerId = -1;
                    sensors[i] = null;
                }
            }
        }

        private static void StorePose(Passer.CVRSystem system, Passer.HmdMatrix34_t pose, uint sensorID) {
            Matrix4x4 m = new Matrix4x4();

            m.m00 = pose.m0;
            m.m01 = pose.m1;
            m.m02 = -pose.m2;
            m.m03 = pose.m3;

            m.m10 = pose.m4;
            m.m11 = pose.m5;
            m.m12 = -pose.m6;
            m.m13 = pose.m7;

            m.m20 = -pose.m8;
            m.m21 = -pose.m9;
            m.m22 = pose.m10;
            m.m23 = -pose.m11;

            m.m30 = 0;
            m.m31 = 0;
            m.m32 = 0;
            m.m33 = 0;

            sensorStates[sensorID].position = GetPosition(m);
            sensorStates[sensorID].rotation = GetRotation(m);
            sensorStates[sensorID].deviceClass = system.GetTrackedDeviceClass(sensorID);
        }

        public static Passer.ETrackedDeviceClass GetDeviceClass(int sensorID) {
            if (sensorStates == null)
                return Passer.ETrackedDeviceClass.Invalid;

            return sensorStates[sensorID].deviceClass;
        }

        public static Vector GetPosition(int sensorID) {
            if (sensorStates == null)
                return Vector.zero;

            return sensorStates[sensorID].position;
        }

        public static Rotation GetRotation(int sensorID) {
            if (sensorStates == null)
                return Rotation.identity;

            return sensorStates[sensorID].rotation;
        }

        public static float GetConfidence(int sensorID) {
            if (sensorStates == null || sensorID < 0 || sensorID > sensorStates.Length)
                return 0;

            return sensorStates[sensorID].confidence;
        }

        public static bool IsPresent(int sensorID) {
            if (sensorStates == null || sensorID < 0 || sensorID > sensorStates.Length)
                return false;

            return sensorStates[sensorID].present;
        }

        public struct Matrix4x4 {
            public float m00;
            public float m01;
            public float m02;
            public float m03;
            public float m10;
            public float m11;
            public float m12;
            public float m13;
            public float m20;
            public float m21;
            public float m22;
            public float m23;
            public float m30;
            public float m31;
            public float m32;
            public float m33;
        }

        public static Rotation GetRotation(Matrix4x4 matrix) {
            Rotation q = Rotation.identity; // new Rotation();
            q.w = (float)Math.Sqrt(Math.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2;
            q.x = (float)Math.Sqrt(Math.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2;
            q.y = (float)Math.Sqrt(Math.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2;
            q.z = (float)Math.Sqrt(Math.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2;
            q.x = _copysign(q.x, matrix.m21 - matrix.m12);
            q.y = _copysign(q.y, matrix.m02 - matrix.m20);
            q.z = _copysign(q.z, matrix.m10 - matrix.m01);
            return q;
        }

        private static float _copysign(float sizeval, float signval) {
            if (float.IsNaN(signval))
                return Math.Abs(sizeval);
            else
                return Math.Sign(signval) == 1 ? Math.Abs(sizeval) : -Math.Abs(sizeval);
        }

        public static Vector GetPosition(Matrix4x4 matrix) {
            var x = matrix.m03;
            var y = matrix.m13;
            var z = matrix.m23;

            return new Vector(x, y, z);
        }
    }

    public interface ISteamSensor {
        int trackerId { get; set; }
    }
}
#endif