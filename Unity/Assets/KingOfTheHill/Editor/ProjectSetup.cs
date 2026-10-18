using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WinterYard;

namespace WinterYardEditor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string Root="Assets/KingOfTheHill";
        const string ScenePath=Root+"/Scenes/WinterYard.unity";
        static ProjectSetup(){EditorApplication.delayCall+=FirstOpen;}

        static void FirstOpen()
        {
            if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
            // Only initialize once. Never overwrite a user's existing scene or pipeline.
            if(!File.Exists(ScenePath)||!GraphicsSettings.defaultRenderPipeline)Prepare();
        }

        [MenuItem("Winter Yard/Prepare project")]
        public static void Prepare() { PrepareProject(); }

        static bool PrepareProject()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return false;
            Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Rendering");AssetDatabase.Refresh();
            if(!GraphicsSettings.defaultRenderPipeline)
            {
                const string rendererPath=Root+"/Rendering/YardRenderer.asset";
                const string pipelinePath=Root+"/Rendering/YardURP.asset";
                var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,rendererPath);}
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
                if(!pipeline)
                {
                    pipeline=UniversalRenderPipelineAsset.Create(renderer);
                    pipeline.name="Winter Yard URP";pipeline.renderScale=1;
                    pipeline.shadowDistance=45;pipeline.msaaSampleCount=2;
                    AssetDatabase.CreateAsset(pipeline,pipelinePath);
                }
                GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            }
            // Runtime-created materials need these shaders retained in standalone builds.
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(var name in new[]{"Universal Render Pipeline/Lit","Universal Render Pipeline/Particles/Unlit"})
            {
                var shader=Shader.Find(name);if(!shader)throw new Exception("Missing shader: "+name);
                bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.companyName="Winter Yard";PlayerSettings.productName="Царь горы — Зимний двор";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;
            PlayerSettings.runInBackground=false;
            // This project deliberately uses the legacy Input API (no Input System package).
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler");
            if(input==null)throw new Exception("Cannot configure Active Input Handling. Set it to Input Manager (Old) in Player Settings.");
            if(input.intValue!=0)
            {
                input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();
                Debug.LogWarning("Active Input Handling changed to Input Manager (Old). Restart Unity before playing or building.");
            }
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Winter Yard").AddComponent<GameSession>();
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            if(!Application.isBatchMode)EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Winter Yard prepared. Open WinterYard scene and press Play.");
            return true;
        }

        [MenuItem("Winter Yard/Build Windows x64")]
        public static void BuildWindows()
        {
            if(!PrepareProject())return;
            string output=Environment.GetEnvironmentVariable("WINTER_YARD_BUILD_PATH");
            if(string.IsNullOrEmpty(output))output="Builds/Windows/KingOfTheHill.exe";
            Build(BuildTarget.StandaloneWindows64,output);
        }

        [MenuItem("Winter Yard/Build macOS (Universal)")]
        public static void BuildMac()
        {
            if(!PrepareProject())return;
            string output=Environment.GetEnvironmentVariable("WINTER_YARD_MAC_BUILD_PATH");
            if(string.IsNullOrEmpty(output))output="Builds/macOS/KingOfTheHill.app";
            int previous=PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone);
            try
            {
                // 2 = Intel x86_64 + Apple silicon universal macOS player.
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,2);
                Build(BuildTarget.StandaloneOSX,output);
            }
            finally { PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,previous); }
        }

        static void Build(BuildTarget target,string output)
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,target))
                throw new Exception("Missing platform module for "+target+". Install matching Build Support in Unity Hub.");
            var directory=Path.GetDirectoryName(output);
            if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{ScenePath},locationPathName=output,
                target=target,options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception(target+" build failed: "+report.summary.result);
            Debug.Log(target+" build succeeded: "+Path.GetFullPath(output));
        }
    }
}
