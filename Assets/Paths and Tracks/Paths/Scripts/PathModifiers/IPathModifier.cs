using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Paths
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class PathModifier : System.Attribute
    {
        public string displayName;

        /// <summary>
        /// Flags for required flags on input PathPoints.
        /// </summary>
        public int requiredInputFlags = PathPoint.NONE;
        public int processCaps = PathPoint.NONE;

        /// <summary>
        /// Specifies the mask for output properties; properties of 
        /// input PathPoints will be included in the output masked
        /// by this mask.
        /// </summary>
        public int passthroughCaps = PathPoint.NONE;

        /// <summary>
        /// Specifies properties that this modifier can generate
        /// for output PathPoints even if they are missing from
        /// input PathPoints.
        /// </summary>
        public int generateCaps = PathPoint.NONE;

        public PathModifier ()
        {
        }
    }

    public class PathModifierContext
    {
        private IPathModifierContainer pathModifierContainer;
        private int inputFlags;
        private IPathInfo pathInfo;

        public PathModifierContext (IPathInfo pathInfo, IPathModifierContainer pathModifierContainer, int inputFlags)
        {
            this.pathInfo = pathInfo;
            this.pathModifierContainer = pathModifierContainer;
            this.inputFlags = inputFlags;
        }

        public PathModifierContext (Path path, IPathModifierContainer pathModifierContainer, int inputFlags)
            : this(path.GetPathInfo(), pathModifierContainer, inputFlags)
        {
        }

        public PathModifierContext (Path path, int inputFlags) : this(path, path.GetPathModifierContainer(), inputFlags)
        {
        }

        public IPathInfo PathInfo {
            get {
                return pathInfo;
            }
        }

        public IPathModifierContainer PathModifierContainer {
            get {
                return pathModifierContainer;
            }
        }

        public int InputFlags {
            get {
                return inputFlags;
            }
        }
    }

    public interface IPathModifier
    {
        bool IsEnabled();

        void SetEnabled(bool value);
        //void SetEnabled(bool enabled);

        void LoadParameters(ParameterStore store);

        void SaveParameters(ParameterStore store);

        PathPoint[] GetModifiedPoints(PathPoint[] points, PathModifierContext context);

        int GetRequiredInputFlags();

        int GetProcessFlags(PathModifierContext context);

        int GetPassthroughFlags(PathModifierContext context);

        int GetGenerateFlags(PathModifierContext context);

        int GetOutputFlags(PathModifierContext context);

        //void DrawInspectorGUI(TrackInspector trackInspector);

        Path[] GetPathDependencies();

        string GetName();

        string GetDescription();

        string GetInstanceName();

        void SetInstanceName(string name);

        string GetInstanceDescription();

        void SetInstanceDescription(string description);

        void Attach(IPathModifierContainer container);

        void Detach();
    }

    public interface IReferenceContainer
    {
        int GetReferentCount();

        UnityEngine.Object GetReferent(int index);

        void SetReferent(int index, UnityEngine.Object obj);

        int AddReferent(UnityEngine.Object obj);

        void RemoveReferent(int index);
    }

    public interface IPathModifierContainer
    {

        IPathModifier[] GetPathModifiers();

        void AddPathModifer(IPathModifier pm);

        void InsertPathModifer(int index, IPathModifier pm);

        void RemovePathModifer(int index);

        int IndexOf(IPathModifier pm);

        bool IsSupportsApplyPathModifier();

        void ApplyPathModifier(int index);

        IReferenceContainer GetReferenceContainer();



    }

    public abstract class AbstractPathModifier : IPathModifier
    {


        internal bool enabled = true;
        private bool _inLoadParameters = false;
        private bool _inSaveParameters = false;
        protected int inputCaps;
        protected int processCaps;
        protected int passthroughCaps;
        protected int generateCaps;
        protected string instanceName;
        protected string instanceDescription;
        private string name;
        private IPathModifierContainer container;

        public AbstractPathModifier ()
        {
            PathModifierUtil.GetPathModifierCapsFromAttributes(GetType(), out inputCaps, out processCaps, out passthroughCaps, out generateCaps);
            this.name = GetDisplayName(GetType());
        }

        private bool _onAttach;

        public void Attach(IPathModifierContainer container)
        {
            if (!_onAttach)
            {
                this.container = container;
                try
                {
                    _onAttach = true;
                    OnAttach();
                } finally
                {
                    _onAttach = false;
                }
            } else
            {
                Debug.LogWarning("Attach() called from OnAttach() - ignoring");
            }
        }

        private bool _onDetach;

        public void Detach()
        {
            if (!_onDetach)
            {

                try
                {
                    _onDetach = true;
                    OnDetach();
                } finally
                {
                    _onDetach = false;
                    this.container = null;
                }
            } else
            {
                Debug.LogWarning("Detach() called from OnDetach() - ignoring");
            }
        }

        public virtual void OnAttach()
        {
        }

        public virtual void OnDetach()
        {

        }

        protected IPathModifierContainer GetContainer()
        {
            return container;
        }

        public static string GetDisplayName(Type toolType)
        {
            string name = CustomTool.GetToolName(toolType);
            if (StringUtil.IsEmpty(name))
            {
                name = GetFallbackDisplayName(toolType);
            }
            return name;
        }

        private static string GetFallbackDisplayName(Type toolType)
        {
            return StringUtil.RemoveStringTail(StringUtil.RemoveStringTail(toolType.Name, "Modifier", 1), "Path", 1);
        }
        
        public virtual bool IsEnabled()
        {
            return enabled;
        }

        public virtual void SetEnabled(bool value)
        {
            this.enabled = value;
        }

        public virtual int GetRequiredInputFlags()
        {
            return inputCaps;
        }

        public virtual int GetProcessFlags(PathModifierContext context)
        {
            return processCaps & context.InputFlags;
        }

        public virtual int GetPassthroughFlags(PathModifierContext context)
        {
            return (passthroughCaps & context.InputFlags) & ~GetProcessFlags(context);
        }

        public virtual int GetGenerateFlags(PathModifierContext context)
        {
            return generateCaps;
        }

        public int GetOutputFlags(PathModifierContext context)
        {
            return (context.InputFlags & (GetProcessFlags(context) | GetPassthroughFlags(context))) | GetGenerateFlags(context);
        }

        public void LoadParameters(ParameterStore store)
        {
            // TODO should we serialize inputCaps, outputCaps etc.?
            if (!_inLoadParameters)
            {
                enabled = store.GetBool("enabled", enabled);
                instanceName = store.GetString("instanceName", instanceName);
                instanceDescription = store.GetString("instanceDescription", instanceDescription);
                try
                {
                    _inLoadParameters = true;
                    OnLoadParameters(store);
                } finally
                {
                    _inLoadParameters = false;
                }
            } else
            {
                Debug.LogWarning("LoadParameters() called from OnLoadParameters() - ignoring");
            }
            
        }

        public virtual void OnSerialize(Serializer ser)
        {
        }

        public void OnLoadParameters(ParameterStore store)
        {
            Serializer ser = new Serializer(store, false);
            OnSerialize(ser);
        }
        
        public void SaveParameters(ParameterStore store)
        {
            // TODO should we serialize inputCaps, outputCaps etc.?
            if (!_inSaveParameters)
            {
                store.SetBool("enabled", enabled);
                store.SetString("instanceName", instanceName);
                store.SetString("instanceDescription", instanceDescription);
                try
                {
                    _inSaveParameters = true;
                    OnSaveParameters(store);
                } finally
                {
                    _inSaveParameters = false;
                }
            } else
            {
                Debug.LogWarning("SaveParameters() called from OnSaveParameters() - ignoring");
            }
        }
        
        public void OnSaveParameters(ParameterStore store)
        {
            Serializer ser = new Serializer(store, true);
            OnSerialize(ser);
        }
        
        public abstract PathPoint[] GetModifiedPoints(PathPoint[] points, PathModifierContext context);

        public virtual Path[] GetPathDependencies()
        {
            return new Path[0];
        }

        public string GetName()
        {
            return name;
        }

        public virtual string GetDescription()
        {
            return "";
        }

        public string GetInstanceName()
        {
            return instanceName;
        }

        public void SetInstanceName(string name)
        {
            this.instanceName = name;
        }

        public string GetInstanceDescription()
        {
            return instanceDescription;
        }

        public void SetInstanceDescription(string description)
        {
            this.instanceDescription = description;
        }
    }


}
