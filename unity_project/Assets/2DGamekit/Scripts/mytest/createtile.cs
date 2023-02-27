using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UI; // for saving and loading photo

public class createtile : MonoBehaviour
{
    public Camera targetCam;
    [SerializeField] private Canvas m_Canvas;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tile;
    [SerializeField] private TMP_Text photoIdxUI;
    [SerializeField] private Image photoImage;
    
    public int PhotoCap = 10;
    [System.NonSerialized] public List<(TileBase[], int, int, bool)> PicStorageTile = new List<(TileBase[], int, int, bool)>();
    [System.NonSerialized] public List<Texture2D> PhotoTempStorage = new List<Texture2D>();
    [System.NonSerialized] public int photoIdx = 0;
    [System.NonSerialized] public Vector2 photo_minValue;
    [System.NonSerialized] public Vector2 photo_maxValue;
    [System.NonSerialized] public bool shootPhoto = false;
    [System.NonSerialized] public bool rotationTake = false; // true for horizontal, false for vertical
    [System.NonSerialized] public int rotationPlace = 0; // 0: default vertical, 1: -90, 2: -180, 3: -270
    
    private float photoWidth = 300; // need tweak
    private float photoHeight = 400; // need tweak
    private float cameraDistance = (float)12; // no idea why, but it works
    private int m_PhotoStorageIdx = 0; // save photo from idx=0;
    private Vector2 boxBorderOffset = new Vector2(22, 22); // in screen space, pixel count
    private bool isSelecting = false;
    private bool isPlacing = false;
    public RectTransform selectionBox;
    // Start is called before the first frame update
    void Start()
    {
        // clear Photo Storage.
        System.IO.DirectoryInfo di = new DirectoryInfo(Application.dataPath + "/Resources/");
        foreach (FileInfo file in di.GetFiles("*PhotoStorage_*"))
        {
            file.Delete(); 
        }
        
    }

    private void LateUpdate()
    {
        // capture image on shot.
        if (shootPhoto)
        {
            Capture(photo_minValue, photo_maxValue);
            shootPhoto = false;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // game control
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
            // reset rotation
            rotationTake = false;
            rotationPlace = 0;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            // change photo taking direction: vertical/horizontal
            rotationTake = !rotationTake;
            // change photo placing direction:
            if (rotationPlace > 2) {
                rotationPlace = 0;
            } else {
                rotationPlace += 1;
            }

            
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
            // reset rotation
            rotationTake = false;
            rotationPlace = 0;
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
            // press "M" to exit placing photo
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("Exiting Placing Photo.");
                isPlacing = false;
            }
            UpdateSelectionBox(isPlacing);
            SelectPhotoIndex(isPlacing);
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
        selectionBox.sizeDelta = rotationTake ? new Vector2(photoHeight, photoWidth) : new Vector2(photoWidth, photoHeight); 
        selectionBox.anchoredPosition = new Vector2(
            Input.mousePosition.x / m_Canvas.scaleFactor,
            Input.mousePosition.y / m_Canvas.scaleFactor
        );
    }

