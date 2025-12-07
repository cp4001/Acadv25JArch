using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// 표준 Exception과 충돌 방지를 위해 별칭 사용
using AcException = Autodesk.AutoCAD.Runtime.Exception;
using Exception = System.Exception;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace MyCadFunctions
{
    // ==========================================
    // [파트 1] AutoCAD 명령 클래스
    // ==========================================
    public class MathGraphCommands
    {
        [CommandMethod("DrawAdvancedGraph")]
        public void DrawAdvancedGraph()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n--- 고급 수학 그래프 그리기 ---");
            ed.WriteMessage("\n사용 가능: +, -, *, /, ^(거듭제곱)");
            ed.WriteMessage("\n함수: sin(x), cos(x), tan(x), sqrt(x), log(x), abs(x)");

            // --- 1. 사용자 입력 받기 ---

            // 1-1. 수식 입력
            PromptStringOptions pso = new PromptStringOptions("\n함수 수식을 입력하세요 (예: sin(x)*x, x^2 - 4): ");
            pso.AllowSpaces = true;
            PromptResult prFormula = ed.GetString(pso);
            if (prFormula.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(prFormula.StringResult))
                return;
            string formulaStr = prFormula.StringResult.ToLower();

            // 수식 파서 준비
            MathParser parser = new MathParser();
            Queue<object> rpnQueue;
            try
            {
                // 수식을 미리 파싱하여 오류 확인 (변수 x는 아직 대입 전)
                rpnQueue = parser.Parse(formulaStr);
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n[수식 오류] {ex.Message}");
                return;
            }

            // 1-2. 범위 입력
            if (!GetDoubleInput(ed, "\n시작 X 값 (Min):", out double xMin)) return;
            if (!GetDoubleInput(ed, "\n종료 X 값 (Max):", out double xMax)) return;
            // 1-3. 축 그리기용 Y 범위
            if (!GetDoubleInput(ed, "\n시작 Y 값 (축 Min):", out double yMin)) return;
            if (!GetDoubleInput(ed, "\n종료 Y 값 (축 Max):", out double yMax)) return;

            // 1-4. 기준 원점 입력
            PromptPointOptions ppo = new PromptPointOptions("\n그래프의 기준 원점(0,0) 위치를 클릭하세요: ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;
            Point3d origin = ppr.Value;


            // --- 2. 그래프 그리기 시작 ---
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    double step = (xMax - xMin) / 500.0; // 약 500개의 점으로 분할 (더 부드럽게)
                    if (step <= 0.001) step = 0.001; // 최소 스텝 안전장치

                    RPNEvaluator evaluator = new RPNEvaluator();

                    using (Polyline pl = new Polyline())
                    {
                        int index = 0;
                        for (double x = xMin; x <= xMax; x += step)
                        {
                            try
                            {
                                // === 핵심: 파싱된 수식에 현재 x값을 넣고 계산 ===
                                double y = evaluator.Evaluate(rpnQueue, x);

                                // NaN(Not a Number)이나 무한대 값 체크 (예: tan(90도), sqrt(-1))
                                if (double.IsNaN(y) || double.IsInfinity(y))
                                    continue;

                                // 좌표 변환 및 추가
                                pl.AddVertexAt(index, new Point2d(origin.X + x, origin.Y + y), 0, 0, 0);
                                index++;
                            }
                            catch
                            {
                                // 계산 불가능한 영역은 건너뜀 (그래프가 끊김)
                                continue;
                            }
                        }

                        pl.ColorIndex = 2; // 노란색
                        if (pl.NumberOfVertices > 1) // 점이 최소 2개는 있어야 선이 됨
                        {
                            btr.AppendEntity(pl);
                            tr.AddNewlyCreatedDBObject(pl, true);
                        }
                        else
                        {
                            ed.WriteMessage("\n경고: 그래프를 그릴 수 있는 유효한 영역이 없습니다.");
                        }
                    }

                    // --- 3. 축 그리기 ---
                    DrawAxis(tr, btr, origin, xMin, xMax, yMin, yMax);

                    tr.Commit();
                    ed.WriteMessage($"\n성공: y={formulaStr} 그래프 작성 완료.");
                }
                catch (AcException ex)
                {
                    ed.WriteMessage($"\nAutoCAD 에러: {ex.Message}");
                    tr.Abort();
                }
                catch (Exception ex)
                {
                    ed.WriteMessage($"\n일반 에러: {ex.Message}");
                    tr.Abort();
                }
            }
        }

        // Double 입력 헬퍼
        private bool GetDoubleInput(Editor ed, string message, out double result)
        {
            PromptDoubleOptions pdo = new PromptDoubleOptions(message);
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            result = pdr.Value;
            return pdr.Status == PromptStatus.OK;
        }

        // 축 그리기 헬퍼
        private void DrawAxis(Transaction tr, BlockTableRecord btr, Point3d origin, double minX, double maxX, double minY, double maxY)
        {
            // X축
            Line xAxis = new Line(new Point3d(origin.X + minX, origin.Y, 0), new Point3d(origin.X + maxX, origin.Y, 0));
            xAxis.ColorIndex = 1; btr.AppendEntity(xAxis); tr.AddNewlyCreatedDBObject(xAxis, true);
            // Y축
            Line yAxis = new Line(new Point3d(origin.X, origin.Y + minY, 0), new Point3d(origin.X, origin.Y + maxY, 0));
            yAxis.ColorIndex = 3; btr.AppendEntity(yAxis); tr.AddNewlyCreatedDBObject(yAxis, true);
        }
    }


    // ==========================================
    // [파트 2] 경량 수학 파서 엔진 (Embedded)
    // ==========================================
    // 1. Shunting-yard 알고리즘으로 중위 표기법 -> 후위 표기법(RPN) 변환
    public class MathParser
    {
        public static readonly Dictionary<string, int> Precedence = new Dictionary<string, int>
        {
            {"+", 1}, {"-", 1}, {"*", 2}, {"/", 2}, {"^", 3}
        };
        public static readonly HashSet<string> Functions = new HashSet<string> { "sin", "cos", "tan", "sqrt", "abs", "log", "exp" };

        public Queue<object> Parse(string expression)
        {
            Queue<object> outputQueue = new Queue<object>();
            Stack<string> operatorStack = new Stack<string>();

            // 토큰 분리 정규식 (숫자, 변수x, 함수명, 연산자, 괄호)
            string pattern = @"(\d+(\.\d+)?)|(x)|([a-z]+)|(\+|-|\*|/|\^|\(|\))";
            MatchCollection matches = Regex.Matches(expression, pattern);

            string lastToken = "";

            foreach (Match match in matches)
            {
                string token = match.Value;

                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    outputQueue.Enqueue(num);
                }
                else if (token == "x")
                {
                    outputQueue.Enqueue("var_x"); // x 변수 표시
                }
                else if (Functions.Contains(token))
                {
                    operatorStack.Push(token);
                }
                else if (token == "(")
                {
                    operatorStack.Push(token);
                }
                else if (token == ")")
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                        outputQueue.Enqueue(operatorStack.Pop());

                    if (operatorStack.Count == 0) throw new Exception("괄호가 맞지 않습니다.");
                    operatorStack.Pop(); // '(' 제거

                    if (operatorStack.Count > 0 && Functions.Contains(operatorStack.Peek()))
                        outputQueue.Enqueue(operatorStack.Pop()); // 함수 푸시
                }
                else if (Precedence.ContainsKey(token)) // 연산자
                {
                    // 단항 연산자 마이너스 처리 (수식 맨 앞이는거나, 괄호 바로 뒤의 '-')
                    if (token == "-" && (lastToken == "" || lastToken == "(" || Precedence.ContainsKey(lastToken)))
                    {
                        // -x 를 0 - x 로 처리하는 대신, 실제로는 -1 * x 로 처리하는게 좋지만 
                        // 간단한 구현을 위해 여기서는 0을 넣고 빼는 식으로 우회
                        outputQueue.Enqueue(0.0);
                    }

                    while (operatorStack.Count > 0 && Precedence.ContainsKey(operatorStack.Peek()) &&
                           Precedence[token] <= Precedence[operatorStack.Peek()])
                    {
                        outputQueue.Enqueue(operatorStack.Pop());
                    }
                    operatorStack.Push(token);
                }
                lastToken = token;
            }

            while (operatorStack.Count > 0)
            {
                if (operatorStack.Peek() == "(" || operatorStack.Peek() == ")")
                    throw new Exception("괄호가 맞지 않습니다.");
                outputQueue.Enqueue(operatorStack.Pop());
            }

            return outputQueue;
        }
    }

    // 2. 후위 표기법(RPN) 계산기
    public class RPNEvaluator
    {
        public double Evaluate(Queue<object> rpnQueue, double xValue)
        {
            Stack<double> stack = new Stack<double>();
            // 큐를 복사해서 사용 (원본 보존)
            Queue<object> queue = new Queue<object>(rpnQueue);

            while (queue.Count > 0)
            {
                object token = queue.Dequeue();

                if (token is double num)
                {
                    stack.Push(num);
                }
                else if (token is string str)
                {
                    if (str == "var_x")
                    {
                        stack.Push(xValue); // 현재 x값 대입
                    }
                    else if (MathParser.Precedence.ContainsKey(str)) // 연산자
                    {
                        double op2 = stack.Pop();
                        double op1 = stack.Pop();
                        switch (str)
                        {
                            case "+": stack.Push(op1 + op2); break;
                            case "-": stack.Push(op1 - op2); break;
                            case "*": stack.Push(op1 * op2); break;
                            case "/": stack.Push(op1 / op2); break;
                            case "^": stack.Push(Math.Pow(op1, op2)); break;
                        }
                    }
                    else if (MathParser.Functions.Contains(str)) // 함수
                    {
                        double val = stack.Pop();
                        switch (str)
                        {
                            case "sin": stack.Push(Math.Sin(val)); break;
                            case "cos": stack.Push(Math.Cos(val)); break;
                            case "tan": stack.Push(Math.Tan(val)); break;
                            case "sqrt": stack.Push(Math.Sqrt(val)); break;
                            case "abs": stack.Push(Math.Abs(val)); break;
                            case "log": stack.Push(Math.Log(val)); break; // 자연로그(ln)
                            case "exp": stack.Push(Math.Exp(val)); break;
                        }
                    }
                }
            }

            if (stack.Count != 1) throw new Exception("수식 계산 오류");
            return stack.Pop();
        }
    }
}
