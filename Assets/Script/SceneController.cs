using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    public GameObject reservedDominosContaner = null;
    public GameObject mainCamera              = null;
    public GameObject field                   = null;
    public GameObject initialDomino           = null;
    public GameObject trigger                 = null;
    public GameObject dominoCountText         = null;

    private GameObject currentCameraTarget = null;
    private GameObject lastDeployedDomino  = null;
    private GameObject dominoPrefab        = null;
    private Vector3    lastDeployedDominoCenter = Vector3.zero;
    private Vector3    lastCameraPos            = Vector3.zero;

    private int currentRaw     = 1;
    private int deployedCount  = 1;
    private int activatedCount = 0;

#region 悪い設計ゾーン
    public static SceneController instance = null;
    public void OnDominoActivated(GameObject go)
    {
        if (go.GetComponent<Domino>().wantCameraFocus)
            this.currentCameraTarget = go;
        
        this.activatedCount++;
    }
    void Start()
    {
        instance = this;
#endregion
        this.dominoPrefab = Resources.Load<GameObject>("Prefab/Domino");
        this.lastDeployedDomino       = this.initialDomino;
        this.lastDeployedDominoCenter = this.initialDomino.transform.position;

        Domino domino = this.lastDeployedDomino.GetComponent<Domino>();
        domino.wantCameraFocus = true;
        domino.cameraFocusPos  = this.lastDeployedDomino.transform.position;
    }

    void Update()
    {
        this.PreUpdate();

        this.UpdateCamera();

        this.UpdateInput();

        this.UpdateUI();
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

        if (Input.GetKeyDown(KeyCode.T)) {
            this.Trigger();
        
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            this.currentRaw++;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            this.currentRaw--;
        }

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
            Vector3 desiredPos = new Vector3(0.0f, 0.01f, 0.0f) + hit.point;
            this.DeployDomino(desiredPos);
        }
    }

    private void UpdateUI()
    {
        Assert.IsNotNull(this.dominoCountText);

        Text text = this.dominoCountText.GetComponent<Text>();
        text.text = this.activatedCount + " / " + this.deployedCount;
    }

    private void DeployDomino(Vector3 desiredPos)
    {
        float fixedDistance    = 0.075f;
        float fixedRawDistance = 0.15f;

        Transform lastDominoTransform = this.lastDeployedDomino.transform;
        Vector3   lastDominoPos       = this.lastDeployedDominoCenter;

        float distance = Vector3.Distance(desiredPos, lastDominoPos);

        if (distance < fixedDistance)
            return;

        Vector3 lastDominoRotEuler = lastDominoTransform.rotation.eulerAngles;

        Vector3 rel = (desiredPos - lastDominoPos) * (fixedDistance / distance);
        desiredPos = lastDominoPos + rel;
        desiredPos.y = 0.05f;

        lastDominoRotEuler.y = Quaternion.LookRotation(desiredPos - lastDominoPos).eulerAngles.y;
        lastDominoTransform.rotation = Quaternion.Euler(lastDominoRotEuler);

        this.lastDeployedDominoCenter = desiredPos;

        bool hasFocused = false;
        for (int i = 1; i <= this.currentRaw; i++) {
            GameObject dominoObj = Instantiate<GameObject>(this.dominoPrefab, desiredPos, lastDominoTransform.rotation);;
            Vector3 right = dominoObj.transform.right * fixedRawDistance;
            Vector3 relRight = right * ((this.currentRaw - 1) * 0.5f) - right * ((i - 1) * 0.5f);
            relRight += (-dominoObj.transform.right * fixedRawDistance / 2) * ((this.currentRaw - 1) * 0.5f);
            dominoObj.transform.position = dominoObj.transform.position + relRight;

            this.lastDeployedDomino = dominoObj;
            this.deployedCount++;

            Domino domino = dominoObj.GetComponent<Domino>();
            domino.wantCameraFocus = false;

            // odd
            if (!hasFocused) {
                if (this.currentRaw % 2 == 1) {
                    if (this.currentRaw == 1 || (i == ((this.currentRaw - 1) / 2))) {
                        domino.wantCameraFocus = true;
                        this.currentCameraTarget = this.lastDeployedDomino;
                    }
                }
                // even
                else if (this.currentRaw % 2 == 0) {
                    if (i == (this.currentRaw / 2)) {
                        domino.wantCameraFocus = true;
                        this.currentCameraTarget = this.lastDeployedDomino;
                    }
                }
            }

            domino.cameraFocusPos = desiredPos;
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
            Vector3 cameraDestPos = this.currentCameraTarget.GetComponent<Domino>().cameraFocusPos + new Vector3(1.15f, 0.0f, 1.15f);
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
