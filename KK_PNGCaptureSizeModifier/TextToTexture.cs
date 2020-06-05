// TextToTexture - Class for apply Text-To-Texture without need for Render Texture
//
// released under MIT License
// http://www.opensource.org/licenses/mit-license.php
//
//@author		Devin Reimer
//@version		1.0.0
//@website 		http://blog.almostlogical.com

//Copyright (c) 2010 Devin Reimer
/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

//20200514 --jim60105
//Change some logic to meet my needs.
//If someone wants to reuse this code, I recommend using the original code.
//Which can be found on the original author's blog: http://blog.almostlogical.com/2010/08/20/adding-text-to-texture-at-runtime-in-unity3d-without-using-render-texture/

using UnityEngine;

public class TextToTexture {
    private const int ASCII_START_OFFSET = 32;
    //private Font customFont;
    internal static Texture2D fontTexture;
    private static int fontCountX = 10;
    private static int fontCountY = 10;
    private static float[] kerningValues = new float[] {
         .201f /* */
        ,.201f /*!*/
        ,.256f /*"*/
        ,.401f /*#*/
        ,.401f /*$*/
        ,.641f /*%*/
        ,.481f /*&*/
        ,.138f /*'*/
        ,.24f /*(*/
        ,.24f /*)*/
        ,.281f /***/
        ,.421f /*+*/
        ,.201f /*,*/
        ,.24f /*-*/
        ,.201f /*.*/
        ,.201f /*/*/
        ,.401f /*0*/
        ,.353f /*1*/
        ,.401f /*2*/
        ,.401f /*3*/
        ,.401f /*4*/
        ,.401f /*5*/
        ,.401f /*6*/
        ,.401f /*7*/
        ,.401f /*8*/
        ,.401f /*9*/
        ,.201f /*:*/
        ,.201f /*;*/
        ,.421f /*<*/
        ,.421f /*=*/
        ,.421f /*>*/
        ,.401f /*?*/
        ,.731f /*@*/
        ,.481f /*A*/
        ,.481f /*B*/
        ,.52f /*C*/
        ,.481f /*D*/
        ,.481f /*E*/
        ,.44f /*F*/
        ,.561f /*G*/
        ,.52f /*H*/
        ,.201f /*I*/
        ,.36f /*J*/
        ,.481f /*K*/
        ,.401f /*L*/
        ,.6f /*M*/
        ,.52f /*N*/
        ,.561f /*O*/
        ,.481f /*P*/
        ,.561f /*Q*/
        ,.52f /*R*/
        ,.481f /*S*/
        ,.44f /*T*/
        ,.52f /*U*/
        ,.481f /*V*/
        ,.68f /*W*/
        ,.481f /*X*/
        ,.481f /*Y*/
        ,.44f /*Z*/
        ,.201f /*[*/
        ,.201f /*\*/
        ,.201f /*]*/
        ,.338f /*^*/
        ,.401f /*_*/
        ,.24f /*`*/
        ,.401f /*a*/
        ,.401f /*b*/
        ,.36f /*c*/
        ,.401f /*d*/
        ,.401f /*e*/
        ,.189f /*f*/
        ,.401f /*g*/
        ,.401f /*h*/
        ,.16f /*i*/
        ,.16f /*j*/
        ,.36f /*k*/
        ,.16f /*l*/
        ,.6f /*m*/
        ,.401f /*n*/
        ,.401f /*o*/
        ,.401f /*p*/
        ,.401f /*q*/
        ,.24f /*r*/
        ,.36f /*s*/
        ,.201f /*t*/
        ,.401f /*u*/
        ,.36f /*v*/
        ,.52f /*w*/
        ,.36f /*x*/
        ,.36f /*y*/
        ,.36f /*z*/
        ,.241f /*{*/
        ,.188f /*|*/
        ,.241f /*}*/
        ,.421f /*~*/
    };

