using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Vivarium.Biology;

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

    public static Texture2D CreateRing(GraphicsDevice graphicsDevice, int radius, int thickness)
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

                float distFromCenter = Vector2.Distance(new Vector2(j, i), new Vector2(center, center));

                if (distFromCenter <= radius && distFromCenter >= radius - thickness)
                {
                    // Simple anti-aliasing
                    float distToEdge = Math.Min(Math.Abs(radius - distFromCenter), Math.Abs((radius - thickness) - distFromCenter));
                    if (distToEdge < 1.0f)
                    {
                        colorData[index] = Color.White * distToEdge;
                    }
                    else
                    {
                        colorData[index] = Color.White;
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

    public static Texture2D CreateStructureTexture(GraphicsDevice graphicsDevice, int size, int cornerRadius, int borderThickness, bool top, bool right, bool bottom, bool left)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var colorData = new Color[size * size];

        float center = size / 2f;
        
        // Hatching parameters
        int hatchSpacing = 25;
        int hatchThickness = 6;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size) + x;

                // Determine Quadrant and relevant neighbors
                bool qTop = y < center;
                bool qLeft = x < center;

                bool nV = qTop ? top : bottom; // Neighbor Vertical (Top or Bottom)
                bool nH = qLeft ? left : right; // Neighbor Horizontal (Left or Right)

                // Calculate distance to the relevant external edges
                // If neighbor exists, distance is effectively infinite (no edge)
                float distV = nV ? float.MaxValue : (qTop ? y : size - 1 - y);
                float distH = nH ? float.MaxValue : (qLeft ? x : size - 1 - x);

                bool isInside = false;
                bool isBorder = false;

                // Logic:
                // If both neighbors are present (Corner connection), it's fully filled.
                // If one neighbor is present, it's a straight edge.
                // If neither, it's a rounded corner.

                if (nV && nH)
                {
                    // Full square
                    isInside = true;
                    isBorder = false; // Internal area
                }
                else if (nV)
                {
                    // Vertical connection (Horizontal edge is boundary)
                    isInside = true;
                    isBorder = distH < borderThickness;
                }
                else if (nH)
                {
                    // Horizontal connection (Vertical edge is boundary)
                    isInside = true;
                    isBorder = distV < borderThickness;
                }
                else
                {
                    // Corner (Both edges are boundaries, rounded)
                    // Calculate distance from the corner center
                    float cx = qLeft ? cornerRadius : size - 1 - cornerRadius;
                    float cy = qTop ? cornerRadius : size - 1 - cornerRadius;

                    // We only care about the "outer" part of the quadrant for rounding
                    // If we are "inside" the corner radius box, we treat it as rect distance
                    // If we are in the "corner" zone, we check radius.
                    
                    // Simplified:
                    // The shape is defined by intersection of two half-planes and a circle?
                    // Actually, for a rounded rect corner:
                    // It is the set of points where distance to (cx, cy) <= radius
                    // BUT only for the region "outside" the inner rectangle.
                    
                    // Let's use the logic from CreateRoundedRect but adapted for quadrants
                    
                    // Local coordinates relative to quadrant corner
                    float lx = qLeft ? x : size - 1 - x;
                    float ly = qTop ? y : size - 1 - y;

                    if (lx >= cornerRadius && ly >= cornerRadius)
                    {
                        // Center area of the cell (far from corners)
                        isInside = true;
                        isBorder = false;
                    }
                    else if (lx < cornerRadius && ly < cornerRadius)
                    {
                        // The actual corner zone
                        float dx = cornerRadius - lx;
                        float dy = cornerRadius - ly;
                        float dist = MathF.Sqrt(dx * dx + dy * dy);
                        
                        if (dist <= cornerRadius)
                        {
                            isInside = true;
                            isBorder = dist >= cornerRadius - borderThickness;
                        }
                    }
                    else
                    {
                        // The straight parts near the corner
                        isInside = true;
                        // Border if close to the edge
                        isBorder = (lx < borderThickness) || (ly < borderThickness);
                    }
                }

                if (isInside)
                {
                    if (isBorder)
                    {
                        colorData[index] = Color.White;
                    }
                    else
                    {
                        // Body
                        // Hatching logic
                        // Diagonal lines: x - y = const
                        // We use (x + y) for one diagonal direction, (x - y) for the other.
                        // Let's use x + y (Top-Right to Bottom-Left hatching)
                        
                        bool isHatch = ((x + y) % hatchSpacing) < hatchThickness;
                        
                        if (isHatch)
                        {
                            colorData[index] = Color.White * 0.6f; // Thinner/Fainter white lines
                        }
                        else
                        {
                            colorData[index] = Color.White * 0.2f; // Semi-transparent body
                        }
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

    public static Texture2D CreateOrganicShape(GraphicsDevice graphicsDevice, int size, int petals, float variance, int borderThickness = 2)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var colorData = new Color[size * size];

        float center = size / 2f;
        // Leave some padding
        float maxRadius = (size / 2f) - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size) + x;
                float dx = x - center;
                float dy = y - center;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                float angle = MathF.Atan2(dy, dx);

                // Organic radius function
                // r = R * (1 + v * sin(n * theta))
                // We normalize so the max extent fits in maxRadius
                // Max extent is at (1+v). So BaseR = maxRadius / (1+v)

                float baseR = maxRadius / (1f + variance);
                float currentLimit = baseR * (1f + variance * MathF.Sin(petals * angle));

                // Anti-aliasing logic
                float distFromEdge = currentLimit - dist;

                if (distFromEdge < -1f)
                {
                    colorData[index] = Color.Transparent;
                }
                else if (distFromEdge < 0f)
                {
                    // Outer Edge Anti-aliasing (0 to 1)
                    float alpha = 1f + distFromEdge; // distFromEdge is between -1 and 0
                    colorData[index] = Color.White * alpha;
                }
                else
                {
                    // Inside
                    if (distFromEdge < borderThickness)
                    {
                        // Border
                        colorData[index] = Color.White;
                    }
                    else
                    {
                        // Inner Body - Semi-transparent
                        colorData[index] = Color.White * 0.3f;
                    }
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }

    public static Texture2D CreateAgentTexture(GraphicsDevice graphicsDevice, int size, DietType diet, int traitIndex)
    {
        var texture = new Texture2D(graphicsDevice, size, size);
        var colorData = new Color[size * size];

        float center = size / 2f;
        float scale = size / 2f; // Map -1..1 to 0..size
        int borderThickness = 10; // Strong outline

        // Trait Analysis
        // 4: Speed, 5: Constitution
        bool isFast = traitIndex == 4;
        bool isTough = traitIndex == 5;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size) + x;

                // Normalize coordinates to [-1, 1]
                float u = (x - center) / scale;
                float v = (y - center) / scale;

                // Aspect ratio correction for Speed trait (Elongate)
                if (isFast)
                {
                    v *= 1.4f; // Make it thinner/longer
                }

                float dist = 1.0f; // Signed Distance: Positive = outside, Negative = inside

                switch (diet)
                {
                    case DietType.Herbivore: // Bacillus (Rod)
                        // Rounded Box / Capsule
                        // Segment from (-0.4, 0) to (0.4, 0), radius 0.5
                        float hx = Math.Clamp(u, -0.4f, 0.4f);
                        float dx = u - hx;
                        float dy = v;
                        dist = MathF.Sqrt(dx * dx + dy * dy) - 0.5f;
                        break;

                    case DietType.Omnivore: // Diplococcus (Peanut)
                        // Two circles blended
                        float d1 = MathF.Sqrt((u + 0.3f) * (u + 0.3f) + v * v) - 0.45f;
                        float d2 = MathF.Sqrt((u - 0.3f) * (u - 0.3f) + v * v) - 0.45f;
                        
                        // Smooth Min (Polynomial)
                        float k = 0.15f;
                        float h = Math.Clamp(0.5f + 0.5f * (d2 - d1) / k, 0.0f, 1.0f);
                        dist = MathHelper.Lerp(d2, d1, h) - k * h * (1.0f - h);
                        break;

                    case DietType.Carnivore: // Virus / Spiky
                        // Radial distance with noise/sine
                        float len = MathF.Sqrt(u * u + v * v);
                        float angle = MathF.Atan2(v, u);
                        // 12 spikes
                        float r = 0.55f + 0.15f * MathF.Sin(12 * angle);
                        dist = len - r;
                        break;
                }

                // Convert back to pixel space for border check
                float distPx = dist * scale;

                if (distPx > 1f)
                {
                    colorData[index] = Color.Transparent;
                }
                else
                {
                    // Alpha for the outer edge AA
                    float alpha = Math.Clamp(1.0f - distPx, 0f, 1f);
                    
                    if (distPx > -borderThickness)
                    {
                        // Border Area (including outer AA)
                        colorData[index] = Color.White * alpha;
                    }
                    else
                    {
                        // Inner Body Area
                        float bodyAlpha = 0.3f;
                        if (isTough) bodyAlpha = 0.6f;
                        colorData[index] = Color.White * bodyAlpha;
                    }
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }
}
