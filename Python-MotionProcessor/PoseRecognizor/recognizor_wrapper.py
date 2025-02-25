import time
import argparse
import sys, os
from collections import deque, Counter
from pathlib import Path

import numpy as np
import json, yaml
import paho.mqtt.client as mqtt

from preprocess import normalizePoints
from utils import load_yaml, load_json, write_data2json, on_mqtt_connect, transform1D, templates_mirror_flip
from recognizorv3 import dtw1, dtw3, recognizor_dynamic
from oneEuroFilter import OneEuroFilter




class Recognizor:
    default_state = "Pointing"
    recognizor_folder = "PoseRecognizor"

    def __init__(self, config_file_path, **kwargs):
        self.config = load_yaml(config_file_path)
        for k, v in self.config["preprocessor"].items():
            setattr(self, k, v)
        for k, v in self.config["recognizor"].items():
            setattr(self, k, v)

        # Set objects key based on the mode 0:Gym, 1:WarpSquare
        self.objs_key = "objects" if self.place == 0 else "fused_objs"

        self.models = {1: dtw1, 3:dtw3}
        self.model = self.models[self.model_id]

        self.data_queue = deque(maxlen=self.window_size)
        self.id_queue = deque(maxlen=self.window_size)
        self.frame_id = 0
        self.new_flag = False
        self.prev_label = None
        self.cur_label = None
        self.cur_record = None
        self.prev_match_id_in_path = -1
        self.cur_data = None

        # Load templates
        # self.templates = self.get_templates()
        # self.template_last_frames = np.array([template['data'][-1] for template in self.templates])

        # Load calibration data
        self.calib = self.get_calib_params()


        # Set up mqtt client and build connection
        self.client = self.setup_mqtt_connection()

        # Initialize default user action
        self.user_status = Recognizor.default_state
        
        # For local test
        self.start_time_host = None
        self.start_time_input = None


        # Variables for debugging and testing
        self.rec_cnt = 0 # Number of recognition times
        self.match_cnt = 0 # Number of matched times
        self.match_record = [] # List of match recording 
        self.final_cnt = 0 # Number of matched times (exclude consecutive match)
        self.match_id = [] # List of matched frame ID (first ID of the matched queue)
        self.match_id_in_path = [] # List of matched frame ID (first ID in the matched path)
        self.match_res = {} # {match_id: matched label, ...}


        # Save recognition result
        self.save_result = kwargs["save_result"]
        self.output_file = kwargs["result_file"]

        # Full body joints
        self.full_joints = None

        # Extention for tracking multiple IDs
        self.cur_frame = {}
        self.all_msg = {"users": []}
        self.id_tracker = self.set_id_tracker()
        self.one_euro_filters = {}



    def get_config_attributes(self):
        return self.config
    
    def preprocess(self, frame_data):
        norm_frame_data = normalizePoints(frame_data, origin=self.origin, vec=self.normal, rotate=self.rotate)

        return norm_frame_data
    
    def setup_mqtt_connection(self):
        client = mqtt.Client()
        client.on_connect = on_mqtt_connect
        client.connect(self.pubhost, self.port)
        return client
        
    
    def get_templates(self):
        templates = []
        for file in self.template_files:
            file_path = os.path.join(self.dataset_folder, file)
            template = load_json(file_path)
            templates.extend(template)

        if (self.mirror_templates):
            templates_LR = []
            for obj in templates:
                #TODO Need to add id switch for swipe
                if obj['label'] == 9:
                    templates_LR.append({**obj, "data": [[templates_mirror_flip(frame,origin=self.origin, normal=self.normal, rotate=self.rotate)[i] for i in self.joints] for frame in obj['data']], "label": 8})
                elif obj['label'] == 8:
                    templates_LR.append({**obj, "data": [[templates_mirror_flip(frame,origin=self.origin, normal=self.normal, rotate=self.rotate)[i] for i in self.joints] for frame in obj['data']], "label": 9})
                elif obj['label'] == 3:
                    templates_LR.append({**obj, "data": [[templates_mirror_flip(frame,origin=self.origin, normal=self.normal, rotate=self.rotate)[i] for i in self.joints] for frame in obj['data']], "label": 2})



                templates_LR.append({**obj, "data": [[frame[i] for i in self.joints] for frame in obj['data']] })
        else:
            templates_LR = [{**obj, "data": [[frame[i] for i in self.joints] for frame in obj['data']] } for obj in templates]
        templates_LR = np.array(templates_LR)
        return templates_LR

    
    def frame_filter(self, input_frame, template_frames, threshold=2.0):
        distances = np.linalg.norm(template_frames - input_frame, axis=(1,2))
        indices_lte_threshold = np.where(distances <= threshold)[0]
        indices_lte_threshold = indices_lte_threshold.tolist()

        return indices_lte_threshold
    
    def trackable_filter(self, data):
        ct = self.calib['center']
        norm = self.calib['norm']

        def _is_trackable(ct, norm, pt):
            # Check if pt is within track_dist to ct, and ct-pt is within track_cos angle to norm
            if np.linalg.norm(pt[:2] - ct[:2]) > self.track_dist:
                return False
            v = pt - ct
            v /= np.linalg.norm(v)
            trackable = np.abs(np.dot(v, norm)) > np.cos(self.track_angle / 180 * np.pi)
            return trackable
        
        trackable_data = {**data, self.objs_key: [obj for obj in data[self.objs_key] if _is_trackable(ct, norm, obj['features'][3:6])]} 

        return trackable_data



    
    def get_calib_params(self):
        calib_file = os.path.join(Recognizor.recognizor_folder, self.calib_file)
        calib_params = load_json(calib_file)
        calib = {"norm": np.array(calib_params['norm']), \
                 "center": np.array(calib_params['center']), \
                 "corners": np.array(calib_params['corners'])}
        return calib
    
    
    def get_closest_n_ids(self, frame_data, center=[1.83, -0.36, 1.44]):
        data = frame_data[self.objs_key]
        # Sort the data based on the distance between the keypoint 1 (neck) and the screen center point (2D coordinates)
        # data = sorted(data, key=lambda u: np.linalg.norm(u['features'][3:5] - self.calib['center'][:2]))
        data = sorted(data, key=lambda u: np.linalg.norm(np.array(u['features'][3:5]) - np.array(center[:2])))
        top_n_ids = [obj['id'] for obj in data[:min(self.n_closest_ids, len(data))]]
        
        return top_n_ids

    def get_track_ids(self, frame_data):
        all_ids = [obj['id'] for obj in frame_data[self.objs_key]]
        if self.track_mode == 0 or (self.track_mode == 2 and self.n_closest_ids >= len(all_ids)):
            return all_ids, all_ids
        elif self.track_mode == 1:
            return self.user_ids, all_ids
        elif self.track_mode == 2:
            closest_n_ids = self.get_closest_n_ids(frame_data)
            return closest_n_ids, all_ids
    
    def set_id_tracker(self):
        id_tracker = MultiIDTracker()
        return id_tracker
    
    def synchronize_time(self, data_fusion_time):
        """
        Synchronizes the processing to match the original data fusion time pacing.

        :param data_fusion_time: The fusion time from the data, in milliseconds.
        """
        if self.start_time_host is None:
            self.start_time_host = time.time()
            self.start_time_input = data_fusion_time * 1e-3
        else:
            # Calculate the time difference between now and the expected time based on data timestamps
            time_delta = max(0, (data_fusion_time * 1e-3 - self.start_time_input) - (time.time() - self.start_time_host))
            time.sleep(time_delta)


    
    def get_model_parameters(self):
        model = {
            "Model": self.models[self.model].__name__, \
            "open-begin": self.open_begin, \
            "open-end": self.open_end, \
            "Data templates": self.template_files, \
            "Single frame distance threshold": self.single_frame_threshold, \
            "Average frame distance threshold": self.avg_frame_threshold, \
            "Window size": self.window_size, \
            "Angle check": self.match_angle
        }
        if self.match_angle:
            model["Single frame angle threshold"] = self.single_angle_threshold
            model["avg_angle_threshold"] = self.avg_angle_threshold

        return model
    
    def get_current_queue(self):
        return self.data_queue, self.id_queue
    
    def get_current_frame(self):
        return self.cur_frame
    
    def get_current_raw_data(self):
        return self.cur_data
    
    def get_current_recognition(self):
        return self.cur_record
    
    def get_recognizor_result(self):
        stats = {
            "Number of recognition times": self.rec_cnt, \
            "Number of matched times": self.match_cnt, \
            "Number of matched times (consecutive match excluded)": self.final_cnt, \
            "Matched ID": self.match_id, \
            "Matched ID in path": self.match_id_in_path, \
            "Matched ID-Label": self.match_res, \
            "Label count": Counter(self.match_res.values())
        }
        
        result = {"Stats": stats, "Matches": self.match_record}
        return result

        
    def empty_data_queue(self):
        if self.data_queue:
            filtered_ids = self.frame_filter(self.data_queue[-1], self.template_last_frames, threshold=self.single_frame_threshold)

            if len(filtered_ids) > 0:
                filtered_templates = self.templates[filtered_ids]
                p, recognition_result = recognizor_dynamic(self.model, self.action_class, self.data_queue, filtered_templates, self.label_dict, \
                                                            self.single_frame_threshold, distance_threshold=self.avg_frame_threshold, \
                                                            open_begin=self.open_begin, open_end=self.open_end, \
                                                            match_angle=self.match_angle, use_polar=self.use_polar, \
                                                            angle_threshold=self.single_angle_threshold, avg_angle_threshold=self.avg_angle_threshold, n_references=self.n_references)
                # ---Print out for debugging and test purposes---------------------
                self.rec_cnt += 1
                # print(f"{self.rec_cnt}th Recognition:")
                # print("Input frame ID: ",  list(self.id_queue))
                # print(recognition_result)
                # ---------------------------------------------------------------- #
                if recognition_result['Msg'] == 'Matched': 
                    self.match_cnt += 1
                    self.cur_record = {'id': self.rec_cnt, 'frames': list(self.id_queue), 'result': recognition_result, 'path': p}
                    self.user_status = recognition_result["Matched label"]
                    self.match_record.append(self.cur_record)
                else:
                    self.user_status = Recognizor.default_state
                
    
    def recognize(self, msg):
        self.user_status = Recognizor.default_state
        ## Load motion data from MQTT server
        data = json.loads(msg.strip())
        self.cur_data = data

        # Clear the data for every new frame
        self.all_msg["users"] = []
        self.cur_frame = {}

        
        ## Check if the data is valid, i.e. Human detected in the data
        if (self.objs_key not in data) or (data[self.objs_key] is None) or (len(data[self.objs_key]) == 0): 
            current_ids = list(self.id_tracker.current_states.keys())
            for curr_id in current_ids:
                self.id_tracker.dec_state(curr_id)
                if curr_id not in self.id_tracker.current_state and curr_id in self.one_euro_filters:
                    del self.one_euro_filters[curr_id]
            self.frame_id += 1
            print(f"Frame {self.frame_id}: No user detected")

            if self.replay:
                self.synchronize_time(data['fusion_time'])

            return
        
        ## Unify the data format
        data[self.objs_key] = [{**obj, "features": transform1D(obj["features"])} for obj in data[self.objs_key]]

        ## Filter data that is not within the trackable region
        # data = self.trackable_filter(data)
        # print("Trackable user count:", len(data[self.objs_key]))

        # Filter the IDs based on the tracking mode
        track_ids, all_ids = self.get_track_ids(data)
        untrackable_ids = list(set(track_ids) - set(all_ids))
        valid_ids = list(set(track_ids) & set(all_ids))

        for id in untrackable_ids:
            self.id_tracker.dec_state(id)
            if id not in self.id_tracker.current_state and id in self.one_euro_filters:
                del self.one_euro_filters[curr_id]


        if len(valid_ids) <= 0:
            self.frame_id += 1
            print(f"Frame {self.frame_id}: Specified IDs {track_ids} are not in trackable IDs {all_ids}")

            if self.replay:
                self.synchronize_time(data['fusion_time'])

            return          

        

        for id in valid_ids:    
            self.id_tracker.inc_state(id)

        # Create a dictionary mapping IDs to their corresponding frame data
        id_to_frame = {u['id']: [u['features'][i:i+3] for i in range(0, len(u['features']), 3)] for u in data[self.objs_key] if u['id'] in valid_ids}

        for id, frame in id_to_frame.items():
            self.cur_frame[id] = frame

            ## Initialize oneEurofilter when receiving the first valid frame
            if id not in self.one_euro_filters:
                if self.use_noise_filter_all:
                    temp = frame
                    temp = [x for pt in temp for x in pt]
                    self.one_euro_filters[id] = OneEuroFilter(t0=data['fusion_time']*1e-3, x0=temp, min_cutoff=self.all_min_cutoff, beta=self.all_beta)
                    print(f"User {id}: OneEuroFilter for all points initialized.")
            else:
                if self.use_noise_filter_all:
                    temp = frame
                    temp = [x for pt in temp for x in pt]
                    temp = self.one_euro_filters[id](data['fusion_time']*1e-3, temp)
                    temp = temp.tolist()
                    temp = [temp[i:i+3] for i in range(0, len(temp), 3)]
                    frame = temp


            self.full_joints = frame
            cur_user_msg = {"id": id, "joints": self.full_joints}
            self.all_msg["users"].append(cur_user_msg)
            print(f"Joints_{id}, Left-Right", self.full_joints[4], self.full_joints[7])

            
            ## (Only for local test with pre-recorded data)
            ## When replay is true, the message will be processed based on the recorded actual time interval
            if self.replay:
                self.synchronize_time(data['fusion_time'])
        


        self.frame_id += 1



