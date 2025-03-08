using UnityEngine;
using UnityEditor;
using Tea_Demos;

namespace Tea_Demos.Editor
{
   // 基础PropertyDrawer类，包含共享的绘制逻辑
   public abstract class BaseCurveDataDrawer : PropertyDrawer
   {
      protected struct LayoutRects
      {
         public Rect labelRect;
         public Rect curveRect;
         public Rect sliderRect;
         public Rect separatorRect;
      }

      protected LayoutRects CalculateLayout(Rect position)
      {
         // 标签占据第一行
         Rect labelRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

         // 计算剩余内容的起始位置（从第二行开始）
         float contentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

         // 计算滑块和曲线的宽度
         float sliderWidth = position.width * 0.4f;
         float spacing = position.width * 0.02f;
         float curveWidth = position.width - sliderWidth - spacing;

         // 计算内容区域的总高度
         float contentHeight = EditorGUIUtility.singleLineHeight * 4.6f;

         return new LayoutRects
         {
            labelRect = labelRect,
            sliderRect = new Rect(position.x, contentY, sliderWidth, contentHeight),
            curveRect = new Rect(position.x + sliderWidth + spacing, contentY, curveWidth, contentHeight - 2.5f),
            separatorRect = new Rect(position.x, contentY + contentHeight + EditorGUIUtility.standardVerticalSpacing,
                                   position.width, 1)
         };
      }

      protected void DrawCurveAndTimer(Rect position, SerializedProperty property, GUIContent label, LayoutRects rects)
      {
         // 绘制标签
         EditorGUI.LabelField(rects.labelRect, label);

         SerializedProperty curveProp = property.FindPropertyRelative("Curve");
         SerializedProperty timerProp = property.FindPropertyRelative("Timer");

         // 绘制Timer滑块（左侧）
         EditorGUI.LabelField(
            new Rect(rects.sliderRect.x, rects.sliderRect.y, rects.sliderRect.width, EditorGUIUtility.singleLineHeight), "Timer");
         timerProp.floatValue = EditorGUI.Slider(
             new Rect(rects.sliderRect.x, rects.sliderRect.y + EditorGUIUtility.singleLineHeight, 
             rects.sliderRect.width, EditorGUIUtility.singleLineHeight),
             timerProp.floatValue, 0f, 2f);

         // 绘制曲线（右侧）
         EditorGUI.PropertyField(rects.curveRect, curveProp, GUIContent.none);

         // 绘制分隔线
         EditorGUI.DrawRect(rects.separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
      }

      protected void DrawExtraSlider(Rect sliderRect, SerializedProperty property, string propertyName, float minValue, float maxValue)
      {
         SerializedProperty prop = property.FindPropertyRelative(propertyName);
         EditorGUI.LabelField(
             new Rect(sliderRect.x, sliderRect.y + EditorGUIUtility.singleLineHeight * 2, sliderRect.width, EditorGUIUtility.singleLineHeight),
             propertyName);
         prop.floatValue = EditorGUI.Slider(
             new Rect(sliderRect.x, sliderRect.y + EditorGUIUtility.singleLineHeight * 3, sliderRect.width, EditorGUIUtility.singleLineHeight),
             prop.floatValue, minValue, maxValue);
      }

      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         return EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 10;
      }
   }

   // 基础CurveData的属性绘制器
   [CustomPropertyDrawer(typeof(CurveData))]
   public class CurveDataDrawer : BaseCurveDataDrawer
   {
      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         EditorGUI.BeginProperty(position, label, property);

         var rects = CalculateLayout(position);
         DrawCurveAndTimer(position, property, label, rects);

         EditorGUI.EndProperty();
      }
   }

   // CurveData_Length的属性绘制器
   [CustomPropertyDrawer(typeof(CurveData_Length))]
   public class CurveDataLengthDrawer : BaseCurveDataDrawer
   {
      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         EditorGUI.BeginProperty(position, label, property);

         var rects = CalculateLayout(position);
         DrawCurveAndTimer(position, property, label, rects);
         DrawExtraSlider(rects.sliderRect, property, "Length", 0f, 1f);

         EditorGUI.EndProperty();
      }

   }
}