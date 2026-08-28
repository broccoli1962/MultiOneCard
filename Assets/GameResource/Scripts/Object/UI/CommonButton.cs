using Backend.Util;
using R3;
using System;
using Backend.Object.Management;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace Backend.Object.UI
{
    public class CommonButton : CachedMonobehaviour, IPointerClickHandler
    {
        public bool interactable = true;
        public bool useAnimation = false;
        public bool useSound = true;
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(useSound))]
        public bool useCustomSoundId = false;
        
        [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(useCustomSoundId))]
        public string customSoundId = "sfx_button";
        public UnityEvent OnClick;

        public Observable<Unit> OnClickAsObservable() =>
            Observable.FromEvent<UnityAction>(
                h => new UnityAction(h),
                h => OnClick.AddListener(h),
                h => OnClick.RemoveListener(h)
            );

#if UNITY_EDITOR
        private void Reset()
        {
            MigrateLegacyButtonIfNeeded();
        }

        private void OnValidate()
        {
            MigrateLegacyButtonIfNeeded();
        }
        
        private void MigrateLegacyButtonIfNeeded()
        {
            if(Application.isPlaying) return;

            Button legacyButton = GetComponent<Button>();

            if(legacyButton == null) return;

            Undo.RecordObject(this, "Migrate Button OnClick To CommonButton");
            ClearPersistentListeners(OnClick);

            int listenerCount = legacyButton.onClick.GetPersistentEventCount();
            for(int i = 0; i < listenerCount; i++){
                UnityEngine.Object target = legacyButton.onClick.GetPersistentTarget(i);
                string methodName = legacyButton.onClick.GetPersistentMethodName(i);
                UnityEventCallState callState = legacyButton.onClick.GetPersistentListenerState(i);
                
                if(target == null || string.IsNullOrEmpty(methodName)) continue;

                if (Delegate.CreateDelegate(typeof(UnityAction), target, methodName, false) is not UnityAction action) continue;

                UnityEventTools.AddPersistentListener(OnClick, action);
                int addedIndex = OnClick.GetPersistentEventCount() - 1;
                OnClick.SetPersistentListenerState(addedIndex, callState);
            }

            EditorUtility.SetDirty(this);
            Undo.DestroyObjectImmediate(legacyButton);
        }

        private static void ClearPersistentListeners(UnityEvent unityEvent){
            for(int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--){
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
        }
#endif
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable){
                eventData.Use();
                return;
            }

            OnClick?.Invoke();

            if(useAnimation)
                gameObject.ButtonAnimation();
            
            eventData.Use();

            if(useSound){
                if(useCustomSoundId){
                    AudioManager.PlaySfx(customSoundId);
                }
                else{
                    AudioManager.PlaySfx("sfx_button");
                }
            }
        }

        private void OnDisable(){
            if(GameStateUtil.IsQuitting) return;

            gameObject.StopButtonAnimation();
        }
    }
}