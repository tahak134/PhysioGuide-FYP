import cv2
import time
import json
import argparse
import numpy as np
import mediapipe as mp

mp_pose = mp.solutions.pose
mp_drawing = mp.solutions.drawing_utils

def main(args):
    """Main function to capture, record, and save pose data."""
    
    print("Initializing MediaPipe Pose...")
    
    print("Opening webcam...")
    cap = cv2.VideoCapture(0)
    
    if not cap.isOpened():
        print("Error: Could not open webcam.")
        return

    print("\n" + "="*50)
    print("      MediaPipe Recorder Ready")
    print(f"     Recording to: {args.output_file}")
    print(f"     Duration: {args.duration} seconds")
    print("="*50 + "\n")
    print("Press 'r' to start your 10-second 'perfect' recording.")
    print("Press 'q' to quit at any time.")
    print("\nEnsure you are in frame and ready...")

    is_recording = False
    start_time = 0
    frame_count = 0

    # Initialize MediaPipe Pose with a context manager
    with mp_pose.Pose(
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5) as pose:
        
        try:
            with open(args.output_file, 'w') as f:
                while True:
                    ret, frame = cap.read()
                    if not ret:
                        print("Error: Failed to grab frame from webcam.")
                        break
                    
                    # Convert the BGR frame to RGB
                    frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                    
                    # Process the frame with MediaPipe
                    results = pose.process(frame_rgb)
                    
                    display_frame = frame # Start with the original frame

                    # --- Recording Logic ---
                    if is_recording:
                        current_time = time.time()
                        elapsed = current_time - start_time
                        
                        if elapsed >= args.duration:
                            # Stop recording
                            is_recording = False
                            print(f"\n...Recording COMPLETE. Saved {frame_count} frames to {args.output_file}")
                            print("Press 'q' to quit or 'r' to record again (will overwrite file).")
                            
                        else:
                            # --- Get keypoints and save data ---
                            if results.pose_landmarks:
                                h, w, _ = frame.shape
                                # Create a list for this frame's keypoints
                                keypoints_list = []
                                for lm in results.pose_landmarks.landmark:
                                    # Convert normalized (0.0-1.0) coords to pixel (x, y) coords
                                    # and use visibility as confidence
                                    keypoints_list.append([lm.x * w, lm.y * h, lm.visibility])

                                # Create the JSON data object for this frame
                                data_object = {
                                    'frame_number': frame_count,
                                    'timestamp': elapsed,
                                    # CRITICAL: This is now 33 landmarks, not 25
                                    'pose_keypoints_2d': keypoints_list 
                                }
                                
                                # Write the JSON object as a single line
                                json.dump(data_object, f)
                                f.write('\n') # Add a newline to separate JSON objects
                                frame_count += 1
                            
                            # Display recording feedback on the raw frame
                            cv2.putText(display_frame, f"RECORDING: {elapsed:.2f}s / {args.duration}s", 
                                        (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                    
                    else: # Not recording
                        # Draw landmarks for visual feedback
                        if results.pose_landmarks:
                            mp_drawing.draw_landmarks(
                                display_frame,
                                results.pose_landmarks,
                                mp_pose.POSE_CONNECTIONS,
                                mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=2, circle_radius=2),
                                mp_drawing.DrawingSpec(color=(0, 0, 255), thickness=2)
                            )

                    # --- Display the webcam feed ---
                    cv2.imshow("MediaPipe Recording - Press 'r' to record, 'q' to quit", display_frame)

                    # --- Key Press Controls ---
                    key = cv2.waitKey(1) & 0xFF

                    if key == ord('q'):
                        print("\nQuitting...")
                        break
                    
                    elif key == ord('r') and not is_recording:
                        print("\nStarting recording...")
                        # Reset file (by re-opening in 'w' mode) and start recording
                        f.seek(0) # Go to start of file
                        f.truncate() # Clear file content
                        is_recording = True
                        start_time = time.time()
                        frame_count = 0

        except Exception as e:
            print(f"An error occurred: {e}")
        finally:
            cap.release()
            cv2.destroyAllWindows()
            print("Script terminated.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Record MediaPipe keypoints from a webcam.")
    parser.add_argument(
        "--output_file", 
        default="Scaption_Perfect.json", 
        help="Name of the output text file (e.g., Scaption_Perfect.json)"
    )
    parser.add_argument(
        "--duration", 
        type=int, 
        default=10, 
        help="Duration of the recording in seconds"
    )
    
    cli_args = parser.parse_args()
    main(cli_args)