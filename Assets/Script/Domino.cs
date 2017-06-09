using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Domino : MonoBehaviour
{
    private bool activated = false;
    private bool sleeping = false;

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contacts = collision.contacts;
        for (int i = 0; i < contacts.Length; i++) {
            ContactPoint contact = contacts[i];
            if (!contact.otherCollider.GetComponent<Domino>())
                return;
        }

        if (!this.activated) {
            SceneController.instance.SetCurrentFollowee(this.gameObject);
        }

        this.activated = true;

        // NOTE: experimental method for perfomance improvement
        StartCoroutine(this.SleepAfterSeconds(1.0f));
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
