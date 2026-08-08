using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Management
{
    public class GameManager : SingletonGameObject<GameManager>
    {
        protected override void OnAwake()
        {
            base.OnAwake();

            Application.targetFrameRate = 60;
        }

        private async UniTask InitializeCore_Internal()
        {
            await AudioManager.InitMixer();
            TableManager.Init();
        }

        private void StartGameplay_Internal()
        {

        }

        private void EndGameplay_Internal()
        {
        }

        private void GameOver_Internal()
        {
        }

        private void StageClear_Internal()
        {
        }

        private void SetPhase_Internal(){
        }

#region Static Public Methods
        public static void EndGameplay() => Instance.EndGameplay_Internal();
        public static void GameOver() => Instance.GameOver_Internal();
        public static void StageClear() => Instance.StageClear_Internal();
        public static void StartGameplay() => Instance.StartGameplay_Internal();
        public static void SetPhase() => Instance.SetPhase_Internal();
        public static UniTask InitializeCore() => Instance.InitializeCore_Internal();
#endregion
    }
}
