using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public class SpawnManager : MonoBehaviour
    {
        public int m_ObjectPoolSize = 15;
        public GameObject m_Prefab;
        public GameObject[] m_Pool;

        public System.Guid assetID { get; set; }

        public delegate GameObject SpawnDelegate(Vector3 position, System.Guid assetID);
        public delegate void UnSpawnDelegate(GameObject spawned);

        // Start is called before the first frame update
        void Start()
        {
            assetID = m_Prefab.GetComponent<NetworkIdentity>().assetId;
            m_Pool = new GameObject[m_ObjectPoolSize];
            for(int i = 0; i < m_ObjectPoolSize; i++)
            {
                m_Pool[i] = Instantiate(m_Prefab, Vector3.zero, Quaternion.identity);
                m_Pool[i].name = "PoolObject" + i;
                m_Pool[i].SetActive(false);
            }

            ClientScene.RegisterSpawnHandler(assetID, SpawnObject, UnSpawnObject);
        }

        public GameObject GetFromPool(Vector3 position)
        {
            foreach(GameObject obj in m_Pool)
            {
                if(!obj.activeInHierarchy)
                {
                    obj.transform.position = position;
                    obj.SetActive(true);
                    return obj;
                }
            }
            return null;
        }

        public GameObject SpawnObject(Vector3 position, System.Guid assetID)
        {
            return GetFromPool(position);
        }

        public void UnSpawnObject(GameObject spawned)
        {
            spawned.SetActive(false);
        }
    }
}