using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Util;
using Paths;

namespace Tracks
{
    public class Track : MonoBehaviour, ISerializationCallbackReceiver, IReferenceContainer
    {

        [SerializeField]
        private Path
            path;
        [SerializeField]
        private string
            trackGeneratorType;
        [SerializeField]
        internal ParameterStore
            parameters;

        // don't serialize!
        private DefaultPathModifierContainer pathModifierContainer = null;

        //[SerializeField]
        public Mesh generatedMesh;

        // Just for editor support!
        // nonserialized
        private TrackSlice[] generatedSlices;

        // nonserialized
        private ITrackGenerator _trackGeneratorInstance;
        private PathPoint[] pathPoints = null;
        private int pathPointFlags = 0;


        // TODO should this be serialized?
        private bool autoUpdateWithPath = false;
        private bool autoUpdateMesh = false;

        [SerializeField]
        private List<UnityEngine.Object>
            referents = new List<UnityEngine.Object>();

        

        public Track()
        {
        
        }
        //~Track() {
        //UnregisterPathChangedListener();
        //}

    
        public bool AutomaticUpdateWithPath
        {
            get { return autoUpdateWithPath; }
            set { this.autoUpdateWithPath = value; }
        }

        public bool AutomaticMeshUpdate
        {
            get { return autoUpdateMesh; }
            set { this.autoUpdateMesh = value; }
        }

        public ITrackGenerator TrackGeneratorInstance
        {
            get
            {
                if (null == _trackGeneratorInstance)
                {
                    if (null != trackGeneratorType && trackGeneratorType.Length > 0)
                    {
                        // Create the instance
                        _trackGeneratorInstance = (ITrackGenerator)Activator.CreateInstance(Type.GetType(trackGeneratorType));
                        // Load parameters
                        // Load params:
                        parameters.OnAfterDeserialize();
                        _trackGeneratorInstance.LoadParameters(GetTrackGeneratorParameterStore());
                    }
                }
                return _trackGeneratorInstance;
            }
        }

        public Path Path
        {
            get
            {
                return path;
            }
            set
            {
                if (this.path != value)
                {
                    this.path = value;
                    this.ConfigurationChanged();
                }
            }
        }

        public string TrackGeneratorType
        {
            get
            {
                return trackGeneratorType;
            }
            set
            {
                if (this.trackGeneratorType != value)
                {
                    // Changed
                    this.trackGeneratorType = value;
                    TrackGeneratorChanged();
                }
            }
        }

        // Returns a copy of the internal array
        public TrackSlice[] TrackSlices
        {
            get
            {
                if (null == generatedSlices)
                {
                    generatedSlices = TrackGeneratorInstance.CreateSlices(this, false);
                }
                TrackSlice[] arr = new TrackSlice[generatedSlices.Length];
                Array.Copy(generatedSlices, arr, generatedSlices.Length);
                return arr;
            }
        }

        public ParameterStore ParameterStore
        {
            get
            {
                return parameters;
            }
        }

        // TODO how to clean up references???
        public int GetReferentCount()
        {
            return referents.Count;
        }
        
        public UnityEngine.Object GetReferent(int index)
        {
            return referents [index];
        }
        public void SetReferent(int index, UnityEngine.Object obj)
        {
            referents [index] = obj;
        }
        public int AddReferent(UnityEngine.Object obj)
        {
            referents.Add(obj);
            return referents.Count - 1;
        }
        public void RemoveReferent(int index)
        {
            referents.RemoveAt(index);
        }

        private void TrackGeneratorChanged()
        {
            this._trackGeneratorInstance = null;
        }
    
        private void RegisterPathChangedListener()
        {
//          Debug.Log ("Registering PathChangedEventHandler on '" + path + "': " + this.gameObject.name);
            PathChangedEventHandler d = new PathChangedEventHandler(PathChanged);
            path.Changed -= d;
            path.Changed += d;
        }

        private void UnregisterPathChangedListener()
        {
//          Debug.Log ("Unregistering PathChangedEventHandler on '" + path + "': " + this.gameObject.name);
            path.Changed -= new PathChangedEventHandler(PathChanged);
        }

        internal void PathChanged(object sender, EventArgs e)
        {
            //NewPath path = (NewPath)sender;
            Debug.Log("PathChanged: " + sender + ", " + e);
            
            ConfigurationChanged();
            
            
        }
        
        public void ConfigurationChanged()
        {

            if (autoUpdateWithPath)
            {
                PathPointsChanged();
                this.generatedSlices = null;
            }

            if (autoUpdateMesh)
            {
                GenerateTrackMesh();
            }


        }

