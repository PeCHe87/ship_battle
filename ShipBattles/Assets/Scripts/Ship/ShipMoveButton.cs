using UnityEngine;

public enum ShipMoveButtonAction
{
    MoveForward,
    MoveBackward,
    RotateLeft,
    RotateRight,
}

public class ShipMoveButton : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    const float ZoneHeightScale = 0.01f;

    [SerializeField] ShipMoveButtonAction actionType = ShipMoveButtonAction.MoveForward;
    [SerializeField] ShipController ship;
    [SerializeField, Tooltip("Horizontal distance the player must be within to use this button.")]
    float interactRadius = 2.5f;
    [SerializeField] Transform zoneVisual;
    [SerializeField] MeshRenderer zoneRenderer;
    [SerializeField] Color ringIdleColor = new Color(1f, 0.55f, 0.1f, 0.22f);
    [SerializeField] Color ringReadyColor = new Color(1f, 0.75f, 0.2f, 0.45f);
    [SerializeField] Color ringActiveColor = new Color(1f, 0.85f, 0.25f, 0.65f);

    MaterialPropertyBlock _propertyBlock;
    bool _playerInRange;
    bool _isOn;

    void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();

        if (ship == null)
            ship = GetComponentInParent<ShipController>();

        SyncZoneScale();
        RefreshVisuals();
    }

    void OnDisable()
    {
        TurnOff();
    }

    void OnDestroy()
    {
        TurnOff();
    }

    void Update()
    {
        TopDownPlayerController player =
            TopDownPlayerController.FindNearestInRange(transform.position, interactRadius);
        bool inRange = player != null;
        if (inRange != _playerInRange)
        {
            _playerInRange = inRange;
            RefreshVisuals();
        }

        if (player == null || !player.WasInteractPressed())
            return;

        if (_isOn)
            TurnOff();
        else
            TurnOn();
    }

    void TurnOn()
    {
        if (_isOn || ship == null)
            return;

        _isOn = true;
        BeginShipAction();
        RefreshVisuals();
    }

    void TurnOff()
    {
        if (!_isOn)
            return;

        _isOn = false;
        EndShipAction();
        RefreshVisuals();
    }

    void BeginShipAction()
    {
        switch (actionType)
        {
            case ShipMoveButtonAction.MoveForward:
                ship.StartMoveForward();
                break;
            case ShipMoveButtonAction.MoveBackward:
                ship.StartMoveBackward();
                break;
            case ShipMoveButtonAction.RotateLeft:
                ship.StartRotateLeft();
                break;
            case ShipMoveButtonAction.RotateRight:
                ship.StartRotateRight();
                break;
        }
    }

    void EndShipAction()
    {
        if (ship == null)
            return;

        switch (actionType)
        {
            case ShipMoveButtonAction.MoveForward:
                ship.StopMoveForward();
                break;
            case ShipMoveButtonAction.MoveBackward:
                ship.StopMoveBackward();
                break;
            case ShipMoveButtonAction.RotateLeft:
                ship.StopRotateLeft();
                break;
            case ShipMoveButtonAction.RotateRight:
                ship.StopRotateRight();
                break;
        }
    }

    void SyncZoneScale()
    {
        if (zoneVisual == null)
            return;

        // Default cylinder radius is 0.5, so scale xz = diameter yields world radius = interactRadius.
        float diameter = interactRadius * 2f;
        zoneVisual.localScale = new Vector3(diameter, ZoneHeightScale, diameter);
    }

    void RefreshVisuals()
    {
        if (zoneRenderer == null)
            return;

        Color ringColor = ringIdleColor;
        if (_isOn)
            ringColor = ringActiveColor;
        else if (_playerInRange)
            ringColor = ringReadyColor;

        zoneRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, ringColor);
        _propertyBlock.SetColor(ColorId, ringColor);
        zoneRenderer.SetPropertyBlock(_propertyBlock);
    }

    void OnValidate()
    {
        SyncZoneScale();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
