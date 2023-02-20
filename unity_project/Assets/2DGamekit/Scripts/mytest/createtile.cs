using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class createtile : MonoBehaviour
{
    public Camera targetCam;
    [SerializeField] private Canvas m_Canvas;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tile;
    [SerializeField] private TMP_Text photoIdxUI;
    private Vector3Int m_Pos = new Vector3Int(2,2,0);

    public int PhotoCap = 10;
    [System.NonSerialized] public List<(TileBase[], int, int)> PicStorageTile = new List<(TileBase[], int, int)>();
    [System.NonSerialized] public int photoIdx = 0;
    
    private Vector2 boxBorderOffset = new Vector2(22, 22); // in screen space, pixel count
    private bool isSelecting = false;
    private bool isPlacing = false;
    public RectTransform selectionBox;
    // Start is called before the first frame update
    void Start()
    {
        tilemap.SetTile(m_Pos, tile);
    }

    // Update is called once per frame
    void Update()
    {
        // // draw tiles
        // if (Input.GetMouseButtonDown(0))
        // {
        //     Vector3 relativePos = targetCam.ScreenToWorldPoint(Input.mousePosition);
        //     relativePos.z = targetCam.nearClipPlane;
        //     Debug.Log(relativePos);
        //     Vector3Int pos = new Vector3Int(Mathf.FloorToInt(relativePos.x), Mathf.FloorToInt(relativePos.y), 0);
        //     tilemap.SetTile(pos, tile);
        // }

        if (Input.GetKeyDown(KeyCode.P))
        {
            // Enter camera mode.
            // - disable player movement?
            // - freeze time?
            // define capture-ables: now only tiles
            // - add other objects later.
            
            Debug.Log("Entering Camera Mode.");
            // show selection box
            isSelecting = true;
            
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Exiting Camera Mode.");
            // Exit camera mode.
            isSelecting = false;
            UpdateSelectionBox(isSelecting);
        }

        if (Input.GetKeyDown(KeyCode.L) && PicStorageTile.Count > 0)
        {
            // L to place photo, if there is any photo taken
            Debug.Log("Placing Photo.");
            isPlacing = true;
            // reset photo index
            photoIdx = 0;
        }

        // Camera Mode
        if (isSelecting)
        {
            UpdateSelectionBox(isSelecting);
            if (Input.GetMouseButtonDown(0))
            {
                // make selection
                SelectObjects();
                isSelecting = false;
                UpdateSelectionBox(isSelecting);
            }
        }
        // Placing Mode
        if (isPlacing)
        {
            UpdateSelectionBox(isPlacing);
            SelectPhotoIndex();
            if (Input.GetMouseButtonDown(0))
            {
                PlaceObjects();
                isPlacing = false;
                UpdateSelectionBox(isPlacing);
            }
        }
    }
    void UpdateSelectionBox(bool update)
    {
        selectionBox.gameObject.SetActive(update);
        float width = 300; // need tweak
        float height = 400; // need tweak
        selectionBox.sizeDelta = new Vector2(width, height);
        selectionBox.anchoredPosition = new Vector2(
            Input.mousePosition.x / m_Canvas.scaleFactor,
            Input.mousePosition.y / m_Canvas.scaleFactor
        );
    }

    void SelectObjects()
    {
        Vector2 minValue = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2) + boxBorderOffset;
        Vector2 maxValue = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2) - boxBorderOffset;
        
        // Select Tiles ------------
        Grid grid = tilemap.layoutGrid;
        // convert to world
        Vector3 worldMin = targetCam.ScreenToWorldPoint(minValue);
        Vector3 worldMax = targetCam.ScreenToWorldPoint(maxValue);

        // then convert to cell
        Vector3Int bottomLeftCell = grid.WorldToCell(worldMin); 
        Vector3Int topRightCell = grid.WorldToCell(worldMax);
        Debug.Log(bottomLeftCell);
        Debug.Log(topRightCell);

        Vector3Int min = Vector3Int.Min(bottomLeftCell, topRightCell);
        Vector3Int max = Vector3Int.Max(bottomLeftCell, topRightCell);
        Vector3Int size = max - min + Vector3Int.one;

        BoundsInt bounds = new BoundsInt(min, size);
        TileBase[] selectedTiles = tilemap.GetTilesBlock(bounds);

        Debug.Log(selectedTiles.Length);
        // foreach (TileBase selected in selectedTiles)
        // {
        //     if (selected is not null && selected.GetType() == typeof(RuleTile))
        //     {
        //         Debug.Log(selected);
        //     }
        // }
        
        // save to storage
        int vertDist = topRightCell.x - bottomLeftCell.x + 1;
        int horiDist = topRightCell.y - bottomLeftCell.y + 1;
        var toSaveTuple = (selectedTiles, vertDist, horiDist);
        PicStorageTile.Add(toSaveTuple);

    }

    void SelectPhotoIndex()
    {
        var totalLength = PicStorageTile.Count;
        // enable photo idx UI
        photoIdxUI.gameObject.SetActive(true);
        // scroll through photos
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            // scroll up
            if (photoIdx < totalLength - 1)
            {
                photoIdx += 1;
            }
            else
            {
                photoIdx = 0;
            }
            Debug.Log("Scroll Up");
        } else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            // scroll down
            if (photoIdx > 0)
            {
                photoIdx -= 1;
            }
            else
            {
                photoIdx = totalLength - 1;
            }
            Debug.Log("Scroll Down");
        }
        // update photo idx UI
        photoIdxUI.text = "Photo: " + photoIdx.ToString("0");

    }
    
    void PlaceObjects()
    {
        // disable photo idx UI
        photoIdxUI.gameObject.SetActive(false);
        
        Vector3 relativePos = targetCam.ScreenToWorldPoint(Input.mousePosition);
        relativePos.z = targetCam.nearClipPlane;
        Debug.Log(relativePos);
        Vector3Int start_pos = new Vector3Int(Mathf.FloorToInt(relativePos.x), Mathf.FloorToInt(relativePos.y), 0);
        start_pos -= new Vector3Int(Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), 0);
        // now only place the first picture
        for (int i = 0; i < PicStorageTile[photoIdx].Item1.Length; i++)
        {
            if (PicStorageTile[photoIdx].Item1[i] is not null && PicStorageTile[photoIdx].Item1[i].GetType() == typeof(RuleTile))
            {
                tilemap.SetTile(start_pos + new Vector3Int(i % PicStorageTile[photoIdx].Item2, Mathf.FloorToInt(i/PicStorageTile[photoIdx].Item2) ,0), tile);
            }
            
        }
    }
}

