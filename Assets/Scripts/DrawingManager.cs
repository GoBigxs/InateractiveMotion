using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;

public class DrawingManager : MonoBehaviour
{
    public static DrawingManager Instance { get; private set; }
    public RawImage canvasImage; // Assign this in the Unity Editor

    private Texture2D canvasTexture;
    private Color[] canvasColors;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCanvas();
        }
    }

    void InitializeCanvas()
    {
        canvasTexture = new Texture2D((int)canvasImage.rectTransform.rect.width, (int)canvasImage.rectTransform.rect.height);
        canvasImage.texture = canvasTexture;
        ClearCanvas();
    }

    public void ClearCanvas()
    {
        canvasColors = new Color[canvasTexture.width * canvasTexture.height];
        for (int i = 0; i < canvasColors.Length; i++)
        {
            canvasColors[i] = Color.clear;
        }
        canvasTexture.SetPixels(canvasColors);
        canvasTexture.Apply();
    }

    public void UpdateCanvas(Color[] colors)
    {
        canvasTexture.SetPixels(colors);
        canvasTexture.Apply();
    }

    public Texture2D GetCanvasTexture()
    {
        return canvasTexture;
    }

    public Color[] GetCanvasColors()
    {
        return canvasColors;
    }

    public void SetCanvasColors(Color[] colors)
    {
        canvasColors = colors;
    }
}
