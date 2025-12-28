# 배관 네트워크 부하 분석 시스템

## 📋 프로젝트 목표

AutoCAD 도면에서 배관 라인 네트워크를 분석하여 각 구간의 부하(Load)를 계산하고 계층 구조로 시각화하는 시스템

---

## 🎯 핵심 기능

### 1. 입력
- **배관 Line 선택**: 사용자가 도면에서 여러 개의 Line 엔티티를 선택
- **Root 지점 선택**: 배관 네트워크의 시작점(Root)을 마우스 클릭으로 지정
- **Xdata 읽기**: 각 Line에 저장된 "Dia" AppName의 Xdata에서 부하값 추출

### 2. 처리
- 선택된 Line들의 **연결 관계 파악**
- Root부터 시작하여 **계층 구조(Tree) 생성**
- 각 Line의 **누적 부하 계산** (하위 Line들의 부하 합산)

### 3. 출력
- **콘솔 출력**: 분석 결과를 텍스트로 표시
- **TreeView UI**: WPF PaletteSet으로 계층 구조 시각화
  - Root Line: 빨간색
  - 중간 Line: 파란색
  - 말단 Line (Leaf): 녹색
  - Xdata가 있는 Leaf에는 부하값 표시

---

## 📊 요구사항

### 입력 데이터

| 항목 | 설명 | 예시 |
|------|------|------|
| Line 엔티티 | AutoCAD Line 객체들 | 40개의 Line |
| Root 위치 | 사용자가 클릭한 Point3d | (100, 200, 0) |
| Xdata | "Dia" 앱의 부하값 | 15.0, 25.0, 40.0 등 |

### 출력 형식

**콘솔 출력 예시:**
```
40개의 Line이 선택되었습니다.
연결 관계 구성 완료.
Root Line 선택됨: Handle=11C08B

총 20개의 Leaf Line 감지됨.
총 20개의 Leaf Line에서 Xdata 부하 읽음.

Leaf Line[11C06F], 부하=40
Line[11C070], 누적 부하=15
Line[11C071], 누적 부하=30
...
Root Line의 총 부하: 65
```

**TreeView 출력 예시:**
```
● Line[11C08B] - 부하: 65.00 - 연결: 2개  (빨간색, Root)
  ├─● Line[11C08F] - 부하: 30.00 - 연결: 3개  (파란색)
  │   ├─● Line[11C0AE] - 부하: 30.00 - 연결: 3개  (파란색)
  │   │   └─● Line[11C0AF] - 부하: 15.00 - 연결: 2개 [Xdata: 15.00]  (녹색, Leaf)
  │   └─...
  └─● Line[11C073] - 부하: 15.00 - 연결: 3개  (파란색)
      └─...
```

---

## 🔧 기술 스택

- **언어**: C# (.NET 8.0)
- **플랫폼**: AutoCAD 2025 .NET API
- **UI**: WPF (PaletteSet)
- **OS**: Windows 11

---

## 📁 파일 구조

```
PipeLoad/
├── PipeLoad.md                    # 이 문서
├── [메인 분석 클래스].cs           # 배관 네트워크 분석 로직
├── [TreeView XAML].xaml           # WPF UI 레이아웃
├── [TreeView Code].xaml.cs        # WPF UI 로직
└── [PaletteSet 관리].cs           # AutoCAD PaletteSet 통합
```

---

## 🎨 AutoCAD PaletteSet 참조 코드

### 기존 성공 사례: `PipeTreeViewPaletteV2.cs`

```csharp
using Autodesk.AutoCAD.Windows;
using System.Windows.Controls;

public class PipeTreeViewPaletteV2
{
    private static PaletteSet _paletteSet;
    
    public static void ShowPalette(/* 데이터 파라미터 */)
    {
        if (_paletteSet == null)
        {
            _paletteSet = new PaletteSet(
                "배관 네트워크 구조 (Line 기반)",
                new System.Guid("12345678-1234-1234-1234-123456789ABC")
            );
            
            var treeView = new PipeTreeViewV2();
            _paletteSet.Add("TreeView", treeView);
            _paletteSet.Size = new System.Drawing.Size(400, 600);
        }
        
        // 데이터 업데이트
        var control = _paletteSet[0].Content as PipeTreeViewV2;
        control?.LoadData(/* 파라미터 */);
        
        _paletteSet.Visible = true;
    }
}
```

### WPF UserControl 구조: `PipeTreeViewV2.xaml`

