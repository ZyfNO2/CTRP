using System;
using LiteRP.FrameData;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public partial class LiteRenderGraphRecorder : IRenderGraphRecorder,IDisposable
    {
        private TextureHandle m_BackbufferColorHandle;
        private RTHandle m_TargetColorHandle;
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            CameraData cameraData = frameData.Get<CameraData>();
            
            CreateRenderGraphCameraRenderTargets(renderGraph, cameraData);
            AddSetupCameraPropertiesPass(renderGraph,cameraData);
            AddDrawObjectsPass(renderGraph,cameraData);
        }

        private void CreateRenderGraphCameraRenderTargets(RenderGraph renderGraph, CameraData cameraData)
        {

            // 设置目标颜色标识符为摄像机目标
            RenderTargetIdentifier targetColorId = BuiltinRenderTextureType.CameraTarget;

            // 如果m_TargetColorHandle为空，则分配一个新的渲染目标句柄
            if(m_TargetColorHandle == null)
                m_TargetColorHandle = RTHandles.Alloc((RenderTargetIdentifier)targetColorId, name:"BackBuffer color");

            // 获取摄像机背景颜色，并将其从SRGB颜色空间转换为当前激活的颜色空间
            Color cameraBackgroundColor = CoreUtils.ConvertSRGBToActiveColorSpace(cameraData.camera.backgroundColor);
            // 创建导入资源参数对象，用于导入后台缓冲区颜色
            ImportResourceParams importBackbufferColorParams = new ImportResourceParams();
            // 设置在首次使用时清除资源
            importBackbufferColorParams.clearOnFirstUse = true;
            // 设置清除颜色为摄像机背景颜色
            importBackbufferColorParams.clearColor = cameraBackgroundColor;
            // 设置在最后一次使用后不丢弃资源
            importBackbufferColorParams.discardOnLastUse = false;
            // 判断当前激活的颜色空间是否为线性空间
            bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

            // 创建渲染目标信息对象，用于描述渲染目标的属性
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            // 设置渲染目标的宽度为屏幕宽度
            importInfoColor.width = Screen.width;
            // 设置渲染目标的高度为屏幕高度
            importInfoColor.height = Screen.height;
            // 设置渲染目标的体积深度为1
            importInfoColor.volumeDepth = 1;
            // 设置渲染目标的MSAA采样数为1
            importInfoColor.msaaSamples = 1;
            // 根据当前颜色空间设置渲染目标的格式
            importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB);
            // 导入纹理到渲染图中，并将句柄赋值给m_BackbufferColorHandle
            m_BackbufferColorHandle = renderGraph.ImportTexture(m_TargetColorHandle, importInfoColor, importBackbufferColorParams);

        }

        public void Dispose()
        {
            RTHandles.Release(m_TargetColorHandle);
            // TODO 在此释放托管资源
            GC.SuppressFinalize(this);
        }
    }
}