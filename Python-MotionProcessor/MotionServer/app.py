import queue
import threading
import math
import numpy as np
import base64
import torch

from flask import Flask, request, jsonify
from diffusers.utils import load_image, make_image_grid

from diffusion_pipe import MidasDepth, DiffusionGenerator
from points2image_utils import (draw_base_flower, draw_pattern_on_image, image2bytes, 
                              find_smallest_square_and_center_points, enhance_image, scale_and_center_points)
from points2linepattern import draw_smooth_curve_with_random_petal, flower_pattern_selector, moving_average, filter_points
from utils_flower import save_json, load_json, get_transparent_img_bytes, remove_bg, save_points_to_file, timer

import sys
sys.path.append('/BiRefNet_All')
sys.path.append('/BiRefNet_All/BiRefNet')
from BiRefNet import inference2

import logging
import signal

log = logging.getLogger('werkzeug')
log.setLevel(logging.WARNING)

app = Flask(__name__)

# Preparation for Diffusion model
midasDepth = MidasDepth()
config_file = "./flower_generator/motion_server/diffusion_config.yaml"
diffusion_generator = DiffusionGenerator(config_file, midasDepth)
flower_patterns = load_json("./flower_generator/motion_server/flower_pattern.json")

default_image_bytes = get_transparent_img_bytes()

# Preparation for BiRefNet (Remove background)
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
model_path = '/BiRefNet_All/BiRefNet_ckpt/BiRefNet_DIS_ep580.pth'
birefnet = inference2.get_model(model_path, device)


print("All models are LOADED")

class TaskExecutor:
    def __init__(self, process_task_function):
        self.task_queue = queue.Queue()
        self.worker_thread = threading.Thread(target=self.worker)
        self.worker_thread.daemon = True
        self.worker_thread.start()
        self.process_task = process_task_function
        self.stop_worker = False

        # Output queue
        self.image = None
        self.image_bytes = None
        self.images = {} # {task_id1: (image), task_id2: (image)}

        # Debug
        self.cnt = 0

        # Task counter
        self.task_id = 0


    
    def add_task(self, *args):
        """Add a task to the queue."""
        self.task_id += 1
        _, user_id, side, task_cnt = args
        self.task_queue.put(args)
        self.images[args[-1]] = None

        # Save task points to a file (Debugging)
        # save_points_to_file(args[0], filename="./flower_generator/motion_server/task_points_twopplWS0311.json")

        print(f"Executor Added drawing task {task_cnt}-{self.task_id} for user {user_id} - {side} side, into the queue")

    
    def worker(self):
        """Worker thread to process tasks serially."""
        while True:
            task_args = self.task_queue.get()
            try:
                sd_img = self.process_task(*task_args)
                if sd_img == "Invalid":
                    self.image = "Invalid"
                    self.image_bytes = default_image_bytes
                    self.images[task_args[-1]] = self.image_bytes
                else:
                    self.image = sd_img
                    self.image_bytes = image2bytes(sd_img)
                    self.images[task_args[-1]] = self.image_bytes
                # print(self.images.keys())
                self.cnt += 1
            # # Remove try-except
            # except Exception as e:
            #     print(f"Error processing task: {e}")
            finally:
                self.task_queue.task_done()

    def shutdown(self):
        """Shut down the worker thread."""
        self.stop_worker = True
        self.worker_thread.join()


def process_image_task(points, user_id, side, task_cnt):
    print(f"Task {task_cnt}: Processing image for [user {user_id}, side {side}]")
    # ================================================================================================
    ## Test purpose only, delete when demoing
    # image_line = draw_pattern_on_image(points)
    # image_line.save(f"./flower_generator/line_circle_test/twopplWS0311/line_img/img_{task_cnt}.png")
    # ================================================================================================

    ## 1. Draw base line-circle
    # image, square_size, center_x, center_y = draw_base_flower(points, resize=False, byte_format=False)
    draw_func, flower_name = flower_pattern_selector(flower_patterns)
    image = draw_smooth_curve_with_random_petal(points, scale=False, draw_func1=draw_func)
    print(f"Image is None: {image == None}, {len(points)}")
    if image == None:
        return "Invalid"
    # image = image.resize((1024,1024))
    
    ## 2. Generate flower image
    # depth_img = midasDepth.get_depth_image(image)
    prompt = f"Vintage oil painting style, {flower_name} flowers, white background, detailed"
    sd_img, depth_img = diffusion_generator.infer(image, prompt=prompt, output_depth=True)

    ## 3. Remove background
    # sd_img = enhance_image(sd_img)
    with timer("Remove background"):
        # sd_img_rb = remove_bg(sd_img, bg_color = (255, 255, 255, 0))
        sd_img_rb = inference2.main(birefnet, device, sd_img)
    # sd_img = enhance_image(sd_img_rb)


    ## 4. Save image (if needed)
    imgs = [image, depth_img, sd_img, sd_img_rb]
    grid = make_image_grid(imgs, rows=1, cols=4)
    grid.save(f"./flower_generator/line_circle_test/twopplWS0311/grid_img_0412/img_{task_cnt}.png")
    
    print(f"Task {task_cnt} Done")
    return sd_img_rb


