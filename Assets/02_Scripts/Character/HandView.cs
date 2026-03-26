using UnityEngine;

public enum EDominantHand
{
    Undefined,
    Left,
    Right,
    Random,
}

public class HandView : MonoBehaviour
{
    public HandWritingLoopController WritingLoopController;
    public HandPinchController PinchController;
    public Transform ValidatingTarget;
    public GameObject Pencil;

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void ShowPencil() { if (Pencil != null) Pencil.SetActive(true); }
    public void HidePencil() { if (Pencil != null) Pencil.SetActive(false); }

    public void MoveTowardTarget(float speed)
    {
        if (ValidatingTarget == null) return;
        transform.position = Vector3.MoveTowards(
            transform.position,
            ValidatingTarget.position,
            speed * Time.deltaTime);
    }
}
