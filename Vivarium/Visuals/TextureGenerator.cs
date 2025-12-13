using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Vivarium.Visuals;

public static class TextureGenerator
{
    public static Texture2D CreateCircle(GraphicsDevice graphicsDevice, int radius)
    {
        int diameter = radius * 2;
        var texture = new Texture2D(graphicsDevice, diameter, diameter);
        var colorData = new Color[diameter * diameter];

        float center = radius;

        for (int i = 0; i < diameter; i++)
        {
            for (int j = 0; j < diameter; j++)
            {
                int index = (i * diameter) + j;

                float distFromCenter = Vector2.Distance(new Vector2(i, j), new Vector2(center, center));

                if (distFromCenter <= radius)
                {
                    // If we are right at the edge, make it opaque white
                    if (radius - distFromCenter < radius / 4)
                    {
                        colorData[index] = Color.White;
                    }
                    else
                    {
                        colorData[index] = Color.Lerp(Color.White, Color.Black, 0.5f);
                    }
                }
                else
                {
                    colorData[index] = Color.Transparent;
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }

    public static Texture2D CreateStar(GraphicsDevice graphicsDevice, int outerRadius, int points = 5)
    {
        int diameter = outerRadius * 2;
        var texture = new Texture2D(graphicsDevice, diameter, diameter);
        var colorData = new Color[diameter * diameter];

        float center = outerRadius;
        float innerRadius = outerRadius * 0.4f;

        float angleStep = MathHelper.TwoPi / points;

        for (int i = 0; i < diameter; i++)
        {
            for (int j = 0; j < diameter; j++)
            {
                int index = (i * diameter) + j;

                float dx = j - center; // x
                float dy = i - center; // y

                float dist = MathF.Sqrt(dx * dx + dy * dy);
                float angle = MathF.Atan2(dy, dx) + MathHelper.PiOver2;

                if (angle < 0) angle += MathHelper.TwoPi;

                float sectorAngle = angle % angleStep;

                float t = MathF.Abs(sectorAngle - (angleStep / 2)) / (angleStep / 2);

                float currentLimit = MathHelper.Lerp(outerRadius, innerRadius, t);

                if (dist < currentLimit - 1.5f)
                {
                    colorData[index] = Color.White;
                }
                else if (dist < currentLimit)
                {
                    float alpha = currentLimit - dist;
                    colorData[index] = Color.White * alpha;
                }
                else
                {
                    colorData[index] = Color.Transparent;
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }

    public static Texture2D CreateRoundedRect(GraphicsDevice graphicsDevice, int size, int cornerRadius, int borderThickness = 0)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var colorData = new Color[size * size];

        float center = size / 2f;
        float innerLimit = (size / 2f) - cornerRadius;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int index = (i * size) + j;

                float dx = Math.Abs(j - center); // x
                float dy = Math.Abs(i - center); // y

                bool isInside = false;
                float distToEdge = 0f;

                if (dx <= innerLimit || dy <= innerLimit)
                {
                    isInside = true;
                    float edgeX = (size / 2f) - dx;
                    float edgeY = (size / 2f) - dy;
                    distToEdge = Math.Min(edgeX, edgeY);
                }
                else
                {
                    float cornerDx = dx - innerLimit;
                    float cornerDy = dy - innerLimit;
                    float distFromCornerCenter = MathF.Sqrt(cornerDx * cornerDx + cornerDy * cornerDy);

                    if (distFromCornerCenter <= cornerRadius)
                    {
                        isInside = true;
                        distToEdge = cornerRadius - distFromCornerCenter;
                    }
                }

                if (isInside)
                {
                    float alpha = Math.Clamp(distToEdge, 0f, 1f);

                    if (borderThickness > 0 && distToEdge < borderThickness)
                    {
                        colorData[index] = Color.Lerp(Color.Gray, Color.White, 0.5f) * alpha;
                    }
                    else
                    {
                        colorData[index] = Color.White * alpha;
                    }
                }
                else
                {
                    colorData[index] = Color.Transparent;
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }

    public static Texture2D CreateTriangle(GraphicsDevice graphicsDevice, int size)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var colorData = new Color[size * size];

        float centerY = size / 2f;
        // Precompute sine of the angle for distance correction
        // Slope is 0.5. Angle is Atan(0.5). Sin(Atan(0.5)) = 0.4472136
        float sinAngle = 0.4472136f;
        float borderThickness = size * 0.15f; // 15% of size

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size) + x;

                float v = Math.Abs((y - centerY) / centerY); // 0 at center, 1 at edges
                float limitX = size * (1.0f - v);

                if (x < limitX)
                {
                    // Inside the triangle
                    float distBase = x;
                    float distSlant = (limitX - x) * sinAngle;

                    float minDist = Math.Min(distBase, distSlant);

                    if (minDist < borderThickness)
                    {
                        colorData[index] = Color.White;
                    }
                    else
                    {
                        // Inner part - semi-transparent (darker) look
                        colorData[index] = Color.Lerp(Color.White, Color.Black, 0.5f);
                    }
                }
                else
                {
                    colorData[index] = Color.Transparent;
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }
}
