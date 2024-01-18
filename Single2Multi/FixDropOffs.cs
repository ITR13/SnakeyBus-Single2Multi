using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Single2Multi;

public class FixDropOffs : MonoBehaviour
{
    private DropOffManager _dropOffManager = null!;
    private Transform _busTranform = null!;
    private float f;
    
    private void Start()
    {
        _dropOffManager = FindObjectOfType<DropOffManager>();
        var transitBus = FindObjectOfType<TransitBus>();
        if (transitBus != null)
        {
            _busTranform = transitBus.transform;
        }
    }

    private void FixedUpdate()
    {
        if(_busTranform != null) return;
        f += Time.deltaTime;
        if (f < 1)
        {
            return;
        }
        var transitBus = FindObjectOfType<TransitBus>();
        if (transitBus != null)
        {
            _busTranform = transitBus.transform;
        }

    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        CheckDropOffs();
    }

    private void CheckDropOffs()
    {
        if (_dropOffManager.inactiveDropOffs.Count > 1) return;
        var dropOffs = _dropOffManager.activeDropOffs.ToList();
        var ids = new int[dropOffs.Count / 2];
        for (var i = 0; i < ids.Length; i++)
        {
            var r = Random.Range(0, dropOffs.Count);
            ids[i] = (dropOffs[r].id);
            dropOffs.RemoveAt(r);
        }

        _dropOffManager.deactivateDropOffs(ids);
    }
}