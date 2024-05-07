using System.Collections;
using UnityEngine;

public class TouchDraw : MonoBehaviour
{
    Coroutine drawing;
    public GameObject linePrefab;

    void Update()
    {
        Debug.Log(drawing == null);
        if (Input.GetMouseButtonDown(0))
        {
            StartLine();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            FinishLine();
            Debug.Log("Mouse up: " + (drawing == null));
        }
    }

    void StartLine()
    {
        if (drawing != null)
        {
            Debug.Log("coroutine stopped");
            StopCoroutine(drawing);
        }
        drawing = StartCoroutine(DrawLine());
        Debug.Log("coroutine started"+ (drawing == null));
    }

    void FinishLine()
    {
        if (drawing != null)
        {
            StopCoroutine(drawing);
            drawing = null;
            Debug.Log("coroutine FinishLine executed");
        }
    }

    IEnumerator DrawLine()
    {
        GameObject newGameObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);

        LineRenderer line = newGameObject.GetComponent<LineRenderer>();
        line.positionCount = 0;
        Debug.Log("New line rendereer crearted");

        while (true)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = Mathf.Abs(Camera.main.transform.position.z); // Adjust for camera depth
            Vector3 position = Camera.main.ScreenToWorldPoint(mousePosition);
            position.z = 0;
            line.positionCount++;
            line.SetPosition(line.positionCount - 1, position);
            Debug.Log("position " + position);
            yield return null;
        }
    }
}
