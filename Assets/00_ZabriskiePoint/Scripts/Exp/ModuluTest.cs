using UnityEngine;
using System.Collections.Generic;

public class ModuluTest : MonoBehaviour
{
    [SerializeField] int imageWidth = 800;
    [SerializeField] int imageHeight = 200;

    Dictionary<int, int[]> dictRowPixels = new Dictionary<int, int[]>();

    [SerializeField]int resolution = 10; // add pixel of every 10th row to the corresponding array

    void Start()
    {
        CreateDictRowPixels();
    }

    void CreateDictRowPixels()
    {
        List<int> rows = new List<int>();

        // Find the rows
        for (int i = 0; i < imageHeight; i++)
        {
            if (i % resolution == 0)
            {
                rows.Add(i);
            }
        }

        // For each row, find the pixels and add to dictionary
        for (int i = 0; i < rows.Count; i++)
        {
            int rowNum = rows[i];
            int startPixel = rowNum * imageWidth;
            int endPixel = startPixel + imageWidth;

            List<int> pixels = new List<int>();

            for (int j = startPixel; j < endPixel; j++)
            {
                pixels.Add(j);
            }

            int[] pixelsArray = pixels.ToArray();

            dictRowPixels.Add(rowNum, pixelsArray);
        }

        PrintDictionary();
    }

    void PrintDictionary()
    {
        foreach (KeyValuePair<int, int[]> entry in dictRowPixels)
        {
            // Print the row number
            Debug.Log("Row: " + entry.Key);

            // Print the pixel array for this row
            string pixels = string.Join(", ", entry.Value);
            Debug.Log("Pixels: " + pixels);
        }
    }
}
