using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProfilerDatas;
using System.IO;
using System;
using System.Diagnostics;
using System.Text;
using UnityEditorInternal;
using System.Linq;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public class ExtractProfilerEditor : EditorWindow
{
    private float memorySize = 1;
    private int memoryDepth = 1;
    private int sampleFrame = 1;
    private int beginFrame = 1;
    private int endFrame = 1;
    private string targetName;
    private GUIStyle headStyle;
    private Rect contentRect = new Rect(100, 330, 700, 280);
    private Rect graphRect = new Rect(100, 330, 700, 280);
    private Rect axisRect = new Rect(80, 310, 800, 300);
    private Color[] layerColor = new Color[4] { Color.green, Color.red, Color.blue, Color.yellow };
    private Vector3[][] points;
    private int sampleCount = 0;
    private int currentPoint = 0;
    private float GCTotalTime = 0;
    private float highFuncPorprotion = 1;
    private float[] CPUUsedTimePorprotion = new float[4];
    private float[] timeThreshold = new float[4];
    private bool clickGraph;
    private bool isSampleCPU = true;
    private bool isSampleGPU = true;
    private bool isSampleGC = true;
    private bool isSampleFuncDetail = true;
    private bool isPrintHighUsedFunc = true;
    private List<int> GCFrame;
    private List<float> cpuData;
    private List<float> gpuData;
    private List<string> CPUFunctionDetail;
    private List<List<string>> highFuncList;
    private List<List<float>> dataList = new List<List<float>>();


    private MemoryElement memoryElement;

    [MenuItem("Window/Extract Profiler Data")]
    static void ShowWindow()
    {
        EditorApplication.ExecuteMenuItem("Window/Profiler");
        ExtractProfilerEditor window = (ExtractProfilerEditor)EditorWindow.GetWindow(typeof(ExtractProfilerEditor));
        window.titleContent = new GUIContent("Profiler数据分析");
        window.wantsMouseMove = true;
        window.Show();
    }


    void OnEnable()
    {
        currentPoint = 0;
        sampleCount = 0;
        timeThreshold[0] = 10;
        timeThreshold[1] = 16;
        timeThreshold[2] = 33;
        timeThreshold[3] = 50;
    }



    void OnGUI()
    {
        if (headStyle == null)
        {
            headStyle = new GUIStyle();
            headStyle.fontSize = 15;
            headStyle.alignment = TextAnchor.MiddleCenter;
            headStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("注意取哪类数据时，请将Profiler切换到对应面板", EditorStyles.whiteLabel);

        // 采样一帧CPU 或 GPU 详细数据
        sampleFrame = EditorGUILayout.IntSlider("Sample Current Frame", sampleFrame, ProfilerDriver.firstFrameIndex + 1, ProfilerDriver.lastFrameIndex - 2);
        EditorGUILayout.BeginHorizontal();
        // 采样CPU
        if (GUILayout.Button("Sample CPU"))
        {
            ProfilerWindow.SetCurrentFrame(sampleFrame - 1);
            PrintDatas("CPU");
        }
        // 采样GPU
        if (GUILayout.Button("Sample GPU"))
        {
            ProfilerWindow.SetCurrentFrame(sampleFrame - 1);
            PrintDatas("GPU");
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Memory Filter（Save in the same directory as Assets）", EditorStyles.whiteLargeLabel);
        // 内存过滤条件
        memorySize = EditorGUILayout.FloatField("Memory Size(MB) >= ", memorySize);
        memoryDepth = EditorGUILayout.IntField("Memory Depth(>=1)", memoryDepth);
        // 采集内存快照
        if (GUILayout.Button("Get Memory Data(One Frame)"))
        {
            if (memoryDepth <= 0)
            {
                memoryDepth = 1;
            }
            ProfilerWindow.RefreshMemoryData();
            ExtractMemory(memoryDepth, memorySize);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("所选区间内CPU耗时超过以下ms,绘制图像显示占用比例（每次改数值后要重新绘制）", EditorStyles.whiteLabel);
        EditorGUILayout.BeginHorizontal();
        timeThreshold[0] = EditorGUILayout.FloatField("CPU耗时 >", timeThreshold[0]);
        timeThreshold[1] = EditorGUILayout.FloatField("CPU耗时 >", timeThreshold[1]);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        timeThreshold[2] = EditorGUILayout.FloatField("CPU耗时 >", timeThreshold[2]);
        timeThreshold[3] = EditorGUILayout.FloatField("CPU耗时 >", timeThreshold[3]);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Test"))
        {
            var list = ProfilerWindow.HighUsedFunc(beginFrame, endFrame, 100);
            Debug.Log(list.Count);
            Debug.Log(list[0].Count);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        // 采样一段时间内的CPU 和 GPU数据
        EditorGUILayout.LabelField("Sample a piece of data", EditorStyles.whiteLargeLabel);
        beginFrame = EditorGUILayout.IntSlider("Begin Frame", beginFrame, ProfilerDriver.firstFrameIndex + 1, ProfilerDriver.lastFrameIndex - 2);
        endFrame = EditorGUILayout.IntSlider("End Frame", endFrame, ProfilerDriver.firstFrameIndex + 1, ProfilerDriver.lastFrameIndex - 2);
        EditorGUILayout.Space();
        targetName = EditorGUILayout.TextField("目标区域/函数名: ", targetName);
        // 选择采集的数据
        EditorGUILayout.LabelField("选择采集的数据", EditorStyles.whiteLabel);
        isSampleCPU = EditorGUILayout.Toggle("CPU", isSampleCPU);
        isSampleGPU = EditorGUILayout.Toggle("GPU", isSampleGPU);
        isSampleGC = EditorGUILayout.Toggle("GC", isSampleGC);
        isSampleFuncDetail = EditorGUILayout.Toggle("FunctionDetail", isSampleFuncDetail);
        EditorGUILayout.BeginHorizontal();
        isPrintHighUsedFunc = EditorGUILayout.Toggle("PrintHighUsedFunc", isPrintHighUsedFunc);
        highFuncPorprotion = EditorGUILayout.FloatField("打印高耗时函数 >(%) ", highFuncPorprotion);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Save Data And Draw（begin - end）"))
        {
            SampleData(beginFrame, endFrame);
        }

        if (GUILayout.Button("Save Data And Draw（min - max）"))
        {
            SampleData(ProfilerDriver.firstFrameIndex + 1,ProfilerDriver.lastFrameIndex - 2);
        }

        if (GUILayout.Button("Clear Data"))
        {
            ClearAllData();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (dataList.Count > 0)
            DrawGraph();

        HandleEvent();
    }


    /// <summary>
    /// 应用程序当前状态的快照
    /// </summary>
    void ExtractMemory(int depth, float size)
    {
        memoryElement = ProfilerWindow.GetMemoryDetail(depth, size * 1024 * 1024);
        PrintMemoryData();
    }



    void PrintDatas(Action<StreamWriter, List<float>, int> action, List<float> list)
    {
        var parent = Directory.GetParent(Application.dataPath);
        var directoryPath = parent.FullName + "/ProfilerData";
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var outputPath = string.Format("{0}/ProfilerDetailed{1:yyyy_MM_dd_HH_mm_ss}.txt", directoryPath, DateTime.Now);
        File.Create(outputPath).Dispose();
        using (var writer = new StreamWriter(outputPath))
        {
            action(writer, list, beginFrame);
            writer.Flush();
            writer.Close();
        }

        Process.Start(outputPath);
    }


    void PrintDatas(string dataType)
    {
        var parent = Directory.GetParent(Application.dataPath);
        var directoryPath = parent.FullName + "/ProfilerData";
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var outputPath = string.Format("{0}/ProfilerDetailed{1:yyyy_MM_dd_HH_mm_ss}.txt", directoryPath, DateTime.Now);
        File.Create(outputPath).Dispose();
        using (var writer = new StreamWriter(outputPath))
        {
            switch (dataType)
            {
                case "CPU":
                    ProfilerWindow.SwitchWindow(ProfilerArea.CPU);
                    ProfilerWindow.WriteCPU(writer, ProfilerWindow.GetCPUDetail(writer));
                    break;
                case "GPU":
                    ProfilerWindow.SwitchWindow(ProfilerArea.GPU);
                    ProfilerWindow.WriteGPU(writer, ProfilerWindow.GetGPUDetail(writer));
                    break;
            }
            writer.Flush();
            writer.Close();
        }
        Process.Start(outputPath);
    }


    /// <summary>
    /// 打印内存数据
    /// </summary>
    void PrintMemoryData()
    {
        var parent = Directory.GetParent(Application.dataPath);
        var directoryPath = parent.FullName + "/ProfilerData";
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var outputPath = string.Format("{0}/ProfilerDetailed{1:yyyy_MM_dd_HH_mm_ss}.txt", directoryPath, DateTime.Now);
        File.Create(outputPath).Dispose();
        if (null != memoryElement)
        {
            var writer = new StreamWriter(outputPath);
            ProfilerWindow.WriteMemoryDetail(writer, memoryElement);
            writer.Flush();
            writer.Close();
        }

        Process.Start(outputPath);
    }



    void DrawGraph()
    {
        // 初始化 points，公有 dataList.Count 组数据，每组数据有sampleCount个元素
        points = new Vector3[dataList.Count][];
        if (points[0] == null || points[0].Length != dataList[0].Count)
        {
            for (int i = 0; i < dataList.Count; i++)
                points[i] = new Vector3[sampleCount];
        }


        // 各条数据最大值
        float[] maxValue = new float[dataList.Count];
        for (int i = 0; i < dataList.Count; i++)
            maxValue[i] = dataList[i].Max();

        // 各数据平均值
        float[] average = new float[dataList.Count];
        for (int i = 0; i < dataList.Count; i++)
            average[i] = dataList[i].Average();

        // 填充坐标，其中每条数据x坐标一定，而y坐标需根据所有数据中最大的那个进行伸缩
        for (int i = 0; i < sampleCount; i++)
        {
            for (int layer = 0; layer < dataList.Count; layer++)
            {
                points[layer][i].x = ((float)i / sampleCount * contentRect.width + contentRect.xMin);
                if (dataList[layer][i] > average[layer] + 500) 
                    points[layer][i].y = 0;
                else 
                    points[layer][i].y = (contentRect.yMax - dataList[layer][i] / maxValue[layer] * contentRect.height);
            }
        }


        

        // 总结
        StringBuilder summary = new StringBuilder();
        summary.Append(string.Format("Count: {0}", sampleCount));
        for (int i = 0; i < dataList.Count; i++)
        {
            summary.Append(string.Format("      Data{0} Average: {1:F3}", i + 1, average[i]));
        }

        EditorGUI.LabelField(new Rect(axisRect.xMin, axisRect.yMin - 70, axisRect.width, 50), summary.ToString(), headStyle);

        // CPU占比
        if (cpuData != null)
        {
            string porprotionStr = string.Format("CPU占比：>{0}ms({1:F2}%)    >{2}ms({3:F2}%)    >{4}ms({5:F2}%)    >{6}ms({7:F2}%)",
                timeThreshold[0],CPUUsedTimePorprotion[0],
                timeThreshold[1],CPUUsedTimePorprotion[1],
                timeThreshold[2],CPUUsedTimePorprotion[2],
                timeThreshold[3],CPUUsedTimePorprotion[3]);
            EditorGUI.LabelField(new Rect(axisRect.xMin, axisRect.yMin - 40, axisRect.width, 50), porprotionStr, headStyle);
        }


        // 画线,画线性图
        Handles.BeginGUI();
        for (int layer = 0; layer < dataList.Count; layer++)
        {
            Handles.color = layerColor[layer];
            Handles.DrawAAPolyLine(points[layer].Where(p => graphRect.Contains(p)).ToArray());
        }
        Handles.EndGUI();


        // 定位线显示在图像内
        if (currentPoint < sampleCount && currentPoint >= 0 && graphRect.Contains(points[0][currentPoint]))
        {
            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(2, new Vector2(points[0][currentPoint].x, axisRect.yMax), new Vector2(points[0][currentPoint].x, axisRect.yMin));
            Handles.EndGUI();
            EditorGUI.LabelField(new Rect(points[0][currentPoint].x - 10, axisRect.yMax + 5, 50, 20), (currentPoint + beginFrame).ToString());
            bool isRight = true;
            for (int i = 0; i < dataList.Count; i++)
            {
                EditorGUI.LabelField(new Rect(points[0][currentPoint].x + (isRight ? 5 : -20), points[i][currentPoint].y, 50, 20), dataList[i][currentPoint].ToString());
                isRight = !isRight;
            }

            // 当前所定位的数据
            StringBuilder detail = new StringBuilder();
            for (int i = 0; i < dataList.Count; i++)
                detail.Append(string.Format("<color={0}>Data{1}:{2}</color>    ", Color2String(layerColor[i]), i + 1, dataList[i][currentPoint]));

            EditorGUI.LabelField(new Rect(axisRect.x, axisRect.yMax + 15, axisRect.width, 30), detail.ToString(), headStyle);

            // GC数次和时间
            StringBuilder gcTime = new StringBuilder();
            if (GCFrame != null)
            {
                gcTime.Append(string.Format("GC Count: {0}", GCFrame.Count));
                foreach (var time in GCFrame)
                    gcTime.Append(string.Format("    GC Frame: {0}", time));
            }
            else
                gcTime.Append(string.Format("GC Count: {0}", 0));

            gcTime.Append(string.Format("    totaltime:{0}ms", GCTotalTime));
            EditorGUI.LabelField(new Rect(axisRect.x, axisRect.yMax + 35, axisRect.width, 30), gcTime.ToString(), headStyle);

            // CPUFunctionDetail
            if (CPUFunctionDetail != null && currentPoint < CPUFunctionDetail.Count)
                EditorGUI.LabelField(new Rect(axisRect.x, axisRect.yMax + 55, axisRect.width, 30), CPUFunctionDetail[currentPoint], headStyle);


            // 高CPU占用函数
            if (highFuncList != null && highFuncList.Count > 0)
            {
                StringBuilder highFuncSB = new StringBuilder();
                int count = 0;
                foreach (var fname in highFuncList[currentPoint])
                {
                    highFuncSB.Append(fname);
                    if (++count % 2 == 0)
                        highFuncSB.Append("\n");
                }

                EditorGUI.TextArea(new Rect(axisRect.x - 50, axisRect.yMax + 85, axisRect.width + 80, 100),
                    highFuncSB.ToString());
            }
        }

        // 坐标轴
        DrawArraw(new Vector2(axisRect.x, axisRect.yMax), new Vector2(axisRect.x, axisRect.yMin), Color.white);
        DrawArraw(new Vector2(axisRect.x, axisRect.yMax), new Vector2(axisRect.xMax, axisRect.yMax), Color.white);
    }



    void HandleEvent()
    {
        Vector2 point = Event.current.mousePosition;
        switch (Event.current.type)
        {
            // 鼠标点击事件，定位到某一点
            case EventType.MouseDown:
                // 没这个点击不了图像
                clickGraph = graphRect.Contains(point);
                if (clickGraph)
                    EditorGUI.FocusTextInControl(null);

                if (Event.current.button == 0 && clickGraph)
                {
                    UpdateCurrentSample();
                    Repaint();
                }
                break;
            // 鼠标拖拽，拖动数据图
            case EventType.MouseDrag:
                if (Event.current.button == 0 && clickGraph)
                {
                    UpdateCurrentSample();
                    Repaint();
                }

                if (Event.current.button == 1 && clickGraph)
                {
                    contentRect.x += Event.current.delta.x;
                    if (contentRect.x > graphRect.x)
                        contentRect.x = graphRect.x;
                    if (contentRect.xMax < graphRect.xMax)
                        contentRect.x = graphRect.xMax - contentRect.width;
                    Repaint();
                }
                break;
            // 
            case EventType.ScrollWheel:
                if (graphRect.Contains(point))
                {
                    if (Event.current.delta.y < 0)
                    {
                        float factor = 0.9f;
                        float maxWidth = graphRect.width * sampleCount / Mathf.Min(50, sampleCount);
                        if (contentRect.width / factor > maxWidth)
                            factor = contentRect.width / maxWidth;

                        // 第一个样本点的坐标左移
                        contentRect.x = (contentRect.x - point.x) / factor + point.x;
                        // 且所有样本点的域变长
                        contentRect.width /= factor;
                    }
                    if (Event.current.delta.y > 0)
                    {
                        float factor = 0.9f;
                        if (contentRect.width * factor < graphRect.width)
                            factor = graphRect.width / contentRect.width;

                        //第一个样本点的坐标右移
                        contentRect.x = (contentRect.x - point.x) * factor + point.x;
                        // 且所有样本点的域变短
                        contentRect.width *= factor;
                    }
                    if (contentRect.x > graphRect.x)
                        contentRect.x = graphRect.x;
                    if (contentRect.xMax < graphRect.xMax)
                        contentRect.x = graphRect.xMax - contentRect.width;

                    Repaint();
                }
                break;
            case EventType.KeyDown:
                if (Event.current.keyCode == KeyCode.LeftArrow)
                    SetCurrentIndex(currentPoint - 1);
                if (Event.current.keyCode == KeyCode.RightArrow)
                    SetCurrentIndex(currentPoint + 1);
                Repaint();
                break;
        }
    }



    /// <summary>
    /// 画箭头
    /// </summary>
    private void DrawArraw(Vector2 from, Vector2 to, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawAAPolyLine(3, from, to);
        Vector2 v0 = from - to;
        v0 *= 10 / v0.magnitude;
        Vector2 v1 = new Vector2(v0.x * 0.866f - v0.y * 0.5f, v0.x * 0.5f + v0.y * 0.886f);
        Vector2 v2 = new Vector2(v0.x * 0.866f + v0.y * 0.5f, v0.x * -0.5f + v0.y * 0.886f);
        Handles.DrawAAPolyLine(3, to + v1, to, to + v2);
        Handles.EndGUI();
    }


    // 如(1,1,1) -> #FFFFFF
    string Color2String(Color color)
    {
        string ret = "#";
        //十六进制
        ret += ((int)(color.r * 255)).ToString("X2");
        ret += ((int)(color.g * 255)).ToString("X2");
        ret += ((int)(color.b * 255)).ToString("X2");
        return ret;
    }


    /// <summary>
    /// 更新当前定位线索引
    /// </summary>
    private void UpdateCurrentSample()
    {
        int index = 0;
        float distance = float.MaxValue;
        Vector2 point = Event.current.mousePosition;
        for (int i = 0; i < sampleCount; i++)
        {
            if (graphRect.Contains(points[0][i]) && Mathf.Abs(point.x - points[0][i].x) < distance)
            {
                distance = Mathf.Abs(point.x - points[0][i].x);
                index = i;
            }
        }
        SetCurrentIndex(index);
    }


    /// <summary>
    /// 设置定位线索引
    /// </summary>
    /// <param name="index"></param>
    private void SetCurrentIndex(int index)
    {
        currentPoint = Mathf.Clamp(index, 0, sampleCount - 1);
    }



    void SaveDataAndDraw()
    {
        if (dataList == null)
            dataList = new List<List<float>>();

        dataList.Clear();
        if (cpuData != null)
            dataList.Add(cpuData);

        if (gpuData != null)
            dataList.Add(gpuData);

        if (dataList.Count > 0 && dataList[0] != null)
            sampleCount = dataList[0].Count;
    }


    void SampleData(int begin, int end)
    {
        ClearAllData();
        if (isSampleCPU)
        {
            CPUUsedTimePorprotion = ProfilerWindow.UsedTimeProportion(begin, end, timeThreshold, timeThreshold.Length);
            cpuData = ProfilerWindow.CPUOrGPUUsedTime(begin, end, ProfilerColumn.TotalTime);
        }

        if (isSampleGPU)
            gpuData = ProfilerWindow.CPUOrGPUUsedTime(begin, end, ProfilerColumn.TotalGPUTime);

        if (isSampleGC)
        {
            GCTotalTime = 0;
            GCFrame = ProfilerWindow.GCCollectCount(begin, end, "GC.Collect", ref GCTotalTime);
        }

        if (isSampleFuncDetail)
            CPUFunctionDetail = ProfilerWindow.CPUFunctionDetail(begin, end, targetName);

        if (isPrintHighUsedFunc)
            highFuncList = ProfilerWindow.HighUsedFunc(begin, end, highFuncPorprotion);

        SaveDataAndDraw();
    }

    void ClearAllData()
    {
        cpuData = null;
        gpuData = null;
        GCFrame = null;
        CPUFunctionDetail = null;
        highFuncList = null;
        dataList.Clear();
    }
}
