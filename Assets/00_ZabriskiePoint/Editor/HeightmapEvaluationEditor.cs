
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HeightmapEvaluation))]
public class HeightmapEvaluationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        base.OnInspectorGUI();
        HeightmapEvaluation heightmapEvaluation = (HeightmapEvaluation)target;
        if(GUILayout.Button("Debug Reset"))
        {
            heightmapEvaluation.Reset();
        }
        
    }
}