class MultiIDTracker:
    def __init__(self, missing_frame_tolerance=3):
        # Initialize current_state as an empty dictionary
        self.current_state = {}
        self.missing_tolerence = missing_frame_tolerance

    def inc_state(self, curr_id):
        if curr_id not in self.current_state:
            print(f"New tracking user ID: {curr_id}")
            self.current_state[curr_id] = self.missing_tolerence  # Initialize new ID with a state of 3
        else:
            self.current_state[curr_id] = min(self.missing_tolerence, self.current_state[curr_id] + 1)  # Increment state by 1, up to a maximum of 3

    def dec_state(self, curr_id):
        if curr_id in self.current_state:
            self.current_state[curr_id] -= 1
            if self.current_state[curr_id] == 0:
                del self.current_state[curr_id]  # Remove the ID from current_state when its state reaches 0
                print(f"Lose tracking user ID: {curr_id}")
        else:
            print(f"ID {curr_id} not found in current tracking states.")

    def print_states(self):
        for id, state in self.current_state.items():
            print(f"ID: {id}, State: {state}")



class Projector2D:
    recognizor_folder = "PoseRecognizor"

    def __init__(self, calib_file):
        self.calib = self.get_calib_params(calib_file)

    def get_calib_params(self, calib_file):
        calib_file = os.path.join(Recognizor.recognizor_folder, calib_file)
        calib_params = load_json(calib_file)
        calib = {"norm": np.array(calib_params['norm']), \
                 "center": np.array(calib_params['center']), \
                 "corners": np.array(calib_params['corners'])}
        return calib
    
    def ray_intersect(self, pt1, pt2):
        norm = self.calib['norm']
        ct = self.calib['center']

        pt1 = np.array(pt1)
        pt2 = np.array(pt2)

        ray = pt2 - pt1
        # calculate angle cos between ray and norm
        ray_angle = np.dot(-norm, ray / np.linalg.norm(ray))
        # calculate distance from pt1 to ct along norm
        pt1_dist = np.dot(norm, pt1 - ct)
        # rescale
        target_dist = pt1_dist / ray_angle
        ret = pt1 + (pt2 - pt1) / np.linalg.norm(pt2 - pt1) * target_dist
        return ret
    
    def calc_position(self, pt):
        corners = self.calib['corners']

        h1 = corners[1] - corners[0]
        x1 = np.dot(pt - corners[0], h1 / np.linalg.norm(h1)) / np.linalg.norm(h1)
        h2 = corners[2] - corners[3]
        x2 = np.dot(pt - corners[3], h1 / np.linalg.norm(h2)) / np.linalg.norm(h2)
        x = (x1 + x2) * .5
        v1 = corners[3] - corners[0]
        y1 = np.dot(pt - corners[0], v1 / np.linalg.norm(v1)) / np.linalg.norm(v1)
        v2 = corners[2] - corners[1]
        y2 = np.dot(pt - corners[1], v2 / np.linalg.norm(v2)) / np.linalg.norm(v2)
        y = (y1 + y2) * .5
        return [x, y]


