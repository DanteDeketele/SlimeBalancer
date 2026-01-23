using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SlimeCollider : MonoBehaviour
{
  [SerializeField] private DecalProjector _shadowTransform;
  public enum SlimeColor
  {
    Green,
    Blue,
    Red,
    Yellow

  }
    public SlimeColor slimeColor;

    void Start()
    {
        if (_shadowTransform != null)
        {
          _shadowTransform.size = transform.localScale;
          _shadowTransform.size = new Vector3(_shadowTransform.size.x, _shadowTransform.size.y, 10f);
          _shadowTransform.pivot = new Vector3(0f, 0f, 5f);
        }
    }

    void LateUpdate()
    {
      if (_shadowTransform != null)
      {
        _shadowTransform.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
      }
    }
}