    void SelectObjects()
    {
        photo_minValue = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2) + boxBorderOffset;
        photo_maxValue = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2) - boxBorderOffset;
        
        // Select Tiles ------------
        Grid grid = tilemap.layoutGrid;
        // convert to world
        Vector3 worldMin = targetCam.ScreenToWorldPoint(new Vector3(photo_minValue.x, photo_minValue.y, cameraDistance));
        Vector3 worldMax = targetCam.ScreenToWorldPoint(new Vector3(photo_maxValue.x, photo_maxValue.y, cameraDistance));

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
        
        // save to storage
        int vertDist = topRightCell.x - bottomLeftCell.x + 1;
        int horDist = topRightCell.y - bottomLeftCell.y + 1;
        var toSaveTuple = (selectedTiles, vertDist, horDist, rotationTake);
        PicStorageTile.Add(toSaveTuple);
        
        // save photo
        // need LateUpdate(); so use a state;
        shootPhoto = true;
    }
    
    void Capture(Vector2 bl, Vector2 tr)
    {
        // bl: bottom left anchor
        // tr: top right anchor

        int width = (int)(tr.x - bl.x);
        int height = (int)(tr.y - bl.y);
        
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = targetCam.targetTexture;
 
        targetCam.Render();
 
        Texture2D image = new Texture2D(width, height);
        image.ReadPixels(new Rect(bl.x, bl.y, width, height), 0, 0);
        image.Apply();
        RenderTexture.active = activeRenderTexture;
        
        byte[] bytes = image.EncodeToPNG();
        
        // also save to temp
        PhotoTempStorage.Add(image);
        // Destroy(image);
 
        File.WriteAllBytes(Application.dataPath + "/Resources/PhotoStorage_" + m_PhotoStorageIdx + ".png", bytes);
        m_PhotoStorageIdx++;
        

    }
    void SelectPhotoIndex(bool update)
    {
        var totalLength = PicStorageTile.Count;
        // enable photo idx UI
        photoIdxUI.gameObject.SetActive(update);
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
        
        // show photo in frame
        // Sprite photo = Resources.Load<Sprite>("PhotoStorage_" + photoIdx);
        var match_rotation = PicStorageTile[photoIdx].Item4 == false; // check if is vertical
        Sprite photo = Sprite.Create(PhotoTempStorage[photoIdx], new Rect(0, 0, PhotoTempStorage[photoIdx].width, PhotoTempStorage[photoIdx].height), new Vector2(0,0));
        photoImage.GetComponent<Image>().sprite = photo;
        photoImage.gameObject.SetActive(update);
        photoImage.GetComponent<RectTransform>().sizeDelta = match_rotation ? new Vector2(photoWidth, photoHeight) : new Vector2(photoHeight, photoWidth);
        photoImage.GetComponent<RectTransform>().eulerAngles = new Vector3(0,0,match_rotation ? 0 : -90);
        switch (rotationPlace)
        {
            case 0:
                photoImage.GetComponent<RectTransform>().eulerAngles = new Vector3(0,0,match_rotation ? 0 : -90);
                break;
            case 1:
                photoImage.GetComponent<RectTransform>().eulerAngles = new Vector3(0,0,match_rotation ? -90 : -180);
                break;
            case 2:
                photoImage.GetComponent<RectTransform>().eulerAngles = new Vector3(0,0,match_rotation ? -180 : -270);
                break;
            case 3:
                photoImage.GetComponent<RectTransform>().eulerAngles = new Vector3(0,0,match_rotation ? -270 : 0);
                break;
        }
        photoImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            Input.mousePosition.x / m_Canvas.scaleFactor,
            Input.mousePosition.y / m_Canvas.scaleFactor
        );
    }
    
    void PlaceObjects()
    {
        photoIdxUI.gameObject.SetActive(false);
        // disable photo idx UI
        // disable photo
        photoImage.gameObject.SetActive(false);
        
        Vector3 relativePos = targetCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));
        Vector3Int start_pos = new Vector3Int(Mathf.FloorToInt(relativePos.x), Mathf.FloorToInt(relativePos.y), 0);
        var match_rotation = PicStorageTile[photoIdx].Item4 == false; // check if is vertical
        switch (rotationPlace)
        {
            case 0:
                start_pos += match_rotation ? new Vector3Int(-Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), -Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), 0) : new Vector3Int(-Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), 0);
                break;
            case 1:
                start_pos += match_rotation ? new Vector3Int(-Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), 0) : new Vector3Int(Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), 0);
                break;
            case 2:
                start_pos += match_rotation ? new Vector3Int(Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), 0) : new Vector3Int(Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), -Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), 0);
                break;
            case 3:
                start_pos += match_rotation ? new Vector3Int(Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), -Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), 0) : new Vector3Int(-Mathf.FloorToInt(PicStorageTile[photoIdx].Item2/2), -Mathf.FloorToInt(PicStorageTile[photoIdx].Item3/2), 0);
                break;
        }
        for (int i = 0; i < PicStorageTile[photoIdx].Item1.Length; i++)
        {
            if (PicStorageTile[photoIdx].Item1[i] is not null && PicStorageTile[photoIdx].Item1[i].GetType() == typeof(RuleTile))
            {
                switch (rotationPlace)
                {
                    case 0:
                        if (match_rotation)
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(i % PicStorageTile[photoIdx].Item2,
                                    Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 0), tile);
                        }
                        else
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 
                                    - i % PicStorageTile[photoIdx].Item2 ,0), tile);
                        }
                        break;
                    case 1:
                        if (match_rotation)
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 
                                    - i % PicStorageTile[photoIdx].Item2 ,0), tile);
                        }
                        else
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(- i % PicStorageTile[photoIdx].Item2,
                                    - Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 0), tile);
                        }
                        break;
                    case 2:
                        if (match_rotation)
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(- i % PicStorageTile[photoIdx].Item2,
                                    - Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 0), tile);
                        }
                        else
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(- Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 
                                     i % PicStorageTile[photoIdx].Item2 ,0), tile);
                        }
                        break;
                    case 3:
                        if (match_rotation)
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(- Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 
                                    i % PicStorageTile[photoIdx].Item2 ,0), tile);
                        }
                        else
                        {
                            tilemap.SetTile(
                                start_pos + new Vector3Int(i % PicStorageTile[photoIdx].Item2,
                                    Mathf.FloorToInt(i / PicStorageTile[photoIdx].Item2), 0), tile);
                        }
                        break;
                }
            }
            
        }
    }
}

