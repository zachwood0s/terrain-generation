using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureHelpers 
{

    public static (float min, float max) GetMinMax(this Texture2D self)
    {
        Color[] pixels = self.GetPixels(0, 0, self.width, self.height);
        float min = pixels[0].grayscale;
        float max = pixels[0].grayscale;
        foreach(var p in pixels)
        {
            if (p.grayscale < min)
                min = p.grayscale;
            if (p.grayscale > max)
                max = p.grayscale;
        }

        return (min, max);
    }

}


 
