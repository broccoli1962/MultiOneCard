using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    /// <summary>
    /// 조커 위계 반원 게이지. 공격값이 가장 큰 색(흑·적·청) 쪽으로 레버가 움직인다.
    /// </summary>
    public sealed class JokerGaugeView : UIView
    {
        public const float BlackAngle = 60f;
        public const float RedAngle = 0f;
        public const float BlueAngle = -60f;

        [SerializeField] private Image _body;
        [SerializeField] private RectTransform _lever;

        private float _targetZ;
        private bool _snapped;

        /// <summary>
        /// 프리팹에 묶인 바디·레버를 찾는다.
        /// </summary>
        public void EnsureLayout()
        {
            if (_body == null)
            {
                var bodyTf = CachedTransform.Find("Body");
                if (bodyTf == null || !bodyTf.TryGetComponent(out _body))
                {
                    TryGetComponent(out _body);
                }
            }

            if (_lever == null)
            {
                var leverTf = CachedTransform.Find("Lever");
                if (leverTf == null)
                {
                    leverTf = CachedTransform.Find("Body/Lever");
                }

                if (leverTf != null)
                {
                    _lever = leverTf as RectTransform;
                }
            }

            if (_body != null)
            {
                _body.raycastTarget = false;
                _body.preserveAspect = true;
            }

            if (_lever != null)
            {
                var image = _lever.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                    image.preserveAspect = true;
                }
            }
        }

        /// <summary>
        /// 현재 조커 공격값으로 레버 목표 각을 정한다. 동률이면 청→적→흑.
        /// </summary>
        public void Bind(int jokerBw, int jokerColor, int jokerMoon)
        {
            EnsureLayout();
            _targetZ = BlueAngle;
            var max = jokerMoon;
            if (jokerColor > max)
            {
                max = jokerColor;
                _targetZ = RedAngle;
            }

            if (jokerBw > max)
            {
                _targetZ = BlackAngle;
            }

            if (_lever != null && !_snapped)
            {
                _lever.localRotation = Quaternion.Euler(0f, 0f, _targetZ);
                _snapped = true;
            }
        }

        /// <summary>
        /// 레버를 목표 각으로 보간한다.
        /// </summary>
        public void Tick()
        {
            if (_lever == null)
            {
                return;
            }

            var z = Mathf.LerpAngle(_lever.localEulerAngles.z, _targetZ, Time.deltaTime * 10f);
            _lever.localRotation = Quaternion.Euler(0f, 0f, z);
        }
    }
}
