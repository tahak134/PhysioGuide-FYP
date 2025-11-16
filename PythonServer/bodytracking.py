"""
shoulder_scaptation_cervical_analysis.py

Real-time analysis for:
 - Active Shoulder Abduction (Scaption)
 - Cervical Retraction (Chin-Tuck)

Displays camera feed with Mediapipe pose overlay
and prints biomechanical metrics + feedback in real time.

Author: ChatGPT (adapted for Hussain)
"""

import cv2
import time
import math
import json
import numpy as np
import mediapipe as mp

# ---- Configuration / thresholds ----
MIN_CONFIDENCE = 0.3
TRUNK_LEAN_THRESHOLD_DEG = 10.0
ELBOW_FLEXION_COMP_THRESHOLD = 20.0
SHOULDER_HIKE_NORM_THRESHOLD = 0.06
SCAPTION_PLANE_ANGLE = 30.0
CVA_CHANGE_MIN_DEG = 4.0
SMOOTH_ALPHA = 0.6

# ---- Helper math functions ----
def angle_between(u, v):
    u, v = np.array(u, float), np.array(v, float)
    nu, nv = np.linalg.norm(u), np.linalg.norm(v)
    if nu == 0 or nv == 0:
        return 0.0
    cos = np.dot(u, v) / (nu * nv)
    return math.degrees(math.acos(np.clip(cos, -1, 1)))

def dist(a, b):
    return float(np.linalg.norm(np.array(a) - np.array(b)))

def unit(v):
    v = np.array(v, float)
    n = np.linalg.norm(v)
    return v / n if n != 0 else v

def rotate_about_x(vec, deg):
    rad = math.radians(deg)
    c, s = math.cos(rad), math.sin(rad)
    R = np.array([[1, 0, 0], [0, c, -s], [0, s, c]])
    return R.dot(vec)

# ---- Smoothing helper ----
class Smoothed:
    def __init__(self, alpha=SMOOTH_ALPHA):
        self.alpha = alpha
        self.val = None
    def update(self, x):
        if x is None:
            return self.val
        if self.val is None:
            self.val = x
        else:
            self.val = self.alpha * x + (1 - self.alpha) * self.val
        return self.val

# ---- Keypoint builder for Mediapipe ----
def build_keypoint_dict(lm):
    mpmap = {
        "Nose": 0, "RShoulder": 12, "RElbow": 14, "RWrist": 16,
        "LShoulder": 11, "LElbow": 13, "LWrist": 15, "MidHip": 23
    }
    kp = {}
    for name, idx in mpmap.items():
        p = lm[idx]
        conf = getattr(p, "visibility", 1.0)
        kp[name] = (p.x, p.y, 0.0, conf)
    # approximate Neck as midpoint of shoulders
    rs, ls = kp["RShoulder"], kp["LShoulder"]
    x = (rs[0] + ls[0]) / 2
    y = (rs[1] + ls[1]) / 2
    conf = (rs[3] + ls[3]) / 2
    kp["Neck"] = (x, y, 0.0, conf)
    return kp

# ---- Metrics computation ----
def compute_metrics(kp, baseline):
    def get(name): return np.array(kp[name][:2])
    nose, neck, rsh, lsh = get("Nose"), get("Neck"), get("RShoulder"), get("LShoulder")
    relb, lel, rw, lw, midhip = get("RElbow"), get("LElbow"), get("RWrist"), get("LWrist"), get("MidHip")

    shoulder_width = dist(rsh, lsh) or 1.0
    torso_v = unit(np.array([0, 1, 0]))  # up

    # Convert to Cartesian (y up)
    to_cart = lambda v: np.array([v[0], -v[1], 0.0])
    neck_c, midhip_c = to_cart(neck), to_cart(midhip)
    rsh_c, relb_c, lsh_c, lel_c = to_cart(rsh), to_cart(relb), to_cart(lsh), to_cart(lel)
    nose_c = to_cart(nose)

    torso_vec = unit(neck_c - midhip_c)
    r_humerus = unit(relb_c - rsh_c)
    l_humerus = unit(lel_c - lsh_c)

    r_humerus_rot = rotate_about_x(r_humerus, -SCAPTION_PLANE_ANGLE)
    l_humerus_rot = rotate_about_x(l_humerus, -SCAPTION_PLANE_ANGLE)
    r_scapt_elev = angle_between(r_humerus_rot, torso_vec)
    l_scapt_elev = angle_between(l_humerus_rot, torso_vec)

    # elbow flexion
    def elbow_angle(sh, el, wr):
        return angle_between(sh - el, wr - el)
    r_elbow = elbow_angle(rsh_c, relb_c, to_cart(rw))
    l_elbow = elbow_angle(lsh_c, lel_c, to_cart(lw))

    # trunk lean
    trunk_lean = angle_between(torso_vec, np.array([0, 1, 0]))

    # shoulder hike
    norm = baseline["shoulder_width"]
    shoulder_hike_norm = abs((neck_c[1] - rsh_c[1]) / norm)

    # CVA (Chin Tuck)
    neck_to_nose = unit(nose_c - neck_c)
    CVA_deg = angle_between(neck_to_nose, np.array([1, 0, 0]))

    return {
        "r_scapt_elev_deg": r_scapt_elev,
        "l_scapt_elev_deg": l_scapt_elev,
        "r_elbow_flex_deg": r_elbow,
        "l_elbow_flex_deg": l_elbow,
        "trunk_lean_deg": trunk_lean,
        "shoulder_hike_norm": shoulder_hike_norm,
        "CVA_deg": CVA_deg,
    }

