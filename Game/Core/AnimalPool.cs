// ============================================================
//  AnimalPool.cs  –  Animal Fall
//  Generic object pool scoped to Animal GameObjects.
//  Spawner borrows an instance; Animal.OnCollected returns it.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class AnimalPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int        initialSize = 16;

    private Queue<GameObject> _free      = new(16);
    private Transform         _poolRoot;

    private void Awake()
    {
        _poolRoot = new GameObject($"{prefab?.name ?? "Animal"}_Pool").transform;
        _poolRoot.SetParent(transform);

        for (int i = 0; i < initialSize; i++)
            _free.Enqueue(CreateNew());
    }

    // ── Borrow ────────────────────────────────────────────────
    public GameObject Borrow(Vector3 worldPos, Transform parent = null)
    {
        GameObject go = _free.Count > 0 ? _free.Dequeue() : CreateNew();
        go.transform.SetParent(parent ?? _poolRoot);
        go.transform.position = worldPos;
        go.SetActive(true);

        // Tell the Animal component which pool owns it
        var animal = go.GetComponent<Animal>();
        if (animal != null) animal.OwningPool = this;

        return go;
    }

    // ── Return ────────────────────────────────────────────────
    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(_poolRoot);
        _free.Enqueue(go);
    }

    // ── Private ───────────────────────────────────────────────
    private GameObject CreateNew()
    {
        var go = Instantiate(prefab, _poolRoot);
        go.SetActive(false);
        return go;
    }
}
