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

                float dist = MathF.Sqrt((dx * dx) + (dy * dy));
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
                    float distFromCornerCenter = MathF.Sqrt((cornerDx * cornerDx) + (cornerDy * cornerDy));

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
        const float sinAngle = 0.4472136f;
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
                    float distToEdge = Math.Min(Math.Abs(radius - distFromCenter), Math.Abs(radius - thickness - distFromCenter));
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
        const int hatchSpacing = 25;
        const int hatchThickness = 6;

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
                        float dist = MathF.Sqrt((dx * dx) + (dy * dy));

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
                float dist = MathF.Sqrt((dx * dx) + (dy * dy));
                float angle = MathF.Atan2(dy, dx);

                // Organic radius function
                // r = R * (1 + v * sin(n * theta))
                // We normalize so the max extent fits in maxRadius
                // Max extent is at (1+v). So BaseR = maxRadius / (1+v)

                float baseR = maxRadius / (1f + variance);
                float currentLimit = baseR * (1f + (variance * MathF.Sin(petals * angle)));

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
        const int borderThickness = 8; // Slightly thinner than before to allow for complex shapes

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = (y * size) + x;

                // Normalize coordinates to [-1, 1]
                float u = (x - center) / scale;
                float v = (y - center) / scale;

                float dist = GetAgentSDF(u, v, diet, traitIndex);

                // Convert back to pixel space
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
                        colorData[index] = Color.White * 0.4f;
                    }
                }
            }
        }

        texture.SetData(colorData);
        return texture;
    }

    private static float GetAgentSDF(float x, float y, DietType diet, int traitIndex)
    {
        float d = 1.0f;
        float len = MathF.Sqrt((x * x) + (y * y));
        float angle = MathF.Atan2(y, x);

        switch (diet)
        {
            case DietType.Herbivore: // Rounded, Bloby
                switch (traitIndex)
                {
                    case 0: // Strength: Rounded Box
                        float bx = Math.Abs(x) - 0.3f;
                        float by = Math.Abs(y) - 0.3f;
                        float dBox = MathF.Sqrt((Math.Max(bx, 0f) * Math.Max(bx, 0f)) + (Math.Max(by, 0f) * Math.Max(by, 0f)));
                        d = dBox + Math.Min(Math.Max(bx, by), 0f) - 0.3f;
                        break;
                    case 1: // Bravery: Rounded Triangle (Blob)
                        // 3 circles blended
                        float d1 = MathF.Sqrt((x * x) + ((y + 0.3f) * (y + 0.3f))) - 0.35f;
                        float d2 = MathF.Sqrt(((x + 0.3f) * (x + 0.3f)) + ((y - 0.2f) * (y - 0.2f))) - 0.35f;
                        float d3 = MathF.Sqrt(((x - 0.3f) * (x - 0.3f)) + ((y - 0.2f) * (y - 0.2f))) - 0.35f;
                        d = SmoothMin(d1, SmoothMin(d2, d3, 0.3f), 0.3f);
                        break;
                    case 2: // Metabolism: Simple Circle
                        d = len - 0.6f;
                        break;
                    case 3: // Perception: Ellipse (Eye-like)
                        d = MathF.Sqrt((x * x) + ((y * 1.5f) * (y * 1.5f))) - 0.55f;
                        break;
                    case 4: // Speed: Egg/Teardrop
                        d = MathF.Sqrt((x * x) + (y * y)) - (0.5f - (0.2f * y));
                        break;
                    case 5: // Constitution: Squircle
                        d = MathF.Pow(MathF.Pow(Math.Abs(x), 4) + MathF.Pow(Math.Abs(y), 4), 0.25f) - 0.6f;
                        break;
                }
                break;

            case DietType.Omnivore: // Segmented
                switch (traitIndex)
                {
                    case 0: // Strength: Tetrad (Square of 4)
                        float ax = Math.Abs(x) - 0.25f;
                        float ay = Math.Abs(y) - 0.25f;
                        d = MathF.Sqrt((ax * ax) + (ay * ay)) - 0.25f;
                        break;
                    case 1: // Bravery: Diplococcus (Peanut)
                        float dLeft = MathF.Sqrt(((x + 0.3f) * (x + 0.3f)) + (y * y)) - 0.35f;
                        float dRight = MathF.Sqrt(((x - 0.3f) * (x - 0.3f)) + (y * y)) - 0.35f;
                        d = SmoothMin(dLeft, dRight, 0.15f);
                        break;
                    case 2: // Metabolism: Chain of 3
                        float c1 = MathF.Sqrt(((x + 0.4f) * (x + 0.4f)) + (y * y)) - 0.25f;
                        float c2 = MathF.Sqrt((x * x) + (y * y)) - 0.25f;
                        float c3 = MathF.Sqrt(((x - 0.4f) * (x - 0.4f)) + (y * y)) - 0.25f;
                        d = Math.Min(c1, Math.Min(c2, c3));
                        break;
                    case 3: // Perception: Curved Chain
                        float a1 = MathF.Sqrt((x * x) + ((y + 0.2f) * (y + 0.2f))) - 0.25f;
                        float a2 = MathF.Sqrt(((x + 0.4f) * (x + 0.4f)) + ((y - 0.2f) * (y - 0.2f))) - 0.25f;
                        float a3 = MathF.Sqrt(((x - 0.4f) * (x - 0.4f)) + ((y - 0.2f) * (y - 0.2f))) - 0.25f;
                        d = Math.Min(a1, Math.Min(a2, a3));
                        break;
                    case 4: // Speed: Worm (Overlapping segments)
                        float hx = Math.Clamp(x, -0.4f, 0.4f);
                        d = MathF.Sqrt(((x - hx) * (x - hx)) + (y * y)) - 0.25f;
                        // Add bumps
                        d -= 0.05f * MathF.Sin(x * 15f);
                        break;
                    case 5: // Constitution: Cluster
                        float centerD = MathF.Sqrt((x * x) + (y * y)) - 0.3f;
                        float sx = Math.Abs(x) - 0.35f;
                        float sy = Math.Abs(y) - 0.35f;
                        float satD = MathF.Sqrt((sx * sx) + (sy * sy)) - 0.2f;
                        d = Math.Min(centerD, satD);
                        break;
                }
                break;

            case DietType.Carnivore: // Spiky / Virus
                switch (traitIndex)
                {
                    case 0: // Strength: Mace (4 big spikes)
                        float r0 = 0.4f + (0.2f * MathF.Abs(MathF.Cos(4 * angle)));
                        d = len - r0;
                        break;
                    case 1: // Bravery: Star (5 points)
                        float r1 = 0.4f + (0.25f * MathF.Cos(5 * angle));
                        d = len - r1;
                        break;
                    case 2: // Metabolism: Gear
                        float r2 = 0.5f + (0.1f * MathF.Sin(12 * angle));
                        d = len - r2;
                        break;
                    case 3: // Perception: Antennae
                        float spike = MathF.Pow(MathF.Abs(MathF.Cos(4 * angle)), 10f);
                        float r3 = 0.35f + (0.3f * spike);
                        d = len - r3;
                        break;
                    case 4: // Speed: Arrow / Shuriken
                        float r4 = 0.3f + (0.3f * MathF.Cos(3 * angle));
                        d = len - r4;
                        break;
                    case 5: // Constitution: Adenovirus (Hexagon-ish with nubs)
                        float qx = MathF.Abs(x);
                        float qy = MathF.Abs(y);
                        float hex = Math.Max((qx * 0.866025f) + (qy * 0.5f), qy);
                        d = hex - 0.5f;
                        d -= 0.1f * MathF.Cos(6 * angle);
                        break;
                }
                break;
        }

        return d;
    }

    private static float SmoothMin(float a, float b, float k)
    {
        float h = Math.Clamp(0.5f + (0.5f * (b - a) / k), 0.0f, 1.0f);
        return MathHelper.Lerp(b, a, h) - (k * h * (1.0f - h));
    }
}