# ---- Feedback ----
def evaluate_flags(metrics, baseline):
    return {
        "compensation_trunk": metrics["trunk_lean_deg"] > TRUNK_LEAN_THRESHOLD_DEG,
        "compensation_elbow_flexion": (
            metrics["r_elbow_flex_deg"] > ELBOW_FLEXION_COMP_THRESHOLD or
            metrics["l_elbow_flex_deg"] > ELBOW_FLEXION_COMP_THRESHOLD
        ),
        "compensation_shoulder_hike": metrics["shoulder_hike_norm"] > SHOULDER_HIKE_NORM_THRESHOLD,
        "chin_tuck_good": (metrics["CVA_deg"] - baseline["CVA_neutral"]) > CVA_CHANGE_MIN_DEG
    }

# ---- Convert NumPy types before JSON ----
def np_to_py(obj):
    if isinstance(obj, (np.bool_, bool)):
        return bool(obj)
    elif isinstance(obj, (np.integer, int)):
        return int(obj)
    elif isinstance(obj, (np.floating, float)):
        return float(obj)
    elif isinstance(obj, dict):
        return {k: np_to_py(v) for k, v in obj.items()}
    elif isinstance(obj, list):
        return [np_to_py(x) for x in obj]
    return obj

# ---- Main ----
def main():
    mp_pose = mp.solutions.pose
    mp_drawing = mp.solutions.drawing_utils
    pose = mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5)
    cap = cv2.VideoCapture(0)

    smoothers = {k: Smoothed() for k in ["r_scapt_elev_deg", "l_scapt_elev_deg", "CVA_deg", "trunk_lean_deg"]}
    baseline = {"shoulder_width": None, "CVA_neutral": None}

    print("INFO: Starting Mediapipe-based physiotherapy tracking.")
    print("Hold neutral pose for 5 seconds to calibrate baseline...")

    baseline_ws, baseline_cvas = [], []
    start_time = time.time()

    # ---- Calibration phase ----
    while time.time() - start_time < 5:
        ret, frame = cap.read()
        if not ret:
            continue
        results = pose.process(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
        if not results.pose_landmarks:
            continue
        lm = results.pose_landmarks.landmark
        kp = build_keypoint_dict(lm)
        rsh, lsh = np.array(kp["RShoulder"][:2]), np.array(kp["LShoulder"][:2])
        nose, neck = np.array(kp["Nose"][:2]), np.array(kp["Neck"][:2])
        shoulder_width = dist(rsh, lsh)
        neck_c, nose_c = np.array([neck[0], -neck[1], 0]), np.array([nose[0], -nose[1], 0])
        cva = angle_between(unit(nose_c - neck_c), np.array([1, 0, 0]))
        baseline_ws.append(shoulder_width)
        baseline_cvas.append(cva)

    baseline["shoulder_width"] = float(np.median(baseline_ws))
    baseline["CVA_neutral"] = float(np.median(baseline_cvas))
    print(f"CALIBRATION DONE: {baseline}")

    # ---- Main loop ----
    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                continue

            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = pose.process(frame_rgb)

            if not results.pose_landmarks:
                cv2.imshow("Physio Tracking", frame)
                if cv2.waitKey(1) & 0xFF == 27:  # ESC
                    break
                continue

            mp_drawing.draw_landmarks(
                frame,
                results.pose_landmarks,
                mp_pose.POSE_CONNECTIONS,
                mp_drawing.DrawingSpec(color=(0,255,0), thickness=2, circle_radius=2),
                mp_drawing.DrawingSpec(color=(0,0,255), thickness=2)
            )

            kp = build_keypoint_dict(results.pose_landmarks.landmark)
            metrics = compute_metrics(kp, baseline)
            for k in smoothers:
                metrics[k] = smoothers[k].update(metrics[k])

            flags = evaluate_flags(metrics, baseline)

            feedback = []
            if flags["compensation_trunk"]:
                feedback.append("Reduce trunk lean; keep torso upright.")
            if flags["compensation_shoulder_hike"]:
                feedback.append("Avoid shoulder shrugging.")
            if flags["compensation_elbow_flexion"]:
                feedback.append("Keep elbows straight during scaption.")
            if flags["chin_tuck_good"]:
                feedback.append("Good chin tuck — maintain posture.")

            output = {
                "t": time.time(),
                "metrics": metrics,
                "flags": flags,
                "feedback": feedback
            }

            # Safe JSON conversion
            output = np_to_py(output)
            print(json.dumps(output))

            # Display feedback text on video
            y0 = 30
            for msg in feedback:
                cv2.putText(frame, msg, (10, y0), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0,0,255), 2)
                y0 += 25

            cv2.imshow("Physio Tracking", frame)
            if cv2.waitKey(1) & 0xFF == 27:  # ESC to quit
                break

    except KeyboardInterrupt:
        print("Stopped by user.")
    finally:
        cap.release()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
