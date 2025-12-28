# 급수 관경 결정 계산 - 모바일 웹 개발 기술 문서

## 1. 개요

### 1.1 목적
건물의 급수 부하단위(Fixture Unit, FU)법을 기반으로 급수 배관의 구경(Pipe Size)을 산정하는 모바일 웹 애플리케이션 개발

### 1.2 기술 스택
- **HTML5**: 구조 및 마크업
- **Tailwind CSS (CDN)**: 스타일링 및 반응형 디자인
- **Shadcn UI (CDN)**: UI 컴포넌트
- **Vanilla JavaScript**: 계산 로직 및 인터랙션
- **모바일 퍼스트**: 반응형 웹 디자인

### 1.3 주요 기능
1. 7가지 위생기구 수량 입력 (대변기, 소변기, 세면기, 청소씽크, 샤워, 욕조, 주방씽크)
2. 실시간 계산 및 결과 표시
3. 상세 계산 과정 표시
4. 모바일 최적화 UI

---

## 2. 데이터 모델

### 2.1 기구 타입 (FixtureType)
```javascript
const FixtureType = {
    name: String,           // 기구명
    unitFactor: Number,     // 15A 환산값 (부하단위)
    minPipeSize: Number,    // 최소 접속 관경 (A)
    isFlushValve: Boolean   // 세정밸브 여부 (대변기 구분용)
};

// 기본 기구 데이터
const FIXTURES = [
    { name: "대변기", unitFactor: 4.9, minPipeSize: 25, isFlushValve: true },
    { name: "소변기", unitFactor: 1.0, minPipeSize: 15, isFlushValve: false },
    { name: "세면기", unitFactor: 1.0, minPipeSize: 15, isFlushValve: false },
    { name: "청소씽크", unitFactor: 2.6, minPipeSize: 20, isFlushValve: false },
    { name: "샤워", unitFactor: 1.0, minPipeSize: 15, isFlushValve: false },
    { name: "욕조", unitFactor: 2.6, minPipeSize: 20, isFlushValve: false },
    { name: "주방씽크", unitFactor: 1.0, minPipeSize: 15, isFlushValve: false }
];
```

### 2.2 계산 결과 (CalcResult)
```javascript
const CalcResult = {
    genLoadSum: Number,      // 일반기구 부하합계
    genRate: Number,         // 일반기구 동시사용율
    genEffective: Number,    // 일반기구 유효부하
    fvLoadSum: Number,       // 대변기 부하합계
    fvRate: Number,          // 대변기 동시사용율
    fvEffective: Number,     // 대변기 유효부하
    fvQtySum: Number,        // 대변기 수량합계
    totalEffective: Number,  // 최종 유효부하
    mainSize: Number         // 최종 결정 관경 (A)
};
```

---

## 3. 핵심 계산 로직

### 3.1 관경 선정 기준표
```javascript
const PIPE_CAPACITY_TABLE = {
    1.0: 15,
    2.6: 20,
    4.9: 25,
    9.2: 32,
    14.5: 40,
    30.0: 50,
    53.0: 65,
    84.6: 80,
    178.0: 100,
    280.0: 125,
    452.0: 150
};
```

### 3.2 대변기(세정밸브) 동시사용률표
**기준: 수량(개수)**
```javascript
function getFlushValveSimultaneity(qty) {
    if (qty <= 0) return 0;
    if (qty <= 2) return 1.0;
    if (qty === 3) return 0.825;
    if (qty === 4) return 0.65;
    if (qty === 5) return 0.60;
    if (qty <= 7) return 0.50;
    if (qty <= 10) return 0.43;
    return 0.40;
}
```

### 3.3 일반기구 동시사용률표
**기준: 부하단위 합계(FU)**
```javascript
function getGeneralSimultaneity(loadSum) {
    if (loadSum <= 0) return 0;
    if (loadSum < 2) return 1.0;
    if (loadSum < 3) return 1.0;
    if (loadSum < 4) return 0.90;
    if (loadSum < 5) return 0.80;
    if (loadSum < 10) return 0.70;
    if (loadSum < 20) return 0.60;
    if (loadSum < 32) return 0.48;
    if (loadSum < 40) return 0.45;
    if (loadSum < 50) return 0.40;
    return 0.35;
}
```