        public void PathPointsChanged()
        {
            this.pathPoints = null;

        }

        public void SaveTrackGeneratorParameters()
        {
            if (null != this.TrackGeneratorInstance)
            {
                ParameterStore store = GetTrackGeneratorParameterStore();
                TrackGeneratorInstance.SaveParameters(store);
            }
//          trackGenerator.SaveParameters (track.GetTrackGeneratorParameterStore ());
        }

        internal ParameterStore GetTrackGeneratorParameterStore()
        {
            return new ParameterStore(this.parameters, _trackGeneratorInstance.GetType().FullName);
        }

        public void OnBeforeSerialize()
        {
            /*if (null != path) {
            UnregisterPathChangedListener();
        }*/

            parameters.OnBeforeSerialize();
        }

        public void OnAfterDeserialize()
        {
            parameters.OnAfterDeserialize();

            GetPathModifierContainer().LoadPathModifiers(parameters);

            if (null != path)
            {
                RegisterPathChangedListener();
            }

            this.TrackGeneratorChanged();

        }

        // Use this for initialization
        void Start()
        {
    
        }
    
        // Update is called once per frame
        void Update()
        {
    
        }

        void OnDrawGizmos()
        {
            // Draw mesh?
            if (null != generatedMesh)
            {
                Gizmos.color = Color.green;
                //Gizmos.DrawWireMesh(generatedMesh);
            }
        }

        /// <summary>
        /// Generates a Mesh with the current TrackGeneratorInstance and assigns it to 
        /// "generatedMesh" property.
        /// </summary>
        public void GenerateTrackMesh()
        {
        
//          Path path = Path;
            ITrackGenerator tg = TrackGeneratorInstance;
            if (null == tg)
            {
                Debug.LogError("TrackGeneratorInstance is null");
                return;
            }
        
            if (generatedMesh == null)
            {
                Debug.Log("Creating new Mesh instance");
                generatedMesh = new Mesh();
            
            } else
            {
                Debug.Log("Updating existing Mesh instance");
                generatedMesh.Clear();
            }

            // Create initial Mesh name:
            if (StringUtil.IsEmpty(generatedMesh.name))
            {
                generatedMesh.name = gameObject.name + "Mesh";
            }
        
            tg.CreateMesh(this, generatedMesh);
        
            // Upate MeshFilter
            MeshFilter mf = GetComponent<MeshFilter>();
            if (null != mf)
            {
            
                mf.mesh = generatedMesh;
            }
        

        }

        protected void UpdatePathPoints()
        {
            if (null == pathPoints)
            {
                if (null != path)
                {
                    pathPoints = path.GetAllPoints();
                    pathPointFlags = path.GetOutputFlags();
                } else
                {
                    pathPoints = new PathPoint[0];
                    pathPointFlags = PathPoint.NONE;
                }
                // TODO we need to provide the Context with a valid Path reference!
                PathModifierContext context = new PathModifierContext(GetPathModifierContainer(), pathPointFlags);
                pathPoints = PathModifierUtil.RunPathModifiers(context, pathPoints, ref pathPointFlags, true);
            }
        }

        public PathPoint[] GetPathPoints(out int ppFlags)
        {
            UpdatePathPoints();
            ppFlags = pathPointFlags;
            return pathPoints;
        }

        public PathPoint[] GetPathPoints()
        {
            UpdatePathPoints();
            return pathPoints;
        }

        public int GetPathOutputFlags()
        {
            // Just to make sure that caches are up-to-date:
            UpdatePathPoints();
            return pathPointFlags;
        }

        public void OnPathModifiersChanged()
        {
            GetPathModifierContainer().SavePathModifiers(parameters);
            ConfigurationChanged();
        }

        protected virtual DefaultPathModifierContainer CreatePathModifierContainer()
        {
            DefaultPathModifierContainer pmc = new DefaultPathModifierContainer(
                OnPathModifiersChanged,
                ConfigurationChanged,
                (out int ppFlags) => {
                ppFlags = path.GetOutputFlags();
                return path.GetAllPoints();
            },
            PathPointsChanged,
                null,
                this);
                
            

            
            return pmc;
        }
        
        public DefaultPathModifierContainer GetPathModifierContainer()
        {
            if (null == pathModifierContainer)
            {
                pathModifierContainer = CreatePathModifierContainer();
            }
            return pathModifierContainer;
        }


    }
}
