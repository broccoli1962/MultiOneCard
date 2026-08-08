using System.Collections.Generic;
using System.Threading;
using Backend.AddressableKey;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace Backend.Object.Management
{
    public class AudioManager : SingletonGameObject<AudioManager>
    {
        private static readonly string _audioSourcePoolKey = "AudioSource";
        private readonly List<string> _preloadAudioClipKeys = new() { "popSound" };

        // PlayerPrefs 키
        private const string PREF_BGM_ENABLED = "AudioManager_BgmEnabled";
        private const string PREF_SFX_ENABLED = "AudioManager_SfxEnabled";

        // 로드된 클립을 캐싱할 딕셔너리
        private readonly Dictionary<string, AudioClip> _clips = new();

        // BGM 관련 변수
        private AudioSource _currentBgm;
        private string _currentBgmKey;
        private string _pendingBgmKey;
        private CancellationTokenSource _bgmCancellationTokenSource;
        private const float _fadeDuration = 0.5f;

        // AudioMixer
        private AudioMixer _mixer;
        private AudioMixerGroup _bgmGroup;
        private AudioMixerGroup _sfxGroup;
        private const string AUDIO_SOURCE_POOL_KEY = "AudioMixer";
        private const string MIXER_BGM_PARAM = "BGMVolume";
        private const string MIXER_SFX_PARAM = "SFXVolume";

        // 설정 프로퍼티
        public bool IsBgmEnabled
        {
            get => PlayerPrefs.GetInt(PREF_BGM_ENABLED, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_BGM_ENABLED, value ? 1 : 0);
                ApplyBgmMixerVolume(value);
                if (!value) StopBgmImmediate();
            }
        }

        public bool IsSfxEnabled
        {
            get => PlayerPrefs.GetInt(PREF_SFX_ENABLED, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PREF_SFX_ENABLED, value ? 1 : 0);
                ApplySfxMixerVolume(value);
            }
        }

        private void OnDisable()
        {
            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = null;
        }

        #region Resource Loading (Lazy Load)

        /// <summary>
        /// 캐시를 확인하고 없다면 어드레서블에서 비동기로 로드하여 반환합니다.
        /// </summary>
        private async UniTask<AudioClip> GetOrLoadClipAsync(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string address = AddressableKeys.Sounds.Get(key);

            // 1. 캐시에서 확인
            if (_clips.TryGetValue(key, out var cachedClip))
            {
                return cachedClip;
            }

            // 2. 캐시에 없으면 로드
            AudioClip clip = await ResourceManager.LoadResourceAsync<AudioClip>(address);

            if (clip != null)
            {
                _clips.TryAdd(key, clip);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Failed to load AudioClip from Addressables: {key}");
            }

            return clip;
        }

        #endregion

        #region SFX Internal Methods

        private async UniTaskVoid PlaySfx_InternalAsync(string key, float pitch = 1f)
        {
            if (!IsSfxEnabled) return;

            var audioClip = await GetOrLoadClipAsync(key);
            if (audioClip == null) return;

            var audioSource = await GetOrCreateAudioSource();
            if (audioSource == null) return;

            audioSource.clip = audioClip;
            audioSource.outputAudioMixerGroup = _sfxGroup;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = 1f;
            audioSource.pitch = pitch;
            audioSource.Play();

            ReturnToPoolAfterPlay(audioSource, audioClip.length).Forget();
        }

        private async UniTaskVoid PlaySfx_DelayAsync(string key, float delay, float pitch = 1f)
        {
            if (!IsSfxEnabled) return;

            // 클립을 먼저 로드해둠 (지연 시간 동안 로딩이 겹치지 않도록)
            var audioClip = await GetOrLoadClipAsync(key);
            if (audioClip == null) return;

            await UniTask.WaitForSeconds(delay);

            // 딜레이 중에 설정이 꺼졌을 수 있으므로 다시 체크
            if (!IsSfxEnabled) return;

            var audioSource = await GetOrCreateAudioSource();
            if (audioSource == null) return;

            audioSource.clip = audioClip;
            audioSource.outputAudioMixerGroup = _sfxGroup;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = 1f;
            audioSource.pitch = pitch;
            audioSource.Play();

            ReturnToPoolAfterPlay(audioSource, audioClip.length).Forget();
        }

        #endregion

        #region BGM Internal Methods

        private async UniTaskVoid PlayBgm_Internal(string key)
        {
            _pendingBgmKey = key;

            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _bgmCancellationTokenSource.Token;

            if (!IsBgmEnabled)
            {
                StopBgmImmediate();
                _pendingBgmKey = null;
                return;
            }

            if (_currentBgmKey == key && _currentBgm != null && _currentBgm.isPlaying)
            {
                _pendingBgmKey = null;
                return;
            }

            // 이전 BGM 정지 및 풀 반환
            if (_currentBgm != null)
            {
                var oldBgm = _currentBgm;
                oldBgm.Stop();
                ReturnToPool(oldBgm);
                _currentBgm = null;
                _currentBgmKey = null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var audioClip = await GetOrLoadClipAsync(key);
            if (audioClip == null)
            {
                _pendingBgmKey = null;
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var newAudioSource = await GetOrCreateAudioSource();
            if (newAudioSource == null) return;

            cancellationToken.ThrowIfCancellationRequested();

            // 비동기 로딩 중 다른 BGM 재생 요청이 들어왔는지 확인
            if (_pendingBgmKey != key)
            {
                ReturnToPool(newAudioSource);
                return;
            }

            newAudioSource.clip = audioClip;
            newAudioSource.outputAudioMixerGroup = _bgmGroup;
            newAudioSource.playOnAwake = false;
            newAudioSource.loop = true;
            newAudioSource.volume = 0f;
            newAudioSource.pitch = 1f;
            newAudioSource.Play();

            _currentBgm = newAudioSource;
            _currentBgmKey = key;
            _pendingBgmKey = null;

            await FadeVolume(newAudioSource, 1f, _fadeDuration);
        }

        private void StopBgmImmediate()
        {
            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = null;

            if (_currentBgm != null)
            {
                _currentBgm.Stop();
                ReturnToPool(_currentBgm);
                _currentBgm = null;
                _currentBgmKey = null;
            }
        }

        private void StopBgm_Internal()
        {
            _bgmCancellationTokenSource?.Cancel();
            _bgmCancellationTokenSource?.Dispose();
            _bgmCancellationTokenSource = null;

            if (_currentBgm != null)
            {
                var oldBgm = _currentBgm;
                _currentBgm = null;
                _currentBgmKey = null;
                FadeOutAndReturn(oldBgm).Forget();
            }
        }

        private async UniTaskVoid FadeOutAndReturn(AudioSource source)
        {
            await FadeVolume(source, 0f, _fadeDuration);
            if (source != null)
            {
                source.Stop();
                ReturnToPool(source);
            }
        }

        private static async UniTask FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            if (source == null || duration <= 0f)
            {
                if (source != null) source.volume = targetVolume;
                return;
            }

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (source == null) return;
                source.volume = Mathf.Lerp(startVolume, targetVolume, t);
                await UniTask.Yield();
            }

            if (source != null) source.volume = targetVolume;
        }

        #endregion

        #region Mixer

        private async UniTask InitMixer_InternalAsync()
        {
            string key = AddressableKeys.Sounds.Get(AUDIO_SOURCE_POOL_KEY);
            _mixer = await ResourceManager.LoadResourceAsync<AudioMixer>(key);

            if (_mixer == null)
            {
                Debug.LogError("[AudioManager] Failed to load AudioMixer. BGM/SFX group routing will be skipped.");
                return;
            }

            var bgmGroups = _mixer.FindMatchingGroups("BGM");
            var sfxGroups = _mixer.FindMatchingGroups("SFX");

            _bgmGroup = bgmGroups.Length > 0 ? bgmGroups[0] : null;
            _sfxGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;

            // 저장된 On/Off 상태를 Mixer에 반영
            ApplyBgmMixerVolume(IsBgmEnabled);
            ApplySfxMixerVolume(IsSfxEnabled);
        }

        private void ApplyBgmMixerVolume(bool enabled)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(MIXER_BGM_PARAM, enabled ? 0f : -80f);
        }

        private void ApplySfxMixerVolume(bool enabled)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(MIXER_SFX_PARAM, enabled ? 0f : -80f);
        }

        private void SetBgmVolume_Internal(float linear)
        {
            if (_mixer == null) return;
            float db = Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
            _mixer.SetFloat(MIXER_BGM_PARAM, db);
        }

        private void SetSfxVolume_Internal(float linear)
        {
            if (_mixer == null) return;
            float db = Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
            _mixer.SetFloat(MIXER_SFX_PARAM, db);
        }

        #endregion

        #region Pooling

        private async UniTask<AudioSource> GetOrCreateAudioSource()
        {
            var pool = await ObjectPoolManager.GetOrCreatePoolAsync<AudioSource>(AddressableKeys.InGame.Get(_audioSourcePoolKey), defaultCapacity: 8, maxSize: 20);
            if (pool == null)
            {
                Debug.LogError($"[AudioManager] Failed to get AudioSource from PoolManager: {_audioSourcePoolKey}");
                return null;
            }
            return pool.Get();
        }

        private async UniTaskVoid PreloadAudioClip_Internal(){
            foreach (var key in _preloadAudioClipKeys){
                var audioClip = await GetOrLoadClipAsync(key);
                if (audioClip == null){
                    Debug.LogError($"[AudioManager] Failed to get AudioClip from PoolManager Preload: {key}");
                }
            }
        }

        private void ReturnToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.outputAudioMixerGroup = null;
            audioSource.volume = 1f;
            audioSource.pitch = 1f;

            ObjectPoolManager.Release(audioSource);
        }

        private async UniTaskVoid ReturnToPoolAfterPlay(AudioSource source, float duration)
        {
            await UniTask.Delay((int)(duration * 1000));
            if (source != null) ReturnToPool(source);
        }

        #endregion

        #region Static Public Methods

        /// <summary>
        /// AudioMixer 에셋을 로드하고 BGM/SFX 그룹을 초기화합니다. Boot 초기화 시 1회 호출됩니다.
        /// </summary>
        public static UniTask InitMixer() => Instance.InitMixer_InternalAsync();

        /// <summary>
        /// BGM 볼륨을 설정합니다 (0~1 선형 값, 내부적으로 dB 변환).
        /// </summary>
        public static void SetBgmVolume(float linear) => Instance.SetBgmVolume_Internal(linear);

        /// <summary>
        /// SFX 볼륨을 설정합니다 (0~1 선형 값, 내부적으로 dB 변환).
        /// </summary>
        public static void SetSfxVolume(float linear) => Instance.SetSfxVolume_Internal(linear);

        /// <summary>
        /// BGM 재생 (페이드 인 적용)
        /// </summary>
        public static void PlayBgm(string key) => Instance.PlayBgm_Internal(key).Forget();

        /// <summary>
        /// BGM 정지 (페이드 아웃 적용)
        /// </summary>
        public static void StopBgm() => Instance.StopBgm_Internal();

        /// <summary>
        /// 사운드 효과음 재생
        /// </summary>
        public static void PlaySfx(string key, float pitch = 1f) => Instance.PlaySfx_InternalAsync(key, pitch).Forget();

        /// <summary>
        /// 딜레이 후 사운드 효과음 재생
        /// </summary>
        public static void PlaySfxDelay(string key, float delay, float pitch = 1f) => Instance.PlaySfx_DelayAsync(key, delay, pitch).Forget();

        /// <summary>
        /// 사운드 효과음 프리로드
        /// </summary>
        public static void PreloadSounds() => Instance.PreloadAudioClip_Internal().Forget();

        #endregion
    }
}
