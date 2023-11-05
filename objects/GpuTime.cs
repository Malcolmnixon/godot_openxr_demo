using System;
using System.Collections.Generic;
using Godot;

namespace GodotOpenXRDemo.objects;

public partial class GpuTime : Label3D
{
    private Viewport _vp;

    private readonly List<double> _times = new();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _vp = GetViewport();

        if (RenderingServer.GetRenderingDevice() != null)
            RenderingServer.ViewportSetMeasureRenderTime(_vp.GetViewportRid(), true);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (RenderingServer.GetRenderingDevice() != null)
        {
            _times.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(_vp.GetViewportRid()));
            if (_times.Count > 100)
                _times.RemoveAt(0);

            var minTime = _times[0];
            var maxTime = _times[0];
            var avgTime = 0.0;

            foreach (var time in _times)
            {
                minTime = Math.Min(minTime, time);
                maxTime = Math.Max(maxTime, time);
                avgTime += time;
            }

            avgTime /= _times.Count;

            Text = $"GPU time:\nmin: {minTime:F3}ms\nmax: {maxTime:F3}ms\navg: {avgTime:F3}ms";
        }
        else
        {
            Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }
    }
}