```xml
<UserControl x:Class="PipeLoadAnalysis.PipeTreeViewV2"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 헤더 -->
        <StackPanel Grid.Row="0" Background="#2C3E50" Padding="10">
            <TextBlock Text="배관 네트워크 구조" 
                       Foreground="White" FontSize="16" FontWeight="Bold"/>
        </StackPanel>

        <!-- TreeView -->
        <TreeView Grid.Row="1" Name="MainTreeView" Padding="5">
            <TreeView.ItemContainerStyle>
                <Style TargetType="TreeViewItem">
                    <Setter Property="IsExpanded" Value="True"/>
                </Style>
            </TreeView.ItemContainerStyle>
        </TreeView>

        <!-- 범례 -->
        <StackPanel Grid.Row="2" Background="#ECF0F1" Padding="10">
            <TextBlock Text="범례" FontWeight="Bold" Margin="0,0,0,5"/>
            <StackPanel Orientation="Horizontal">
                <Ellipse Width="10" Height="10" Fill="Red" Margin="0,0,5,0"/>
                <TextBlock Text="Root Line" Margin="0,0,15,0"/>
                <!-- ... -->
            </StackPanel>
        </StackPanel>
    </Grid>
</UserControl>
```

---

## ⚙️ 주요 제약사항

### AutoCAD API
- **공식 문서 메서드만 사용**: 모든 API는 AutoCAD 2025 공식 문서에 명시된 것만 사용
- **Transaction 관리**: 모든 데이터베이스 작업은 Transaction 내에서 수행
- **Entity OpenMode**: Read/Write 모드를 명확히 구분

### 성능
- **대용량 처리**: 수백 개의 Line도 빠르게 처리
- **응답성**: UI는 즉시 표시되어야 함

### 사용자 경험
- **직관적 UI**: TreeView는 확장된 상태로 표시
- **명확한 피드백**: 콘솔에 진행 상황 표시
- **오류 처리**: 잘못된 입력에 대한 안내 메시지

---

## 📝 용어 정의

| 용어 | 설명 |
|------|------|
| **Root Line** | 배관 네트워크의 시작점 (최상위) |
| **Leaf Line** | 말단 배관 (더 이상 분기하지 않음) |
| **중간 Line** | Root와 Leaf 사이의 배관 |
| **부하(Load)** | 각 Line의 Xdata에 저장된 값 |
| **누적 부하** | 해당 Line + 모든 하위 Line의 부하 합계 |
| **연결** | 두 Line의 끝점이 만나는 것 (허용 오차 1e-6) |

---

## 🎯 성공 기준

1. ✅ 선택한 모든 Line의 연결 관계를 정확히 파악
2. ✅ Root부터 Leaf까지 계층 구조를 올바르게 생성
3. ✅ 각 Line의 누적 부하를 정확히 계산
4. ✅ TreeView에서 Root/중간/Leaf를 색상으로 구분
5. ✅ Xdata가 있는 Leaf Line에 부하값 표시
6. ✅ 사용자가 TreeView를 직관적으로 이해 가능

---

## 🚀 명령어 스펙

### PIPELOAD
- **기능**: 배관 네트워크 분석 + 콘솔 출력
- **절차**:
  1. Line 선택 프롬프트
  2. Root 위치 선택 프롬프트
  3. 분석 수행
  4. 콘솔에 결과 출력

### PIPELOAD_PALETTE
- **기능**: 배관 네트워크 분석 + PaletteSet 표시
- **절차**:
  1. Line 선택 프롬프트
  2. Root 위치 선택 프롬프트
  3. 분석 수행
  4. PaletteSet에 TreeView 표시

---

## 📌 참고사항

### Xdata 읽기
- AppName: "Dia"
- 데이터 타입: Double
- 없을 경우: 0.0으로 처리

### 허용 오차
- 점 일치 판단: 1e-6 (0.000001)
- Line의 두 끝점이 이 거리 내에 있으면 "연결됨"으로 판단

### 색상 코드
- **빨간색**: RGB(255, 0, 0) - Root Line
- **파란색**: RGB(0, 0, 255) - 중간 Line
- **녹색**: RGB(0, 255, 0) - Leaf Line

---

## 🔄 향후 확장 가능성

- 다양한 부하 계산 방식 지원
- 네트워크 유효성 검증 (순환 구조 탐지)
- Excel/CSV 내보내기
- 3D 시각화
- 실시간 부하 수정 기능

---

**작성일**: 2025-12-27  
**프로젝트 위치**: `C:\Users\junhoi\Desktop\Work\Acadv25JArch\PipeLoad\`
