// add under-score when building in Unity, bacause Unity is unable to reference Nuget libraries.
// but to enable server-client communication, remove under-score and build in Visual Studio.
#define HOGE_

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using HoloToolkit.Unity.SpatialMapping;
using Newtonsoft.Json.Linq;
using HoloToolkit.Unity.InputModule;

#if HOGE

using Quobject.SocketIoClientDotNet.Client;

#endif

/// <summary>
/// s.kora 2018.04.13
/// Class for changing selection of cloth/flower options
/// </summary>
public class OptionSwitcher : MonoBehaviour
{
    //public static OptionSwitcher instance;
    public GameObject FocusedGameObject { get; private set; }

    //private GameObject oldFocusedObject = null;
    private float gazeMaxDistance = 300;

    public GameObject tableSetObject; // table set object to drop on a physical table
    public GameObject instructionText; // Instruction Text for initial screen
    private GestureRecognizer recognizer; // Hololens input recognizer

    private List<GameObject> clothList = new List<GameObject>();

    private List<string> matArray;

    private int TapCountCloth;

    private List<string> avtArray;

    private int TapCountFlower;

    private bool initiateTable;
    private Vector3 initiatePosition;

#if HOGE
    private Socket socket;
#endif

    public bool Hit { get; private set; }

    private void Start()
    {
#if HOGE
        SetSocket();
#endif
        TapCountFlower = 0;
        TapCountCloth = 0;
        FocusedGameObject = null;
        initiateTable = false;

        // subscribing to the Hololens API gesture recognizer to track user gestures
        recognizer = new GestureRecognizer();
        recognizer.SetRecognizableGestures(GestureSettings.Tap);
        recognizer.Tapped += TapHandler;
        recognizer.StartCapturingGestures();

        //statically setting available options for now, should be extracted from Azure DB and dynamically set
        // Flower Prefab names
        avtArray = new List<string>();
        avtArray.Add("FlowerRed");
        avtArray.Add("FlowerPurple");
        avtArray.Add("FlowerYellow");
        avtArray.Add("FlowerWhite");

        // cloth material names
        matArray = new List<string>();
        matArray.Add("green");
        matArray.Add("pink");
        matArray.Add("yellow");
        matArray.Add("blue");
    }

    private void Update()
    {
        RaycastHit hitInfo;
        // Initialise Raycasting.
        Hit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, gazeMaxDistance);
        // Check whether raycast has hit.
        if (Hit == true)
        {
            // Check whether the hit has a collider.
            if (hitInfo.collider != null)
            {
                // Set the focused object with what the user just looked at.
                FocusedGameObject = hitInfo.collider.gameObject;
            }
            else
            {
                // Object looked on is not valid, set focused gameobject to null.
                FocusedGameObject = null;
            }

            //Detect gazing location to set a table when this app just started
            if (!initiateTable)
            {
                initiatePosition = GazeManager.Instance.HitPosition;
            }
        }
        else
        {
            // No object looked upon, set focused gameobject to null.
            FocusedGameObject = null;
        }
    }

    /// <summary>
    /// Respond to Tap Input.
    /// </summary>
    private void TapHandler(TappedEventArgs obj)
    {
        // if a table is not instantiated, create a table and delete instruction
        if (!initiateTable && tableSetObject != null && initiatePosition != null)
        {
            Quaternion q = new Quaternion();
            q = Quaternion.identity;

            // Set a table set
            FocusedGameObject = Instantiate(tableSetObject, initiatePosition, q);

            // if one table is generated, set true to avoid creating more table sets
            initiateTable = true;

            if (instructionText != null)
            {
                //Delete instruction text for creating a table
                GameObject.Destroy(instructionText);
            }

            foreach (GameObject foundObj in GameObject.FindGameObjectsWithTag("GazeCloth"))
            {
                // create an array of luncheon mat objects to change color at once
                clothList.Add(foundObj);
            }
        }

        // Change flower
        if (FocusedGameObject != null && FocusedGameObject.CompareTag("GazeFlower"))
        {
            // variable to get next position of array (this should be dynamic too eventually)
            int nextPos = TapCountFlower % 4;

            // Get Flower object position
            Vector3 placePosition = new Vector3(FocusedGameObject.transform.position.x, FocusedGameObject.transform.position.y, FocusedGameObject.transform.position.z);
            // Destroy current Flower Object
            GameObject.Destroy(FocusedGameObject);

            Quaternion q = new Quaternion();
            q = Quaternion.identity;

            // Load prefab asset from ressources folder
            GameObject prefab = (GameObject)Resources.Load(avtArray[nextPos]);
            //Set
            FocusedGameObject = Instantiate(prefab, placePosition, q);
#if HOGE
            // send changing request to server
            string jstr = "{ \"flower\":\"" + avtArray[nextPos] + "\"}";
            JObject json = JObject.Parse(jstr);
            socket.Emit("chat", json);
#endif
            TapCountFlower++;
        }
        else if (FocusedGameObject != null && FocusedGameObject.CompareTag("GazeCloth"))
        {
            //Change Cloth color

            // variable to get next position of array (this should be dynamic too eventually)
            int nextPos = TapCountCloth % 4;

            // Load prefab asset from resources/materials folder
            Material m = Resources.Load("Materials/" + matArray[nextPos], typeof(Material)) as Material;

            // Set color to all luncheon mats
            foreach (GameObject clothObj in clothList)
            {
                clothObj.GetComponent<Renderer>().material.color = m.color;
            }

            TapCountCloth++;
#if HOGE
            string jstr = "{ \"cloth\": \"" + matArray[nextPos] + "\"}";
            JObject json = JObject.Parse(jstr);
            socket.Emit("chat", json);
#endif
        }
    }

#if HOGE

    private void SetSocket()
    {
        this.socket = IO.Socket("http://localhost:3000");
        socket.On(Socket.EVENT_CONNECT, () =>
        {
            Debug.Log("Connected");
        });
        socket.On("chat", (data) =>
        {
            string param = data.ToString();
            if (JObject.Parse(param)["flower"] != null)
            {
                string flowerFab = JObject.Parse(param)["flower"].ToString();
                Vector3 placePosition = new Vector3(FocusedGameObject.transform.position.x, FocusedGameObject.transform.position.y, FocusedGameObject.transform.position.z);
                GameObject.Destroy(FocusedGameObject);
                //配置する回転角を設定
                Quaternion q = new Quaternion();
                q = Quaternion.identity;
                GameObject prefab = (GameObject)Resources.Load("Prefab/" + flowerFab);
                //配置
                FocusedGameObject = Instantiate(prefab, placePosition, q);
            }

            if (JObject.Parse(param)["cloth"] != null)
            {
                string clothFab = JObject.Parse(param)["cloth"].ToString();
                Material m = Resources.Load("Material/" + clothFab, typeof(Material)) as Material;

                foreach (GameObject clothObj in clothList)
                {
                    clothObj.GetComponent<Renderer>().material = m;
                }
            }
        });
    }

#endif
}