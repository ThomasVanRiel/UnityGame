using UnityEngine;
using UnityEditor;
using System.Collections;

[ExecuteInEditMode]
public class BillBoardScript : MonoBehaviour {

    public Vector3 Forward;

    void Update() {
        //transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position, Vector3.up);
        //transform.rotation = Camera.main.transform.rotation * Quaternion.Euler(0, 180, 0);
        transform.LookAt(Camera.main.transform);
    }
}
