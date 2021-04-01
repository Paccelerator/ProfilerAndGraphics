using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace ProfilerDatas
{
    public class CPUElement
    {
        private string FunctionName;
        private string TotalPercent;
        private string SelfPercent;
        private string Calls;
        private string GCMemory;
        private string TotalTime;
        private string SelfTime;
        private string RelatedObjName;

        public List<CPUElement> children;
        public static List<CPUElement> allCPUEle;

        public int Depth { get; private set; }



        public static CPUElement Create(UnityEditorInternal.ProfilerProperty root)
        {
            allCPUEle = new List<CPUElement>();
            if (allCPUEle == null) return null;
            if (root == null) return null;
            CPUElement rootEle = new CPUElement()
            {
                FunctionName = root.propertyName,
                TotalPercent = root.GetColumn(ProfilerColumn.TotalPercent),
                SelfPercent = root.GetColumn(ProfilerColumn.SelfPercent),
                Calls = root.GetColumn(ProfilerColumn.Calls),
                GCMemory = root.GetColumn(ProfilerColumn.GCMemory),
                TotalTime = root.GetColumn(ProfilerColumn.TotalTime),
                SelfTime = root.GetColumn(ProfilerColumn.SelfTime),
                RelatedObjName = root.GetColumn(ProfilerColumn.ObjectName),
                Depth = root.depth
            };
            if (allCPUEle != null) 
                allCPUEle.Add(rootEle);

            while (root.Next(root.HasChildren))
            {
                CPUElement ele = new CPUElement()
                {
                    FunctionName = root.propertyName,
                    TotalPercent = root.GetColumn(ProfilerColumn.TotalPercent),
                    SelfPercent = root.GetColumn(ProfilerColumn.SelfPercent),
                    Calls = root.GetColumn(ProfilerColumn.Calls),
                    GCMemory = root.GetColumn(ProfilerColumn.GCMemory),
                    TotalTime = root.GetColumn(ProfilerColumn.TotalTime),
                    SelfTime = root.GetColumn(ProfilerColumn.SelfTime),
                    RelatedObjName = root.GetColumn(ProfilerColumn.ObjectName),
                    Depth = root.depth
                };
                if (allCPUEle != null) allCPUEle.Add(ele);
            }

            SetChildren();
            return rootEle;
        }


        private static void SetChildren()
        {
            for (int i = 1; i < allCPUEle.Count; i++)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (allCPUEle[i].Depth - 1 == allCPUEle[j].Depth)
                    {
                        if (allCPUEle[j].children == null)
                            allCPUEle[j].children = new List<CPUElement>();
                        allCPUEle[j].children.Add(allCPUEle[i]);
                        break;
                    }
                }
            }
        }


        public override string ToString()
        {
            string ans = string.Format("[{0}]{1} : \ttotal: {2}ms({3}) ; \tself:  {4}ms({5})", Depth, new string(' ', Depth * 6) + FunctionName, TotalTime,
                TotalPercent, SelfTime, SelfPercent);
            return ans;
        }
    }

}
