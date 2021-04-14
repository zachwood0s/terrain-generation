using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;
using UnityEditor;

public class DensityGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    public Texture2D heightmap;
    public int blurAmt;
    public int iter;


    [Button]
    public void Regenerate()
    {
        if (heightmap == null)
            return;

        var tex = new Texture2D(heightmap.width, heightmap.height);

        for(int x = 0; x < heightmap.width - 1; x++) 
        {
            for(int y = 0; y < heightmap.height - 1; y++) 
            {
                float steep = GetSteepness(x, y);
                tex.SetPixel(x, y, new Color(steep, steep, steep));
            }
        }

        tex = FastBlur(tex, blurAmt, 4);

        var (min, max) = tex.GetMinMax();
        float oldRange = max - min;

        for(int x = 0; x < heightmap.width - 1; x++) 
        {
            for(int y = 0; y < heightmap.height - 1; y++) 
            {
                float steep = (tex.GetPixel(x, y).grayscale - min) / oldRange;
                tex.SetPixel(x, y, new Color(steep, steep, steep));
            }
        }

        AssetDatabase.CreateAsset(tex, "Assets/densitymap.asset");
    } 

    private float GetSteepness(int x, int y) 
    {
        float height = heightmap.GetPixel(x, y).grayscale;

        float dx = heightmap.GetPixel(x + 1, y).grayscale - height;
        float dy = heightmap.GetPixel(x, y + 1).grayscale - height;

        return Mathf.Sqrt(dx * dx + dy + dy);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private float avgR = 0;
    private float avgG = 0;
    private float avgB = 0;
    private float avgA = 0;
    private float blurPixelCount = 0;
    
    Texture2D FastBlur(Texture2D image, int radius, int iterations){
    
        Texture2D tex = image;
    
        for (var i = 0; i < iterations; i++) {
    
            tex = BlurImage(tex, radius, true);
            tex = BlurImage(tex, radius, false);
        }
    
        return tex;
    }
    
    Texture2D BlurImage(Texture2D image, int blurSize, bool horizontal){
    
        Texture2D blurred = new Texture2D(image.width, image.height);
        int _W = image.width;
        int _H = image.height;
        int xx, yy, x, y;
    
        if (horizontal) {
    
            for (yy = 0; yy < _H; yy++) {
    
                for (xx = 0; xx < _W; xx++) {
    
                    ResetPixel();
    
                    //Right side of pixel
                    for (x = xx; (x < xx + blurSize && x < _W); x++) {
    
                        AddPixel(image.GetPixel(x, yy));
                    }
    
                    //Left side of pixel
                    for (x = xx; (x > xx - blurSize && x > 0); x--) {
    
                        AddPixel(image.GetPixel(x, yy));
                    }
    
                    CalcPixel();
    
                    for (x = xx; x < xx + blurSize && x < _W; x++) {
    
                        blurred.SetPixel(x, yy, new Color(avgR, avgG, avgB, 1.0f));
                    }
                }
            }
        }
    
        else {
    
            for (xx = 0; xx < _W; xx++) {
    
                for (yy = 0; yy < _H; yy++) {
    
                    ResetPixel();
    
                    //Over pixel
                    for (y = yy; (y < yy + blurSize && y < _H); y++) {
    
                        AddPixel(image.GetPixel(xx, y));
                    }
    
                    //Under pixel
                    for (y = yy; (y > yy - blurSize && y > 0); y--) {
    
                        AddPixel(image.GetPixel(xx, y));
                    }
    
                    CalcPixel();
    
                    for (y = yy; y < yy + blurSize && y < _H; y++) {
    
                        blurred.SetPixel(xx, y, new Color(avgR, avgG, avgB, 1.0f));
                    }
                }
            }
        }
    
        blurred.Apply();
        return blurred;
    }
    
    void AddPixel(Color pixel) {
    
        avgR += pixel.r;
        avgG += pixel.g;
        avgB += pixel.b;
        blurPixelCount++;
    }
    
    void ResetPixel() {
    
        avgR = 0.0f;
        avgG = 0.0f;
        avgB = 0.0f;
        blurPixelCount = 0;
    }
    
    void CalcPixel() {
    
        avgR = avgR / blurPixelCount;
        avgG = avgG / blurPixelCount;
        avgB = avgB / blurPixelCount;
    }
}
