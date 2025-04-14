using LiteRP.FrameData;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public partial class LiteRenderGraphRecorder
    {
        private static readonly ProfilingSampler s_DrawObjectsProfilingSampler = new ProfilingSampler(name: "Draw Objects");
        private static readonly ShaderTagId s_shaderTagId = new ShaderTagId("SRPDefaultUnlit"); //渲染标签ID
        
        internal class DrawObjectsPassData
        {
            internal TextureHandle backBufferHandle;
            internal RendererListHandle opaqueRenderListHandle;
            internal RendererListHandle transparentRenderListHandle;
        }

        private void AddDrawObjectsPass(RenderGraph renderGraph, ContextContainer frameData)
        {
            CameraData cameraData = frameData.Get<CameraData>();
            using (var builder = renderGraph.AddRasterRenderPass<DrawObjectsPassData>(passName: "Draw Objects Pass", out var passData, s_DrawObjectsProfilingSampler))
            {
                //声明创建或引用的资源
                //不透明渲染列表
                RendererListDesc opaqueRenderDesc = new RendererListDesc(passName: s_shaderTagId, cameraData.cullingResults, cameraData.camera);
                opaqueRenderDesc.sortingCriteria = SortingCriteria.CommonOpaque;
                opaqueRenderDesc.renderQueueRange = RenderQueueRange.opaque;
                passData.opaqueRenderListHandle = renderGraph.CreateRendererList(opaqueRenderDesc);
                //RenderGraph引用透明渲染列表
                builder.UseRendererList(passData.opaqueRenderListHandle);
                
                
                //创建不透明渲染列表
                RendererListDesc transparentRenderDesc = new RendererListDesc(passName: s_shaderTagId, cameraData.cullingResults, cameraData.camera);
                transparentRenderDesc.sortingCriteria = SortingCriteria.CommonTransparent;
                transparentRenderDesc.renderQueueRange = RenderQueueRange.transparent;
                passData.transparentRenderListHandle = renderGraph.CreateRendererList(transparentRenderDesc);
                //RenderGraph引用不透明渲染列表
                builder.UseRendererList(passData.transparentRenderListHandle);
                
                //导入BackBuffer
                passData.backBufferHandle = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CurrentActive);
                builder.SetRenderAttachment(passData.backBufferHandle,0,AccessFlags.Write);
                
                //设置全局渲染状态
                builder.AllowPassCulling(false);
                
                
                builder.SetRenderFunc((DrawObjectsPassData passData, RasterGraphContext context) =>
                {
                    //调用渲染指令绘制
                    
                    context.cmd.DrawRendererList(passData.opaqueRenderListHandle);
                    context.cmd.DrawRendererList(passData.transparentRenderListHandle);
                    
                });
            }
        }
    }
}