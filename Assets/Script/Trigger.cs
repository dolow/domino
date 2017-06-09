using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    public bool triggered { get; private set; }

    private bool dienabling = false;

    void Start()
    {
        this.triggered = false;
    }

    public void Fire(Transform target)
    {
        this.triggered = true;
        Vector3 dest = target.position + target.forward * -0.05f;
        dest.y = this.transform.position.y;
        this.transform.position = dest;

        Vector3 force = target.position - this.transform.position;
        force.x *= 300.0f;
        force.y = 0.0f;
        force.z *= 300.0f;

        this.GetComponent<Rigidbody>().AddForce(force);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (this.dienabling)
            return;
        
        this.dienabling = true;
        StartCoroutine(Disenable());
    }

    private IEnumerator Disenable()
    {
        yield return new WaitForSeconds(0.5f);

        this.gameObject.SetActive(false);
    }
}