### 3.4 관경 결정 함수
```javascript
function determineSize(effectiveLoad) {
    if (effectiveLoad <= 0) return 0;
    
    const limits = Object.keys(PIPE_CAPACITY_TABLE)
        .map(k => parseFloat(k))
        .sort((a, b) => a - b);
    
    for (let limit of limits) {
        if (effectiveLoad <= limit) {
            return PIPE_CAPACITY_TABLE[limit];
        }
    }
    
    // 최대값 초과 시
    return PIPE_CAPACITY_TABLE[limits[limits.length - 1]];
}
```

### 3.5 메인 계산 함수
```javascript
function calculate(quantities) {
    // quantities = [대변기, 소변기, 세면기, 청소씽크, 샤워, 욕조, 주방씽크]
    
    const result = {
        genLoadSum: 0,
        genRate: 0,
        genEffective: 0,
        fvLoadSum: 0,
        fvRate: 0,
        fvEffective: 0,
        fvQtySum: 0,
        totalEffective: 0,
        mainSize: 0
    };
    
    // 1. 그룹 분리 및 부하 계산
    let fvInputs = [];  // 대변기 그룹
    let genInputs = []; // 일반기구 그룹
    
    quantities.forEach((qty, index) => {
        const fixture = FIXTURES[index];
        const load = qty * fixture.unitFactor;
        
        if (fixture.isFlushValve) {
            fvInputs.push({ quantity: qty, load: load });
        } else {
            genInputs.push({ quantity: qty, load: load });
        }
    });
    
    // 2. 대변기 그룹 계산
    result.fvQtySum = fvInputs.reduce((sum, item) => sum + item.quantity, 0);
    result.fvLoadSum = fvInputs.reduce((sum, item) => sum + item.load, 0);
    result.fvRate = getFlushValveSimultaneity(result.fvQtySum);
    result.fvEffective = result.fvLoadSum * result.fvRate;
    
    // 3. 일반기구 그룹 계산
    result.genLoadSum = genInputs.reduce((sum, item) => sum + item.load, 0);
    result.genRate = getGeneralSimultaneity(result.genLoadSum);
    result.genEffective = result.genLoadSum * result.genRate;
    
    // 4. 최종 계산
    result.totalEffective = result.genEffective + result.fvEffective;
    result.mainSize = determineSize(result.totalEffective);
    
    return result;
}
```

### 3.6 간편 계산 함수 (외부 호출용)
```javascript
/**
 * 간편 계산 함수
 * @param {number} 대변기개수
 * @param {number} 소변기개수
 * @param {number} 세면기개수
 * @param {number} 청소씽크개수
 * @param {number} 샤워개수
 * @param {number} 욕조개수
 * @param {number} 주방씽크개수
 * @returns {number} 결정된 관경(A)
 */
function calcSelDia(대변기, 소변기, 세면기, 청소씽크, 샤워, 욕조, 주방씽크) {
    const quantities = [대변기, 소변기, 세면기, 청소씽크, 샤워, 욕조, 주방씽크];
    const result = calculate(quantities);
    return result.mainSize;
}

// 사용 예시
const dia = calcSelDia(3, 8, 8, 3, 2, 4, 0); // 50
```

---

## 4. UI 구성

### 4.1 레이아웃 구조
```
┌─────────────────────────────────┐
│        헤더 (타이틀)             │
├─────────────────────────────────┤
│                                 │
│     입력 영역 (7개 기구)         │
│     - Input 필드들              │
│                                 │
├─────────────────────────────────┤
│     계산 버튼                    │
├─────────────────────────────────┤
│                                 │
│     결과 표시 영역               │
│     - 최종 관경 (큰 글씨)        │
│     - 상세 계산 결과             │
│                                 │
└─────────────────────────────────┘
```

### 4.2 주요 UI 컴포넌트

#### 입력 필드
```html
<!-- 예시: 대변기 입력 -->
<div class="space-y-2">
    <label class="text-sm font-medium">대변기</label>
    <input 
        type="number" 
        id="toilet" 
        min="0" 
        value="0"
        class="w-full px-4 py-3 text-lg border rounded-lg"
        placeholder="0">
</div>
```

#### 계산 버튼
```html
<button 
    id="calculateBtn"
    class="w-full py-4 text-lg font-bold text-white bg-blue-600 rounded-lg">
    계산하기
</button>
```

