---
name: generate-unit-portrait
description: >-
  UnitImage 캐릭터 초상화를 배경 없이 생성한다. rembg 금지.
  마젠타 크로마키 생성 후 Tools/PortraitCutout/cutout.py 로 하드 알파를 뽑는다.
  머리가 잘리거나 번짐/헤일로가 생기면 이 스킬을 쓴다.
---

# Unit 초상화 생성

`rembg`와 소프트 매트는 헤어 번짐·체커보드 헤일로를 만든다. 쓰지 않는다.

## 생성

1. `Tools/PortraitCutout/cutout.py`의 `PORTRAIT_PROMPT_PREFIX`를 프롬프트 앞에 붙인다.
2. 배경은 반드시 균일 마젠타 `#FF00FF`. 그라데이션·이펙트·블룸 금지.
3. 머리·헤어 전체가 프레임 안. 상/좌/우 최소 12% 여백. 크라운·옆머리가 가장자리에 닿으면 안 된다.
4.  bust(머리+어깨). 葉佐乃 셀채색 만화 풍. 오리지널 캐릭터.

## 컷아웃

```text
python Tools/PortraitCutout/cutout.py --input <src> --output Assets/GameResource/Images/UnitImage
```

- 테두리에서 크로마를 flood-fill 한다.
- 알파는 0 또는 255만. 반투명 가장자리 금지.
- `head_cropped_top` FAIL 이면 해당 ID를 여백을 더 주고 재생성한다.

## 임포트

Sprite / Single / Alpha Is Transparency / From Input. `.meta`는 만들지 않는다.
