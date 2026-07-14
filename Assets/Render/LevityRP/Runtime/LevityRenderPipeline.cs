using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LevityRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();

    public LevityRenderPipeline()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        throw new System.NotImplementedException();
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context, cameras[i]);
        }
    }
}