using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ReflectionDemo : MonoBehaviour 
{

	// Use this for initialization
	void Start ()
    {
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //ProfilerWindow.WriteMemoryDetail(ProfilerWindow.GetMemoryDetail(1, 5 * 1024 * 1024));
        }
	}




    //void GetDatas()
    //{
    //    List<object> windows = new List<object>();

    //    Assembly assembly = typeof(EditorWindow).Assembly;
    //    CustomType typeTool = new CustomType(assembly.GetType("UnityEditor.ProfilerWindow"));
    //    var m_ProfilerWindows = typeTool.PrivateStaticField<IList>("m_ProfilerWindows");
    //    foreach (var window in m_ProfilerWindows)
    //    {
    //        windows.Add(window);
    //    }

    //    foreach (var typeinfo in windows)
    //    {
    //        Type type = typeinfo.GetType();
    //        var field = type.GetField("m_CurrentArea", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
    //        ProfilerArea profilerArea = (ProfilerArea) field.GetValue(typeinfo); // 得到值
    //        if (profilerArea == ProfilerArea.Memory)
    //        {
    //            var memoryListType = type.GetField("m_MemoryListView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
    //            var memoryList = memoryListType.GetValue(typeinfo);
    //            Debug.Log(memoryList);

    //            Type val = memoryList.GetType();
    //            var rootInfo = val.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
    //            var root = rootInfo.GetValue(memoryList);
    //            Debug.Log(root);

    //            Type rootType = root.GetType();
    //            var childrenInfo = rootType.GetField("description",
    //                BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField);
    //            var totalMemory = (long)childrenInfo.GetValue(root);
    //            Debug.Log(totalMemory / 1024.0 / 1024);

    //        }
    //    }
    //}
}
