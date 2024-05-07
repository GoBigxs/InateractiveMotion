import numpy as np
import torch
import cv2
from PIL import Image
import random
import time
import os

from diffusers import (StableDiffusionXLControlNetPipeline, ControlNetModel, AutoencoderKL, 
                       LCMScheduler, DDIMScheduler, TCDScheduler) #, EDMDPMSolverMultistepScheduler
from diffusers.utils import load_image
from transformers import pipeline, DPTImageProcessor, DPTForDepthEstimation
from huggingface_hub import hf_hub_download

from utils_flower import load_yaml, timer, remove_bg


class MidasDepth:
    def __init__(self, size=(1024,1024)):
        self.depth_estimator_midas = DPTForDepthEstimation.from_pretrained("Intel/dpt-hybrid-midas").to("cuda")
        self.feature_extractor_midas = DPTImageProcessor.from_pretrained("Intel/dpt-hybrid-midas")
        self.size = size

        black_img = Image.new("RGB", (1024, 1024), (0, 0, 0))
        _ = self.get_depth_image(black_img)

    def get_depth_image(self, image):
        image = self.feature_extractor_midas(images=image, return_tensors="pt").pixel_values.to("cuda")
        with torch.no_grad(), torch.autocast("cuda"):
            depth_map = self.depth_estimator_midas(image).predicted_depth

        depth_map = torch.nn.functional.interpolate(
            depth_map.unsqueeze(1),
            size=self.size,
            mode="bicubic",
            align_corners=False,
        )
        depth_min = torch.amin(depth_map, dim=[1, 2, 3], keepdim=True)
        depth_max = torch.amax(depth_map, dim=[1, 2, 3], keepdim=True)
        depth_map = (depth_map - depth_min) / (depth_max - depth_min)
        image = torch.cat([depth_map] * 3, dim=1)

        image = image.permute(0, 2, 3, 1).cpu().numpy()[0]
        image = Image.fromarray((image * 255.0).clip(0, 255).astype(np.uint8))
        return image


class DiffusionGenerator:
    def __init__(self, config_file, DepthExtractor):
        self.config = load_yaml(config_file)

        self.models = self.config["models"]
        self.pipe_param = self.config["pipe_param"]
        self.content = self.config["content"]
        self.scheduler_mapping = {
            "LCMScheduler": LCMScheduler,
            "DDIMScheduler": DDIMScheduler,
            "PNDMScheduler": TCDScheduler
        }

        self.depth_extractor = DepthExtractor

        self.pipe = self.build_pipeline()

        # Warm up
        # black_img = Image.new("RGB", (1024, 1024), (0, 0, 0))
        # _ = self.infer(black_img)
    

    def build_pipeline(self):
        controlnet = ControlNetModel.from_pretrained(self.models["control_net"], torch_dtype=torch.float16)
        vae = AutoencoderKL.from_pretrained(self.models["vae"], torch_dtype=torch.float16)
        pipe = StableDiffusionXLControlNetPipeline.from_pretrained(
                self.models["sd_model"], 
                vae = vae,
                controlnet=controlnet, 
                torch_dtype=torch.float16).to("cuda")
        
        if self.models["use_multi_loras"]:
            pipe.load_lora_weights(self.models["lora"], weight_name=self.models["lora_weight"], adapter_name=self.models["adapter_name"])
            pipe.load_lora_weights(self.models["lora_2"], weight_name=self.models["lora_weight_2"], adapter_name=self.models["adapter_name_2"])
            scheduler = self.scheduler_mapping.get(self.models["scheduler"])
            pipe.scheduler = scheduler.from_config(pipe.scheduler.config)
            pipe.set_adapters([self.models["adapter_name"], self.models["adapter_name_2"]], adapter_weights=[self.models["adapter_weight"], self.models["adapter_weight_2"]])
            pipe.fuse_lora(adapter_names=["lcm", "vintage"])
        else:
            if self.models["lora_weight"]:
                pipe.load_lora_weights(self.models["lora"], weight_name=self.models["lora_weight"])
            else:
                pipe.load_lora_weights(self.models["lora"])
            pipe.fuse_lora()

        return pipe


    def set_infer_params(self, prompt, negative_prompt, img, generator):
        params = {
            "prompt": prompt,
            "negative_prompt": negative_prompt,
            "image": img,
            "num_images_per_prompt": self.pipe_param["num_images_per_prompt"],
            "num_inference_steps": self.pipe_param["num_inference_steps"],
            "guidance_scale": self.pipe_param["guidance_scale"],
            "controlnet_conditioning_scale": self.pipe_param["controlnet_conditioning_scale"],
            "control_guidance_end": self.pipe_param["control_guidance_end"],
            "generator": generator
        }

        if "eta" in self.pipe_param and self.pipe_param["eta"] is not None:
            params["eta"] = self.pipe_param["eta"]

        if not self.models["use_multi_loras"]:
            params["cross_attention_kwargs"] = {"scale": self.pipe_param["lora_scale"]}

        return params
        
    
    
    @torch.inference_mode()
    def infer(self, image, prompt=None, negative_prompt=None, seed=None, output_depth=False):
        if not prompt:
            prompt = self.content["prompt"]
            negative_prompt = self.content["negative_prompt"]
        else:
            prompt = prompt
            if negative_prompt:
                negative_prompt = negative_prompt
            else:
                negative_prompt = self.content["negative_prompt"]


        seed = random.randrange(0, 2**63) if not seed else seed
        generator = torch.Generator("cuda").manual_seed(seed)

        if image == None:
            print("Image is None!!")

        with timer("Depth"):
            depth_img = self.depth_extractor.get_depth_image(image)

        infer_params = self.set_infer_params(prompt, negative_prompt, depth_img, generator)

        with torch.autocast("cuda"):
            with timer("Inference"):
                image = self.pipe(**infer_params).images[0]

            if output_depth:
                return image, depth_img
            else:
                return image


if __name__ == "__main__":
    midasDepth = MidasDepth()
    config_file = "./flower_generator/motion_server/diffusion_config.yaml"

    image = load_image("./flower_generator/unity_draw_test/img_origin/3linesRWSID720_l2_p17_circle2.PNG")

    diffusion_generator = DiffusionGenerator(config_file, midasDepth)

    folder_path = 'flower_generator/line_circle_test/twopplWS0311/line_flower_img'
    file_names = [f for f in os.listdir(folder_path) if os.path.isfile(os.path.join(folder_path, f))]

    save_folder = "flower_generator/line_circle_test/twopplWS0311/sd_flower_img"
    save_folder_rb = "flower_generator/line_circle_test/twopplWS0311/sd_flower_rb_img"


    for file in file_names:
        image = load_image(f"{folder_path}/{file}")
    
        res = diffusion_generator.infer(image)
        res.save(f"{save_folder}/{file}")

        res_rb = remove_bg(res, bg_color = (255, 255, 255, 0))
        res_rb.save(f"{save_folder_rb}/{file}")


        


