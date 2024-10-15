using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunSpawn : MonoBehaviour
{
    [SerializeField] int seed;
    [SerializeField] int iterations = 10;
    [SerializeField] List<float> runners;
    [SerializeField] List<GameObject> roomPrefabs;
    [SerializeField] GameObject roomInit;
    [SerializeField] GameObject roomBoss;
    public List<GameObject> rooms;

    [HideInInspector] public HashSet<Vector2Int> roomsToSpawn;
    Vector2Int furthestRoom; //habitacion mas lejana para el boss
    float distAux = 0;

    List<Vector2Int> runnersPositions;
    List<Vector2Int> runnersDirection;

    int ROOM_SIZE = 10; //grande de cada room


    private void Awake()
    {
        if(seed != -1)
        {
            Random.InitState(seed); //con -1 generamos siempre cosas aleatorias
        }
        runnersPositions = new List<Vector2Int>();
        runnersDirection = new List<Vector2Int>();
        roomsToSpawn = new HashSet<Vector2Int> ();
        rooms = new List<GameObject>();
    }

    void Start()
    {
        run();
        spawn();
    }

    void run()
    {
        //inicializamos runners
        for (int i = 0; i < runners.Count; i++)
        {
            runnersPositions.Add(Vector2Int.zero);
            runnersDirection.Add(Vector2Int.zero);
        }

        //siempre creamos la room 0,0
        roomsToSpawn.Add(Vector2Int.zero);

        //iteramos y por cada runner ajustamos la direccion y posicion. Apuntamos donde por donde van corriendo en el HashSet
        for (int i = 0; i < iterations; i++)
        {
            for (int j = 0; j < runners.Count; j++)
            {
                bool turn = (Random.value < runners[j]);
                if(turn)
                {
                    runnersDirection[j] = turnDir(runnersDirection[j]);
                }
                runnersPositions[j] = runnersPositions[j] + runnersDirection[j];
                roomsToSpawn.Add(runnersPositions[j]);

                float distToCentre = Vector2Int.Distance(Vector2Int.zero, runnersPositions[j]);
                if(distToCentre > distAux)
                {
                    distAux = distToCentre;
                    furthestRoom = runnersPositions[j];
                }
            }
        }
    }
    Vector2Int turnDir(Vector2Int dir)
    {
        int turnSign = (Random.value < 0.5f)? 1 : -1;  //si giramos para un lado o otro
        if(dir.x == 0)
        {
            return new Vector2Int(turnSign, 0);
        }
        else
        {
            return new Vector2Int(0, turnSign);
        }
    }

    //Creamos las rooms, si es la mas lejana es la del boss
    void spawn()
    {
        foreach(Vector2Int v in roomsToSpawn)
        {
            int roomID = Random.Range(0, roomPrefabs.Count);
            GameObject roomInstance;
            if(v == Vector2Int.zero)
            {
                roomInstance = Instantiate(roomInit, new Vector3(v.x * ROOM_SIZE, 0, v.y * ROOM_SIZE), Quaternion.identity, transform);
            }
            else if (v == furthestRoom)
            {
                roomInstance = Instantiate(roomBoss, new Vector3(v.x * ROOM_SIZE, 0, v.y * ROOM_SIZE), Quaternion.identity, transform);
            }
            else
            {
                roomInstance = Instantiate(roomPrefabs[roomID], new Vector3(v.x * ROOM_SIZE, 0, v.y * ROOM_SIZE), Quaternion.identity, transform);
            }
            rooms.Add(roomInstance);
        }
    }
}
