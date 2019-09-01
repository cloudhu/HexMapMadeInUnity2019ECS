using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

    public Color[] colors;

    private Color activeColor;
    private float lastClickTime=0;
    int activeElevation;

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
            MainWorld.Instance.EditCell(hit.point, activeColor,activeElevation,brushSize);
        }
    }

    /// <summary>
    /// 选择颜色
    /// </summary>
    /// <param name="index">颜色的索引</param>
    public void SelectColor(int index)
    {
        if (index>=colors.Length)
        {
            Debug.Log(index + "超出了长度：" + colors.Length);
            return;
        }
        activeColor = colors[index];
    }

    /// <summary>
    /// 设置海拔
    /// </summary>
    /// <param name="elevation">高度</param>
    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    int brushSize;

    public void SetBrushSize(float size)
    {
        brushSize = (int)size;
    }
}