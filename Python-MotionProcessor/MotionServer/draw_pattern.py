import numpy as np
import random 
import math
import cairo


def draw_simple_flower(ctx, x, y, petal_count=6, radius=50, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    ctx.set_source_rgb(1, 1, 0)  # Yellow center
    ctx.arc(x, y, radius / 4, 0, 2 * math.pi)
    ctx.fill()

    ctx.set_source_rgb(1, 0, 0)  # Red petals

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))

    for i in range(petal_count):
        angle = (2 * math.pi / petal_count) * i
        ctx.save()
        ctx.translate(x, y)
        ctx.rotate(angle)
        ctx.scale(radius, radius)
        ctx.move_to(0, 0)
        ctx.curve_to(-0.2, -0.2, -0.5, -0.5, 0, -1)
        ctx.curve_to(0.5, -0.5, 0.2, -0.2, 0, 0)
        ctx.fill()
        ctx.restore()

    ctx.restore()

def draw_spiral(ctx, x, y, turns=4, radius=50, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    ctx.set_source_rgb(0, 0, 1)  # Blue spiral
    ctx.move_to(x, y)

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))

    for i in range(turns * 360):
        angle = math.radians(i)
        r = radius * (i / (turns * 360))
        dx = r * math.cos(angle)
        dy = r * math.sin(angle)
        ctx.line_to(x + dx, y + dy)
    ctx.stroke()
    ctx.restore()

def draw_overlapping_petals(ctx, x, y, petal_count=8, radius=50, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    ctx.set_source_rgb(0.9, 0.1, 0.5)  # Pink petals

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))

    for i in range(petal_count):
        angle = (2 * math.pi / petal_count) * i
        ctx.save()
        ctx.translate(x, y)
        ctx.rotate(angle)
        ctx.scale(radius * 1.5, radius)
        ctx.move_to(0, 0)
        ctx.curve_to(-0.3, -0.3, -0.6, -0.6, 0, -1)
        ctx.curve_to(0.6, -0.6, 0.3, -0.3, 0, 0)
        ctx.fill()
        ctx.restore()
    ctx.restore()

def draw_radial_symmetry_flower(ctx, x, y, layer_count=5, petal_count=12, radius=20, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))


    for layer in range(1, layer_count + 1):
        for i in range(petal_count):
            angle = (2 * math.pi / petal_count) * i
            ctx.set_source_rgb(random.random(), random.random(), random.random())  # Random color
            ctx.save()
            ctx.translate(x, y)
            ctx.rotate(angle)
            ctx.scale(radius * layer, radius * layer)
            ctx.move_to(0, 0)
            ctx.curve_to(-0.1, -0.1, -0.25, -0.25, 0, -0.5)
            ctx.curve_to(0.25, -0.25, 0.1, -0.1, 0, 0)
            ctx.fill()
            ctx.restore()
    ctx.restore()

def draw_rotated_layers_flower(ctx, x, y, layer_count=3, petal_count=8, radius=30, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))

    for layer in range(layer_count):
        angle_offset = (2 * math.pi / petal_count) * layer / layer_count
        for i in range(petal_count):
            angle = (2 * math.pi / petal_count) * i + angle_offset
            ctx.set_source_rgb(0.5, 0.8, 0.1)  # Light green
            ctx.save()
            ctx.translate(x, y)
            ctx.rotate(angle)
            ctx.scale(radius * (layer + 1), radius * (layer + 1))
            ctx.move_to(0, 0)
            ctx.curve_to(-0.2, -0.2, -0.4, -0.4, 0, -0.7)
            ctx.curve_to(0.4, -0.4, 0.2, -0.2, 0, 0)
            ctx.fill()
            ctx.restore()
    ctx.restore()

def draw_flower_with_central_pattern(ctx, x, y, inner_circle_radius=15, outer_circle_count=10, outer_circle_radius=5, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)
    
    ctx.set_source_rgb(0.7, 0.7, 0.2)  # Yellowish
    ctx.arc(x, y, inner_circle_radius, 0, 2 * math.pi)
    ctx.fill()
    
    ctx.set_source_rgb(0.5, 0.3, 0.7)  # Purplish

    offset = random.uniform(1-radius_variation, 1+radius_variation)
    inner_circle_radius *= offset  
    outer_circle_radius *= offset

    for i in range(outer_circle_count):
        angle = (2 * math.pi / outer_circle_count) * i
        dx = (inner_circle_radius + outer_circle_radius) * math.cos(angle)
        dy = (inner_circle_radius + outer_circle_radius) * math.sin(angle)
        ctx.arc(x + dx, y + dy, outer_circle_radius, 0, 2 * math.pi)
        ctx.fill()
    ctx.restore()