#### 결과 표시
```html
<div class="result-section">
    <!-- 최종 관경 -->
    <div class="text-center">
        <div class="text-sm text-gray-600">최종 결정 관경</div>
        <div id="mainSize" class="text-5xl font-bold text-red-600">
            - - A
        </div>
    </div>
    
    <!-- 상세 결과 -->
    <div class="grid grid-cols-2 gap-4 mt-6">
        <div>
            <div class="text-xs text-gray-500">일반기구 부하</div>
            <div id="genLoad" class="text-lg font-semibold">-</div>
        </div>
        <div>
            <div class="text-xs text-gray-500">대변기 부하</div>
            <div id="fvLoad" class="text-lg font-semibold">-</div>
        </div>
        <!-- 추가 상세 정보... -->
    </div>
</div>
```

---

## 5. 이벤트 핸들링

### 5.1 계산 버튼 클릭
```javascript
document.getElementById('calculateBtn').addEventListener('click', function() {
    // 1. 입력값 수집
    const quantities = [
        parseInt(document.getElementById('toilet').value) || 0,
        parseInt(document.getElementById('urinal').value) || 0,
        parseInt(document.getElementById('sink').value) || 0,
        parseInt(document.getElementById('cleaningSink').value) || 0,
        parseInt(document.getElementById('shower').value) || 0,
        parseInt(document.getElementById('bathtub').value) || 0,
        parseInt(document.getElementById('kitchenSink').value) || 0
    ];
    
    // 2. 계산 수행
    const result = calculate(quantities);
    
    // 3. 결과 표시
    displayResult(result);
});
```

### 5.2 결과 표시 함수
```javascript
function displayResult(result) {
    // 최종 관경
    document.getElementById('mainSize').textContent = 
        result.mainSize > 0 ? `${result.mainSize} A` : '- - A';
    
    // 일반기구 정보
    document.getElementById('genLoad').textContent = result.genLoadSum.toFixed(1);
    document.getElementById('genRate').textContent = result.genRate.toFixed(2);
    document.getElementById('genEffective').textContent = result.genEffective.toFixed(2);
    
    // 대변기 정보
    document.getElementById('fvLoad').textContent = result.fvLoadSum.toFixed(1);
    document.getElementById('fvRate').textContent = 
        `${result.fvRate.toFixed(2)} (수량: ${result.fvQtySum}개)`;
    document.getElementById('fvEffective').textContent = result.fvEffective.toFixed(2);
    
    // 최종 유효부하
    document.getElementById('totalEffective').textContent = result.totalEffective.toFixed(2);
}
```

---

## 6. 모바일 최적화

### 6.1 반응형 디자인 원칙
- **모바일 퍼스트**: 320px 이상 기준
- **터치 친화적**: 버튼 최소 44x44px
- **가독성**: 폰트 크기 최소 16px
- **입력 편의성**: Number input에 적절한 키패드

### 6.2 Tailwind CSS 브레이크포인트
```html
<!-- 모바일 기본, 태블릿 이상에서 2열 -->
<div class="grid grid-cols-1 md:grid-cols-2 gap-4">
    <!-- 입력 필드들 -->
</div>
```

### 6.3 메타 태그
```html
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<meta name="mobile-web-app-capable" content="yes">
```

---

## 7. CDN 리소스

### 7.1 Tailwind CSS
```html
<script src="https://cdn.tailwindcss.com"></script>
```

### 7.2 Shadcn UI (선택사항)
Shadcn UI는 기본적으로 컴포넌트 복사 방식이므로, 필요한 컴포넌트 스타일을 직접 구현하거나 headlessui를 사용:
```html
<script src="https://unpkg.com/@headlessui/vue@latest/dist/headlessui.umd.js"></script>
```

또는 순수 Tailwind만으로 구현 권장.

---

## 8. 완성 HTML 구조 예시

