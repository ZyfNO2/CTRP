using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    public class LiteRenderPipeline : RenderPipeline
    {
        private readonly static ShaderTagId s_ShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            //开始渲染上下文
            BeginContextRendering(context, cameras);

            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                RenderCamera(context,camera);

            }
            
            //结束渲染上下文
            EndContextRendering(context, cameras);
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            //开始渲染相机
            BeginCameraRendering(context, camera);

            //剔除
            ScriptableCullingParameters cullingParameters;
            if(!camera.TryGetCullingParameters( out cullingParameters))
                return;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            //CommandBuffer
            CommandBuffer cmd = CommandBufferPool.Get(camera.name);
            //camera par 
            context.SetupCameraProperties(camera);
            
            bool clearSkybox = camera.clearFlags == CameraClearFlags.Skybox;
            bool clearDepth = camera.clearFlags != CameraClearFlags.Nothing;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color;

            
            //clean render target 清理渲染目标
            cmd.ClearRenderTarget(true,true,CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor) );
            
            
            //绘制天空盒
            if (clearSkybox)
            {
                var skyBoxRendererList = context.CreateSkyboxRendererList(camera);
                cmd.DrawRendererList(skyBoxRendererList);
            }
            
            //SortingSettings 设置渲染排序
            var sortSettings = new SortingSettings(camera);
            //DrawSettings  指定渲染状态
            var drawSettings = new DrawingSettings(s_ShaderTagId, sortSettings);
            
            
            
            
            //绘制不透明
            sortSettings.criteria = SortingCriteria.CommonOpaque;
            //指定渲染过滤设置
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            //创建渲染列表
            var rendererListParams = new RendererListParams(cullingResults, drawSettings, filteringSettings);
            var rendererList = context.CreateRendererList(ref rendererListParams);
            //绘制渲染列表
            cmd.DrawRendererList(rendererList);
            
            //绘制透明
            sortSettings.criteria = SortingCriteria.CommonTransparent;
            //指定渲染过滤设置
            filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            //创建渲染列表
            rendererListParams = new RendererListParams(cullingResults, drawSettings, filteringSettings);
            rendererList = context.CreateRendererList(ref rendererListParams);
            //绘制渲染列表
            cmd.DrawRendererList(rendererList);
            
            
            //FilterSettings 指定渲染过滤设置
            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            //render list 渲染列表
            var renderListParams = new RendererListParams(cullingResults, drawSettings, filterSettings);
            var renderList = context.CreateRendererList(ref renderListParams);
            //draw render list 绘制渲染列表
            cmd.DrawRendererList(renderList);
            //commit to buffer
            context.ExecuteCommandBuffer(cmd);
            //clean buffer
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            //commit context
            context.Submit();
            
            
            //结束渲染相机
            EndCameraRendering(context, camera);
        }
        
    }
}