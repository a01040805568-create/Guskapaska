# 구스카파스카 (Guskapaska)

가위바위보 + 코인/젬 베팅 메커니즘이 결합된 1인용 카드 게임.

## 기술 스택

- Unity 6.3 LTS (6000.3.15f1)
- 2D Built-In Render Pipeline
- UGUI + TextMeshPro
- 타겟 해상도: 1920×1080 (Landscape)
- 타겟 플랫폼: Windows / Mac Standalone

## 개발 시작하기

### 1. 의존성 설치
- Unity 6.3 LTS (6000.3.15f1) 설치 (Unity Hub 사용)
- Git LFS 설치: https://git-lfs.com

### 2. 리포 클론
\`\`\`bash
git lfs install
git clone https://github.com/YOUR_USERNAME/Guskapaska.git
cd Guskapaska
\`\`\`

### 3. Unity Hub로 열기
- Unity Hub → Projects → Add → 클론한 폴더 선택
- 첫 실행 시 Library 폴더 재생성에 몇 분 소요됨

### 4. 작업 시작
- `Assets/Scenes/MainMenu.unity` 부터 시작
- 기획 문서: `docs/00_GameDesign.md`, `docs/01_ProjectOverview.md`
- Unity 6 주의사항: `docs/02_Unity6_Guidelines.md`

## 브랜치 규칙

- `main`: 안정 버전, 직접 푸시 금지
- `dev`: 개발 통합 브랜치
- `feature/<stage>-<name>`: 기능별 브랜치 (예: `feature/stage1-core-logic`)

## 커밋 메시지 규칙

\`\`\`
<type>: <subject>

<body (optional)>
\`\`\`

타입:
- `feat`: 새 기능
- `fix`: 버그 수정
- `refactor`: 리팩토링
- `style`: 포맷팅
- `docs`: 문서
- `chore`: 빌드/설정 등
- `asset`: 아트/사운드 등 에셋 변경

## 라이선스

(추후 결정)