def draw_nested_spirals(ctx, x, y, spiral_count=3, turns=5, radius=150, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))

    for spiral in range(spiral_count):
        radius_step = radius / (turns * 360 * spiral_count)
        radius_inner = radius_step * spiral
        ctx.set_source_rgb(0.2, 0.2 + 0.15 * spiral, 1 - 0.15 * spiral)  # Gradient blue
        ctx.move_to(x, y)
        for i in range(turns * 360):
            angle = math.radians(i)
            radius_inner += radius_step
            dx = radius_inner * math.cos(angle)
            dy = radius_inner * math.sin(angle)
            ctx.line_to(x + dx, y + dy)
        ctx.stroke()
    ctx.restore()
    
def draw_flower_with_petals_and_stems(ctx, x, y, petal_count=12, stem_length=100, petal_length=50, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    ctx.set_source_rgb(0, 0.5, 0)  # Green for stem
    ctx.move_to(x, y)
    # ctx.line_to(x, y + stem_length)
    ctx.stroke()
    
    ctx.set_source_rgb(1, 0.5, 0.5)  # Light red/pink for petals

    offset = random.uniform(1-radius_variation, 1+radius_variation)
    stem_length *= offset  
    petal_length *= offset


    for i in range(petal_count):
        angle = (2 * math.pi / petal_count) * i
        ctx.save()
        ctx.translate(x, y)
        ctx.rotate(angle)
        ctx.move_to(0, 0)
        ctx.line_to(0, -petal_length)
        ctx.stroke()
        ctx.restore()
    ctx.restore()

def draw_concentric_flowers(ctx, x, y, flower_count=2, base_petal_count=6, base_radius=30, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    base_radius = random.uniform(base_radius * (1 - radius_variation), base_radius * (1 + radius_variation))


    for flower in range(flower_count):
        petal_count = base_petal_count + flower * 4
        radius = base_radius * (flower + 1)
        ctx.set_source_rgb(1 - 0.2 * flower, 0.1 * flower, 0.5 + 0.1 * flower)
        for i in range(petal_count):
            angle = (2 * math.pi / petal_count) * i
            ctx.save()
            ctx.translate(x, y)
            ctx.rotate(angle)
            ctx.scale(radius, radius)
            ctx.move_to(0, 0)
            ctx.curve_to(-0.1, -0.1, -0.25, -0.25, 0, -0.5)
            ctx.curve_to(0.25, -0.25, 0.1, -0.1, 0, 0)
            ctx.fill()
            ctx.restore()
    ctx.restore()


def draw_flower_with_alternating_petal_colors(ctx, x, y, petal_count=8, radius=50, rotation_angle=0, radius_variation=0):
    ctx.save()  # Save the context state before applying the overall rotation
    ctx.translate(x, y)  # Move to the center of the pattern
    ctx.rotate(rotation_angle)  # Rotate the whole pattern by the given angle
    ctx.translate(-x, -y)  # Move back to ensure rotation around the center (x, y)

    colors = [(1, 0, 0), (0, 1, 0)]  # Red and Green

    radius = random.uniform(radius * (1 - radius_variation), radius * (1 + radius_variation))

    for i in range(petal_count):
        ctx.set_source_rgb(*colors[i % 2])
        angle = (2 * math.pi / petal_count) * i
        ctx.save()
        ctx.translate(x, y)
        ctx.rotate(angle)
        ctx.scale(radius, radius)
        ctx.move_to(0, 0)
        ctx.curve_to(-0.2, -0.2, -0.5, -0.5, 0, -1)
        ctx.curve_to(0.5, -0.5, 0.2, -0.2, 0, 0)
        ctx.fill()
        ctx.restore()
    ctx.restore()
    


def draw_neiles_parabola(ctx, center_x, center_y, x_scale=45, y_scale=15, steps=100, rotation_angle=-0.5, petal_count=1, radius_variation=0):
    magnitude_range = math.pi / 10

    offset = random.uniform(1-radius_variation, 1+radius_variation)
    x_scale *= offset
    y_scale *= offset

    
    for i in range(petal_count):
        adjustment = random.uniform(-magnitude_range, magnitude_range)

        rotation_angle = rotation_angle + adjustment
        ctx.save()
        ctx.translate(center_x, center_y)
        ctx.rotate(rotation_angle)
        ctx.translate(-center_x, -center_y)

        # Set up fill color for the parabola
        ctx.set_source_rgb(0.5, 0.6, 0.9)  # Light blue fill
        ctx.set_line_width(2)

        start_x, end_x = 0, 2
        step_size = (end_x - start_x) / steps

        max_x_max_y = max_x_min_y = None

        ctx.new_path()

        # Plotting for positive y
        for i in range(steps + 1):
            x = start_x + (step_size * i)
            y = (x**3)**0.5
            x_scaled, y_scaled = center_x + x * x_scale, center_y - y * y_scale
            if i == steps:
                max_x_max_y = (x_scaled, y_scaled)
            if i == 0:
                ctx.move_to(x_scaled, y_scaled)
            else:
                ctx.line_to(x_scaled, y_scaled)

        # Plotting for negative y (mirror)
        for i in range(steps + 1):
            x = end_x - (step_size * i)
            y = -(x**3)**0.5
            x_scaled, y_scaled = center_x + x * x_scale, center_y - y * y_scale
            if i == 0:
                max_x_min_y = (x_scaled, y_scaled)
            ctx.line_to(x_scaled, y_scaled)

        ctx.close_path()
        ctx.fill()
        # ctx.restore()

        # Draw an ellipse between max_x_max_y and max_x_min_y
        if max_x_max_y and max_x_min_y:
            draw_water_drop_curve(ctx, max_x_max_y, max_x_min_y)
            # draw_ellipse_between_points(ctx, max_x_max_y, max_x_min_y, 0.4, angle=rotation_angle)  # Assuming ellipse_ratio as 0.5

        ctx.restore()

def draw_water_drop_curve(ctx, point1, point2):
    dx = point2[0] - point1[0]
    dy = point2[1] - point1[1]
    distance = math.sqrt(dx**2 + dy**2)
    local_angle = math.atan2(dy, dx)
 
    # Calculate center and radius of the ellipse
    center_x, center_y = (point1[0] + point2[0]) / 2, (point1[1] + point2[1]) / 2
    radius_y = distance / 2
    radius_x = distance / 2
 
    ctx.save()
    ctx.translate(center_x, center_y)
    ctx.rotate(local_angle)
    ctx.scale(radius_x, radius_y)
 
    # Draw the water drop shape
    ctx.new_path()
    ctx.move_to(1, 0)  # Switched x and y coordinates
    ctx.curve_to(0.5, 0.6, -0.4, 0.3, -1, 0)  # Switched x and y coordinates
    ctx.curve_to(-0.4, -0.3, 0.5, -0.6, 1, 0)  # Switched x and y coordinates
    ctx.close_path()
 
    # Set fill color for the water drop curve and fill it
    ctx.set_source_rgb(0.55, 0.65, 0.95)  # Blue color for the water drop curve
    ctx.fill()
    ctx.restore()

   


def draw_wavy_line(ctx, start_point, end_point, waves=1, amplitude=50):
    ctx.move_to(*start_point)
    
    # Calculate the total distance and angle between the points
    dx = end_point[0] - start_point[0]
    dy = end_point[1] - start_point[1]
    length = math.sqrt(dx**2 + dy**2)
    angle = math.atan2(dy, dx)
    
    # Calculate wave segment length
    segment_length = length / (waves * 2)
    
    for wave in range(waves):
        for part in range(2):
            # Calculate control and end points for each wave segment
            control_x = start_point[0] + math.cos(angle) * (segment_length * (1 + 2 * wave + part)) - math.sin(angle) * (amplitude if part % 2 == 0 else -amplitude)
            control_y = start_point[1] + math.sin(angle) * (segment_length * (1 + 2 * wave + part)) + math.cos(angle) * (amplitude if part % 2 == 0 else -amplitude)
            end_x = start_point[0] + math.cos(angle) * segment_length * (2 * wave + part + 1)
            end_y = start_point[1] + math.sin(angle) * segment_length * (2 * wave + part + 1)
            
            # Draw the quadratic Bezier curve for the segment
            ctx.curve_to(control_x, control_y, control_x, control_y, end_x, end_y)
    
    # Stroke the path
    ctx.stroke()


def draw_ellipse_between_points(ctx, point1, point2, ellipse_ratio=10, angle=0):
    dx = point2[0] - point1[0]
    dy = point2[1] - point1[1]
    distance = math.sqrt(dx**2 + dy**2)
    angle = math.atan2(dy, dx)

    # Calculate center and radius of the ellipse
    center_x, center_y = (point1[0] + point2[0]) / 2, (point1[1] + point2[1]) / 2
    radius_x, radius_y = distance / 2, distance / 2 * ellipse_ratio

    ctx.save()
    ctx.translate(center_x, center_y)
    ctx.rotate(angle)
    ctx.scale(radius_x, radius_y)
    ctx.arc(0, 0, 1, 0, 2 * math.pi)  # Draw unit circle, which will be scaled to an ellipse

    # Set fill color for the ellipse and fill it
    ctx.set_source_rgb(0.45, 0.55, 0.85)  # Red color for the ellipse
    ctx.fill()
    ctx.restore()

def draw_circle_between_points(ctx, point1, point2):
    # Calculate the distance between the points
    dx = point2[0] - point1[0]
    dy = point2[1] - point1[1]
    distance = math.sqrt(dx**2 + dy**2)
    
    # Calculate the midpoint
    midpoint = ((point1[0] + point2[0]) / 2, (point1[1] + point2[1]) / 2)
    
    # Save the current state
    ctx.save()
    
    # Move to the midpoint
    ctx.move_to(midpoint[0], midpoint[1])
    
    # Draw the circle
    ctx.arc(midpoint[0], midpoint[1], distance / 2, 0, 2 * math.pi)
    
    # # Restore the context and stroke
    # ctx.restore()
    # ctx.stroke()

    # Set fill color for the ellipse and fill it
    ctx.set_source_rgb(0.45, 0.55, 0.85)  # Red color for the ellipse
    ctx.fill()
    ctx.restore()
