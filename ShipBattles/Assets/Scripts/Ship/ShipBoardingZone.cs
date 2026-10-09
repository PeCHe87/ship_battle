using UnityEngine;

public class ShipBoardingZone : MonoBehaviour
{
    public enum Kind
    {
        Enter,
        Exit
    }

    [SerializeField] Kind kind;

    ShipIdentity _ship;

    public Kind ZoneKind => kind;
    public ShipIdentity Ship => _ship;

    void Awake()
    {
        _ship = GetComponentInParent<ShipIdentity>();
    }
}
