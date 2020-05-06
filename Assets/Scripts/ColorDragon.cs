using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorDragon : MonoBehaviour
{
    public enum ColorType
    {
        YELLOW,
        PURPLE,
        RED,
        BLUE,
        GREEN,
        PINK,
        ANY,
        COUNT
    }
    [System.Serializable]
    public struct ColorSprite
    {
        public ColorType color;
        public Sprite sprite;
    }

    public ColorSprite[] ColorSprites;

    private Dictionary<ColorType, Sprite> ColorSpriteDict;

    private SpriteRenderer sprite;

    public int NumColors
    {
        get
        {
            return ColorSprites.Length;
        }
    }

    private ColorType color;
    public ColorType Color { get => color; set => SetColor(value); }


    private void Awake()
    {
        sprite = transform.Find("Dragon").GetComponent<SpriteRenderer>();
        ColorSpriteDict = new Dictionary<ColorType, Sprite>();
        for (int i = 0; i < ColorSprites.Length; i++)
        {
            if (!ColorSpriteDict.ContainsKey(ColorSprites[i].color))
            {
                ColorSpriteDict.Add(ColorSprites[i].color, ColorSprites[i].sprite);
            }
        }
    }

    public void SetColor(ColorType newColor)
    {
        color = newColor;
        if (ColorSpriteDict.ContainsKey(newColor))
        {
            sprite.sprite = ColorSpriteDict[newColor];
        }
    }
}
