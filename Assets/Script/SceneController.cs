using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SceneController : MonoBehaviour
{
    public GameObject reservedDominosContaner = null;
    public GameObject mainCamera              = null;
    public GameObject field                   = null;
    public GameObject initialDomino           = null;
    public GameObject trigger                 = null;

    private GameObject currentCameraTarget = null;
    private GameObject lastDeployedDomino  = null;
    private GameObject dominoPrefab        = null;
    private Vector3    lastCameraPos       = Vector3.zero;

    #region 悪い設計ゾーン
    public static SceneController instance = null;
    public void SetCurrentFollowee(GameObject go)
    {
        this.currentCameraTarget = go;
    }
    void Start()
    {
        instance = this;
#endregion
        this.dominoPrefab = Resources.Load<GameObject>("Prefab/Domino");
        this.lastDeployedDomino = this.initialDomino;
    }

    void Update()
    {
        this.PreUpdate();

        this.UpdateCamera();

        this.UpdateInput();
    }

    void PreUpdate()
    {
        this.lastCameraPos = this.mainCamera.transform.position;
    }

    private void UpdateInput()
    {
        Assert.IsNotNull<GameObject>(this.mainCamera);

        if (Vector3.Distance(this.lastCameraPos, this.mainCamera.transform.position) > 0.01f)
            return;

        Camera camera = this.mainCamera.GetComponent<Camera>();

#if !UNITY_EDITOR
        Touch[] touches = Input.touches;
        if (touches.Length == 0)
            return;

        Touch touch = touches[0];
        Vector3 touchPoistion = touch.position;
#else
        if (!Input.GetMouseButton(0))
            return;

        Vector3 touchPoistion = Input.mousePosition;
#endif

        Assert.IsNotNull<GameObject>(this.dominoPrefab);
        Assert.IsNotNull<GameObject>(this.lastDeployedDomino);

        Ray ray = camera.ScreenPointToRay(touchPoistion);
        RaycastHit hit = new RaycastHit();
        MeshCollider collider = this.field.GetComponent<MeshCollider>();

        if (collider.Raycast(ray, out hit, 10.0f)) {
            float fixedDistance = 0.05f;

            Vector3 desiredPos = new Vector3(0.0f, 0.01f, 0.0f) + hit.point;

            Transform lastDominoTransform = this.lastDeployedDomino.transform;
            Vector3   lastDominoPos       = lastDominoTransform.position;

            float distance = Vector3.Distance(desiredPos, lastDominoPos);

            if (distance < fixedDistance)
                return;

            Vector3 lastDominoRotEuler = lastDominoTransform.rotation.eulerAngles;

            Vector3 rel = (desiredPos - lastDominoPos) * (fixedDistance / distance);
            desiredPos = lastDominoPos + rel;

            lastDominoRotEuler.y = Quaternion.LookRotation(desiredPos - lastDominoPos).eulerAngles.y;
            lastDominoTransform.rotation = Quaternion.Euler(lastDominoRotEuler);

            this.lastDeployedDomino = Instantiate<GameObject>(this.dominoPrefab, desiredPos, lastDominoTransform.rotation);
            this.currentCameraTarget = this.lastDeployedDomino;
        }
    }

    private void UpdateCamera()
    {
        if (this.currentCameraTarget == null)
            return;

        Transform targetTransform = this.currentCameraTarget.transform;
        Transform mainCameraTransform = this.mainCamera.transform;

        Vector3 targetPos = targetTransform.position;
        Vector3 cameraCurrentPos = mainCameraTransform.position;

        // play mode
        if (this.trigger.GetComponent<Trigger>().triggered) {
            Vector3 cameraDestPos = targetPos + new Vector3(0.4f, 0.4f, 0.4f);

            Quaternion cameraDestRot = Quaternion.LookRotation(targetPos - cameraDestPos);

            Vector3 targetRot = targetTransform.rotation.eulerAngles;
            Quaternion cameraCurrentRot = mainCameraTransform.rotation;

            this.mainCamera.transform.position = Vector3.Lerp(cameraCurrentPos, cameraDestPos, 0.1f);
            this.mainCamera.transform.rotation = Quaternion.Lerp(cameraCurrentRot, cameraDestRot, 0.1f);
        }
        // deploy mode
        else {
            Vector3 cameraDestPos = targetPos + new Vector3(1.15f, 0.0f, 1.15f);
            cameraDestPos.y = cameraCurrentPos.y;
            this.mainCamera.transform.position = Vector3.Lerp(cameraCurrentPos, cameraDestPos, 0.1f);
        }
    }

    public void Trigger()
    {
        Assert.IsNotNull<GameObject>(this.trigger);
        Assert.IsNotNull<GameObject>(this.initialDomino);

        if (this.trigger.GetComponent<Trigger>().triggered)
            return;

        this.currentCameraTarget = this.initialDomino;
        Debug.Log(this.initialDomino.transform.position);
        this.trigger.SetActive(true);

        StartCoroutine(TriggerRoutine());
    }

    private IEnumerator TriggerRoutine()
    {
        yield return null;

        this.trigger.GetComponent<Trigger>().Fire(this.initialDomino.transform);
    }

    public void DeployPreset()
    {
        GameObject start = GameObject.Find("Start");
        Transform baseDomino = start.GetComponentInChildren<Transform>();

        this.currentCameraTarget = baseDomino.gameObject;

        if (this.reservedDominosContaner == null)
            return;
        
        // use prepared disabled dominos
        if (false) {
            this.reservedDominosContaner.SetActive(true);

            Transform[] children = this.reservedDominosContaner.GetComponentsInChildren<Transform>();

            // 0 would be container it self
            for (int i = 1; i < children.Length; i++) {
                Transform child = children[i];
                Vector3 pos = baseDomino.position;
                pos.z -= 0.05f * i;
                child.position = pos;
            }
        }

        // create new domino instances
        {
            int max = 30;
            for (int i = 0; i < max; i++) {
                Vector3 pos = baseDomino.position;
                pos.z -= 0.05f * (i + 1);

                if (i == (max - 1)) {
                    this.lastDeployedDomino = Instantiate<GameObject>(this.dominoPrefab, pos, baseDomino.rotation);
                }
                else {
                    Instantiate<GameObject>(this.dominoPrefab, pos, baseDomino.rotation);
                }
            }
        }
    }
}
