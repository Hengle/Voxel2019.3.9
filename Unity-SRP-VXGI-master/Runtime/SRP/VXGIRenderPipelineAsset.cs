using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using RenderPipeline = UnityEngine.Rendering.RenderPipeline;

[CreateAssetMenu(fileName = "VXGIRenderPipeline.asset", menuName = "Rendering/VXGI Render Pipeline Asset", order = 320)]
public class VXGIRenderPipelineAsset : RenderPipelineAsset
{
    public bool dynamicBatching;
    public bool SRPBatching;

    [Header("Lighting Settings")]
    public bool environmentLighting = true;
    public bool environmentReflections = true;

    public override Material defaultMaterial => (Material)Resources.Load("VXGI/Material/Default");

    public override Material defaultParticleMaterial => (Material)Resources.Load("VXGI/Material/Default-Particle");


    public override Shader defaultShader => Shader.Find("VXGI/Standard");


    protected override RenderPipeline CreatePipeline() => new VXGIRenderPipeline(this);
}
