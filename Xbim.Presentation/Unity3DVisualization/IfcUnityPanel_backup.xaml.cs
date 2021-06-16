//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using Xbim.Common;
//using Xbim.Common.Geometry;
//using Xbim.Ifc;
//using Xbim.Presentation.LayerStyling;

//namespace Xbim.Presentation.Unity3DVisualization
//{
//    /// <summary>
//    /// Panel to visualize IFC in Unity
//    /// </summary>
//    public partial class IfcUnityPanel_backup : UnityIntegrationToolbox.UnityIntegrationPanel
//    {


//        /// <summary>
//        /// IfcStore with the model to visualize
//        /// </summary>
//        public IfcStore Model
//        {
//            get { return (IfcStore)GetValue(ModelProperty); }
//            set { SetValue(ModelProperty, value); }
//        }

//        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
//        public static readonly DependencyProperty ModelProperty =
//            DependencyProperty.Register("Model", typeof(IfcStore), typeof(IfcUnityPanel_backup),
//                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
//                    OnModelChanged));

//        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            IfcUnityPanel_backup unityPanel = d as IfcUnityPanel_backup;
//            unityPanel.ReloadModel();
//        }

//        /// <summary>
//        /// Reloads the entire model
//        /// </summary>
//        public void ReloadModel()
//        {
//            this.ClearModelView();
//            this.LoadModelView();
//        }

//        /// <summary>
//        /// Load the current model
//        /// </summary>
//        public void LoadModelView()
//        {
//            using (var geometryStore = this.Model.GeometryStore)
//            {
//                using (IGeometryStoreReader geomReader = geometryStore.BeginRead())
//                {
//                    HashSet<short> excludedTypes = this.Model.DefaultExclusions(null);
//                    IEnumerable<XbimShapeInstance> shapeInstances = GetShapeInstancesToRender(geomReader, excludedTypes);
//                    foreach (XbimShapeInstance shape in shapeInstances)
//                    {
//                        IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shape.ShapeGeometryLabel);
//                        DamageModelingTools.UnityCommunication.Messaging.GeometricEntityData objectData = new DamageModelingTools.UnityCommunication.Messaging.GeometricEntityData()
//                        {
//                            ObjectID = shape.IfcProductLabel.ToString()
//                        };
//                        objectData.Read(shapeGeom.ShapeData);
//                        base.UnityHostElement.SendMessageToUnity(DamageModelingTools.UnityCommunication.Messaging.MessageType.ADD, objectData);
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Send the message to clear the model
//        /// </summary>
//        public void ClearModelView()
//        {
//            base.UnityHostElement.SendMessageToUnity(DamageModelingTools.UnityCommunication.Messaging.MessageType.CLEAR, null);
//        }

//        protected IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
//        {
//            var shapeInstances = geomReader.ShapeInstances
//                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
//                            &&
//                            !excludedTypes.Contains(s.IfcTypeId));
//            return shapeInstances;
//        }
//    }
//}
