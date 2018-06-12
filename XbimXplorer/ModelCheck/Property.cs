using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Newtonsoft.Json;
using System.IO;

using Xbim.ModelGeometry.Scene;
using Xbim.Common.Geometry;
using System.Collections.ObjectModel;

namespace XbimXplorer.ModelCheck
{
    public class PropertyItem
    {
        public String Name { get; set; }
        public String Value { get; set; }
        public int IfcLabel { get; set; }
        public String PropertySetName { get; set; }
    }
    /// <summary>
    /// 一个实体的信息，其中有三个基本信息：GUID,TYPE(所属于的IFC类)，LABEL(EXPRESS文件中的label号)
    /// 其他信息：包括非引用的EXPRESS属性信息，propertyset中的属性信息统一放在properties中
    /// </summary>
    public class Entity
    {
        //对于一个entity，只保存最最基础的guid,label,type；剩下的全部放在属性里
        public String GUID { get; set; }
        public String TYPE { get; set; }
        public int LABEL { get; set; }

        private SortedDictionary<string, string> _properties = new SortedDictionary<string, string>();
        public SortedDictionary<string, string> properties { get { return _properties; } }
        public String AABB { get; set; }
    }

    public class IFCFile
    {
        public List<Entity> Elements { get; set; }
        public SortedDictionary<string, SortedDictionary<int, List<int>>> Rels { get; set; }
    }


    public class PropertyExtract
    {

        IfcStore model;
        Xbim3DModelContext context;


        public PropertyExtract(IfcStore _model, Xbim3DModelContext _context)
        {
            model = _model;
            context = _context;
        }

        /// <summary>
        /// 获取一个ifcproduct的boundingbox
        /// </summary>
        /// <param name="product">指定的product</param>
        /// <returns>XbimRect3D精度为2位的盒子</returns>
        public XbimRect3D GetAABB(IIfcProduct product)
        {
            //Xbim3DModelContext context = new Xbim3DModelContext(model);
            //context.CreateContext();
            XbimRect3D prodBox = XbimRect3D.Empty;

            if (context.ShapeInstancesOf(product).Count() == 0)
                return prodBox;

            foreach (var shp in context.ShapeInstancesOf(product))
            {
                var bb = shp.BoundingBox;
                bb = XbimRect3D.TransformBy(bb, shp.Transformation);
                if (prodBox.IsEmpty) prodBox = bb;
                else prodBox.Union(bb);
            }

            //精度为2位小数
            prodBox.Round(2);
            return prodBox;
            //Console.WriteLine(prodBox.ToString());

        }

        /// <summary>
        /// 获取整个文件的属性信息
        /// </summary>
        /// <returns>IFCFile</returns>
        public IFCFile getAllIfcProperty()
        {
            var products = model.Instances.OfType<IIfcProduct>();
            IFCFile ifc = new IFCFile();
            List<Entity> productList = new List<Entity>();
            foreach (var product in products)
            {
                productList.Add(getBasicInfo(product));
            }
            ifc.Elements = productList;

            SortedDictionary<string, SortedDictionary<int, List<int>>> rels = new SortedDictionary<string, SortedDictionary<int, List<int>>>();
            SortedDictionary<int, List<int>> rels_contain = GetRelContains();
            rels.Add("isContaining", rels_contain);
            ifc.Rels = rels;
            return ifc;
        }

        public IFCFile getFilterIfcProperty(HashSet<string> filterSet)
        {
            IEnumerable<IPersistEntity> products = new List<IPersistEntity>();
            foreach(var filterIFC in filterSet)
            {
                products = products.Union(model.Instances.OfType(filterIFC, false));
            }
            
            IFCFile ifc = new IFCFile();
            List<Entity> productList = new List<Entity>();
            foreach (var product in products)
            {
                productList.Add(getBasicInfo(product));
            }
            ifc.Elements = productList;

            SortedDictionary<string, SortedDictionary<int, List<int>>> rels = new SortedDictionary<string, SortedDictionary<int, List<int>>>();
            SortedDictionary<int, List<int>> rels_contain = GetRelContains();
            rels.Add("isContaining", rels_contain);
            ifc.Rels = rels;
            return ifc;
        }

