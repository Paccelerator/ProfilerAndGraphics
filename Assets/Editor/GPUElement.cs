using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace ProfilerDatas
{
    public class GPUElement
    {
        private string FuncName;
        private string TotalGPUPercent;
        private string SelfGPUPercent;
        private string SelfGPUTime;
        private string DrawCalls;
        private string TotalGPUTime;
        private string RelatedObjName;
        public List<GPUElement> children;
        public static List<GPUElement> allGPUEle;

        public int Depth { get; private set; }

        public static GPUElement Create(UnityEditorInternal.ProfilerProperty root)
        {
            allGPUEle = new List<GPUElement>();
            if (allGPUEle == null) return null;
            if (root == null) return null;
            GPUElement rootEle = new GPUElement
            {
                FuncName = root.propertyName,
                TotalGPUPercent = root.GetColumn(ProfilerColumn.TotalGPUPercent),
                SelfGPUPercent = root.GetColumn(ProfilerColumn.SelfGPUPercent),
                DrawCalls = root.GetColumn(ProfilerColumn.DrawCalls),
                TotalGPUTime = root.GetColumn(ProfilerColumn.TotalGPUTime),
                SelfGPUTime = root.GetColumn(ProfilerColumn.SelfGPUTime),
                RelatedObjName = root.GetColumn(ProfilerColumn.ObjectName),
                Depth = root.depth
            };
            if (allGPUEle != null) 
                allGPUEle.Add(rootEle);

            while (root.Next(root.HasChildren))
            {
                GPUElement ele = new GPUElement
                {
                    FuncName = root.propertyName,
                    TotalGPUPercent = root.GetColumn(ProfilerColumn.TotalGPUPercent),
                    DrawCalls = root.GetColumn(ProfilerColumn.DrawCalls),
                    TotalGPUTime = root.GetColumn(ProfilerColumn.TotalGPUTime),
                    RelatedObjName = root.GetColumn(ProfilerColumn.ObjectName),
                    Depth = root.depth
                };
                if (allGPUEle != null) allGPUEle.Add(ele);
            }

            SetChildren();
            return rootEle;
        }


        private static void SetChildren()
        {
            for (int i = 1; i < allGPUEle.Count; i++)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (allGPUEle[i].Depth - 1 == allGPUEle[j].Depth)
                    {
                        if (allGPUEle[j].children == null)
                            allGPUEle[j].children = new List<GPUElement>();
                        allGPUEle[j].children.Add(allGPUEle[i]);
                        break;
                    }
                }
            }
        }


        public override string ToString()
        {
            string ans = string.Format("[{0}]{1} : \ttotal: {2}ms({3}) ; \tself: {4}ms({5}) ; \tDrawCalls: {6}", Depth, new string(' ', Depth * 6) + FuncName, TotalGPUTime,
                TotalGPUPercent, SelfGPUTime, SelfGPUPercent, DrawCalls);
            return ans;
        }
    }

}