    //placementX and Y - placement within texture size, texture size = textureWidth and textureHeight (square)
    public static Texture2D CreateTextToTexture(string text, int textPlacementX, int textPlacementY, int textureSize, float characterSize/*, float lineSpacing*/) {
        Texture2D txtTexture = CreatefillTexture2D(Color.clear, textureSize, textureSize);
        int fontGridCellWidth = (int)(fontTexture.width / fontCountX);
        int fontGridCellHeight = (int)(fontTexture.height / fontCountY);
        int fontItemWidth = (int)(fontGridCellWidth * characterSize);
        int fontItemHeight = (int)(fontGridCellHeight * characterSize);
        Vector2 charTexturePos;
        Color[] charPixels;
        float textPosX = textPlacementX;
        float textPosY = textPlacementY;
        float charKerning;
        char letter;

        for (int n = 0; n < text.Length; n++) {
            letter = text[n];
            charTexturePos = GetCharacterGridPosition(letter);
            charTexturePos.x *= fontGridCellWidth;
            charTexturePos.y *= fontGridCellHeight;
            charPixels = fontTexture.GetPixels((int)charTexturePos.x, fontTexture.height - (int)charTexturePos.y - fontGridCellHeight, fontGridCellWidth, fontGridCellHeight);
            Texture2D temp = new Texture2D(fontGridCellWidth, fontGridCellHeight);
            temp.SetPixels(charPixels);
            temp = Extension.Extension.Scale(temp, fontItemWidth, fontItemHeight);

            //charPixels = changeDimensions(charPixels, fontGridCellWidth, fontGridCellHeight, fontItemWidth, fontItemHeight);

            //txtTexture.SetPixels((int)textPosX, (int)textPosY, fontItemWidth, fontItemHeight, charPixels);
            txtTexture = Extension.Extension.OverwriteTexture(txtTexture, temp, (int)textPosX, (int)textPosY);

            charKerning = GetKerningValue(letter);
            textPosX += (fontItemWidth * charKerning); //add kerning here
        }
        txtTexture.Apply();
        return txtTexture;
    }

    //doesn't yet support special characters
    //trailing buffer is to allow for area where the character might be at the end
    public static int CalcTextWidthPlusTrailingBuffer(string text/*, int decalTextureSize*/, float characterSize) {
        char letter;
        float width = 0;
        int fontItemWidth = (int)((fontTexture.width / fontCountX) * characterSize);

        for (int n = 0; n < text.Length; n++) {
            letter = text[n];
            //if (n < text.Length - 1) {
                width += fontItemWidth * GetKerningValue(letter);
            //} else {
            //    //last letter ignore kerning for buffer
            //    width += fontItemWidth;
            //}
        }

        return (int)width;
    }

    private static Color[] changeDimensions(Color[] originalColors, int originalWidth, int originalHeight, int newWidth, int newHeight) {
        Color[] newColors;
        Texture2D originalTexture;
        int pixelCount;
        float u;
        float v;

        if (originalWidth == newWidth && originalHeight == newHeight) {
            newColors = originalColors;
        } else {
            newColors = new Color[newWidth * newHeight];
            originalTexture = new Texture2D(originalWidth, originalHeight);

            originalTexture.SetPixels(originalColors);
            for (int y = 0; y < newHeight; y++) {
                for (int x = 0; x < newWidth; x++) {
                    pixelCount = x + (y * newWidth);
                    u = (float)x / newWidth;
                    v = (float)y / newHeight;
                    newColors[pixelCount] = originalTexture.GetPixelBilinear(u, v);
                }
            }
        }

        return newColors;
    }

    private static Vector2 GetCharacterGridPosition(char c) {
        int codeOffset = c - ASCII_START_OFFSET;

        return new Vector2(codeOffset % fontCountX, (int)codeOffset / fontCountX);
    }

    private static float GetKerningValue(char c) {
        return kerningValues[((int)c) - ASCII_START_OFFSET] + 0.1f;   //Add 0.1f for more kerning, cause I add ourter boarder on fonts
    }

    private static Texture2D CreatefillTexture2D(Color color, int textureWidth, int textureHeight) {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        int numOfPixels = texture.width * texture.height;
        Color[] colors = new Color[numOfPixels];
        for (int x = 0; x < numOfPixels; x++) {
            colors[x] = color;
        }

        texture.SetPixels(colors);

        return texture;
    }
}