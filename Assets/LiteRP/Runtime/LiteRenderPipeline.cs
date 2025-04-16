using System.Collections.Generic;
using LiteRP.FrameData;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public class LiteRenderPipeline : RenderPipeline
    {
        // 定义一个静态的ShaderTagId，用于标识着色器标签
        private readonly static ShaderTagId s_ShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        // 渲染图对象，用于构建渲染流程
        private RenderGraph m_RenderGraph = null;
        // 渲染图记录器，用于记录渲染图的构建过程
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

        // 初始化渲染图的方法

        private void InitializeRenderGraph()

        {

            // 初始化渲染图的句柄系统，设置屏幕宽高
            RTHandles.Initialize(Screen.width, Screen.height);

            // 创建渲染图实例
            m_RenderGraph = new RenderGraph("LiteRenderGraph");

            // 创建渲染图记录器实例
            m_LiteRenderGraphRecorder = new LiteRenderGraphRecorder();

            // 创建上下文容器实例
            m_ContextContainer = new ContextContainer();

        }
        
        //清理渲染图
        private void ClearUpRenderGraph()
        {
            m_ContextContainer?.Dispose();
            m_ContextContainer = null;
            m_LiteRenderGraphRecorder?.Dispose();
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
            
            
            //CommandBuffer 从命令缓冲池中拿到CMD
            CommandBuffer cmd = CommandBufferPool.Get(camera.name);
         
            
            
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
            // 获取相机的剔除参数
            ScriptableCullingParameters cullingParameters;
            if(!camera.TryGetCullingParameters( out cullingParameters))
                return false;
            // 执行剔除操作
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            // 获取或创建相机数据容器
            CameraData cameraData = m_ContextContainer.GetOrCreate<CameraData>();
            // 设置相机数据和剔除结果
            cameraData.camera = camera;
            cameraData.cullingResults = cullingResults;
            return true;
        }
        // 记录并执行渲染图的方法
        private void RecordAndExecuteRenderGraph(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            // 创建渲染图参数
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