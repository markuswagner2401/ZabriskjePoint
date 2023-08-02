using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightmapAnalysis : MonoBehaviour
{


    // compute Shaders
    public ComputeShader hillAndTroughShader;

    [SerializeField] int maxClusters = 10;

    RenderTexture inputRenderTexture;

    RenderTexture colorMap;

    // Reference to texture

    [SerializeField] Renderer rend;
    [SerializeField] String heightmapRef = "_Texture";
    [SerializeField] Material material;

    Texture heightmap;

    //public CustomRenderTexture heightmap;

    //public Texture heightmapDebug;
    public float clusterThreshold = 3f;

    public float neighborThreshold = 20f;

    public int blurInterations = 20;

    public int textureSteps = 20;

    public int border = 10;

    public bool detectHills = true;

    public bool detectTroughs = true;




    // Debugging Rendertextures
    public RenderTexture debugSource;
    public RenderTexture debugResult;


    // Indicator spawning

    public bool spawnDebugIndicators = true;

    public GameObject hillIndicator;
    public GameObject hillChainIndicator;
    public GameObject troughIndicator;
    public GameObject troughChainIndicator;
    public Transform uVOrigin;
    public GameObject meshObject;
    public float heightFactor = 5f;

    //




    public struct StructuredCluster
    {
        public List<Vector3> hills;
        public Vector2 centerPosition;
    }

    public struct NeighborCluster
    {
        public List<Vector2> centerPositions;
        public Vector3 heighestPosition;
    }

    private List<List<Vector3>> clusteredHills;
    private List<List<Vector3>> clusteredTroughs;
    StructuredCluster[] structuredHills;
    StructuredCluster[] structuredTroughs;
    List<NeighborCluster> neighborHills = new List<NeighborCluster>();
    List<NeighborCluster> neighborTroughs = new List<NeighborCluster>();


    ///

    private float meshWidth;
    private float meshDepth;


    private ComputeBuffer hillsBuffer;
    private ComputeBuffer troughsBuffer;
    private int threadGroupsX;
    private int threadGroupsY;
    private int threadGroupsZ;

    private List<GameObject> spawnedHillIndicators = new List<GameObject>();
    private List<GameObject> spawnedTroughIndicators = new List<GameObject>();
    private List<GameObject> spawnedHillNeighborsIndicators = new List<GameObject>();
    private List<GameObject> spawnedTroughNeighborsIndicators = new List<GameObject>();



    private void Start()
    {
        Mesh mesh = meshObject.GetComponent<MeshFilter>().mesh;
        Vector3 scale = meshObject.transform.localScale;

        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }

        if (material == null && rend != null)
        {
            material = rend.material;
        }




        meshWidth = mesh.bounds.size.x * scale.x;
        meshDepth = mesh.bounds.size.z * scale.z;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            AnalyseHeightmap();
        }

    }

    public int GetMainHillsCount()
    {
        return neighborHills.Count;
    }

    public Vector3[] GetMainHillsPositions()
    {
        Vector3[] positions = new Vector3[neighborHills.Count];
        for (int i = 0; i < neighborHills.Count; i++)
        {
            Vector3 center = new Vector3();
            center = TransposePosition(neighborHills[i].heighestPosition.x, neighborHills[i].heighestPosition.z, heightmap.width, heightmap.height, meshWidth, meshDepth, uVOrigin, neighborHills[i].heighestPosition.y);
            positions[i] = center;
        }
        return positions;
    }

    public Vector2[] GetMainHillsTexturePositions()
    {
        Vector2[] positions = new Vector2[neighborHills.Count];

        for (int i = 0; i < neighborHills.Count; i++)
        {
            Vector2 position = new Vector2();
            position.x = neighborHills[i].heighestPosition.x;
            position.y = neighborHills[i].heighestPosition.z;
            positions[i] = position;
        }

        return positions;
    }

    public Vector3[] GetMainHillsTexturePositionsAndHeights()
    {
        Vector3[] positions = new Vector3[neighborHills.Count];

        for (int i = 0; i < neighborHills.Count; i++)
        {
            positions[i] = neighborHills[i].heighestPosition;
        }

        return positions;
    }



    public Vector2[] GetMainTroughsTexturePositions()
    {
        Vector2[] positions = new Vector2[neighborTroughs.Count];

        for (int i = 0; i < neighborTroughs.Count; i++)
        {
            Vector2 position = new Vector2();
            position.x = neighborTroughs[i].heighestPosition.x;
            position.y = neighborTroughs[i].heighestPosition.z;
            positions[i] = position;
        }

        return positions;

    }

    public Vector3[] GetMainTroughsTexturePositionsAndHeights()
    {
        Vector3[] positions = new Vector3[neighborTroughs.Count];

        for (int i = 0; i < neighborTroughs.Count; i++)
        {
            positions[i] = neighborTroughs[i].heighestPosition;
        }

        return positions;
    }

    public int GetMainTroughsCount()
    {
        return neighborTroughs.Count;
    }

    public Vector3[] GetMainTroughsPositions()
    {
        Vector3[] positions = new Vector3[neighborTroughs.Count];
        for (int i = 0; i < neighborTroughs.Count; i++)
        {
            Vector3 center = new Vector3();
            center = TransposePosition(neighborTroughs[i].heighestPosition.x, neighborTroughs[i].heighestPosition.z, heightmap.width, heightmap.height, meshWidth, meshDepth, uVOrigin, neighborTroughs[i].heighestPosition.y);
            positions[i] = center;
        }
        return positions;
    }

    public float GetHeightOfHeighestHill()
    {
        float height = 0;
        foreach (var item in neighborHills)
        {
            height = Mathf.Max(height, item.heighestPosition.y);
        }

        return height;
    }

    public float GetHeightOfLowestTrough()
    {
        float height = 0;
        foreach (var item in neighborTroughs)
        {
            height = Mathf.Max(height, item.heighestPosition.y);
        }

        return 1 - height;
    }



    public void AnalyseHeightmap()
    {
        heightmap = material?.GetTexture(heightmapRef);

        if (heightmap == null)
        {
            Debug.LogError("no heightmap found on material of renderer on " + gameObject.name + ". HeightmapAnalysis not working");
            return;
        }

        inputRenderTexture = new RenderTexture(heightmap.width, heightmap.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            enableRandomWrite = true
        };

        inputRenderTexture.Create();

        Graphics.Blit(heightmap, inputRenderTexture);
        FindHillsAndTroughs(inputRenderTexture);
        //StartCoroutine(BlurAndFindHillsAndTroughs(inputRenderTexture));
        //StartCoroutine(TextureStepssAndFindHillsAndTroughs(inputRenderTexture));
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    // Finding hills and troughs
    IEnumerator TextureStepssAndFindHillsAndTroughs(RenderTexture inputRenderTexture)
    {
        // RenderTexture steppedTexture = new RenderTexture(crt.width, crt.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        // {
        //     enableRandomWrite = true
        // };

        // steppedTexture.Create();

        // Graphics.Blit(crt, steppedTexture);

        TextureSteps textureSteps = GetComponent<TextureSteps>();

        Graphics.Blit(textureSteps.TextureToSteps(inputRenderTexture, this.textureSteps), inputRenderTexture);

        StartCoroutine(BlurAndFindHillsAndTroughs(inputRenderTexture));

        //FindHillsAndTroughs(steppedTexture);


        yield break;
    }

    IEnumerator BlurAndFindHillsAndTroughs(RenderTexture inputRenderTexture)
    {
        // RenderTexture blurredTexture = new RenderTexture(crt.width, crt.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        // {
        //     enableRandomWrite = true
        // };

        // blurredTexture.Create();
        // Graphics.Blit(crt, blurredTexture);

        int counter = 0;

        BlurTexture blurTexture = GetComponent<BlurTexture>();

        while (counter < blurInterations)
        {
            counter += 1;
            Graphics.Blit(blurTexture.Blur(inputRenderTexture), inputRenderTexture);
            yield return null;

        }

        //StartCoroutine(FindHillsAndTroughsCoroutine(blurredTexture));
        FindHillsAndTroughs(inputRenderTexture);

        // todo release blurred Texture

        yield break;
    }

    void FindHillsAndTroughs(RenderTexture inputRenderTexture)
    {
        // Create a writable heightmap from the input texture
        // RenderTexture writableHeightmap = CreateWritableHeightmap(inputRenderTexture);

        colorMap = new RenderTexture(inputRenderTexture.width, inputRenderTexture.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        {
            enableRandomWrite = true
        };

        colorMap.Create();


        // Perform Gaussian Blur on the writableHeightmap
        // GaussianBlur(writableHeightmap);

        // Debug: Check the result of the Gaussian Blur

        if (debugSource.width != inputRenderTexture.width)
        {
            Debug.LogError("Resolution mismatch of debugSource Rendertexture and heighmap");
        }
        else
        {
            Graphics.Blit(inputRenderTexture, debugSource);
        }


        // Calculate the number of threads
        threadGroupsX = Mathf.CeilToInt(inputRenderTexture.width / 8f);
        threadGroupsY = Mathf.CeilToInt(inputRenderTexture.height / 8f);
        threadGroupsZ = 1;

        int totalPixels = inputRenderTexture.width * inputRenderTexture.height;

        // Create buffers to store the results
        hillsBuffer = new ComputeBuffer(totalPixels, sizeof(float) * 3);
        troughsBuffer = new ComputeBuffer(totalPixels, sizeof(float) * 3);

        // Set the parameters for the hill and trough compute shader
        hillAndTroughShader.SetTexture(0, "Heightmap", inputRenderTexture);
        hillAndTroughShader.SetBuffer(0, "HillsBuffer", hillsBuffer);
        hillAndTroughShader.SetBuffer(0, "TroughsBuffer", troughsBuffer);
        hillAndTroughShader.SetInt("HeightmapWidth", inputRenderTexture.width);
        hillAndTroughShader.SetInt("HeightmapHeight", inputRenderTexture.height);
        hillAndTroughShader.SetTexture(0, "ColorMap", colorMap);

        // Run the compute shader
        hillAndTroughShader.Dispatch(0, threadGroupsX, threadGroupsY, threadGroupsZ);

        // Create arrays to receive the results
        Vector3[] hills = new Vector3[totalPixels];
        Vector3[] troughs = new Vector3[totalPixels];

        // Copy the results from the compute buffers to the arrays
        hillsBuffer.GetData(hills);
        troughsBuffer.GetData(troughs);

        //RenderTexture result = new RenderTexture(writableHeightmap.width, writableHeightmap.height, 0);

        if (debugResult.width != colorMap.width)
        {
            Debug.LogError("Resolution mismatch of heightmap analysis colormap and debugResult Rendertexture");
        }

        else
        {
            Graphics.Blit(colorMap, debugResult);
        }


        // Filter Hills and Throughs (Filter out unused array elements)

        List<Vector3> filteredHills = new List<Vector3>();
        List<Vector3> filteredTroughs = new List<Vector3>();

        // Debug.Log("Hills to filter: " + hills.Length);
        // Debug.Log("Check Buffer: " + writableHeightmap.width * writableHeightmap.height);

        if (detectHills)
        {
            for (int i = 0; i < hills.Length; i++)
            {

                if (hills[i].z > 0.1f) // filter out hills that are low
                {
                    filteredHills.Add(hills[i]);
                }
            }

            Debug.Log($"Hills found: {filteredHills.Count}");

        }

        if (detectTroughs)
        {
            for (int i = 0; i < troughs.Length; i++)
            {
                if (troughs[i].z > 0.1f) // filter out troughs that are high
                {
                    filteredTroughs.Add(troughs[i]);
                }

            }

            Debug.Log($"Troughs found: {filteredTroughs.Count}");

        }


        // return;

        ////Clustering

        if (detectHills)
        {
            clusteredHills = ClusteredList(filteredHills);
            clusteredHills = RemoveBorderClusters(clusteredHills, border);
            Debug.Log("Clustered Hills: " + clusteredHills.Count);
            structuredHills = ConvertToStructuredHillArray(clusteredHills);

            neighborHills = ClusterNeighbous(structuredHills, neighborThreshold);

            Debug.Log("Hill Chains: " + neighborHills.Count);
        }


        if (detectTroughs)
        {
            clusteredTroughs = ClusteredList(filteredTroughs);
            clusteredTroughs = RemoveBorderClusters(clusteredTroughs, border);
            Debug.Log("Clustered Troughs: " + clusteredTroughs.Count);
            structuredTroughs = ConvertToStructuredHillArray(clusteredTroughs);
            neighborTroughs = ClusterNeighbous(structuredTroughs, neighborThreshold);

            Debug.Log("Trough Chains: " + neighborTroughs.Count);
        }


        /// Spawning

        SpawnIndicators();



        // Cleanup
        Cleanup();


    }

    void Cleanup()
    {
        hillsBuffer?.Release();
        troughsBuffer?.Release();
        inputRenderTexture?.Release();
        colorMap?.Release();
    }

    // clustering

    List<List<Vector3>> ClusteredList(List<Vector3> filteredBuffer)
    {
        List<List<Vector3>> clusteredElements = new List<List<Vector3>>();

        foreach (Vector3 hill in filteredBuffer)
        {
            Vector2 hillPoint = new Vector2(hill.x, hill.y);
            bool foundCluster = false;

            for (int i = 0; i < clusteredElements.Count; i++) // clusters
            {
                //if(Mathf.Abs(clusteredElements[i][0].z - hill.z ) > 0.001f) continue;
                if (!Mathf.Approximately(clusteredElements[i][0].z, hill.z)) continue; // continue to next cluster if colorvalue is differnt

                for (int j = 0; j < clusteredElements[i].Count; j++)
                {
                    Vector2 point = new Vector2(clusteredElements[i][j].x, clusteredElements[i][j].y);

                    if (Vector2.Distance(hillPoint, point) <= clusterThreshold)
                    {
                        clusteredElements[i].Add(hill);
                        foundCluster = true;
                        break;
                    }
                }

                if (foundCluster) // If a cluster is found, break the outer loop too
                {
                    break;
                }
            }

            if (!foundCluster)
            {
                List<Vector3> newCluster = new List<Vector3> { hill };
                clusteredElements.Add(newCluster);
            }

            if (clusteredElements.Count > maxClusters)
            {
                Debug.Log("Max clusters reached, breaking Brute force Clustering");
                break;
            }
        }

        return clusteredElements;
    }

    private List<List<Vector3>> RemoveBorderClusters(List<List<Vector3>> clusters, int border)
    {
        List<List<Vector3>> centralClusters = new List<List<Vector3>>();
        foreach (var cluster in clusters)
        {
            bool containsBorderPixel = false;

            foreach (var element in cluster)
            {
                if (element.x <= border || element.x >= heightmap.width - border || element.y <= border || element.y >= heightmap.height - border)
                {
                    containsBorderPixel = true;
                    break;
                }
            }
            if (!containsBorderPixel)
            {
                centralClusters.Add(cluster);
            }

        }

        return centralClusters;
    }

    public StructuredCluster[] ConvertToStructuredHillArray(List<List<Vector3>> clusteredHills)
    {
        StructuredCluster[] structuredHills = new StructuredCluster[clusteredHills.Count];

        for (int i = 0; i < clusteredHills.Count; i++)
        {
            StructuredCluster sh = new StructuredCluster();
            sh.hills = clusteredHills[i];
            sh.centerPosition = GetClusterCenter(sh.hills);
            structuredHills[i] = sh;
        }

        // Sorting the array
        Array.Sort(structuredHills, (x, y) => y.hills[0].z.CompareTo(x.hills[0].z));

        return structuredHills;
    }

    Vector2 GetClusterCenter(List<Vector3> clusterElements)
    {
        Vector2 sum = Vector2.zero;
        int maxCount = 100;
        int counter = 0;
        for (int i = 0; i < clusterElements.Count; i += 10)
        {
            counter += 1;

            sum += new Vector2(clusterElements[i].x, clusterElements[i].y);

            if (counter > maxCount) break;
        }
        return sum / counter;
    }

    /// neighbours

    public List<NeighborCluster> ClusterNeighbous(StructuredCluster[] structuredClusters, float maxDistance)
    {
        List<NeighborCluster> neighborClusters = new List<NeighborCluster>();



        List<List<int>> neighborClustersIndices = new List<List<int>>();

        List<int> readyIndices = new List<int>();


        for (int i = 0; i < structuredClusters.Length; i++)
        {
            if (readyIndices.Contains(i)) continue;

            int currentIndexInChain = i;
            List<int> currentChainCluster = new List<int>();

            bool alreadyInCluster = false;

            for (int j = 0; j < neighborClustersIndices.Count; j++)
            {
                if (neighborClustersIndices[j].Contains(currentIndexInChain))
                {
                    alreadyInCluster = true;
                    currentChainCluster = neighborClustersIndices[j];
                }
                if (alreadyInCluster) break;
            }

            if (!alreadyInCluster)
            {
                currentChainCluster.Add(currentIndexInChain);
                neighborClustersIndices.Add(currentChainCluster);
            }

            bool breakChain = false;

            List<int> usedIndices = new List<int>();

            while (!breakChain)
            {
                // Clustering

                int closestIndex = FindClosestCluster(i, usedIndices, structuredClusters);
                if (closestIndex < 0) break;

                if (Vector2.Distance(structuredClusters[closestIndex].centerPosition, structuredClusters[currentIndexInChain].centerPosition) > maxDistance)
                {
                    // break chain -> is solo cluster
                    readyIndices.Add(i);
                    breakChain = true;
                    continue;
                }

                // Add the closestIndex to the currentChainCluster and readyIndices
                currentChainCluster.Add(closestIndex);
                readyIndices.Add(closestIndex);
                usedIndices.Add(closestIndex);

                currentIndexInChain = closestIndex;

            }

        }

        // create Clusters from indice list

        for (int i = 0; i < neighborClustersIndices.Count; i++)
        {
            NeighborCluster newNeighborCluster = new NeighborCluster();

            List<Vector2> newCenterPositions = new List<Vector2>();

            // read out center positions
            // find highest member

            int heighestNeighbor = 0;
            float heighestHeight = 0;

            for (int j = 0; j < neighborClustersIndices[i].Count; j++)
            {
                int currentIndex = neighborClustersIndices[i][j];
                newCenterPositions.Add(structuredClusters[currentIndex].centerPosition);

                if (structuredClusters[currentIndex].hills[0].z > heighestHeight)
                {
                    heighestHeight = structuredClusters[currentIndex].hills[0].z;
                    heighestNeighbor = currentIndex;
                }
            }

            newNeighborCluster.centerPositions = newCenterPositions;
            newNeighborCluster.heighestPosition = new Vector3(structuredClusters[heighestNeighbor].centerPosition.x,
                                                                structuredClusters[heighestNeighbor].hills[0].z,
                                                                structuredClusters[heighestNeighbor].centerPosition.y);

            // Add the new neighbor cluster to the list
            neighborClusters.Add(newNeighborCluster);
        }

        return neighborClusters;


    }

    int FindClosestCluster(int ownIndex, List<int> usedIndices, StructuredCluster[] clustersToCompare)
    {
        int closestCluster = -1;
        float closestDistance = 1000000;
        Vector2 ownCenterPosition = clustersToCompare[ownIndex].centerPosition;

        for (int i = 0; i < clustersToCompare.Length; i++)
        {
            if (ownIndex == i || usedIndices.Contains(i)) continue;
            float currentDistance = Vector2.Distance(ownCenterPosition, clustersToCompare[i].centerPosition);
            if (currentDistance < closestDistance)
            {
                closestCluster = i;
                closestDistance = currentDistance;
            }
        }
        return closestCluster;
    }

    // Indicator spawning

    private void SpawnIndicators()
    {
        if (!spawnDebugIndicators) return;

        foreach (var item in spawnedHillIndicators)
        {
            Destroy(item);
        }

        foreach (var item in spawnedTroughIndicators)
        {
            Destroy(item);
        }

        foreach (var item in spawnedHillNeighborsIndicators)
        {
            Destroy(item);
        }

        foreach (var item in spawnedTroughNeighborsIndicators)
        {
            Destroy(item);
        }

        spawnedHillIndicators.Clear();
        spawnedTroughIndicators.Clear();
        spawnedHillNeighborsIndicators.Clear();
        spawnedTroughNeighborsIndicators.Clear();


        if (detectHills)
        {
            //old way
            // if (hillIndicator != null)
            // {

            //     foreach (var item in structuredHills)
            //     {
            //         Vector3 center = new Vector3();
            //         center = TransposePosition(item.centerPosition.x, item.centerPosition.y, heightmapDebug.width, heightmapDebug.height, meshWidth, meshDepth, uVOrigin, item.hills[0].z);
            //         GameObject indicator = SpawnIndicator(center, hillIndicator, uVOrigin);
            //         spawnedHillIndicators.Add(indicator);
            //     }

            //     foreach (var item in neighborHills)
            //     {
            //         Vector3 center = new Vector3();
            //         center = TransposePosition(item.heighestPosition.x, item.heighestPosition.z, heightmapDebug.width, heightmapDebug.height, meshWidth, meshDepth, uVOrigin, item.heighestPosition.y);
            //         GameObject indicator = SpawnIndicator(center, hillChainIndicator, uVOrigin);
            //         spawnedHillNeighborsIndicators.Add(indicator);
            //     }
            // }
        }


        if (detectHills)
        {
            foreach (var item in neighborHills)
            {
                Color highestNeighborColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
                Color.RGBToHSV(highestNeighborColor, out float H, out float S, out float V);
                S *= 0.5f; // reduce the saturation to half
                Color neighbourColor = Color.HSVToRGB(H, S, V);
                Vector3 center = new Vector3();
                center = TransposePosition(item.heighestPosition.x, item.heighestPosition.z, heightmap.width, heightmap.height, meshWidth, meshDepth, uVOrigin, item.heighestPosition.y);
                GameObject heighestNBIndicator = SpawnIndicator(center, hillChainIndicator, uVOrigin);
                spawnedHillNeighborsIndicators.Add(heighestNBIndicator);
                SetColor(heighestNBIndicator, highestNeighborColor);

                foreach (var position in item.centerPositions)
                {
                    Vector3 center2 = new Vector3();
                    center2 = TransposePosition(position.x, position.y, heightmap.width, heightmap.height, meshWidth, meshDepth, uVOrigin, item.heighestPosition.y);
                    GameObject nB = SpawnIndicator(center2, hillIndicator, uVOrigin);
                    spawnedHillIndicators.Add(nB);
                    SetColor(nB, neighbourColor);
                }

            }
        }



        if (detectTroughs)
        {
            // old way
            // if (troughIndicator != null)
            // {
            //     foreach (var item in structuredTroughs)
            //     {
            //         Vector3 center = new Vector3();
            //         center = TransposePosition(item.centerPosition.x, item.centerPosition.y, heightmapDebug.width, heightmapDebug.height, meshWidth, meshDepth, uVOrigin, item.hills[0].z);
            //         GameObject indicator = SpawnIndicator(center, troughIndicator, uVOrigin);
            //         spawnedTroughIndicators.Add(indicator);
            //     }

            //     foreach (var item in neighborTroughs)
            //     {
            //         Vector3 center = new Vector3();
            //         center = TransposePosition(item.heighestPosition.x, item.heighestPosition.z, heightmapDebug.width, heightmapDebug.height, meshWidth, meshDepth, uVOrigin, item.heighestPosition.y);
            //         GameObject indicator = SpawnIndicator(center, troughChainIndicator, uVOrigin);
            //         spawnedTroughNeighborsIndicators.Add(indicator);
            //     }
            // }

            if (detectTroughs)
            {
                foreach (var item in neighborTroughs)
                {
                    Color highestTroughColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
                    Color.RGBToHSV(highestTroughColor, out float H, out float S, out float V);
                    S *= 0.5f; // reduce the saturation to half
                    Color neighbourColor = Color.HSVToRGB(H, S, V);
                    Vector3 center = new Vector3();
                    center = TransposePosition(item.heighestPosition.x, item.heighestPosition.z, heightmap.width, heightmap.height, meshWidth, meshDepth, uVOrigin, item.heighestPosition.y);
                    GameObject heighestTroughIndicator = SpawnIndicator(center, troughChainIndicator, uVOrigin);
                    spawnedTroughNeighborsIndicators.Add(heighestTroughIndicator);
                    SetColor(heighestTroughIndicator, highestTroughColor);

                    foreach (var position in item.centerPositions)
                    {
                        Vector3 center2 = new Vector3();
                        center2 = TransposePosition(position.x, position.y, heightmap.width, heightmap.height, meshWidth, meshDepth, uVOrigin, item.heighestPosition.y);
                        GameObject nB = SpawnIndicator(center2, troughIndicator, uVOrigin);
                        spawnedTroughIndicators.Add(nB);
                        SetColor(nB, neighbourColor);
                    }
                }
            }
        }

    }


    public Vector3 TransposePosition(float idx, float idy, int textureWidth, int textureHeight, float meshWidth, float meshDepth, Transform origin, float yStrength)
    {
        float u = -(float)idx / textureWidth;
        float v = -(float)idy / textureHeight;

        // float posX = origin.position.x + (u * meshWidth);
        // float posY = origin.position.y + yStrength * heightFactor;
        // float posZ = origin.position.z + (v * meshDepth);

        // return new Vector3(posX, posY, posZ);

        ///
        Vector3 position = new Vector3();

        position = origin.position;
        position += origin.right * ((u * meshWidth));
        position += origin.up * (yStrength * heightFactor);
        position += origin.forward * (v * meshDepth);

        return position;



    }

    GameObject SpawnIndicator(Vector3 position, GameObject prefab, Transform parent)
    {
        GameObject indicator = Instantiate(prefab, position, Quaternion.identity, parent);

        return indicator;
    }

    void SetColor(GameObject gameObject, Color color)
    {

        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (renderer != null)
        {

            Material mat = renderer.material;



            if (mat.HasProperty("_Color"))
            {
                print("set color");

                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();


                propBlock.SetColor("_Color", color);


                renderer.SetPropertyBlock(propBlock);
            }
        }
        else
        {
            Debug.LogError("No Renderer found on GameObject");
        }

    }





    ///// helpers







}
