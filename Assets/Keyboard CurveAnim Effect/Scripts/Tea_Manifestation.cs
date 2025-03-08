using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
namespace Tea_Demos
{

   /// <summary>
   /// 表现效果(动画/音频)
   /// </summary>
   public class Tea_Manifestation : MonoBehaviour
   {
      #region other
      /// <summary>按钮样式数据</summary>
      [SerializeField] private DrawData[] drawDatas;

      [SerializeField] private Transform pointerAxis;
      [SerializeField] private Transform pointer;
      [SerializeField] private Transform Keyboard1;
      [SerializeField] private Transform Keyboard2;
      [SerializeField] private Transform[] buttons;
      [SerializeField] private MeshRenderer KeyboardRenderer;
      [SerializeField] private SpriteRenderer bg;
      /// <summary>按钮网格过滤器缓存</summary>
      private MeshRenderer[] buttonRenderer;
      /// <summary>保存按钮的移动动画Tween</summary>
      private Tweener[] keyTweeners;
      private Tweener KeyboardTweenerPHor;
      private Tweener KeyboardTweenerPy;
      private Tweener KeyboardTweenerRx;
      private Tweener KeyboardTweenerRz;

      #endregion

      #region 鼠标动画
      public MouseAnimData MouseAnimData => Tea_Calculate.MouseAnimData;
      public InputData InputData => Tea_Calculate.InputData;
      private Vector3 m_offset;

      private Quaternion targetRotation; // 目标旋转
      private Vector3 currentRotation = Vector3.zero; // 当前累积的旋转角度
      #endregion

      #region Data
      [SerializeField] ButtonAnimData buttonAnimData;
      [SerializeField] KeyboardAnimData KeyboardAnimData;
      #endregion

      #region Audio
      [SerializeField] private AudioClip[] clips;
      [SerializeField] private AudioSource sourcePrefab;  // 音频源预制体
      private Queue<AudioSource> audioSourcePool;  // 音频源对象池
      private List<AudioSource> activeAudioSources;  // 当前活跃的音频源
      #endregion

      private void Awake()
      {
         Tea_Calculate.drawDataIndex = drawDatas.Length;
         Tea_Calculate.mouseMpvement = Delegate_MouseMovement;
         Tea_Calculate.keyAnim = KeyAnim;
         Tea_Calculate.KeyboardAnim = KeyboardAnim;
         Tea_Calculate.audioPlay = AudioPlay;

         Tea_Calculate.key = buttons;

         keyTweeners = new Tweener[buttons.Length];
         List<MeshRenderer> meshRenderers = new();
         for (int i = 0; i < buttons.Length; i++)
         {
            meshRenderers.Add(buttons[i].GetComponent<MeshRenderer>());
         }
         buttonRenderer = meshRenderers.ToArray();

         InitAudioPool();  // 初始化音频对象池

         if ((float)Screen.width / Screen.height < 1)
         {
            pointerAxis.gameObject.SetActive(false);
         }
      }

      private void Update()
      {
         UpdatePointerRotation();
      }

      #region 鼠标移动
      private void Delegate_MouseMovement(Vector3 target, Vector2 mouseOffset)
      {
         pointerAxis.position = target;
         m_offset = mouseOffset;
      }
      // 更新指针旋转
      private void UpdatePointerRotation()
      {
         var offset = m_offset * MouseAnimData.rotateMulti;

         // 累积旋转角度（考虑最大旋转角度限制）
         // 现在mouseX影响X轴旋转，mouseY影响Z轴旋转
         currentRotation.x = Mathf.Clamp(currentRotation.x + offset.x, -MouseAnimData.maxRotateAngle, MouseAnimData.maxRotateAngle);
         currentRotation.z = Mathf.Clamp(currentRotation.z + offset.y, -MouseAnimData.maxRotateAngle, MouseAnimData.maxRotateAngle);

         // 当鼠标停止移动时，逐渐回正
         if (Mathf.Approximately(offset.x, 0f) && Mathf.Approximately(offset.y, 0f))
         {
            currentRotation.x = Mathf.Lerp(currentRotation.x, 0, MouseAnimData.returnSpeed * Time.deltaTime);
            currentRotation.z = Mathf.Lerp(currentRotation.z, 0, MouseAnimData.returnSpeed * Time.deltaTime);
         }

         // 检测鼠标按下和抬起
         // 隐藏鼠标
         if (InputData.isMouseDown) { Cursor.visible = false; }

         // 鼠标点击时额外增加Z轴旋转
         float clickZRotation = InputData.isMouseDown ? MouseAnimData.clickRotateZ : 0f;

         // 创建目标旋转 - 只使用X轴和Z轴旋转，Y轴保持不变
         targetRotation = Quaternion.Euler(currentRotation.x, 0, currentRotation.z + clickZRotation);

         // 平滑插值到目标旋转
         pointer.rotation = Quaternion.Lerp(pointer.rotation, targetRotation, MouseAnimData.rotateSmoothTime);
      }
      #endregion