class DrawingDataManager:
    def __init__(self, task_executor):
        self.task_executor = task_executor
        self.current_points = {}
        self.pen_up_counter = {}
        self.task_cnt = 0
        self.task_line_ids = {}

        self.task_prev_pos = []
        self.task_prev_id = -1

    def _initialize_user_side(self, user_id, side):
        if user_id not in self.current_points:
            self.current_points[user_id] = {}
            self.pen_up_counter[user_id] = {}
            self.task_line_ids[user_id] = {}
        if side not in self.current_points[user_id]:
            self.current_points[user_id][side] = []
            self.pen_up_counter[user_id][side] = 0
            self.task_line_ids[user_id][side] = {}


    def process_drawing_data(self, data):
        user_id = data['userID']
        side = data['side']
        line_id = data['tempID']
        start_x, start_y = data['startX'], data['startY']
        end_x, end_y = data['endX'], data['endY']
        pen_touch = data['penTouchingPaper']

        self._initialize_user_side(user_id, side)

        if pen_touch:
            self.pen_up_counter[user_id][side] = 0
            if not self.current_points[user_id][side] or (self.current_points[user_id][side][-1][0] != start_x and self.current_points[user_id][side][-1][1] != start_y):
                self.current_points[user_id][side].extend([[start_x, start_y], [end_x, end_y]])
                self.task_line_ids[user_id][side][line_id] = self.task_line_ids[user_id][side].get(line_id, 0) + 1
        else:
            self.pen_up_counter[user_id][side] += 1
            # if line_id not in self.task_line_ids[user_id][side]:
            #     self.task_line_ids[user_id][side][line_id] = 0
            if self.pen_up_counter[user_id][side] >= 10 and self.current_points[user_id][side]:
                task_points = self.current_points[user_id][side]
                self.task_cnt += 1

                avg_pts = moving_average(task_points)
                filtered_pts = filter_points(avg_pts)
                pts, img_size, center_x, center_y = find_smallest_square_and_center_points(filtered_pts)
                pts, _ = scale_and_center_points(np.array(pts), img_size)

                img_size = min(round(img_size), 200)
                if self.task_prev_pos != [center_x, center_y] and self.task_prev_id != self.task_cnt:
                    self.task_executor.add_task(pts, user_id, side, self.task_cnt)
                    line_ids = list(self.task_line_ids[user_id][side].keys())
                    line_ids_cnt = self.task_line_ids[user_id][side]
                    print(f"Added drawing task {self.task_executor.task_id} for user {user_id} - {side} side, into the queue")
                    print(line_ids_cnt)
                    self.current_points[user_id][side] = []
                    self.task_line_ids[user_id][side] = {}
                    self.task_prev_pos = [center_x, center_y]
                    self.task_prev_id = self.task_cnt
                    msg = {'new_task': True, 'task_id': self.task_executor.task_id, 'line_ids': line_ids, 'user_id': user_id, 'side': side, 'pos': [center_x, center_y], 'size': math.ceil(img_size)}
                    print("Debug", msg)
                    return msg
        return {'new_task': False, 'task_id': self.task_cnt, 'line_ids': [], 'user_id': user_id, 'side': side, 'pos': None, 'size': None}



class Server:
    def __init__(self, drawing_data_manager):
        self.request_cnt = 0
        self.drawing_data_manager = drawing_data_manager

    def receive_data(self):
        self.request_cnt += 1
        data = request.get_json()

        task_info = self.drawing_data_manager.process_drawing_data(data)

        return jsonify(task_info)

        # if task_added:
        #     return jsonify({'message': 'Image generation task started successfully', 'new_task': task_added, 'task_id': task_cnt})
        # else:    
        #     return jsonify({'message': 'Data received successfully', 'new_task': task_added, 'task_id': task_cnt})



# Initialize server
task_executor = TaskExecutor(process_image_task)
drawing_data_manager = DrawingDataManager(task_executor)
server = Server(drawing_data_manager)

get_cnt = 0


@app.route('/receive', methods=['PUT'])
def receive_data():
    return server.receive_data()


@app.route('/get_image', methods=['GET'])
def get_flower_image():
    global get_cnt
    # print("Get request:", get_cnt)
    task_id = int(request.args.get('taskID'))
    # result = task_executor.images.pop(task_id, None)
    result = task_executor.images.get(task_id, None)
    # print(f"Get request: {task_id}", type(task_id), task_id in task_executor.images, result==None)
    if result == None:
        return jsonify({'task_status': False, 'image_bytes': None})
    else:
        get_cnt += 1
        print("Image_cnt", get_cnt, task_id)
        return jsonify({'task_status': True, 'image_bytes': base64.b64encode(result).decode('utf-8')})


if __name__ == '__main__':
    app.run(host='localhost', port=5000)
    # img_path = './flower_generator/motion_server/image_1.png'
    # img = load_image(img_path)
    # img = remove_bg(img, bg_color = (255, 255, 255, 0))
    # img.save("./flower_generator/motion_server/image_1_res.png")