        public SortedDictionary<int, List<int>> GetRelContains()
        {
            SortedDictionary<int, List<int>> containing = new SortedDictionary<int, List<int>>();

            var containEntities = model.Instances.OfType<IIfcRelContainedInSpatialStructure>();
            foreach (var entity in containEntities)
            {
                ObservableCollection<PropertyItem> properties = new ObservableCollection<PropertyItem>();
                FillObjectData(entity, properties);
                int relating = 0;
                List<int> related = new List<int>();
                foreach (var prop in properties)
                {

                    //属性对应的IFCLABEL=0，代表这个属性是EXPRESS中的一个直接用text或者number就可以表示的一种属性
                    if (prop.IfcLabel != 0)
                    {
                        if (prop.Name == "RelatingStructure")
                        {
                            relating = prop.IfcLabel;
                        }
                        //注意，这里是观察得到的，name和value都是被包含的实体的IFCTYPE
                        if (prop.Name == prop.Value || prop.Name == "RelatedElements (∞)")
                        {
                            related.Add(prop.IfcLabel);
                        }

                    }

                }
                containing.Add(relating, related);
            }

            var aggregateEntities = model.Instances.OfType<IIfcRelAggregates>();
            foreach (var entity in aggregateEntities)
            {
                ObservableCollection<PropertyItem> properties = new ObservableCollection<PropertyItem>();
                FillObjectData(entity, properties);
                int relating = 0;
                List<int> related = new List<int>();
                foreach (var prop in properties)
                {

                    //属性对应的IFCLABEL=0，代表这个属性是EXPRESS中的一个直接用text或者number就可以表示的一种属性
                    if (prop.IfcLabel != 0)
                    {
                        if (prop.Name == "RelatingObject")
                        {
                            relating = prop.IfcLabel;
                        }
                        //注意，这里是观察得到的，name和value都是被包含的实体的IFCTYPE
                        if (prop.Name == prop.Value || prop.Name == "RelatedObjects (∞)")
                        {
                            related.Add(prop.IfcLabel);
                        }

                    }

                }
                if (containing.ContainsKey(relating))
                {
                    containing[relating] = containing[relating].Union(related).ToList();
                }
                else
                {
                    containing.Add(relating, related);
                }
            }

            //Console.WriteLine(containing);

            return containing;
        }

       
        
        /// <summary>
        /// 获取一个指定实体的信息，包括express属性信息和definedbyProperties信息
        /// 其中，express属性只获取直接有值的，不获取引用的
        /// 另外，express属性也获取了几何表达形式IfcProductDefinitionShape
        /// </summary>
        /// <param name="entity">实体，IfcProduct的子类</param>
        /// <returns></returns>
        public Entity getBasicInfo(IPersistEntity entity)
        {
            ObservableCollection<PropertyItem> properties = new ObservableCollection<PropertyItem>();
            FillObjectData(entity, properties);
            Entity newEntity = new Entity();
            newEntity.LABEL = entity.EntityLabel;
            foreach (var prop in properties)
            {
                if (prop.Name == "GlobalId") newEntity.GUID = prop.Value;
                else if (prop.Name == "Type") newEntity.TYPE = prop.Value;
                else
                {
                    //属性对应的IFCLABEL=0，代表这个属性是EXPRESS中的一个直接用text或者number就可以表示的一种属性
                    //并不是一个引用
                    if (prop.IfcLabel == 0)
                    {
                        newEntity.properties.Add(prop.Name, prop.Value);
                    }
                    //专门处理几何信息
                    if (prop.Value == "IfcProductDefinitionShape")
                    {
                        var value = string.Join(",", getRepresentation(prop.IfcLabel));
                        newEntity.properties.Add(prop.Name, value);
                    }
                }


                //Console.WriteLine(prop.Name + " " + prop.Value+ " "+ prop.IfcLabel);
            }

            ObservableCollection<PropertyItem> properties2 = new ObservableCollection<PropertyItem>();
            FillPropertyData(entity, properties2);
            foreach (var prop in properties2)
            {
                if (!newEntity.properties.ContainsKey(prop.Name))
                    newEntity.properties.Add(prop.Name, prop.Value);
                //Console.WriteLine(prop.Name + " " + prop.Value + " " + prop.IfcLabel);
            }

            var toproduct = entity as IIfcProduct;
            XbimRect3D AABB = GetAABB(toproduct);
            if (AABB.IsEmpty) newEntity.AABB = "";
            else newEntity.AABB = AABB.ToString();

            return newEntity;

        }

        //输入模型和ifcproductdefinitionshape的label，返回几何表达方法
        private HashSet<string> getRepresentation(int label)
        {

            HashSet<string> reps = new HashSet<string>();
            IIfcProductDefinitionShape shape = (IIfcProductDefinitionShape)model.Instances[label];
            var representations = shape.Representations;
            foreach (var item in representations)
            {
                reps.Add(item.RepresentationType);
                //Console.WriteLine("look!"+item.RepresentationType);
            }
            return reps;
        }

        //FillOjbectData,GetPropItem, ReportProperty主要处理关于EXPRESSTYPE的属性
        private void FillObjectData(IPersistEntity _entity, ObservableCollection<PropertyItem> _objectProperties)
        {
            if (_objectProperties.Count > 0)
                return; //don't fill unless empty
            if (_entity == null)
                return;

            //_objectProperties.Add(new PropertyItem { Name = "Label", Value = "#" + _entity.EntityLabel, PropertySetName = "General" });

            var ifcType = _entity.ExpressType;
            _objectProperties.Add(new PropertyItem { Name = "Type", Value = ifcType.Type.Name, PropertySetName = "General" });

            var ifcObj = _entity as IIfcObject;


            var props = ifcType.Properties.Values;
            foreach (var prop in props)
            {
                ReportProp(_entity, prop, false, _objectProperties);
            }


        }