class ZoomDetector:
    """Avoid consecutive same zoom action detections within [interval] seconds"""
    def __init__(self, default_state, interval=1):
        self.interval = interval
        self.default_state = default_state
        self.last_state = default_state
        self.prev_time = None

    def detect_zoom(self, user_state):
        cur_time = time.time()  

        if self.prev_time == None or cur_time - self.prev_time > self.interval:
            self.last_state = self.default_state
        
        self.prev_time = cur_time

        # Check the detected value and update last_state
        if user_state == "ZoomIn" and self.last_state != "ZoomOut":
            self.last_state = "ZoomIn"
            return "ZoomIn"
        elif user_state == "ZoomOut" and self.last_state != "ZoomIn":
            self.last_state = "ZoomOut"
            return "ZoomOut"
        else:
            print(f"{user_state} is deactivated in UI")
            return self.default_state               
        

if __name__ == "__main__":
    config_file1 = os.path.join(Recognizor.recognizor_folder, "config.yml")
    config_file2 = os.path.join(Recognizor.recognizor_folder, "config_press.yml")

    # If save the recognition result
    save_result = False
    result_file = "recognition_result.txt"

    # If apply oneEuroFilter to all body keypoints, parameter could be modified in the program above

    recognizor = Recognizor(config_file1,  save_result = save_result, result_file = result_file)
    # recognizor_press = Recognizor(config_file2, save_result = save_result, result_file = result_file)

    
    # zoom_detector = ZoomDetector(Recognizor.default_state, interval=1.4)


    client = recognizor.client
    client.loop_start()
    

    
    try:
        for r in sys.stdin:
            recognizor.recognize(r)

            # # Action recognition
            # if recognizor.user_status == recognizor_press.user_status:
            #     res = recognizor.user_status
            # elif recognizor.user_status != Recognizor.default_state:
            #     res = recognizor.user_status
            # elif recognizor_press.user_status != Recognizor.default_state:
            #     res = recognizor_press.user_status
            # else: #TODO: Different non-default actions recognized
            #     res = Recognizor.default_state
            res = Recognizor.default_state

            # # Consecutive zoom-in/zoom-out check
            # if res == "ZoomIn" or res == "ZoomOut":
            #     res = zoom_detector.detect_zoom(res)
                
            
            # pub_msg = {"cursor_l": pt_l, "cursor_r": pt_r, "joints": joint_coords, "user": res}
            pub_msg = recognizor.all_msg
            client.publish(recognizor.pubtopic, payload=json.dumps(pub_msg))
            # print(recognizor.frame_id, pub_msg)

    except KeyboardInterrupt:
        print("\nTesting interrupted by user.")    
    finally:
        client.loop_stop()
        # recognizor.empty_data_queue()
        # recognizor_press.empty_data_queue()
        result = recognizor.get_recognizor_result()
        # result2 = recognizor_press.get_recognizor_result()
        
        print("Recognizor_swipe_zoom")
        print('\n'.join([f'{key}: {value}' for key, value in result["Stats"].items()]))
        # print("Recognizor_press")
        # print('\n'.join([f'{key}: {value}' for key, value in result2["Stats"].items()]))

        if save_result:
            with open(result_file, "a") as f:
                print("Recognizor_swipe_zoom",  file=f)
                print('\n'.join([f'{key}: {value}' for key, value in result["Stats"].items()]),  file=f)
                # print("Recognizor_press",  file=f)
                # print('\n'.join([f'{key}: {value}' for key, value in result2["Stats"].items()]),  file=f)

