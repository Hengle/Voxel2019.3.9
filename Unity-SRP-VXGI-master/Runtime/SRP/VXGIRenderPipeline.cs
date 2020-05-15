using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Rendering;

public class VXGIRenderPipeline : RenderPipeline {
  public static bool isD3D11Supported {
    get { return _D3D11DeviceType.Contains(SystemInfo.graphicsDeviceType); }
  }


  public PerObjectData perObjectData {
    get { return _perObjectData; }
  }

  static readonly ReadOnlyCollection<GraphicsDeviceType> _D3D11DeviceType = new ReadOnlyCollection<GraphicsDeviceType>(new[] {
    GraphicsDeviceType.Direct3D11,
    GraphicsDeviceType.Direct3D12,
    GraphicsDeviceType.XboxOne,
    GraphicsDeviceType.XboxOneD3D12
  });

  CommandBuffer _command;
  CullingResults _cullResults;

  FilteringSettings _filterSettings;
  PerObjectData _perObjectData;
  VXGIRenderer _renderer;

  public static void TriggerCameraCallback(Camera camera, string message, Camera.CameraCallback callback) {
    camera.SendMessage(message, SendMessageOptions.DontRequireReceiver);
    if (callback != null) callback(camera);
  }

  public VXGIRenderPipeline(VXGIRenderPipelineAsset asset) {
    _renderer = new VXGIRenderer(this);
    _command = new CommandBuffer() { name = "VXGI.RenderPipeline" };
    _filterSettings = new FilteringSettings() { renderQueueRange = RenderQueueRange.opaque };




    _perObjectData = PerObjectData.None;

    if (asset.environmentLighting) _perObjectData |= PerObjectData.LightProbe;
    if (asset.environmentReflections) _perObjectData |= PerObjectData.ReflectionProbes;

    Shader.globalRenderPipeline = "VXGI";

    GraphicsSettings.lightsUseLinearIntensity = true;
    GraphicsSettings.useScriptableRenderPipelineBatching = asset.SRPBatching;
  }

  public  void Dispose() {


    _command.Dispose();

    Shader.globalRenderPipeline = string.Empty;
  }

  protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras) {
    //base.Render(renderContext, cameras);
    BeginFrameRendering(renderContext,cameras);

    foreach (var camera in cameras) {
      BeginCameraRendering(renderContext,camera);

      var vxgi = camera.GetComponent<VXGI>();

      if (vxgi != null && vxgi.isActiveAndEnabled) {
        vxgi.Render(renderContext, _renderer);
      } else {
        bool rendered = false;

#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView) {
          ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }

        if (Camera.main != null) {
          vxgi = Camera.main.GetComponent<VXGI>();

          if (vxgi != null && vxgi.isActiveAndEnabled) {
            vxgi.Render(renderContext, camera, _renderer);
            rendered = true;
          }
        }
#endif

        if (!rendered) RenderFallback(renderContext, camera);
      }
    }

    renderContext.Submit();
  }

  void RenderFallback(ScriptableRenderContext renderContext, Camera camera) {
    TriggerCameraCallback(camera, "OnPreRender", Camera.onPreRender);

    _command.ClearRenderTarget(true, true, Color.black);
    renderContext.ExecuteCommandBuffer(_command);
    _command.Clear();

    TriggerCameraCallback(camera, "OnPreCull", Camera.onPreCull);

    if (!camera.TryGetCullingParameters(camera, out var cullingParams)) return;
    _cullResults = renderContext.Cull(ref cullingParams);

        var drawSettings = new DrawingSettings( new ShaderTagId("ForwardBase"),new SortingSettings() );
    drawSettings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
    drawSettings.SetShaderPassName(2, new ShaderTagId("Always"));
    drawSettings.SetShaderPassName(3, new ShaderTagId("Vertex"));
    drawSettings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
    drawSettings.SetShaderPassName(5, new ShaderTagId("VertexLM"));

    drawSettings.enableDynamicBatching = true;
    drawSettings.enableInstancing = true;

        renderContext.SetupCameraProperties(camera);
    renderContext.DrawRenderers(_cullResults, ref drawSettings, ref _filterSettings);
    renderContext.DrawSkybox(camera);

    TriggerCameraCallback(camera, "OnPostRender", Camera.onPostRender);
  }
}
