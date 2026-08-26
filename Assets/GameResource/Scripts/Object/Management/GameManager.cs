using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            Application.targetFrameRate = 60;
            if (WebBuild.IsPlayer)
            {
                Application.runInBackground = true;
            }

            DisplaySettings.ApplySaved();
        }

        private async UniTask InitializeCore_Internal()
        {
            await AudioManager.InitMixer();
            AudioManager.PreloadSounds();
            TableManager.Init();
        }

        public static UniTask InitializeCore() => Instance.InitializeCore_Internal();
    }
}
