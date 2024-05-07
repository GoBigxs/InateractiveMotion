from PIL import Image, ImageDraw, ImageEnhance
import numpy as np
import random 
import math
import io
import cv2
from scipy.interpolate import CubicSpline

from utils_flower import load_json, timer
from diffusion_pipe import MidasDepth, DiffusionGenerator

from diffusers.utils import load_image

def find_smallest_square_and_center_points(points):
    points = np.array(points)
    
    min_x, min_y = np.min(points, axis=0)
    max_x, max_y = np.max(points, axis=0)
    
    width = max_x - min_x
    height = max_y - min_y
    
    margin_percentage = 0.1
    margin = max(width, height) * margin_percentage
    side_length = max(width, height) + 2 * margin
    
    # Find the center of the bounding box
    center_x = min_x + width / 2
    center_y = min_y + height / 2
    
    # Find the center of the square
    square_center = side_length / 2
    
    # Center the points
    centered_points = points - [center_x, center_y] + [square_center, square_center]

    
    return centered_points, side_length, center_x, center_y

def scale_and_center_points(points, original_square_size, target_size=1024):
    scale_factor = target_size / original_square_size
    scaled_points = points * scale_factor
    # Center the points in the target image size
    offset = (target_size - (original_square_size * scale_factor)) / 2
    scaled_and_centered_points = scaled_points + offset

    # Invert y-axis
    scaled_and_centered_points[:, 1] = target_size - scaled_and_centered_points[:, 1] - offset * 2

    return scaled_and_centered_points, scale_factor

def draw_pattern_on_image(points, image_size=1024, brush_width=10):
    """
    Draw the pattern defined by `points` on a PIL image.
    """
    # Create a blank image with a white background
    image = Image.new("RGB", (image_size, image_size), (255, 255, 255))
    draw = ImageDraw.Draw(image)

    # centered_points, square_size, center_x, center_y = find_smallest_square_and_center_points(points)
    # scaled_points, _ = scale_and_center_points(np.array(centered_points), square_size)


    # # Draw lines between points
    for i in range(len(points) - 1):
        draw.line([tuple(points[i]), tuple(points[i+1])], fill="red", width=brush_width, joint="curve")

    # for i in range(len(points2) - 1):
    #     draw.line([tuple(points2[i]), tuple(points2[i+1])], fill="green", width=brush_width, joint="curve")


    return image


def get_line_direction(points, threshold=0.25):
    min_x, max_x = min(points, key=lambda x: x[0])[0], max(points, key=lambda x: x[0])[0]
    min_y, max_y = min(points, key=lambda x: x[1])[1], max(points, key=lambda x: x[1])[1]
    width = max_x - min_x
    height = max_y - min_y
    
    ratio = height / width if width != 0 else float('inf')

    if ratio > threshold:
        return "vertical"
    else:
        return "horizontal"

def convert_image_format(image):
    """
    Convert an image between CV2 and PIL formats automatically based on the input type.
    
    :param image: An image either in OpenCV (NumPy array) or PIL format.
    :return: The converted image, in PIL format if input was CV2, and vice versa.
    """
    if isinstance(image, np.ndarray):
        # The input is an OpenCV image, so convert to PIL
        # Convert from BGR to RGB
        rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        # Convert to PIL Image
        return Image.fromarray(rgb_image)
    elif isinstance(image, Image.Image):
        # The input is a PIL image, so convert to OpenCV
        # Convert from RGB to BGR
        bgr_image = cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR)
        return bgr_image
    else:
        raise TypeError("Unsupported input type. The image must be either a PIL Image or an OpenCV image.")
    

def smooth_line(points):
    """
    Perform spline interpolation on the input list of points to smooth the line.
    """
    # Convert points to numpy array
    points_array = np.array(points)
    
    # Perform cubic spline interpolation
    t = np.arange(len(points))
    cs_x = CubicSpline(t, points_array[:, 0])
    cs_y = CubicSpline(t, points_array[:, 1])
    
    # Interpolate points along the spline
    num_interpolated_points = len(points) * 10
    interpolated_points = np.column_stack((cs_x(np.linspace(0, len(points) - 1, num_interpolated_points)),
                                           cs_y(np.linspace(0, len(points) - 1, num_interpolated_points))))
    
    # Convert interpolated points back to list of tuples
    smoothed_points = [tuple(point) for point in interpolated_points]
    
    return smoothed_points





def draw_flower_like_shapes(points, image_size=1024, brush_width=7, scale=1.5):
    """
    Draw the pattern defined by `points` on an image with a transparent background,
    placing flower-like shapes with a more organic appearance at intervals along the line.
    """
    # Create a blank image with a transparent background ('RGBA' mode for transparency)
    image = Image.new("RGB", (image_size, image_size), (255, 255, 255))
    draw = ImageDraw.Draw(image)

    # Get line direction
    line_dir = get_line_direction(points)
    draw_dir = random.choice(['R', 'L'])

    # Parameters to control the appearance of the flowers
    min_petals, max_petals = 1, 3
    min_radius, max_radius = round(35*scale), round(50*scale)
    brush_width = round(brush_width * scale)

    min_points_between_shapes = 25
    max_points_between_shapes = 35
    next_shape_at = random.randint(min_points_between_shapes, max_points_between_shapes)

    points_since_last_shape = random.randint(2, len(points))

    for i in range(len(points) - 1):
        draw.line([tuple(points[i]), tuple(points[i+1])], fill="black", width=brush_width, joint="curve")
        points_since_last_shape += 1

        if (line_dir == 'horizontal' and draw_dir == 'L' and points[i+1][0] < 700) or \
            (line_dir == 'horizontal' and draw_dir == 'R' and points[i+1][0] > 350) or \
            (line_dir == 'vertical' and points[i+1][1] < 700):
            if points_since_last_shape >= next_shape_at:
                points_since_last_shape = 0
                next_shape_at = random.randint(min_points_between_shapes, max_points_between_shapes)

                center_point = points[i+1]
                petals = random.randint(min_petals, max_petals)

                for _ in range(petals):
                    angle = random.uniform(0, 2 * math.pi)
                    radius = random.randint(min_radius, max_radius)
                    offset = random.uniform(0.8, 1.2)  # Offset for center of each petal to create irregularity
                    
                    # Calculate petal's center point based on angle and offset
                    petal_center = (
                        center_point[0] + offset * radius * math.cos(angle),
                        center_point[1] + offset * radius * math.sin(angle)
                    )

                    # Draw each petal as an ellipse with random size and orientation
                    draw.ellipse([
                        petal_center[0] - radius, petal_center[1] - radius,
                        petal_center[0] + radius, petal_center[1] + radius
                    ], fill="black", outline="red")

    return image