      #region 键盘动画
      public void KeyboardAnim(int clickIndex, Vector3 offset, bool isDown, int updateIndex)
      {
         KeyboardTweenerPHor?.Kill();
         KeyboardTweenerPy?.Kill();
         KeyboardTweenerRx?.Kill();
         KeyboardTweenerRz?.Kill();

         CurveData curveDataP;
         CurveData curveDataR;
         string str = "键盘动画组";
         if (updateIndex >= 0 && !isDown)
         {
            str += "更新";
            curveDataP = KeyboardAnimData.kBoard_ReleaseOverM;
            curveDataR = KeyboardAnimData.kBoard_ReleaseOverR;

            KeyboardRenderer.sharedMaterial = drawDatas[updateIndex].material;
            bg.color = drawDatas[updateIndex].bgColor;
         }
         else if (isDown)
         {
            str += "按下";
            curveDataP = KeyboardAnimData.kBoard_ClickM;
            curveDataR = KeyboardAnimData.kBoard_ClickR;
         }
         else
         {
            str += "弹起";
            curveDataP = KeyboardAnimData.kBoard_ReleaseM;
            curveDataR = KeyboardAnimData.kBoard_ReleaseR;
         }
         //Debug.Log($"{str}\n{clickIndex}|{offset}|{isDown}|{updateIndex}");
         Vector3 offset_PHor = new Vector3(-offset.z, 0, offset.x) * KeyboardAnimData.Length_HorizMulti;
         Vector3 offset_Py = Vector3.down * ((clickIndex != 0 ? KeyboardAnimData.Length : 0) + clickIndex * KeyboardAnimData.LengthAdd);

         // 如果角度在限制范围内，直接执行原来的动画
         Vector3 offset_R = offset * KeyboardAnimData.Angle;

         KeyboardTweenerPHor = Keyboard1.DOLocalMove(offset_PHor, curveDataP.Timer)
            .SetEase(curveDataP.Curve);
         KeyboardTweenerPy = Keyboard2.DOLocalMove(offset_Py, curveDataP.Timer)
            .SetEase(curveDataP.Curve);

         float Rx = AnglesSet(Keyboard1.localEulerAngles.x);
         float Rz = AnglesSet(Keyboard2.localEulerAngles.z);

         Keyboard1.localEulerAngles = Vector3.right * Rx;
         KeyboardTweenerRx = Keyboard1.DOLocalRotate(Vector3.right * offset_R.x, curveDataR.Timer)
            .SetEase(curveDataR.Curve);

         Keyboard2.localEulerAngles = Vector3.forward * Rz;
         KeyboardTweenerRz = Keyboard2.DOLocalRotate(Vector3.forward * offset_R.z, curveDataR.Timer)
            .SetEase(curveDataR.Curve);

         static float AnglesSet(float _base)
         {
            if (_base > 180) _base -= 360;
            else if (_base < -180) _base += 360;

            if (_base > 90) _base /= 2;
            return _base;
         }
      }

      #endregion

      #region 按钮动画

      public void KeyAnim(int index, int type, bool isDown)
      {
         string logStr = "执行按钮动画: ";
         keyTweeners[index]?.Kill();
         float yoffset = 0;
         CurveData curveData;
         if (type < 0 && isDown)
         {
            curveData = buttonAnimData.btn_Hover;
            yoffset = -buttonAnimData.btn_Hover.Length;
            logStr += "放置";
         }
         else if (type < 0 && !isDown)
         {
            curveData = buttonAnimData.btn_Leave;
            logStr += "离开";
         }
         // 按下
         else if (isDown)
         {
            curveData = buttonAnimData.btn_Click;
            yoffset = -buttonAnimData.btn_Click.Length;
            logStr += "点击" + yoffset;
         }
         // 弹起
         else
         {
            buttonRenderer[index].sharedMaterial = drawDatas[type].material;
            curveData = buttonAnimData.btn_Release;
            logStr += "弹起";
         }
         AnimationCurve curve = curveData.Curve;
         float timer = curveData.Timer;
         keyTweeners[index] = buttons[index].DOLocalMoveY(yoffset, timer)
             .SetEase(curve);
         //Debug.Log(logStr);
      }
      #endregion