```html
<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>급수 관경 결정 계산기</title>
    <script src="https://cdn.tailwindcss.com"></script>
</head>
<body class="bg-gray-50">
    <div class="container max-w-2xl mx-auto p-4">
        <!-- 헤더 -->
        <header class="mb-6">
            <h1 class="text-2xl font-bold text-center">급수 관경 결정 계산</h1>
        </header>
        
        <!-- 입력 영역 -->
        <section class="bg-white rounded-lg shadow p-6 mb-4">
            <h2 class="text-lg font-semibold mb-4">위생기구 수량 입력</h2>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <!-- 입력 필드들 반복 -->
            </div>
        </section>
        
        <!-- 계산 버튼 -->
        <button id="calculateBtn" class="w-full py-4 ...">
            계산하기
        </button>
        
        <!-- 결과 영역 -->
        <section id="resultSection" class="bg-white rounded-lg shadow p-6 mt-4 hidden">
            <!-- 결과 표시 -->
        </section>
    </div>
    
    <script>
        // 전역 상수
        const FIXTURES = [...];
        const PIPE_CAPACITY_TABLE = {...};
        
        // 계산 함수들
        function getFlushValveSimultaneity(qty) { ... }
        function getGeneralSimultaneity(loadSum) { ... }
        function determineSize(effectiveLoad) { ... }
        function calculate(quantities) { ... }
        function displayResult(result) { ... }
        
        // 이벤트 리스너
        document.getElementById('calculateBtn').addEventListener('click', ...);
    </script>
</body>
</html>
```

---

## 9. 검증 테스트 케이스

### 9.1 기본 테스트
```javascript
// 입력: 대변기 3, 소변기 8, 세면기 8, 청소씽크 3, 샤워 2, 욕조 4, 주방씽크 0
// 기대 결과: 50A

const testResult = calcSelDia(3, 8, 8, 3, 2, 4, 0);
console.assert(testResult === 50, '테스트 실패: 50A 예상');
```

### 9.2 계산 과정 검증
```javascript
const quantities = [3, 8, 8, 3, 2, 4, 0];
const result = calculate(quantities);

console.log('일반기구 부하:', result.genLoadSum);        // 36.2
console.log('일반기구 동시율:', result.genRate);         // 0.45
console.log('일반기구 유효부하:', result.genEffective);  // 16.29
console.log('대변기 부하:', result.fvLoadSum);          // 14.7
console.log('대변기 동시율:', result.fvRate);            // 0.825 (3개 기준)
console.log('대변기 유효부하:', result.fvEffective);     // 12.13
console.log('최종 유효부하:', result.totalEffective);    // 28.42
console.log('최종 관경:', result.mainSize);              // 50
```

---

## 10. 개발 체크리스트

- [ ] HTML 기본 구조 생성
- [ ] Tailwind CSS CDN 연결
- [ ] 7개 입력 필드 구현
- [ ] 계산 로직 JavaScript 구현
  - [ ] FIXTURES 상수 정의
  - [ ] PIPE_CAPACITY_TABLE 정의
  - [ ] getFlushValveSimultaneity() 함수
  - [ ] getGeneralSimultaneity() 함수
  - [ ] determineSize() 함수
  - [ ] calculate() 함수
  - [ ] calcSelDia() 함수
- [ ] 결과 표시 영역 구현
- [ ] 이벤트 핸들러 연결
- [ ] 모바일 반응형 테스트
- [ ] 테스트 케이스 검증
- [ ] 숫자 포맷팅 (소수점 처리)
- [ ] 입력 검증 (음수 방지)
- [ ] 로딩 상태 표시 (선택)
- [ ] 애니메이션 효과 (선택)

---

## 11. 추가 개선 사항 (선택)

### 11.1 입력값 저장 (LocalStorage)
```javascript
// 저장
function saveInputs() {
    const inputs = {
        toilet: document.getElementById('toilet').value,
        // ... 나머지
    };
    localStorage.setItem('waterDiaInputs', JSON.stringify(inputs));
}

// 불러오기
function loadInputs() {
    const saved = localStorage.getItem('waterDiaInputs');
    if (saved) {
        const inputs = JSON.parse(saved);
        document.getElementById('toilet').value = inputs.toilet || 0;
        // ... 나머지
    }
}
```

### 11.2 결과 히스토리
계산 결과를 배열로 저장하여 이전 계산 내역 확인 가능

### 11.3 PDF 출력
html2canvas + jsPDF 라이브러리로 결과 PDF 저장

### 11.4 다크모드
Tailwind의 다크모드 클래스 활용

---

## 12. 참고 문서

- 원본 알고리즘: `급수관로관경결정.md`
- C# 구현: `PipeSizingEngine.cs`
- Tailwind CSS: https://tailwindcss.com/docs
- MDN Web Docs: https://developer.mozilla.org/

---

**문서 버전**: 1.0  
**작성일**: 2025.12.25  
**기반**: WinForms 급수 관경 결정 프로그램