def points2lines(data, max_untouch=5):
    """_summary_
    Turn received data from unity to seperate lines
    Return: list of lines, line: list of 2D points
    """
    lines = []  # List to hold all lines
    current_line = []  # Current line being processed
    false_count = 0  # Counter for consecutive falses

    for point in data:
        if point['penTouchingPaper']:
            # If the pen is touching the paper, reset false counter
            false_count = 0
            
            # Append start and end points to the current line
            current_line.append((point['startX'], point['startY']))
            current_line.append((point['endX'], point['endY']))
        else:
            false_count += 1
            # If 5 consecutive points with penTouchingPaper False, start a new line
            if false_count == max_untouch:
                if current_line:  # If the current line is not empty
                    lines.append(current_line)  # Save the finished line
                    current_line = []  # Start a new line
                false_count = 0  # Reset false count after starting a new line

    # Check if there's an unfinished line when data ends
    if current_line:  # If the last line is not empty
        lines.append(current_line)  # Append the last line to the list of lines

    return lines


def draw_base_flower(points, resize=True, byte_format=True):
    centered_points, square_size, center_x, center_y = find_smallest_square_and_center_points(points)
    scaled_points, _ = scale_and_center_points(np.array(centered_points), square_size)
    image = draw_flower_like_shapes(scaled_points)
    square_size = round(square_size)
    
    if resize:
        image = image.resize((square_size, square_size))

    if not byte_format:
        return image, square_size, center_x, center_y
    else:
        with io.BytesIO() as img_byte_arr:
            image.save(img_byte_arr, format='PNG')  # You can choose format as per your requirement (e.g., 'JPEG')
            img_bytes = img_byte_arr.getvalue()

        return img_bytes, square_size, center_x, center_y
    
def image2bytes(image):
    with io.BytesIO() as img_byte_arr:
        image.save(img_byte_arr, format='PNG')
        img_bytes = img_byte_arr.getvalue()
    return img_bytes



def enhance_image(input_image, brightness_factor=1.5, saturation_factor=1.5):
    """
    Enhance the brightness and saturation of a PIL Image.

    Parameters:
    - input_image: PIL.Image object. The image to be enhanced.
    - brightness_factor: float. The factor by which to enhance the brightness. 
      Values > 1 will increase brightness, values < 1 will decrease.
    - saturation_factor: float. The factor by which to enhance the saturation. 
      Values > 1 will increase saturation, values < 1 will decrease.

    Returns:
    - PIL.Image object. The enhanced image.
    """
    # Enhance brightness
    enhancer = ImageEnhance.Brightness(input_image)
    brighter_image = enhancer.enhance(brightness_factor)
    
    # Enhance saturation
    enhancer = ImageEnhance.Color(brighter_image)
    enhanced_image = enhancer.enhance(saturation_factor)
    
    return enhanced_image


    

if __name__ == '__main__':
    input_img = load_image("./flower_generator/line_circle_test/twopplWS0311/line_img/img_1.png")

    smoothed_img = smooth_jagged_lines_opencv(input_img)
    smoothed_img = convert_image_format(smoothed_img)
    smoothed_img.save("./flower_generator/line_circle_test/twopplWS0311/line_img/img_1_smoothed.png")

    '''
    midasDepth = MidasDepth()
    config_file = "./flower_generator/motion_server/diffusion_config.yaml"
    diffusion_generator = DiffusionGenerator(config_file, midasDepth)

    with timer("Points-to-image"):
        data = load_json("./flower_generator/motion_server/Data_Unity/3linesRWSID720_2.txt")

        lines = points2lines(data)
        print(len(lines))

        for i in range(len(lines)):
            points = np.array(lines[0])
        

            # centered_points, square_size, _, _ = find_smallest_square_and_center_points(points)

            # # Scale and center points within the target 1024x1024 image size
            # scaled_points, _ = scale_and_center_points(np.array(centered_points), square_size)

            # # Draw the pattern on the image
            # image = draw_flower_like_shapes(scaled_points)
            # # image = add_leaves_around_line(scaled_points)

            # # If you need the image as a NumPy array
            # image_array = np.array(image)
            image, _, _, _ = draw_base_flower(points, resize=False, byte_format=False)
            res = diffusion_generator.infer(image)
            res.save(f"./flower_generator/motion_server/sdxl_imgs/image_{i}.png")


            # Show the image (if running in an environment that supports displaying images)
            # image.show()
    '''