        private PropertyItem GetPropItem(object propVal)
        {
            var retItem = new PropertyItem();

            var pe = propVal as IPersistEntity;
            var propLabel = 0;
            if (pe != null)
            {
                propLabel = pe.EntityLabel;
            }
            var ret = propVal.ToString();
            if (ret == propVal.GetType().FullName)
            {
                ret = propVal.GetType().Name;
            }

            retItem.Value = ret;
            retItem.IfcLabel = propLabel;

            return retItem;
        }

        private void ReportProp(IPersistEntity entity, ExpressMetaProperty prop, bool verbose, ObservableCollection<PropertyItem> _objectProperties)
        {
            var propVal = prop.PropertyInfo.GetValue(entity, null);
            if (propVal == null)
            {
                if (!verbose)
                    return;
                propVal = "<null>";
            }

            if (prop.EntityAttribute.IsEnumerable)
            {
                var propCollection = propVal as IEnumerable<object>;

                if (propCollection != null)
                {
                    var propVals = propCollection.ToArray();

                    switch (propVals.Length)
                    {
                        case 0:
                            if (!verbose)
                                return;
                            _objectProperties.Add(new PropertyItem { Name = prop.PropertyInfo.Name, Value = "<empty>", PropertySetName = "General" });
                            break;
                        case 1:
                            var tmpSingle = GetPropItem(propVals[0]);
                            tmpSingle.Name = prop.PropertyInfo.Name + " (∞)";
                            tmpSingle.PropertySetName = "General";
                            _objectProperties.Add(tmpSingle);
                            break;
                        default:
                            foreach (var item in propVals)
                            {
                                var tmpLoop = GetPropItem(item);
                                tmpLoop.Name = item.GetType().Name;
                                tmpLoop.PropertySetName = prop.PropertyInfo.Name;
                                _objectProperties.Add(tmpLoop);
                            }
                            break;
                    }
                }
                else
                {
                    if (!verbose)
                        return;
                    _objectProperties.Add(new PropertyItem { Name = prop.PropertyInfo.Name, Value = "<not an enumerable>" });
                }
            }
            else
            {
                var tmp = GetPropItem(propVal);
                tmp.Name = prop.PropertyInfo.Name;
                tmp.PropertySetName = "General";
                _objectProperties.Add(tmp);
            }
        }

        //FillPropertyData, AddPropertySet, AddProperty主要处理关于propertyset的属性
        private void FillPropertyData(IPersistEntity _entity, ObservableCollection<PropertyItem> _properties)
        {
            if (_properties.Any()) //don't try to fill unless empty
                return;
            //now the property sets for any 

            if (_entity is IIfcObject)
            {
                var asIfcObject = (IIfcObject)_entity;
                foreach (
                    var pSet in
                        asIfcObject.IsDefinedBy.Select(
                            relDef => relDef.RelatingPropertyDefinition as IIfcPropertySet)
                    )
                    AddPropertySet(pSet, _properties);
            }
            else if (_entity is IIfcTypeObject)
            {
                var asIfcTypeObject = _entity as IIfcTypeObject;
                if (asIfcTypeObject.HasPropertySets == null)
                    return;
                foreach (var pSet in asIfcTypeObject.HasPropertySets.OfType<IIfcPropertySet>())
                {
                    AddPropertySet(pSet, _properties);
                }
            }
        }

        private void AddPropertySet(IIfcPropertySet pSet, ObservableCollection<PropertyItem> _properties)
        {
            if (pSet == null)
                return;
            foreach (var item in pSet.HasProperties.OfType<IIfcPropertySingleValue>()) //handle IfcPropertySingleValue
            {
                AddProperty(item, pSet.Name, _properties);
            }
            foreach (var item in pSet.HasProperties.OfType<IIfcComplexProperty>()) // handle IfcComplexProperty
            {
                // by invoking the undrlying addproperty function with a longer path
                foreach (var composingProperty in item.HasProperties.OfType<IIfcPropertySingleValue>())
                {
                    AddProperty(composingProperty, pSet.Name + " / " + item.Name, _properties);
                }
            }
        }

        private void AddProperty(IIfcPropertySingleValue item, string groupName, ObservableCollection<PropertyItem> _properties)
        {
            var val = "";
            var nomVal = item.NominalValue;
            if (nomVal != null)
                val = nomVal.ToString();
            _properties.Add(new PropertyItem
            {
                IfcLabel = item.EntityLabel,
                PropertySetName = groupName,
                Name = item.Name,
                Value = val
            });
        }




    }
}
