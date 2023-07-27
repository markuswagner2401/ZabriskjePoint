using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorCamDepthTextureProvider))]
public class DepthCheckEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ColorCamDepthTextureProvider colorCamDepthTextureProvider = (ColorCamDepthTextureProvider)target;
        if(GUILayout.Button("Debug Changing Start"))
        {
            colorCamDepthTextureProvider.DebugOnDepthChaningStart();
        }

        if(GUILayout.Button("Debug Changing End"))
        {
            colorCamDepthTextureProvider.DebugOnDephtsChangingEnd();
        }
    }
}
