using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Unity.Netcode;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class SceneLoadManager : Singleton<SceneLoadManager>
    {
        [SerializeField]
        private string firstSceneToLoad = "MainMenu";
        public float loadingProgress { get; private set; }

        public Action OnLoadingStarted;
        public Action OnLoadingFinished;

        private void Awake()
        {
            DontDestroyOnLoad(this);

            if (firstSceneToLoad.Length > 0)
                StartCoroutine(ProcessRegularSceneLoading(firstSceneToLoad, true));
        }

        public void LoadNetworkScene(string sceneName)
        {
            OnLoadingStarted?.Invoke();

            //Switch to loading scene first
            SceneManager.LoadScene("LoadingScene");

            //Fires on LoadScene command below
            NetworkManager.Singleton.SceneManager.OnLoad += OnServerLoadScene;

            NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        private void OnServerLoadScene(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            StartCoroutine(ProcessNetworkSceneLoading(asyncOperation));
        }

        private IEnumerator ProcessNetworkSceneLoading(AsyncOperation asyncOperation)
        {
            loadingProgress = 0f;

            while (!asyncOperation.isDone)
            {
                loadingProgress = asyncOperation.progress;

                if (asyncOperation.progress > 0.9f)
                {
                    loadingProgress = 1;
                }

                yield return new WaitForFixedUpdate();
            }

            SceneManager.UnloadSceneAsync("LoadingScene");

            OnLoadingFinished?.Invoke();
        }
        public void SubscribeOnNetworkSceneUpdates()
        {
            NetworkManager.Singleton.SceneManager.OnSynchronize += OnClientSynchronizeScene;
            NetworkManager.Singleton.SceneManager.OnLoad += OnClientLoadScene;
        }

        public void UnsubscribeNetworkSceneUpdates()
        {
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSynchronize -= OnClientSynchronizeScene;
                NetworkManager.Singleton.SceneManager.OnLoad -= OnClientLoadScene;
                NetworkManager.Singleton.SceneManager.OnLoad -= OnServerLoadScene;
            }
        }
        private void OnClientSynchronizeScene(ulong clientID)
        {
            OnLoadingStarted?.Invoke();
            //Switch to loading scene first
            SceneManager.LoadScene("LoadingScene");
        }

        private void OnClientLoadScene(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
        {
            //Load scene synchronized with server (additive)
            StartCoroutine(ProcessNetworkSceneLoading(asyncOperation));
        }

        public void LoadRegularScene(string sceneName, bool useLoadScreen = true)
        {
            StartCoroutine(ProcessRegularSceneLoading(sceneName, useLoadScreen));
        }

        private IEnumerator ProcessRegularSceneLoading(string sceneToLoad, bool useLoadScene = true)
        {
            if (useLoadScene)
                SceneManager.LoadScene("LoadingScene");

            loadingProgress = 0f;

            AsyncOperation ao = SceneManager.LoadSceneAsync(sceneToLoad);
            ao.allowSceneActivation = true;

            while (!ao.isDone)
            {
                loadingProgress = ao.progress;

                if (ao.progress > 0.9f)
                {
                    loadingProgress = 1;
                }

                yield return 0;
            }
        }
    }
}