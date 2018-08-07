using System.Reflection;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.XplorerPluginSystem
{
    public interface IXbimXplorerPluginMasterWindow
    {
        DrawingControl3D DrawingControl { get; }
        //这里模仿drawing control，目的是让主窗口可以get到properties数据
        IfcMetaDataControl PropertiesControl { get; } 
        IPersistEntity SelectedItem { get; set; }
        IfcStore Model { get; }
        void BroadCastMessage(object sender, string messageTypeString, object messageData);
        void RefreshPlugins();
        bool Activate();
        bool Focus();

        Xbim3DModelContext GetContext();
        string GetOpenedModelFileName();
        void ReportCheckProgress(string text);
        void SetProgressBar(bool startMove);
        string GetAssemblyLocation(Assembly requestingAssembly);

    }
}