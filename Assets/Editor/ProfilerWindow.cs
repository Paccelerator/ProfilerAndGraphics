using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace ProfilerDatas
{
    public static class ProfilerWindow
    {
        private static CustomType window = null;
        private static ProfilerProperty CPUOrGPUproperty;



        /// <summary>
        /// 获取打开的 ProfilerWindows(虽然是 list，但是打印它的 Count 为“1”，意味着 ProfilerWindows 里面只有一个元素，就是当前打开的 ProfilerWindow)
        /// </summary>
        private static void AccessProfilerWindows()
        {
            Assembly assembly = typeof(EditorWindow).Assembly;
            var profilerTyep = assembly.GetType("UnityEditor.ProfilerWindow");
            if (profilerTyep == null)
            {
                UnityEngine.Debug.LogError("未获取到 ProfilerWindow，检查版本是否支持");
                return;
            }

            var profiler = new CustomType(profilerTyep);
            if (profiler == null || profiler.InnerType == null)
                return;

            var list = profiler.PrivateStaticField<IList>("m_ProfilerWindows");
            if (list == null)
            {
                UnityEngine.Debug.LogError("ProfilerWindow 的 m_ProfilerWindows 为空或未打开 Profiler");
                return;
            }

            foreach (var _window in list)
            {
                window = new CustomType(_window);
            }
        }



        public static bool IsWindowEmpty()
        {
            if (window == null)
                AccessProfilerWindows();

            if (window == null)
            {
                UnityEngine.Debug.LogError("未获取到ProfierWindow，检查Profiler是否打开");
                return true;
            }

            return false;
        }



        /// <summary>
        /// 确认当前 ProfilerWindow 图表是否为对应的 area 图表
        /// </summary>
        private static bool ConfirmWindow(ProfilerArea area)
        {
            if (area != GetCurrentProfilerArea())
            {
                UnityEngine.Debug.LogWarning("确认切换到对应Profiler面板");
                return false;
            }

            return true;
        }


        /// <summary>
        /// 获取当前 ProfilerWindow 对应的图标
        /// </summary>
        public static ProfilerArea GetCurrentProfilerArea()
        {
            if (IsWindowEmpty())
                return ProfilerArea.AreaCount;

            // m_CurrentArea：当前窗口的类型
            ProfilerArea area = (ProfilerArea)window.PrivateInstanceField("m_CurrentArea");

            return area;
        }


        /// <summary>
        /// 切换 ProfilerWindow 的当前图表
        /// </summary>
        public static void SwitchWindow(ProfilerArea area)
        {
            if (IsWindowEmpty())
                return;

            if (area == GetCurrentProfilerArea())
                return;

            // 反射得到 m_CurrentArea 的字段信息
            var windowFieldInfo = window.InnerType.GetField("m_CurrentArea", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            if (windowFieldInfo == null)
                return;

            // 设置 m_CurrentArea 的值
            windowFieldInfo.SetValue(window.InnerObject, area);
            CustomType UISystemProfiler = new CustomType(window.PrivateInstanceField("m_UISystemProfiler"));
            if (UISystemProfiler != null && UISystemProfiler.InnerType != null)
            {
                // CurrentAreaChanged：切换m_CurrentArea
                UISystemProfiler.CallPublicInstanceMethod("CurrentAreaChanged", area);
            }
            else
                UnityEngine.Debug.LogError("UISystemProfiler 为空");
        }




        /// <summary>
        /// 将内存窗口下的数据进行过滤，存储在 MemoryElement 实例中
        /// </summary>
        /// <param name="filterDepth">过滤深度</param>
        /// <param name="filterSize">过滤内存大小</param>
        public static MemoryElement GetMemoryDetail(int filterDepth, float filterSize)
        {
            if (!ConfirmWindow(ProfilerArea.Memory))
                return null;

            if (IsWindowEmpty())
                return null;

            // 得到内存窗口下的内存数据记录类
            CustomType memoryList = new CustomType(window.PrivateInstanceField("m_MemoryListView"));
            if (memoryList == null || memoryList.InnerObject == null)
            {
                UnityEngine.Debug.LogError("m_MemoryListView获取失败，请检查Unity版本");
                return null;
            }

            var root = memoryList.PrivateInstanceField("m_Root");
            return root != null ? MemoryElement.Create(new CustomType(root), 0, filterDepth, filterSize) : null;
        }



        /// <summary>
        /// 写入内存数据
        /// </summary>
        public static void WriteMemoryDetail(StreamWriter writer, MemoryElement root)
        {
            if (root == null)
                return;

            writer.WriteLine(root.ToString());
            foreach (var child in root.children)
            {
                WriteMemoryDetail(writer, child);
            }
        }

        /// <summary>
        /// 相当于内存Detailed面板的 Take Sample
        /// </summary>
        public static void RefreshMemoryData()
        {
            if (IsWindowEmpty())
                return;

            if (!ConfirmWindow(ProfilerArea.Memory))
                SwitchWindow(ProfilerArea.Memory);

            window.CallPrivateInstanceMethod("RefreshMemoryData");

        }




        /// <summary>
        /// 获取当前所 Click 的帧
        /// </summary>
        public static int GetCurrentFrame()
        {
            if (IsWindowEmpty())
                return -1;

            return (int)window.PrivateInstanceField("m_CurrentFrame") + 1;
        }


        /// <summary>
        /// 设置当前帧为 currentFrame
        /// </summary>
        /// <param name="currentFrame"></param>
        public static void SetCurrentFrame(int currentFrame)
        {
            if (IsWindowEmpty())
                return;

            if (currentFrame > ProfilerDriver.lastFrameIndex)
            {
                UnityEngine.Debug.LogError("当前帧不合理");
                return;
            }

            window.CallPrivateInstanceMethod("SetCurrentFrame", currentFrame);
        }


        /// <summary>
        /// 当前帧变化时可通过该函数获取CPU 或 GPU的属性类
        /// </summary>
        /// <returns></returns>
        private static ProfilerProperty GetCPUOrGPUProperty()
        {
            if (IsWindowEmpty() || window.InnerType == null)
                return null;

            // 获取 ProfileWindow 的无参 GetRootProfilerProperty 方法信息
            var methodInfo = window.InnerType.GetMethod("GetRootProfilerProperty", BindingFlags.Public | BindingFlags.Instance, null,
                new Type[] { }, null);

            if (methodInfo == null || window.InnerObject == null)
                return null;

            return (ProfilerProperty)methodInfo.Invoke(window.InnerObject, null);
        }


        /// <summary>
        /// 更新并获取CPU or GPU数据，获取后判断是否获取成功
        /// </summary>
        /// <returns></returns>
        private static bool IsPropertyEmpty()
        {
            CPUOrGPUproperty = GetCPUOrGPUProperty();
            if (CPUOrGPUproperty == null)
            {
                UnityEngine.Debug.LogError("ProfierWindow 的 GetRootProfilerProperty 调用失败, 未获取到 m_CPUOrGPUProfilerProperty");
                return true;
            }

            if (!CPUOrGPUproperty.frameDataReady)
            {
                UnityEngine.Debug.LogError("当前帧未有数据，请检查采样的帧数据在Profiler图像内的帧范围内");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取 GPU 数据,返回根元素
        /// </summary>
        /// <param name="writer"></param>
        public static GPUElement GetGPUDetail(StreamWriter writer)
        {
            if (IsWindowEmpty())
                return null;

            if (IsPropertyEmpty())
                return null;

            return GPUElement.Create(CPUOrGPUproperty);
        }


        /// <summary>
        /// 从跟打印所有GPU数据
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="root"></param>
        public static void WriteGPU(StreamWriter writer, GPUElement root)
        {
            if (root == null)
                return;

            writer.WriteLine(root.ToString());
            if (root.children == null)
                return;

            foreach (var child in root.children)
                WriteGPU(writer, child);
        }



        /// <summary>
        /// 获取GPU数据，返回根元素
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public static CPUElement GetCPUDetail(StreamWriter writer)
        {
            if (IsWindowEmpty())
                return null;

            if (IsPropertyEmpty())
                return null;

            return CPUElement.Create(CPUOrGPUproperty);
        }


        /// <summary>
        /// 从跟打印所有CPU数据
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="root"></param>
        public static void WriteCPU(StreamWriter writer, CPUElement root)
        {
            if (root == null)
                return;

            writer.WriteLine(root.ToString());
            if (root.children == null)
                return;

            foreach (var child in root.children)
                WriteCPU(writer, child);
        }



        public static float GetCPUOrGPUTotalTimeOneFrame(ProfilerColumn colume)
        {
            if (IsPropertyEmpty())
                return -1;

            string ans = CPUOrGPUproperty.GetColumn(colume);
            float ret = 0;
            if (string.IsNullOrEmpty(ans))
                return -1;

            bool canConvert = float.TryParse(ans, out ret);
            if (canConvert)
                return float.Parse(ans);
            else
                return -1;

        }


        /// <summary>
        /// 获取CPU一段时间的耗时走势
        /// </summary>
        /// <param name="beginFrame"></param>
        /// <param name="endFrame"></param>
        /// <returns></returns>
        public static List<float> CPUOrGPUUsedTime(int beginFrame, int endFrame, ProfilerColumn column)
        {
            List<float> usedTime = new List<float>();
            if (usedTime == null)
                return null;

            float totalTime = 0;
            for (int i = beginFrame - 1; i < endFrame; i++)
            {
                SetCurrentFrame(i);
                float time = GetCPUOrGPUTotalTimeOneFrame(column);
                //if (time < 0)
                //{
                //    UnityEngine.Debug.LogError("获取数据错误，检查当前帧是否存在数据");
                //    return null;
                //}

                usedTime.Add(time);
                totalTime += time;
            }
            return usedTime;
        }


        public static void WriteCPUOrGPUTime(StreamWriter write, List<float> list, int frame)
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                write.WriteLine(string.Format("frame {0} : {1}ms", frame + i, list[i]));
            }
        }


        /// <summary>
        /// 计算GC次数，并记录在什么时候
        /// </summary>
        public static List<int> GCCollectCount(int beginFrame, int endFrame, string funcName,ref float totalTime)
        {
            List<int> frame = new List<int>();
            for (int i = beginFrame - 1; i < endFrame; i++)
            {
                SetCurrentFrame(i);
                ProfilerProperty property = FindFunc(funcName);
                if (property != null)
                {
                    totalTime += float.Parse(property.GetColumn(ProfilerColumn.TotalTime));
                    frame.Add(i + 1);
                }
            }
            return frame;
        }


        public static List<string> CPUFunctionDetail(int beginFrame, int endFrame, string funcName)
        {
            List<string> details = new List<string>(endFrame - beginFrame + 1);
            if (details == null || string.IsNullOrEmpty(funcName))
            {
                UnityEngine.Debug.LogWarning("请输入函数名");
                return null;
            }

            for (int i = beginFrame - 1; i < endFrame; i++)
            {
                SetCurrentFrame(i);
                ProfilerProperty property = FindFunc(funcName);
                if (property != null)
                {
                    details.Add(string.Format("totalTime:{0}ms({1})  selfTime:{2}ms({3})  CGMemory:{4}  RelatedObj:{5}",
                        property.GetColumn(ProfilerColumn.TotalTime),
                        property.GetColumn(ProfilerColumn.TotalPercent),
                        property.GetColumn(ProfilerColumn.SelfTime),
                        property.GetColumn(ProfilerColumn.SelfPercent),
                        property.GetColumn(ProfilerColumn.GCMemory),
                        property.GetColumn(ProfilerColumn.ObjectName)));
                }
                else
                    details.Add("null");
            }
            return details;
        }




        // TODO, 可能有同名，返回值为数组更好
        private static ProfilerProperty FindFunc(string funcName)
        {
            if (IsPropertyEmpty())
                return null;

            ProfilerProperty property = CPUOrGPUproperty;
            while (property.Next(property.HasChildren))
            {
                if (property.propertyName == funcName)
                    return property;
            }

            return null;
        }


        public static float[] UsedTimeProportion(int beginFrame, int endFrame, float[] threshold,int length)
        {
            int[] num = new int[length];
            float[] ans = new float[length];
            for (int i = beginFrame - 1; i < endFrame; i++)
            {
                SetCurrentFrame(i);
                if (!IsPropertyEmpty())
                {
                    float usedTime = float.Parse(CPUOrGPUproperty.GetColumn(ProfilerColumn.TotalTime));
                    for (int j = 0; j < length; j++)
                    {
                        if (usedTime > threshold[j])
                            num[j]++;
                    }
                }
            }

            for (int i = 0; i < length; i++)
                ans[i] = num[i] / (float)(endFrame - beginFrame + 1) * 100;

            return ans;
        }


        public static List<List<string>> HighUsedFunc(int beginFrame, int endFrame,float threshold)
        {
            List<List<string>> funNames = new List<List<string>>();
            for (int i = beginFrame - 1; i < endFrame; i++)
            {
                SetCurrentFrame(i);
                if (!IsPropertyEmpty())
                {
                    List<string> funName = new List<string>();
                    ProfilerProperty property = CPUOrGPUproperty;
                    while (property.Next(property.HasChildren))
                    {
                        string str = System.Text.RegularExpressions.Regex.Replace( property.GetColumn(ProfilerColumn.TotalPercent), @"[^\d.\d]", "");
                        if(float.Parse(str) > threshold)
                            funName.Add(string.Format("{0}----耗时：{1}ms({2})\t", 
                                property.propertyName,property.GetColumn(ProfilerColumn.TotalTime),property.GetColumn(ProfilerColumn.TotalPercent)));
                    }
                    funNames.Add(funName);
                }
            }

            return funNames;
        }


        public static void GetActiveVisibleFrameIndex()
        {
            if (IsWindowEmpty())
                return;


            var info = window.InnerType.GetMethod("GetActiveVisibleFrameIndex", BindingFlags.Public | BindingFlags.Instance);

            int ans = (int)info.Invoke(window.InnerObject, null);
            UnityEngine.Debug.Log(ans);
        }
    }
}

