using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Domino : MonoBehaviour
{
    public bool    wantCameraFocus = true;
    public Vector3 cameraFocusPos  = Vector3.zero;
    private bool activated = false;
    private bool sleeping  = false;

    void Start()
    {
        BoxCollider collider = this.GetComponent<BoxCollider>();
        collider.enabled = false;

        Rigidbody rigidbody = this.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = false;
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        StartCoroutine(LateActivate());
    }

    IEnumerator LateActivate()
    {
        yield return new WaitForSeconds(1.0f);

        BoxCollider collider = this.GetComponent<BoxCollider>();
        collider.enabled = true;

        Rigidbody rigidbody = this.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = true;
        rigidbody.isKinematic = false;
        rigidbody.useGravity = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contacts = collision.contacts;
        for (int i = 0; i < contacts.Length; i++) {
            ContactPoint contact = contacts[i];
            if (!contact.otherCollider.GetComponent<Domino>())
                return;
        }

        if (!this.activated) {
            SceneController.instance.OnDominoActivated(this.gameObject);
        }

        this.activated = true;

        // NOTE: experimental method for perfomance improvement
        //StartCoroutine(this.SleepAfterSeconds(1.0f));
    }
    IEnumerator SleepAfterSeconds(float time)
    {
        if (this.sleeping)
            yield break;
        
        this.sleeping = true;

        yield return new WaitForSeconds(time);

        this.GetComponent<BoxCollider>().enabled = false;
        Rigidbody rigidbody = this.GetComponent<Rigidbody>();
        rigidbody.detectCollisions = false;
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }
}
