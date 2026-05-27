# ARK_SA_FontChanger

ARK: Survival Ascended font changer for Korean UI readability.

## 제작 취지

ARK: Survival Ascended를 한국어로 플레이할 때 일부 UI, 채팅창, 생물 정보, 부족 로그 등의 글자가 너무 가늘거나 읽기 어려운 문제가 있어 이를 개선하기 위해 제작했습니다.

이 도구는 사용자가 선택한 폰트를 `ShooterGame-Windows_P.pak`에 적용하거나, 해당 파일이 없을 경우 새로 생성합니다. 원본 클라이언트 파일을 직접 수정하지 않는 방식으로 동작합니다.

## 주요 기능

- 대부분의 UI 텍스트와 채팅창 폰트 변경
- 하단 생물 이름, 부족 로그, 핫바 하단 폰트 변경
- 이름표시 가독성 개선 모드 설치/복원
- 기존 유저 번역 패치가 있는 경우 해당 PAK에 폰트 적용
- 기존 패치가 없는 경우 새 `ShooterGame-Windows_P.pak` 생성
- 원본/기존 PAK 백업 옵션
- 설정 자동 저장

## 사용법

1. `ARK_SA_FontChanger.exe`를 실행합니다.
2. ARK: Survival Ascended 설치 경로를 선택합니다.
   - 예: `...\SteamLibrary\steamapps\common\ARK Survival Ascended`
3. `FontHere` 폴더에 사용할 `.ttf` 또는 `.otf` 폰트를 넣습니다.
4. 프로그램에서 `커스텀폰트 새로불러오기`를 누릅니다.
5. Part 1, Part 2에 적용할 폰트를 선택합니다.
6. 필요하면 `원본/기존 PAK 백업`을 체크합니다.
7. `폰트 적용하기`를 누릅니다.

## 폴더 구조

```text
ARK_SA_FontChanger.exe
DATA
FontHere
```

- `ARK_SA_FontChanger.exe`: 실행 파일
- `DATA`: 패킹 도구와 이름표시 개선용 데이터
- `FontHere`: 사용자가 적용할 폰트를 넣는 폴더

## 적용 파트

- Part 1: 대부분의 UI 텍스트와 채팅창
- Part 2: 하단 생물 이름 / 부족 로그 / 핫바 하단 폰트

## 주의사항

- 이 도구는 한국어 버전에서만 테스트를 완료했습니다.
- 다른 언어에서 정상 작동을 보장하지 않습니다.
- 일본어, 영어 등 다른 언어용 폰트 경로는 게임 내부에서 다를 수 있습니다.
- 다른 언어를 지원하려면 소스 코드에서 해당 언어의 폰트 경로를 찾아 수정해서 사용해 주세요.
- 기존 `ShooterGame-Windows_P.pak`가 있는 경우, 해당 파일은 유저 번역 패치일 수 있습니다. 적용 전 백업 옵션 사용을 권장합니다.

## English

## Purpose

This tool was created because ARK: Survival Ascended can be difficult to read when playing with the Korean UI. Some UI text, chat text, creature info, and tribe log text may appear too thin or unclear.

The tool applies selected fonts to `ShooterGame-Windows_P.pak`. If that file does not exist, it creates a new one. It does not directly modify the original client files.

## Features

- Change fonts for most UI text and the chat window
- Change fonts for bottom creature names, tribe logs, and lower hotbar text
- Install or restore a nameplate readability improvement mod
- Apply font changes to an existing user translation patch
- Create a new `ShooterGame-Windows_P.pak` if no patch exists
- Optional backup of existing/original PAK files
- Automatic settings save

## How to Use

1. Run `ARK_SA_FontChanger.exe`.
2. Select your ARK: Survival Ascended install path.
   - Example: `...\SteamLibrary\steamapps\common\ARK Survival Ascended`
3. Put your `.ttf` or `.otf` font files into the `FontHere` folder.
4. Click `Reload custom fonts`.
5. Select fonts for Part 1 and Part 2.
6. Enable backup if needed.
7. Click `Apply font`.

## Folder Structure

```text
ARK_SA_FontChanger.exe
DATA
FontHere
```

- `ARK_SA_FontChanger.exe`: application executable
- `DATA`: packing tool and nameplate readability data
- `FontHere`: folder for user font files

## Font Targets

- Part 1: Most UI text and chat window
- Part 2: Bottom creature name / tribe log / lower hotbar font

## Notes

- This tool has only been tested with the Korean version.
- Normal operation in other languages is not guaranteed.
- Font paths for Japanese, English, or other languages may be different inside the game files.
- Source code is provided so users can modify the target font paths for other languages if needed.
- If `ShooterGame-Windows_P.pak` already exists, it may be a user translation patch. Using the backup option is recommended before applying changes.

## License / Credits

Created by Youtube / CHZZK 조마일즈  
Contact: mileschokr@gmail.com

