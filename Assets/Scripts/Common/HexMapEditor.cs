using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    public Color[] colors ;

    private Color activeColor;
    private float lastClickTime=0;

    void Awake()
    {
        colors = new Color[]
        {
            Color.blue,
            Color.yellow,
            Color.green,
            Color.white,
            Color.red,
            Color.grey
        };
        SelectColor(0);
    }

    void Update()
    {
        lastClickTime += Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())//
        {
            if (lastClickTime > 0.5f)//防止点击太频繁，系统反应不过来
            {
                lastClickTime = 0f;
                HandleInput();
            }
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            MainWorld.Instance.ColorCell(hit.point, activeColor);
        }
    }

    public void SelectColor(int index)
    {
        if (index>=colors.Length)
        {
            Debug.Log(index + "超出了长度：" + colors.Length);
            return;
        }
        activeColor = colors[index];
    }
}