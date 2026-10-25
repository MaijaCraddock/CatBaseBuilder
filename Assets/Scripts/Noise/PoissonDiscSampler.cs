using UnityEngine;
using System;
using System.Collections.Generic;
//shamelessly stealing the code of https://observablehq.com/@jrus/bridson-fork/2 
public sealed class Sample
{
    public double X { get; }
    public double Y { get; }

    public Sample(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public enum SampleEventType
{
    Add,
    Remove
}

public sealed class PoissonDiscEvent
{
    public SampleEventType Type { get; }
    public Sample Sample { get; }
    public Sample Parent { get; }

    public PoissonDiscEvent(
        SampleEventType type,
        Sample sample,
        Sample parent = null)
    {
        Type = type;
        Sample = sample;
        Parent = parent;
    }
}

public static class PoissonDiscSampler
{   
    public static List<Vector2> GenerateN(double width, double height, int N)
    {
        List<Vector2> points = new List<Vector2>();
        double radius = Math.Sqrt((0.63 * (width * height)) / N);
        foreach(PoissonDiscEvent ev in Generate(width, height, radius))
        {
            if(ev.Type != SampleEventType.Add){continue;}
            Vector2 point = new Vector2((float)ev.Sample.X, (float)ev.Sample.Y);
            points.Add(point);
        }
        return points;
    }

    private static IEnumerable<PoissonDiscEvent> Generate(double width, double height, double radius)
    {
        const int k = 13; // maximum number of samples before rejection
        const int m = 5;  // a number mutually prime to k
        double radius2 = radius * radius;
        double cellSize = radius * Math.Sqrt(0.5);
        int gridWidth = (int)Math.Ceiling(width / cellSize);
        int gridHeight = (int)Math.Ceiling(height / cellSize);
        Sample[] grid = new Sample[gridWidth * gridHeight];
        List<Sample> queue = new List<Sample>();
        double rotx = Math.Cos(2 * Math.PI * m / k);
        double roty = Math.Sin(2 * Math.PI * m / k);

        // Pick the first sample.
        yield return new PoissonDiscEvent(SampleEventType.Add, AddSample(width / 2, height / 2));

        // Pick a random existing sample from the queue.
        while (queue.Count > 0)
        {
            //using unity, cant use System.Random.Shared.NextDouble(queue.Count)
            int i = UnityEngine.Random.Range(0, queue.Count);
            Sample parent = queue[i];
            const double epsilon = 1e-7;
            double t = TanPi2(2 * UnityEngine.Random.value - 1);
            double q = 1 / (1 + t * t);
            double dw;
            double dx = q != 0 ? (1 - t * t) * q : -1;
            double dy = q != 0 ? 2 * t * q : 0;

            bool accepted = false;

            // Make a new candidate.
            for (int j = 0; j < k; j++)
            {
                dw = dx * rotx - dy * roty; // temporary name for dx
                dy = dx * roty + dy * rotx;
                dx = dw;
                double r = radius * (1 + epsilon + 0.65 * UnityEngine.Random.value * UnityEngine.Random.value);
                double x = parent.X + r*dx;
                double y = parent.Y + r*dy;

                // Accept candidates that are inside the allowed extent
                // and farther than 2 * radius to all existing samples.
                if ((0 <= x) && (x < width) && (0 <= y) && (y < height) && Far(x, y)) {
                    Sample sample = AddSample(x, y);
                    yield return new PoissonDiscEvent(SampleEventType.Add, sample, parent);
                    accepted = true;
                    break;
                }
            }
            if (accepted) continue;

            // If none of k candidates were accepted, remove it from the queue.
            Sample rSample = queue[^1]; //not really a queue, just read from end and remove afterwards to "pop"
            queue.RemoveAt(queue.Count - 1);
            if (i < queue.Count) queue[i] = rSample;
            yield return new PoissonDiscEvent(SampleEventType.Remove, parent);
        }

        // fast approximation of tan(πx/2)
        static double TanPi2(double a)
        {
            double b = 1 - a * a;
            return a * (-0.0187108*b + 0.31583526 + 1.27365776/b);
        }

        bool Far(double x, double y)
        {
            int i = (int)(x / cellSize);
            int j = (int)(y / cellSize);
            int i0 = Math.Max(i - 2, 0);
            int j0 = Math.Max(j - 2, 0);
            int i1 = Math.Min(i + 3, gridWidth);
            int j1 = Math.Min(j + 3, gridHeight);

            for (j = j0; j < j1; ++j)
            {
                int o = j * gridWidth;
                for (i = i0; i < i1; ++i)
                {
                    Sample s = grid[o + i];
                    if (s != null)
                    {
                        double dx = s.X - x;
                        double dy = s.Y - y;

                        if (dx * dx + dy * dy < radius2) return false;
                    }
                }
            }
            return true;
        }

        Sample AddSample(double x, double y)
        {
            Sample sample = new Sample(x, y);
            grid[gridWidth * (int)(y / cellSize) + (int)(x / cellSize)] = sample;
            queue.Add(sample);
            return sample;
        }
    }
}
