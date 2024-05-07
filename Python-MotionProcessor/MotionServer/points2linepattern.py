from PIL import Image, ImageDraw, ImageEnhance
from pathlib import Path
import numpy as np
import random, math
import io, os
import cv2
from scipy.interpolate import CubicSpline, splprep, splev
import cairo

from utils_flower import load_json, timer
from points2image_utils import (draw_base_flower, draw_pattern_on_image, image2bytes, scale_and_center_points,
                          find_smallest_square_and_center_points, enhance_image,
                          convert_image_format)
import draw_pattern

from diffusion_pipe import MidasDepth, DiffusionGenerator
from diffusers.utils import load_image, make_image_grid



def moving_average(points, window_size=100):
    """
    Perform moving average on the input list of points to smooth the line.
    """
    if len(points) < 6:
        return points
    
    window_size = min(round(len(points)*0.2), window_size)
    smoothed_points = []
    for i in range(len(points)):
        # Calculate the average of points in the window
        avg_x = sum(p[0] for p in points[max(0, i - window_size + 1):i + 1]) / min(i + 1, window_size)
        avg_y = sum(p[1] for p in points[max(0, i - window_size + 1):i + 1]) / min(i + 1, window_size)
        smoothed_points.append((avg_x, avg_y))
    return smoothed_points


def filter_points(points, min_distance=3):
    """
    Filter points based on minimum distance between consecutive points.
    """
    filtered_points = [points[0]]  # Add the first point
    for i in range(1, len(points)):
        # Calculate distance between consecutive points
        dist = np.linalg.norm(np.array(points[i]) - np.array(filtered_points[-1]))
        if dist > min_distance:
            filtered_points.append(points[i])
    return filtered_points


def interpolate_points(points, min_required=4):
    """
    Ensure the list of points has at least `min_required` elements by interpolating
    between existing points.
    """
    if len(points) >= min_required:
        # If there are enough points, return them as-is
        return points
    
    # Calculate the number of additional points needed
    additional_points_needed = min_required - len(points)
    
    # Generate the extra points via interpolation
    interpolated_points = []
    
    # Add the original points to the new list
    interpolated_points.extend(points)
    
    # If there is only one point, duplicate it to meet the requirement
    if len(points) == 1:
        for _ in range(additional_points_needed):
            interpolated_points.append(points[0])
        return interpolated_points
    
    # If there are two points, linearly interpolate to create extra points
    elif len(points) == 2:
        point1, point2 = points
        for i in range(1, additional_points_needed + 1):
            t = i / (additional_points_needed + 1)
            new_x = (1 - t) * point1[0] + t * point2[0]
            new_y = (1 - t) * point1[1] + t * point2[1]
            interpolated_points.insert(1, (new_x, new_y))
    
    # If there are three points, interpolate to add a new point between them
    elif len(points) == 3:
        point1, point2, point3 = points
        # Interpolate between point1 and point2
        t = 0.5  # Midpoint
        mid_x = (1 - t) * point1[0] + t * point2[0]
        mid_y = (1 - t) * point1[1] + t * point2[1]
        interpolated_points.insert(1, (mid_x, mid_y))
    
    return interpolated_points


def smooth_image(input_image, method='gaussian', kernel_size=5, bilateral_d=9, bilateral_sigma=75):
    """
    Smooth an image using OpenCV, either by Gaussian Blur or Bilateral Filter.
    
    :param input_image: A filepath to an image or a PIL.Image object.
    :param method: Smoothing method ('gaussian' or 'bilateral').
    :param kernel_size: Kernel size for Gaussian Blur.
    :param bilateral_d: Diameter of each pixel neighborhood for Bilateral Filter.
    :param bilateral_sigma: Sigma values for Bilateral Filter.
    :return: Smoothed image in OpenCV format.
    """
    # Load the image if a filename is provided
    if isinstance(input_image, str):
        image = cv2.imread(input_image)
    elif isinstance(input_image, Image.Image):
        # Convert PIL Image to OpenCV format
        image = np.array(input_image) 
        # Convert RGB to BGR 
        image = image[:, :, ::-1].copy() 
    
    # Apply smoothing
    if method == 'gaussian':
        smoothed_image = cv2.GaussianBlur(image, (kernel_size, kernel_size), 0)
    elif method == 'bilateral':
        smoothed_image = cv2.bilateralFilter(image, bilateral_d, bilateral_sigma, bilateral_sigma)
    else:
        raise ValueError("Unsupported smoothing method.")

    return smoothed_image





def catmull_rom_to_bezier(p0, p1, p2, p3):
    # Calculate control points for a cubic Bézier curve equivalent to the Catmull-Rom segment
    c1 = p1
    c2 = (-p0 + 6*p1 + p2) / 6
    c3 = (p1 + 6*p2 - p3) / 6
    c4 = p2
    return c1, c2, c3, c4


