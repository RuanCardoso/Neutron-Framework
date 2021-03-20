using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LinkObject : MonoBehaviour
{
    public GameObject @object;

    void Update()
    {
        if (transform.position != @object.transform.position)
            transform.position = @object.transform.position;
        if (transform.rotation != @object.transform.rotation)
            transform.rotation = @object.transform.rotation;
    }
}