using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2Dマップ上を自由に移動・ズームできるカメラコントローラー
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("カメラの移動速度")]
    private float _panSpeed = 10f;

    [Header("Zoom Settings")]
    [SerializeField, Tooltip("カメラのズーム速度")]
    private float _zoomSpeed = 15f;
    [SerializeField] private float _minZoomSize = 2f;
    [SerializeField] private float _maxZoomSize = 15f;

    [Header("Map Bounds")]
    [SerializeField] private BoxCollider2D _boundsCollider;

    private Camera _camera;
    private InputAction _rightClickAction;
    private InputAction _mouseDeltaAction;
    private InputAction _scrollAction;

    private float _fixedZPosition;

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        Vector3 initPos = transform.position;
        if (initPos.z >= 0f)
        {
            initPos.z = -10f;
            transform.position = initPos;
            Debug.Log("[Camera] Z座標が0以上だったため、エラー防止のため自動的に -10 に補正しました。");
        }

        _rightClickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/rightButton");
        _mouseDeltaAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/delta");
        _scrollAction = new InputAction(type: InputActionType.Value, binding: "<Mouse>/scroll");
    }

    private void Start()
    {
        _fixedZPosition = transform.position.z;
    }

    private void OnEnable()
    {
        _rightClickAction.Enable();
        _mouseDeltaAction.Enable();
        _scrollAction.Enable();
    }

    private void OnDisable()
    {
        _rightClickAction.Disable();
        _mouseDeltaAction.Disable();
        _scrollAction.Disable();
    }

    private void LateUpdate()
    {
        EnsureMapBoundsFound();

        HandlePan(); 
        HandleZoom();
        ClampCameraToBounds();
    }

    private void EnsureMapBoundsFound()
    {
        if (_boundsCollider != null) return;

        GameObject boundsObj = GameObject.Find("MapBounds"); 
        if (boundsObj != null)
        {
            _boundsCollider = boundsObj.GetComponent<BoxCollider2D>();
            if (_boundsCollider != null)
            {
                Debug.Log("<color=green>[Camera] MapBounds の自動取得に成功しました！</color>");
            }
        }
    }

    private void HandlePan()
    {
        if (!_rightClickAction.IsPressed()) return;

        Vector2 mouseDelta = _mouseDeltaAction.ReadValue<Vector2>();
        if (mouseDelta == Vector2.zero) return;

        Vector3 panDirection = new Vector3(-mouseDelta.x, -mouseDelta.y, 0f);
        float currentZoomMultiplier = _camera.orthographicSize / 5f; 
        transform.position += panDirection * (_panSpeed * currentZoomMultiplier * 0.005f * Time.deltaTime);
    }

    private void HandleZoom()
    {
        float scrollValue = _scrollAction.ReadValue<Vector2>().y;
        if (Mathf.Approximately(scrollValue, 0f)) return;

        float scrollDirection = Mathf.Sign(scrollValue);
        _camera.orthographicSize -= scrollDirection * _zoomSpeed * Time.deltaTime;
    }

    private void ClampCameraToBounds()
    {
        if (_boundsCollider == null)
        {
            Vector3 pos = transform.position;
            pos.z = _fixedZPosition;
            transform.position = pos;
            return;
        }

        Bounds bounds = _boundsCollider.bounds;

        float maxVertSize = bounds.size.y / 2f;
        float maxHoriSize = (bounds.size.x / 2f) / _camera.aspect;

        float absoluteMaxZoom = Mathf.Min(maxVertSize, maxHoriSize);

        float dynamicMaxZoom = Mathf.Min(_maxZoomSize, absoluteMaxZoom);
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, _minZoomSize, dynamicMaxZoom);

        float camHalfHeight = _camera.orthographicSize;
        float camHalfWidth = camHalfHeight * _camera.aspect;

        Vector3 clampedPosition = transform.position;
        float minX = bounds.min.x + camHalfWidth;
        float maxX = bounds.max.x - camHalfWidth;
        float minY = bounds.min.y + camHalfHeight;
        float maxY = bounds.max.y - camHalfHeight;

        if (minX > maxX) clampedPosition.x = bounds.center.x;
        else clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);

        if (minY > maxY) clampedPosition.y = bounds.center.y;
        else clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);

        clampedPosition.z = _fixedZPosition;
        transform.position = clampedPosition;
    }
}