def draw_petal2(ctx, x, y):
    """
    Draws a petal with random size and orientation at the specified position.
    """
    # Randomly adjust the size and rotation for a more natural look
    size = random.uniform(60, 90)  # Adjust the size range as needed
    angle = random.uniform(0, 2*math.pi)
    
    ctx.save()
    ctx.translate(x, y)
    ctx.rotate(angle)
    ctx.scale(size, size)
    ctx.move_to(0, 0)
    ctx.curve_to(-0.2, -0.2, -0.5, -0.5, 0, -1)
    ctx.curve_to(0.5, -0.5, 0.2, -0.2, 0, 0)
    ctx.set_source_rgb(1, random.uniform(0.4, 0.6), random.uniform(0.4, 0.6))  # Vary color slightly
    ctx.fill()
    ctx.restore()





def draw_smooth_curve_with_random_petal(points, scale=False, width=1024, height=1024, line_width=7, 
                                        draw_func1=draw_petal2, min_interval=0.1, max_interval=0.3,
                                        draw_func2=draw_petal2, min_interval2=0.05, max_interval2=0.2,
                                        size_variation=0.2):
    if len(points) < 4:
        # print("Need at least 4 points to draw a smooth curve")
        points = interpolate_points(points)
        return

    if scale:
        points = moving_average(points)
        points = filter_points(points)
        points, square_size, center_x, center_y = find_smallest_square_and_center_points(points)
        points, _ = scale_and_center_points(np.array(points), square_size)
        print(f"Images scaled to size 1024. Original size: {square_size}")

    point_len = len(points)
    min_interval = max(1,math.floor(min_interval*point_len))
    max_interval = max(min_interval+1, math.floor(max_interval*point_len))
    min_interval2 = max(1,math.floor(min_interval2*point_len))
    max_interval2 = max(min_interval2+1, min_interval2)
    

    surface = cairo.ImageSurface(cairo.FORMAT_ARGB32, width, height)
    ctx = cairo.Context(surface)

    # Set background to white
    ctx.set_source_rgb(1, 1, 1)  # White
    ctx.paint()

    points = np.array(points, dtype=float)
    ctx.move_to(*points[1])

    # Store petal positions for later drawing
    petal_positions = []
    petal2_positions = []

    # Initial random interval for the first petal
    next_petal_at = random.randint(min_interval, max_interval)
    next_petal2_at = random.randint(min_interval2, max_interval2)




    for i in range(1, point_len - 2):
        p0, p1, p2, p3 = points[i-1], points[i], points[i+1], points[i+2]
        c1, c2, c3, c4 = catmull_rom_to_bezier(p0, p1, p2, p3)
        ctx.curve_to(*c2, *c3, *c4)

        if i == 1:
            start_point = c2
        if i == len(points) - 3:
            end_point = c4

        # Check if it's time to draw a petal
        if i == next_petal_at and i != 1 and i != len(points) - 3:
            petal_positions.append((p1, p2))  # Store both points for variation
            # Set the interval for the next petal
            next_petal_at += random.randint(min_interval, max_interval)

        if i == next_petal2_at:
            petal2_positions.append((p1, p2))
            next_petal2_at += random.randint(min_interval2, max_interval2)

    # Draw the curve
    ctx.set_source_rgb(0, 0, 0)  # Black for the curve
    ctx.set_line_width(line_width)
    ctx.stroke()

    # Draw petals at stored positions with random variations
    for pos in petal_positions:
        for _ in range(random.randint(1, 2)):  # Draw 1-3 petals at each position for overlap
            # draw_petal2(ctx, *pos[random.randint(0, 1)])  # Choose randomly between the two points
            if draw_func1.__name__ == "draw_neiles_parabola":
                draw_func1(ctx, *pos[random.randint(0, 1)], radius_variation=size_variation)
            else:
                rotation_angle = random.uniform(0, 2 * math.pi)
                draw_func1(ctx, *pos[random.randint(0, 1)], radius_variation=size_variation, rotation_angle=rotation_angle)
               

    for pos in petal2_positions:
        for _ in range(random.randint(1, 3)):
            draw_func2(ctx, *pos[random.randint(0, 1)])

    # Draw petals at the start/end based on the smaller y-coordinate    
    shape_point = start_point if start_point[1] < end_point[1] else end_point
    for _ in range(random.randint(2, 5)):
        # draw_petal2(ctx, *shape_point) 
        if draw_func1.__name__ == "draw_neiles_parabola":
            draw_func1(ctx, *shape_point, radius_variation=size_variation)
        else:
            rotation_angle = random.uniform(0, 2 * math.pi)
            draw_func1(ctx, *shape_point, radius_variation=size_variation, rotation_angle=rotation_angle)

    # Convert Cairo surface into PIL Image
    data = surface.get_data()
    image = Image.frombuffer("RGBA", (width, height), data, "raw", "BGRA", 0, 1)
    image = image.convert("RGB")
    print("Flower:", len(petal_positions))
    print("Leaf:", len(petal2_positions))

    return image


