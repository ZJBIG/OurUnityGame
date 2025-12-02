using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class CamCtrl : MonoBehaviour
{
    void Start()
    {

    }
    void Update()
    {
        Transform player = GameObject.Find("Player").transform;
        Vector3 pos = new Vector3(player.position.x, player.position.y, transform.position.z);
        transform.position = new Vector3(player.position.x, player.position.y, pos.z);
    }
}
