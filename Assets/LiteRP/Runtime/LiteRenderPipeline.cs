using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public class LiteRenderPipeline : RenderPipeline
    {
        private readonly static ShaderTagId s_ShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        private RenderGraph m_RenderGraph = null;
        private LiteRenderGraphRecorder m_LiteRenderGraphRecorder = null;//渲染图记录器
        private ContextContainer m_ContextContainer = null;//上下文容器


        public LiteRenderPipeline()
        {
            InitializeRenderGraph();
        }
        
        protected override void Dispose(bool disposing)
        {
            ClearUpRenderGraph();
            base.Dispose(disposing);
        }

        //初始化渲染图
        private void InitializeRenderGraph()
        {
            m_RenderGraph = new RenderGraph("LiteRenderGraph");
            m_LiteRenderGraphRecorder = new LiteRenderGraphRecorder();
            m_ContextContainer = new ContextContainer();
        }
        
        //清理渲染图
        private void ClearUpRenderGraph()
        {
            m_ContextContainer?.Dispose();
            m_ContextContainer = null;
            m_LiteRenderGraphRecorder = null;
            m_RenderGraph?.Cleanup();
            m_RenderGraph = null;
        }
        
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
           
        }
        //new
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            //开始渲染上下文
            BeginContextRendering(context, cameras);

            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                RenderCamera(context,camera);
            }
            //结束渲染图
            m_RenderGraph.EndFrame();
            //结束渲染上下文
            EndContextRendering(context, cameras);
        }

        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            //开始渲染相机
            BeginCameraRendering(context, camera);

            if (!PrepareFrameData(context, camera))
            {
                return;
            }
            

            //剔除
            ScriptableCullingParameters cullingParameters;
            if(!camera.TryGetCullingParameters( out cullingParameters))
                return;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            
            //CommandBuffer 
            CommandBuffer cmd = CommandBufferPool.Get(camera.name);
            //camera par 相机参数
            context.SetupCameraProperties(camera);
            
            //记录执行RG
            RecordAndExecuteRenderGraph(context,camera,cmd);
            
            
            //commit to buffer 提交到缓冲
            context.ExecuteCommandBuffer(cmd);
            //clean buffer
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            //commit context
            context.Submit();
            
            //结束渲染相机
            EndCameraRendering(context, camera);
        }

        private bool PrepareFrameData(ScriptableRenderContext context, Camera camera)
        {
            return true;
        }

        private void RecordAndExecuteRenderGraph(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            RenderGraphParameters renderGraphParameters = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };
            m_RenderGraph.BeginRecording(renderGraphParameters);
            //开始记录
            m_LiteRenderGraphRecorder.RecordRenderGraph(m_RenderGraph,m_ContextContainer);
            
            m_RenderGraph.EndRecordingAndExecute();
            
            
        }
        
    }
}