def flower_pattern_selector(json_data):
    """
    Randomly selects a pair of func_name and flower_name from the same section in the provided JSON data.

    Args:
    data : JSON formatted data with the pre-defined flower patterns from the json file (flower_pattern.json)

    Returns:
    tuple: A tuple containing the selected function and flower name.
    """

    pattern = random.choice(json_data['patterns'])
    func_name = random.choice(pattern['func_names'])
    flower_name = random.choice(pattern['flowers'])

    draw_func = getattr(draw_pattern, func_name, draw_petal2)

    return (draw_func, flower_name)
    

if __name__ == '__main__':
    midasDepth = MidasDepth()
    config_file = "./flower_generator/motion_server/diffusion_config.yaml"
    diffusion_generator = DiffusionGenerator(config_file, midasDepth)

    input_img = load_image("./flower_generator/line_circle_test/twopplWS0311/line_img/img_1.png")
    line_file = "./flower_generator/motion_server/task_points_goodLines2WSID477Mar21.json"

    patterns = load_json("./flower_generator/motion_server/flower_pattern.json")

    lines = load_json(line_file)
    line_ids = [1, 8, 10, 13]

    output_dir = "./flower_generator/line_circle_test/twopplWS0311/draw_pattern_test"
    Path(output_dir).mkdir(parents=True, exist_ok=True)

    for pattern in patterns["patterns"]:
        pattern_dir = os.path.join(output_dir, str(pattern["id"]))
        Path(pattern_dir).mkdir(parents=True, exist_ok=True)

        for func_name in pattern["func_names"]:
            draw_func1 = getattr(draw_pattern, func_name, draw_petal2)
            draw_func2 = draw_petal2
            
            func_dir = os.path.join(pattern_dir, func_name)
            Path(func_dir).mkdir(parents=True, exist_ok=True)

            
            for flower_name in pattern["flowers"]:
                all_imgs = []
                img_filename = f"{flower_name.replace(', ', '_').replace(' ', '_')}.png"
                img_path = os.path.join(func_dir, img_filename)

                for line_id in line_ids:
                    points = lines[line_id]
                    prompt = f"Vintage oil painting style, {flower_name} flowers, white background, detailed"

                    avg_pts = moving_average(points)
                    filtered_pts = filter_points(avg_pts)
                    print(f"Line {line_id}:", len(points), len(filtered_pts))

                    points, square_size, center_x, center_y = find_smallest_square_and_center_points(points)
                    points, _ = scale_and_center_points(np.array(points), square_size)

                    points2, square_size, center_x, center_y = find_smallest_square_and_center_points(filtered_pts)
                    points2, _ = scale_and_center_points(np.array(points2), square_size)

                    # Original Line
                    img = draw_pattern_on_image(points)

                    # Smoothed Line with added patterns
                    img2 = draw_smooth_curve_with_random_petal(points2, draw_func1=draw_func1, draw_func2=draw_func2)

                    # Generated image
                    sd_img, depth_img = diffusion_generator.infer(img2, prompt=prompt, output_depth=True)

                    grid = make_image_grid([img, img2, depth_img, sd_img], rows=1, cols=4)
                    all_imgs.append(grid)
                
                all_grids = make_image_grid(all_imgs, rows=len(line_ids), cols=1)
                all_grids.save(img_path)






    """
    points = lines[13]

    avg_pts = moving_average(points)
    filtered_pts = filter_points(avg_pts)
    print(len(points), len(filtered_pts))

    points, square_size, center_x, center_y = find_smallest_square_and_center_points(points)
    points, _ = scale_and_center_points(np.array(points), square_size)

    points2, square_size, center_x, center_y = find_smallest_square_and_center_points(filtered_pts)
    points2, _ = scale_and_center_points(np.array(points2), square_size)

    draw_func1 = draw_pattern.draw_simple_flower
    draw_func2 = draw_petal2
    # img1 = draw_smooth_curve_with_random_petal(points, draw_func1=draw_func1, draw_func2=draw_func2)
    img2 = draw_smooth_curve_with_random_petal(points2, draw_func1=draw_func1, draw_func2=draw_func2)
    img = draw_pattern_on_image(points)

    from diffusion_pipe import MidasDepth, DiffusionGenerator
    midasDepth = MidasDepth()
    config_file = "./flower_generator/motion_server/diffusion_config.yaml"
    diffusion_generator = DiffusionGenerator(config_file, midasDepth)

    # depth_img = midasDepth.get_depth_image(image)
    sd_img, depth_img = diffusion_generator.infer(img2, output_depth=True)

    grid = make_image_grid([img, depth_img, img2, sd_img], rows=2, cols=2)

    img2.save(f"./flower_generator/line_circle_test/twopplWS0311/draw_pattern/draw_img_0411/img_{draw_func1.__name__}.png")
    grid.save(f"./flower_generator/line_circle_test/twopplWS0311/draw_pattern/grid_img_0411/img_{draw_func1.__name__}.png")
    """