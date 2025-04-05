using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class SetUpLiteRP : MonoBehaviour
{
   [FormerlySerializedAs("CurrentPipelineAsset")] public RenderPipelineAsset currentPipelineAsset;

   private void OnEnable()
   {
      GraphicsSettings.defaultRenderPipeline = currentPipelineAsset;
   }

   private void OnValidate()
   {
      GraphicsSettings.defaultRenderPipeline = currentPipelineAsset;
   }
}
