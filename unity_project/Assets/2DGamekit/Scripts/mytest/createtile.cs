using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class createtile : MonoBehaviour
{
    public Camera targetCam;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tile;
    private Vector3Int m_Pos = new Vector3Int(2,2,0); 
    // Start is called before the first frame update
    void Start()
    {
        tilemap.SetTile(m_Pos, tile);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 relativePos = targetCam.ScreenToWorldPoint(Input.mousePosition);
            relativePos.z = targetCam.nearClipPlane;
            // Vector3 relativePos = Input.mousePosition;
            Debug.Log(relativePos);
            Vector3Int pos = new Vector3Int(Mathf.FloorToInt(relativePos.x), Mathf.FloorToInt(relativePos.y), 0);
            tilemap.SetTile(pos, tile);
        }
    }
}
