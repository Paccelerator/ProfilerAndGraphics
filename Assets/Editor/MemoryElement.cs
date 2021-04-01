using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ProfilerDatas
{
    /// <summary>
    /// 存储反射来的内存数据
    /// </summary>
    public class MemoryElement : IComparable<MemoryElement>
    {
        // children , totalMemory, name 等这些字段的命名需要与 Unity 的 MemoryElement 保存一致
        public List<MemoryElement> children = new List<MemoryElement>();
        public long totalMemory;
        public string name;

        public int Depth { get; private set; }

        private MemoryElement() { }


        /// <summary>
        /// 从 rootMemoryElement 提取单帧内容信息
        /// </summary>
        public static MemoryElement Create(CustomType rootMemoryElement, int depth, int filterDepth, float filterSize)
        {
            if (rootMemoryElement == null) return null;
            MemoryElement memoryElement = new MemoryElement { Depth = depth };
            // 注意 children 并没有复制过来，这里的 List<MemoryElement> 与 Unity 的 List<MemoryElement> 不一样， 因为各自的 MemoryElement 不同
            CustomType.CopeReflectFields(memoryElement, rootMemoryElement.InnerObject, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField);

            // 获取 children 数据
            var currentChildren = rootMemoryElement.PublicInstanceField<IList>("children");
            if (currentChildren == null) 
                return memoryElement;

            // dfs，复制 children
            foreach (var child in currentChildren)
            {
                MemoryElement childElement = Create(new CustomType(child), depth + 1, filterDepth, filterSize);
                if (childElement == null) continue;
                if (memoryElement.Depth > filterDepth - 1) continue;
                if (!(childElement.totalMemory >= filterSize)) continue;
                memoryElement.children.Add(childElement);
            }
            // 按内存从大到小排序
            memoryElement.children.Sort();
            return memoryElement;
        }


        public override string ToString()
        {
            //string text = string.IsNullOrEmpty(name) ? "root" : name;
            //return string.Format(text + " : '\t'{0} mb", totalMemory / 1024.0 /1024);

            var text = string.IsNullOrEmpty(name) ? "root" : name;
            var text2 = "KB";
            var num = totalMemory / 1024f;
            if (num > 512f)
            {
                num /= 1024f;
                text2 = "MB";
            }

            var resultString = string.Format(new string('\t', Depth) + " {0}, {1}{2}", text, num, text2);
            return resultString;
        }


        public int CompareTo(MemoryElement other)
        {
            if (other.totalMemory != totalMemory)
            {
                return (int)(other.totalMemory - totalMemory);
            }

            if (string.IsNullOrEmpty(name)) return -1;
            return !string.IsNullOrEmpty(other.name) ? string.Compare(name, other.name, StringComparison.Ordinal) : 1;
        }
    }
}
