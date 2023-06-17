using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistionaryTest2 : MonoBehaviour
{
    [SerializeField] int imageWidth = 800;
    [SerializeField] int imageHeight = 200;

    [SerializeField] int heightSegments = 4;
    [SerializeField] int widthSegments = 4;

    Dictionary<int, int> lookupPixelRow = new Dictionary<int, int>();

    Dictionary<int, int> lookupPixelSegment = new Dictionary<int, int>();

    private void Start()
    {
        CreatePixelRowLookup();
    }

    private void CreatePixelRowLookup()
    {
        int totalPixels = imageHeight * imageWidth;
        for (int i = 0; i < totalPixels; i++)
        {
            lookupPixelRow.Add(i, (int)Mathf.Floor(i / imageWidth));
        }
    }

    private void CreatePixelSegmentLookup()
    {

        int segmentHeight = imageHeight / heightSegments; // height of each segment
        int segmentWidth = imageWidth / widthSegments; // width of each segment

        // Check if the image height and width are evenly divisible by the number of segments
        if (imageHeight % heightSegments != 0 || imageWidth % widthSegments != 0)
        {
            Debug.LogError("The image dimensions are not evenly divisible by the number of segments. This can cause Problems");
        }



        

        int totalPixels = imageHeight * imageWidth;
        for (int i = 0; i < totalPixels; i++)
        {
            int row = i / imageWidth; // calculate row of the pixel
            int col = i % imageWidth; // calculate column of the pixel

            int segmentRow = row / segmentHeight; // calculate segment row
            int segmentCol = col / segmentWidth; // calculate segment column

            // calculate unique segment number
            int segmentNumber = segmentRow * widthSegments + segmentCol;

            lookupPixelSegment.Add(i, segmentNumber);
        }
    }
}
