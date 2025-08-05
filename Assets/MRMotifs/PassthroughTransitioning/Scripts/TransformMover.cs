using UnityEngine;
using System.Collections;

public class TransformMover : MonoBehaviour
{
    [Header("Transform References")]
    [SerializeField] private Transform targetTransform;

    [Header("Timing")]
    [SerializeField] private float moveDuration = 1f;

    [Header("World Transform Matching")]
    [SerializeField] private bool matchWorldRotationAndScale = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startWorldScale;

    private Coroutine moveCoroutine;

    private void Awake()
    {
        // Store original world transform
        startPosition = transform.position;
        startRotation = transform.rotation;
        startWorldScale = transform.lossyScale;
    }

    public void MoveToTarget()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveRoutine(
            transform.position,
            targetTransform.position,
            transform.rotation,
            targetTransform.rotation,
            transform.lossyScale,
            targetTransform.lossyScale
        ));
    }

    public void MoveToStart()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveRoutine(
            transform.position,
            startPosition,
            transform.rotation,
            startRotation,
            transform.lossyScale,
            startWorldScale
        ));
    }

    private IEnumerator MoveRoutine(
        Vector3 fromPos, Vector3 toPos,
        Quaternion fromRot, Quaternion toRot,
        Vector3 fromScale, Vector3 toWorldScale
    )
    {
        float time = 0f;
        Vector3 startLocalScale = transform.localScale; // base for relative scaling

        Vector3 toLocalScale = toWorldScale;
        if (transform.parent != null)
        {
            Vector3 parentScale = transform.parent.lossyScale;
            toLocalScale = new Vector3(
                toWorldScale.x / parentScale.x,
                toWorldScale.y / parentScale.y,
                toWorldScale.z / parentScale.z
            );
        }

        while (time < moveDuration)
        {
            float t = time / moveDuration;
            transform.position = Vector3.Lerp(fromPos, toPos, t);

            if (matchWorldRotationAndScale)
            {
                transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
                transform.localScale = Vector3.Lerp(startLocalScale, toLocalScale, t);
            }

            time += Time.deltaTime;
            yield return null;
        }

        transform.position = toPos;

        if (matchWorldRotationAndScale)
        {
            transform.rotation = toRot;
            transform.localScale = toLocalScale;
        }

        moveCoroutine = null;
    }
}
