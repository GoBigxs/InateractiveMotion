import json, yaml
import time
import io
from PIL import Image

from rembg import remove, new_session


def save_json(data, filename):
    with open(filename, 'w') as f:
        json.dump(data, f, indent=4)

def load_json(filename):
    with open(filename, 'r') as f:
        lines = json.load(f)

    return lines

def load_yaml(config_file):
    try:
        with open(config_file, 'r') as file:
            config = yaml.safe_load(file)
        return config
    except yaml.YAMLError as error:
        print(f"Error parsing YAML file: {error}")


def get_transparent_img_bytes(size=(50,50)):
    image = Image.new("RGBA", size, (0, 0, 0, 0))

    # Convert the image to bytes
    img_byte_arr = io.BytesIO()
    image.save(img_byte_arr, format='PNG')
    img_bytes = img_byte_arr.getvalue()

    return img_bytes


def remove_bg(input_img, bg_color = (255, 255, 255, 255), model_name="isnet-general-use",save_path=None):
    if isinstance(input_img, str):
        input = Image.open(input_img)
    else:
        input = input_img
    session = new_session(model_name)
    output = remove(input, session=session, bgcolor=bg_color, alpha_matting=True, alpha_matting_foreground_threshold=270,alpha_matting_background_threshold=20, alpha_matting_erode_size=11)
    # output = output.convert('RGB')
    if save_path:
        output.save(save_path)
    return output


def save_points_to_file(points, filename="points_data.json"):
    # Read the existing data from the file, if the file exists
    try:
        with open(filename, 'r') as file:
            existing_data = json.load(file)
    except (FileNotFoundError, json.JSONDecodeError):
        existing_data = []

    # Append the new points to the existing data
    existing_data.append(points)

    # Write the updated data back to the file
    with open(filename, 'w') as file:
        json.dump(existing_data, file, indent=4)




class timer:
    def __init__(self, method_name="timed process"):
        self.method = method_name

    def __enter__(self):
        self.start = time.time()
        print(f"{self.method} starts")

    def __exit__(self, exc_type, exc_val, exc_tb):
        end = time.time()
        print(f"{self.method} took {str(round(end - self.start, 2))}s")