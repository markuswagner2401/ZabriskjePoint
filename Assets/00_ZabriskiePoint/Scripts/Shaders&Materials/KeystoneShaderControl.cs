using UnityEngine;
using System;

public class KeystoneShaderControl : MonoBehaviour
{
    public Material keystoneMaterial;

    private Vector4[] cornerOffsets = new Vector4[4]; // Store the offsets of the corners

    private void Start()
    {
        if (keystoneMaterial == null)
        {
            Debug.LogError("Keystone material is not assigned!");
            return;
        }
        
        // Fetch the main texture from the material.
        Texture mainTex = keystoneMaterial.mainTexture;

        // Check if mainTex is null
        if (mainTex == null)
        {
            Debug.LogError("Main texture of the Keystone Material is null!");
            return;
        }

        // Get texture size
        int width = mainTex.width;
        int height = mainTex.height;
        Debug.Log("Texture Size: " + width + "x" + height);
    }

    // Public methods for UnityEvent calls
    public void SetCorner0Horizontal(string pixels) { UpdateCorner(0, pixels, true); }
    public void SetCorner0Vertical(string pixels) { UpdateCorner(0, pixels, false); }
    public void SetCorner1Horizontal(string pixels) { UpdateCorner(1, pixels, true); }
    public void SetCorner1Vertical(string pixels) { UpdateCorner(1, pixels, false); }
    public void SetCorner2Horizontal(string pixels) { UpdateCorner(2, pixels, true); }
    public void SetCorner2Vertical(string pixels) { UpdateCorner(2, pixels, false); }
    public void SetCorner3Horizontal(string pixels) { UpdateCorner(3, pixels, true); }
    public void SetCorner3Vertical(string pixels) { UpdateCorner(3, pixels, false); }

    private void UpdateCorner(int corner, string pixels, bool isHorizontal)
    {
        // Parsing string to integer
        int pixelOffset;
        if (!int.TryParse(pixels, out pixelOffset))
        {
            Debug.LogError("Invalid pixel input. Please enter a valid integer.");
            return;
        }

        // Normalized float value calculation
        float normalizedOffset = isHorizontal 
            ? ConvertPixelToNormalized(pixelOffset, keystoneMaterial.mainTexture.width)
            : ConvertPixelToNormalized(pixelOffset, keystoneMaterial.mainTexture.height);

        // Update the appropriate component of the offset
        if (isHorizontal)
        {
            cornerOffsets[corner].x = normalizedOffset;
        }
        else
        {
            cornerOffsets[corner].y = normalizedOffset;
        }

        // Update shader
        SetCorner(corner, cornerOffsets[corner]);
    }

    // Method to set corner offsets
    private void SetCorner(int corner, Vector4 offset)
    {
        // Set shader offset based on corner index
        switch(corner)
        {
            case 0: // up left
                keystoneMaterial.SetVector("_TopLeftOffset", offset);
                break;
            case 1: // up right
                keystoneMaterial.SetVector("_TopRightOffset", offset);
                break;
            case 2: // down left
                keystoneMaterial.SetVector("_BottomLeftOffset", offset);
                break;
            case 3: // down right
                keystoneMaterial.SetVector("_BottomRightOffset", offset);
                break;
            default:
                Debug.LogError("Invalid corner index. Accepted values are 0, 1, 2, or 3.");
                break;
        }
    }

    // Method to convert pixel offset to normalized float based on texture size
    private float ConvertPixelToNormalized(int pixel, int dimension)
    {
        // Return normalized value
        return (float)pixel / dimension;
    }
}