using UnityEngine;
namespace Tea_Demos
{
   public class Tea_InputControl : MonoBehaviour
   {
      public static bool TouchType;

      private Camera rayCamera; // 用于发射射线的相机
      [SerializeField] private bool editRay = true; // 是否在Scene视图中显示射线
      [SerializeField] private KeyCode[] keys = new KeyCode[4];
      [SerializeField] private float cameraDistanceMulti = 1;
      private InputData InputData => Tea_Calculate.InputData;

      private Vector3 basePoint;
      private Vector3 forward = default;
      private void Awake()
      {
         // 获取相机，如果没有指定则使用主相机
         if (!rayCamera) rayCamera = Camera.main;

         basePoint = rayCamera.transform.localPosition;
         forward = basePoint.normalized;
         TouchType = (float)Screen.width / Screen.height < 1;
         // 根据屏幕长宽比调整相机位置
         AdjustCameraPosition();
      }
      private void AdjustCameraPosition()
      {
         // 如果是窄屏（长宽比小于1），调整相机距离
         if (TouchType)
         {
            // 计算新的距离，可以根据需要调整系数
            var forwardAdd = (1f / Screen.width / Screen.height) * cameraDistanceMulti * forward;

            rayCamera.transform.localPosition = basePoint + forwardAdd;
         }
         else
         {
            rayCamera.transform.localPosition = basePoint;
         }
      }

      private void Update()
      {
         MouseInput();
         KeyboardInput();
         if (!TouchType) HandleRayDetection(Input.mousePosition);
      }
      private void MouseInput()
      {
         if (Input.GetKeyDown(KeyCode.Escape))
         {
            Cursor.visible = true;//鼠标显示
         }
         InputData.isMouseDown = Input.GetMouseButton(0);

         // 移动端处理触摸输入
         if (Input.touchCount > 0)
         {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
               HandleRayDetection(touch.position);
               Tea_Calculate.keyClick.Invoke(-1, true);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
               Tea_Calculate.keyClick.Invoke(-1, false);
            }
            else if (touch.phase == TouchPhase.Stationary)
            {
               HandleRayDetection(touch.position);
            }

            // 处理触摸移动
            if (touch.phase == TouchPhase.Moved)
            {
               HandleRayDetection(touch.position);
            }
         }

         // PC端处理鼠标输入
         if (Input.GetMouseButtonDown(0))
         {
            Tea_Calculate.keyClick.Invoke(-1, true);
            Cursor.visible = false;//鼠标显示
         }
         else if (Input.GetMouseButtonUp(0))
         {
            Tea_Calculate.keyClick.Invoke(-1, false);
         }
      }

      /// <summary> 处理射线检测 </summary>
      private void HandleRayDetection(Vector3 screenPosition)
      {
         // 从相机创建一条射向指定屏幕位置的射线
         Ray ray = rayCamera.ScreenPointToRay(screenPosition);

         // 创建两个LayerMask用于分别检测图层0和图层6
         int layer0Mask = 1 << 0;  // 图层0的掩码
         int layer6Mask = 1 << 6;  // 图层6的掩码

         // 检测图层0（按钮层）
         RaycastHit[] buttonHits = Physics.RaycastAll(ray, InputData.rayLength, layer0Mask);
         Transform hoverButton = null;
         foreach (RaycastHit hit in buttonHits)
         {
            if (editRay)
            {
               Debug.DrawLine(ray.origin, hit.point, Color.red, Time.deltaTime);
            }
            hoverButton = hit.transform;
            InputData.pointerPosition = hit.point;
         }

         // 检测图层6
         RaycastHit[] layer6Hits = Physics.RaycastAll(ray, InputData.rayLength, layer6Mask);
         foreach (RaycastHit hit in layer6Hits)
         {
            if (editRay)
            {
               DrawDebugSphere(hit.point, InputData.sphereRadius, Color.blue);
            }

            // 移动端使用触摸位置的delta
            if (Input.touchCount > 0)
            {
               Touch touch = Input.GetTouch(0);
               Tea_Calculate.mouseMpvement.Invoke(
                  hit.point,
                  touch.deltaPosition);
            }
            // PC端使用鼠标移动
            Tea_Calculate.mouseMpvement.Invoke(
               hit.point,
               new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
         }

         // 处理悬停按钮的变化
         if (!InputData.isMouseDown && InputData.hoverButton != hoverButton)
         {
            var Before = InputData.hoverButton;
            InputData.hoverButton = hoverButton;

            Tea_Calculate.keyHover?.Invoke(Before, hoverButton);
         }
      }

      private void KeyboardInput()
      {
         for (int i = 0; i < keys.Length; i++)
         {
            if (Input.GetKeyDown(keys[i]))
            {
               Tea_Calculate.keyClick.Invoke(i, true);
            }
            else if (Input.GetKeyUp(keys[i]))
            {
               Tea_Calculate.keyClick.Invoke(i, false);
            }
         }
      }

      // 在场景视图中绘制调试球体
      private void DrawDebugSphere(Vector3 center, float radius, Color color)
      {
         // 在多个方向绘制短线段，形成球体的视觉效果
         Vector3[] directions = new Vector3[]
         {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back,
                new Vector3(1, 1, 1).normalized, new Vector3(-1, 1, 1).normalized,
                new Vector3(1, -1, 1).normalized, new Vector3(-1, -1, 1).normalized,
                new Vector3(1, 1, -1).normalized, new Vector3(-1, 1, -1).normalized,
                new Vector3(1, -1, -1).normalized, new Vector3(-1, -1, -1).normalized
         };

         foreach (Vector3 dir in directions)
         {
            Debug.DrawLine(center, center + dir * radius, color, Time.deltaTime);
         }
      }

   }
   [System.Serializable]
   public class InputData
   {
      public float rayLength = 100f; // 射线长度
      public float sphereRadius = 0.1f; // 用于显示图层6碰撞点的球体半径

      public Vector3 pointerPosition;
      public bool isMouseDown = false; // 鼠标按下状态
      public Transform hoverButton;
   }
}
