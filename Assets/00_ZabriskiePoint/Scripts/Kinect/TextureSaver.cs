using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

public class TextureSaver: MonoBehaviour
{
    
    public static void SaveRenderTextureToDisk(RenderTexture renderTexture, string filename)
    {
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        
        // Capture the source texture
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        
        // Convert texture to .png format
        byte[] pngData = texture2D.EncodeToPNG();

        #if UNITY_EDITOR
        // Save to disk
        string filePath = Path.Combine(Application.dataPath, filename + ".png");
        File.WriteAllBytes(filePath, pngData);

        // Refresh the Assets database of the editor, to update it with the new file
        AssetDatabase.Refresh();
        #endif

        // Cleaning up memory
        Object.Destroy(texture2D);
        RenderTexture.active = null;
    }

    public IEnumerator WaitAndSaveRenderTextureToDisk(RenderTexture renderTexture, string filename, float timer)
    {
        // Wait for the specified time
        yield return new WaitForSeconds(timer);

        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        
        // Capture the source texture
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        
        // Convert texture to .png format
        byte[] pngData = texture2D.EncodeToPNG();

        #if UNITY_EDITOR
        // Save to disk
        string filePath = Path.Combine(Application.dataPath, filename + ".png");
        File.WriteAllBytes(filePath, pngData);

        // Refresh the Assets database of the editor, to update it with the new file
        AssetDatabase.Refresh();
        #endif
        
        // Cleaning up memory
        Object.Destroy(texture2D);
        RenderTexture.active = null;
    }
}


