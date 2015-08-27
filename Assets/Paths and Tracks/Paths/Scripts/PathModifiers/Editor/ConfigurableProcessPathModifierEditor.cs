using UnityEngine;
using System;
using System.Reflection;

using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using Util;
using Util.Editor;
using Paths;

namespace Paths.Editor
{
    [CustomToolEditor(typeof(ConfigurableProcessPathModifier))]
    public class FallbackConfigurableProcessPathModifierEditor : ConfigurableProcessPathModifierEditor
    {
        protected override void DrawGeneralConfigurationGUI(PathModifierEditorContext context)
        {
            FallbackCustomToolEditor.DoDrawInspectorGUI(context, 
                                                         "PositionFunction", "DirectionFunction", "UpVectorFunction", 
                                                         "DistanceFromPreviousFunction", "DistanceFromBeginFunction", "AngleFunction");
        }
//        public override void OnDrawInspectorGUI(PathModifierEditorContext context)
//        {
//            //base.OnDrawInspectorGUI(context);
//        }
    }

    public abstract class ConfigurableProcessPathModifierEditor : AbstractPathModifierEditor
    {
        delegate void DrawFunctionOptionsFunc (PathModifierEditorContext context);

        public override sealed void DrawInspectorGUI(PathModifierEditorContext context)
        {
            OnDrawInspectorGUI(context);
        }

        public virtual void OnDrawInspectorGUI(PathModifierEditorContext context)
        {
            DrawDefaultInspectorGUI(context);
        }

        public void DrawDefaultInspectorGUI(PathModifierEditorContext context)
        {
            //            ConfigurableProcessPathModifier pm = (ConfigurableProcessPathModifier)context.PathModifier;
            DrawFunctionSelections(context);
            DrawGeneralConfigurationGUI(context);
            //            
            //            EditorGUI.BeginChangeCheck();
            //
            //            if (EditorGUI.EndChangeCheck())
            //            {
            //                Undo.RecordObject(context.Target, "GenerateComponents Configuration");
            //                context.TargetModified();
            //                EditorUtility.SetDirty(context.Target);
            //            }
        }

        static string[] ToStringArray<T>(T[] objects)
        {
            string[] strs = new string[objects.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                strs [i] = objects [i].ToString();
            }
            return strs;
        }
        
        PathModifierFunction FunctionSelection(PathModifierEditorContext context, string label, PathModifierFunction[] allowedFunctions, PathModifierFunction currentFunction, DrawFunctionOptionsFunc drawOptionsFunc)
        {
            string[] allowedValues = ToStringArray(allowedFunctions);
            int currentFunctionIndex = -1;
            for (int i = 0; i < allowedFunctions.Length; i++)
            {
                if (allowedFunctions [i] == currentFunction)
                {
                    currentFunctionIndex = i;
                    break;
                }
            }
            EditorGUI.BeginDisabledGroup(allowedValues.Length < 1);
            if (allowedValues.Length == 1)
            {
                //EditorGUILayout.LabelField(label, allowedValues [0], GUI.skin.textField);
                currentFunctionIndex = 0;
            }
            currentFunctionIndex = EditorGUILayout.Popup(label, currentFunctionIndex, allowedValues);
            EditorGUI.EndDisabledGroup();

            if (currentFunctionIndex >= 0 && currentFunctionIndex < allowedFunctions.Length)
            {
                currentFunction = allowedFunctions [currentFunctionIndex];
            } else
            {
                currentFunction = default(PathModifierFunction);
            }
            
            if (null != drawOptionsFunc)
            {
                if (currentFunction == PathModifierFunction.Generate || currentFunction == PathModifierFunction.Process)
                {
                    EditorGUI.indentLevel++;
                    drawOptionsFunc(context);
                    EditorGUI.indentLevel--;
                }
            }
            
            return currentFunction;
        }
        
        public void DrawFunctionSelections(PathModifierEditorContext context)
        {
            ConfigurableProcessPathModifier pm = (ConfigurableProcessPathModifier)context.PathModifier;
            
            EditorGUI.BeginChangeCheck();
            
            pm.PositionFunction = FunctionSelection(context, "Position Fn", pm.AllowedPositionFunctions, pm.PositionFunction, DrawPositionFunctionGUI);
            pm.DirectionFunction = FunctionSelection(context, "Direction Fn", pm.AllowedDirectionFunctions, pm.DirectionFunction, DrawDirectionFunctionGUI);
            pm.UpVectorFunction = FunctionSelection(context, "Up Vector Fn", pm.AllowedUpVectorFunctions, pm.UpVectorFunction, DrawUpVectorFunctionGUI);
            pm.AngleFunction = FunctionSelection(context, "Angle Fn", pm.AllowedAngleFunctions, pm.AngleFunction, DrawAngleFunctionGUI);

            pm.DistanceFromPreviousFunction = FunctionSelection(context, "Dist[prev] Fn", pm.AllowedDistanceFromPreviousFunctions, pm.DistanceFromPreviousFunction, DrawDistanceFunctionGUI);
            pm.DistanceFromBeginFunction = FunctionSelection(context, "Dist[begin] Fn", pm.AllowedDistanceFromBeginFunctions, pm.DistanceFromBeginFunction, DrawDistanceFunctionGUI);

            if (EditorGUI.EndChangeCheck())
            {
                //                Undo.RecordObject(context.Target, "GenerateComponents Configuration");
                context.TargetModified();
                EditorUtility.SetDirty(context.Target);
            }
            
        }

        protected virtual void DrawPositionFunctionGUI(PathModifierEditorContext context)
        {
        }

        protected virtual void DrawDirectionFunctionGUI(PathModifierEditorContext context)
        {
        }

        protected virtual void DrawUpVectorFunctionGUI(PathModifierEditorContext context)
        {
        }

        protected virtual void DrawAngleFunctionGUI(PathModifierEditorContext context)
        {
        }

        protected virtual void DrawDistanceFunctionGUI(PathModifierEditorContext context)
        {
        }

        protected virtual void DrawGeneralConfigurationGUI(PathModifierEditorContext context)
        {

        }

    }
    
}
