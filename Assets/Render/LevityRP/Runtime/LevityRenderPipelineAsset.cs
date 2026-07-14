using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/LevityRenderPipeline")]
public class LevityRenderPipelineAsset : RenderPipelineAsset
{

    protected override RenderPipeline CreatePipeline()
    {
        return new LevityRenderPipeline();
    }
}