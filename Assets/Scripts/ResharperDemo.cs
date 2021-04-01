using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;



public class ResharperDemo : MonoBehaviour
{
    public GameObject cube;
    public GameObject sphere;
    public Material mat;

    private List<GameObject> go1 = new List<GameObject>();
    private List<GameObject> go2 = new List<GameObject>();
    private bool flag = true;
    void Start ()
    {
        
    }


    void Update()
    {
        if (flag)
        {
            Profiler.BeginSample("aaaa");
            GameObject go = Instantiate(cube);
            GameObject sp = Instantiate(sphere);
            go2.Add(sp);
            go1.Add(go);
            Profiler.EndSample();
            if (go1.Count > 500)
                flag = !flag;
        }
        else
        {
            go2.RemoveAt(0);
            go1.RemoveAt(0);
            if (go1.Count < 10)
                flag = !flag;
        }
    }



    void ProfilerFun()
    {
        transform.LookAt(new Vector3(UnityEngine.Random.Range(1,90),23,23));
    }




}
