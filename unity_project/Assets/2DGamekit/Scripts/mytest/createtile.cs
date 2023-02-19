using System;
using System.Collections;
using System.Collections.Generic;
using BTAI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class createtile : MonoBehaviour
{
    public Camera targetCam;
    [SerializeField] private Canvas m_Canvas;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tile;
    private Vector3Int m_Pos = new Vector3Int(2,2,0);

    private Vector2 boxBorderOffset = new Vector2(22, 22); // in screen space, pixel count
    private bool isSelecting = false;
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
            UpdateSelectionBox();
        }
        
        // Camera Mode
        if (isSelecting)
        {
            UpdateSelectionBox();
            if (Input.GetMouseButtonDown(0))
            {
                // make selection
                SelectObjects();
                isSelecting = false;
                UpdateSelectionBox();
            }
        }
    }
    void UpdateSelectionBox()
    {
        selectionBox.gameObject.SetActive(isSelecting);
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
        foreach (TileBase selected in selectedTiles)
        {
            if (selected is not null && selected.GetType() == typeof(RuleTile))
            {
                Debug.Log(selected);
            }
        }
        
    }
}

