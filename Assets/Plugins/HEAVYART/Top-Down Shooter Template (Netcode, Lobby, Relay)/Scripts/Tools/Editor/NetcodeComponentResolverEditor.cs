using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

//This component is made to prevent from showing popup "NetworkBehaviours require a NetworkObject"
//It adds NetworkObject on select and removes it on deselect. 
//To fix issues just select object (click on it) and deselect it (click on a free space or another object). 
//Resolver will remove NetworkObject component on disable event.

namespace HEAVYART.TopDownShooter.Netcode
{
    [CustomEditor(typeof(NetcodeComponentResolver))]
    public class NetcodeComponentResolverEditor : Editor
    {
        private NetworkObject component;

        private static GameObject root;
        private static GameObject rootOutsidePrefabMode;

        private static NetworkObject objectToDestroy;

        //Object selected
        private void OnEnable()
        {
            //Skip if component is disabled
            if ((target as NetcodeComponentResolver).enabled == false) return;

            if (Application.isPlaying) return;

            root = (target as NetcodeComponentResolver).transform.root.gameObject;

            //Buffer root object in regular editor mode
            if (StageUtility.GetStage(root) == StageUtility.GetMainStage())
                rootOutsidePrefabMode = root;

            component = root.GetComponent<NetworkObject>();

            if (component != null)
            {
                //Existing of the component means that we've just added it (code below) and Editor refreshed itself (fired OnDisable() and OnEnable())
                //So we here again at OnEnable() where we resetting objectToDestroy, while DestroyWithDelay() is delaying after the recent refresh
                objectToDestroy = null;
            }
            else
                component = root.AddComponent<NetworkObject>();

            //Place this component on top
            UnityEditorInternal.ComponentUtility.MoveComponentUp((target as NetcodeComponentResolver));

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += OnApplicationQuit;
            PrefabStage.prefabStageClosing += OnExitPrefabMode;
        }

        //Object deselected
        private void OnDisable()
        {
            //Skip if component is disabled
            if (target != null && (target as NetcodeComponentResolver).enabled == false) return;

            //Don't save data in play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode == true) return;

            if (component != null && component.GetComponent<NetcodeComponentResolver>() != null)
                objectToDestroy = component;

            DestroyWithDelay();
        }

        private async void DestroyWithDelay()
        {
            if (objectToDestroy != null)
            {
                await Task.Delay(10);

                DestroyImmediate(objectToDestroy, true);
            }

            EditorApplication.quitting += OnApplicationQuit;

            if (root == null) return;

            //Save data. Regular editor mode
            if (StageUtility.GetStage(root) == StageUtility.GetMainStage())
                SaveData();
        }

        private void OnExitPrefabMode(PrefabStage obj)
        {
            PrefabStage.prefabStageClosing -= OnExitPrefabMode;

            if (rootOutsidePrefabMode == null) return;

            if (rootOutsidePrefabMode.GetComponent<NetcodeComponentResolver>() != null)
            {
                //Clear data on quit from prefab mode
                DestroyImmediate(rootOutsidePrefabMode.GetComponent<NetworkObject>(), true);
                SaveData();
            }

        }

        private void OnPlayModeChanged(PlayModeStateChange newState)
        {
            //Quit from prefab mode
            if (newState == PlayModeStateChange.ExitingEditMode)
                StageUtility.GoToMainStage();

            //Running a game with object selected in inspector      
            if (newState == PlayModeStateChange.EnteredPlayMode)
            {
                if (rootOutsidePrefabMode.GetComponent<NetcodeComponentResolver>() != null)
                {
                    //Clear data
                    DestroyImmediate(objectToDestroy, true);

                    if (rootOutsidePrefabMode != null)
                        DestroyImmediate(rootOutsidePrefabMode.GetComponent<NetworkObject>(), true);

                    //Deselect object
                    Selection.objects = default;
                }

                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            }
        }

        private void OnApplicationQuit()
        {
            if (rootOutsidePrefabMode.GetComponent<NetcodeComponentResolver>() != null)
            {
                DestroyImmediate(rootOutsidePrefabMode.GetComponent<NetworkObject>(), true);
                SaveData();
            }
        }

        private void SaveData()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }
    }
}