      #region audio
      private void InitAudioPool()
      {
         audioSourcePool = new Queue<AudioSource>();
         activeAudioSources = new List<AudioSource>();

         // 初始化对象池，预先创建5个AudioSource
         for (int i = 0; i < 5; i++)
         {
            CreateNewAudioSource();
         }
      }

      private void CreateNewAudioSource()
      {
         AudioSource newSource = Instantiate(sourcePrefab, transform);
         newSource.gameObject.SetActive(false);
         audioSourcePool.Enqueue(newSource);
      }

      private AudioSource GetAudioSourceFromPool()
      {
         // 如果池中没有可用的AudioSource，创建新的
         if (audioSourcePool.Count == 0)
         {
            CreateNewAudioSource();
         }

         AudioSource source = audioSourcePool.Dequeue();
         source.gameObject.SetActive(true);
         activeAudioSources.Add(source);
         return source;
      }

      private void ReturnAudioSourceToPool(AudioSource source)
      {
         source.Stop();
         source.gameObject.SetActive(false);
         activeAudioSources.Remove(source);
         audioSourcePool.Enqueue(source);
      }

      public void AudioPlay(int clipIndex)
      {
         if (clipIndex < 0 || clipIndex >= clips.Length) return;

         AudioSource source = GetAudioSourceFromPool();
         source.clip = clips[clipIndex];
         source.Play();

         // 使用协程在音频播放完成后将AudioSource返回池中
         StartCoroutine(ReturnToPoolAfterPlay(source));
      }

      private IEnumerator ReturnToPoolAfterPlay(AudioSource source)
      {
         yield return new WaitForSeconds(source.clip.length);
         ReturnAudioSourceToPool(source);
      }

      private void OnDestroy()
      {
         // 清理所有活跃的音频源
         foreach (var source in activeAudioSources.ToArray())
         {
            ReturnAudioSourceToPool(source);
         }
      }
      #endregion
   }
   [System.Serializable]
   public class MouseAnimData
   {
      #region 鼠标动画
      public float rotateMulti = 5f; // 鼠标移动旋转灵敏度
      public float maxRotateAngle = 15f; // 最大旋转角度
      public float clickRotateZ = 25f; // 点击时Z轴旋转角度
      public float rotateSmoothTime = 0.2f; // 旋转平滑时间
      public float returnSpeed = 1f; // 指针回正速度
      #endregion
   }


   [System.Serializable]
   public class ButtonAnimData
   {
      public CurveData_Length btn_Hover;
      public CurveData btn_Leave;

      public CurveData_Length btn_Click;
      public CurveData btn_Release;
   }
   [System.Serializable]
   public class KeyboardAnimData
   {
      [Range(0, 1f)] public float Length;
      [Range(0, 1f)] public float Length_HorizMulti;
      [Range(0, 1f)] public float LengthAdd;
      [Range(0, 45f)] public float Angle;
      public CurveData kBoard_ClickM;
      public CurveData kBoard_ClickR;

      public CurveData kBoard_ReleaseM;
      public CurveData kBoard_ReleaseR;

      public CurveData kBoard_ReleaseOverM;
      public CurveData kBoard_ReleaseOverR;
   }

   [System.Serializable]
   public class CurveData
   {
      public AnimationCurve Curve;
      [Range(0, 2f)] public float Timer;
   }
   [System.Serializable]
   public class CurveData_Length : CurveData
   {
      [Range(0, 1f)] public float Length;
   }
   /// <summary>
   /// 按钮显示数据类
   /// </summary>
   [System.Serializable]
   public class DrawData
   {
      /// <summary>按钮材质</summary>
      public Material material;
      /// <summary>对应的背景颜色</summary>
      public Color bgColor;
   }
}