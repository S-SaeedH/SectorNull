using System.Collections;
using UnityEngine;

public class SpareEndingController : MonoBehaviour
{
    [Header("Objects")]
    public Transform cutscenePlayer;
    public Transform headCamera;

    [Header("Movement Timing")]
    public float waitBeforeMove = 3f;
    public float moveDuration = 8f;
    public float lookUpDuration = 4.5f;

    [Header("Movement")]
    public float moveDistance = 5f;

    [Header("Look Up")]
    public float lookUpAngle = -35f;

    public void PlaySpareEndingMovement()
    {
        StopAllCoroutines();
        StartCoroutine(SpareEndingRoutine());
    }

    private IEnumerator SpareEndingRoutine()
    {
        if (cutscenePlayer == null || headCamera == null)
            yield break;

        Vector3 startPosition = cutscenePlayer.position;

        Vector3 forwardDirection = cutscenePlayer.forward;
        forwardDirection.y = 0f;
        forwardDirection.Normalize();

        Vector3 endPosition = startPosition + forwardDirection * moveDistance;

        Quaternion startHeadRotation = headCamera.localRotation;
        Quaternion lookUpRotation = startHeadRotation * Quaternion.Euler(lookUpAngle, 0f, 0f);

        yield return new WaitForSeconds(waitBeforeMove);

        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);

            cutscenePlayer.position = Vector3.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        cutscenePlayer.position = endPosition;

        timer = 0f;

        while (timer < lookUpDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / lookUpDuration);

            headCamera.localRotation = Quaternion.Slerp(startHeadRotation, lookUpRotation, t);

            yield return null;
        }

        headCamera.localRotation = lookUpRotation;
    }
}