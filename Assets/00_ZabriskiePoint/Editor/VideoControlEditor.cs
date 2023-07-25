using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VideoControl))]
public class VideoControlEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        VideoControl videoControl = (VideoControl)target;

        if(GUILayout.Button("Integrate"))
        {
            videoControl.Integrate("");
        }

        if(GUILayout.Button("Desintegrate"))
        {
            videoControl.Desintegrate();
        }
    }
}