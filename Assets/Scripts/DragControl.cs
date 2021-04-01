using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragControl : MonoBehaviour ,IDragHandler,IBeginDragHandler{

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
    }



    public void OnDrag(PointerEventData eventData)
    {
		Debug.Log("drag drag");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("begin drag");
    }
}
