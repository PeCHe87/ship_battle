using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Move force")]
    [SerializeField] float baseMoveForce = 20f;
    [SerializeField] float moveForceIncrement = 40f;
    [SerializeField] float maxMoveForce = 80f;
    [SerializeField] float moveForceDecrement = 60f;

    [Header("Turn speed (degrees/sec)")]
    [SerializeField] float baseTurnSpeed = 40f;
    [SerializeField] float turnSpeedIncrement = 60f;
    [SerializeField] float maxTurnSpeed = 120f;
    [SerializeField] float turnSpeedDecrement = 80f;

    [Header("Thruster particles")]
    [SerializeField] ParticleSystem forwardParticles;
    [SerializeField] ParticleSystem rotateLeftParticles;
    [SerializeField] ParticleSystem rotateRightParticles;

    [Header("Thruster audio")]
    [SerializeField, Tooltip("Loop played while any move action (forward/back/left/right) is active. One source per ship.")]
    AudioClip propulsionSfx;

    bool _moveForward;
    bool _moveBackward;
    bool _rotateLeft;
    bool _rotateRight;

    float _currentMoveForce;
    float _lastMoveDirection;
    float _currentTurnSpeed;
    float _lastTurnDirection;

    Rigidbody _body;
    AudioSource _propulsionSource;

    bool AnyMoveActionActive =>
        _moveForward || _moveBackward || _rotateLeft || _rotateRight;

    public void StartMoveForward()
    {
        _moveForward = true;
        SetParticlesPlaying(forwardParticles, true);
        RefreshPropulsionSfx();
    }

    public void StopMoveForward()
    {
        _moveForward = false;
        SetParticlesPlaying(forwardParticles, false);
        RefreshPropulsionSfx();
    }

    public void StartMoveBackward()
    {
        _moveBackward = true;
        RefreshPropulsionSfx();
    }

    public void StopMoveBackward()
    {
        _moveBackward = false;
        RefreshPropulsionSfx();
    }

    public void StartRotateLeft()
    {
        _rotateLeft = true;
        SetParticlesPlaying(rotateLeftParticles, true);
        RefreshPropulsionSfx();
    }

    public void StopRotateLeft()
    {
        _rotateLeft = false;
        SetParticlesPlaying(rotateLeftParticles, false);
        RefreshPropulsionSfx();
    }

    public void StartRotateRight()
    {
        _rotateRight = true;
        SetParticlesPlaying(rotateRightParticles, true);
        RefreshPropulsionSfx();
    }

    public void StopRotateRight()
    {
        _rotateRight = false;
        SetParticlesPlaying(rotateRightParticles, false);
        RefreshPropulsionSfx();
    }

    public void StopAll()
    {
        _moveForward = false;
        _moveBackward = false;
        _rotateLeft = false;
        _rotateRight = false;
        _currentMoveForce = 0f;
        _lastMoveDirection = 0f;
        _currentTurnSpeed = 0f;
        _lastTurnDirection = 0f;
        SetParticlesPlaying(forwardParticles, false);
        SetParticlesPlaying(rotateLeftParticles, false);
        SetParticlesPlaying(rotateRightParticles, false);
        RefreshPropulsionSfx();
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.centerOfMass = Vector3.zero;
        SetupPropulsionSource();
        SetParticlesPlaying(forwardParticles, false);
        SetParticlesPlaying(rotateLeftParticles, false);
        SetParticlesPlaying(rotateRightParticles, false);
    }

    void OnDisable()
    {
        if (_propulsionSource != null && _propulsionSource.isPlaying)
            _propulsionSource.Stop();
    }

    void SetupPropulsionSource()
    {
        if (propulsionSfx == null)
            return;

        _propulsionSource = gameObject.AddComponent<AudioSource>();
        _propulsionSource.clip = propulsionSfx;
        _propulsionSource.loop = true;
        _propulsionSource.playOnAwake = false;
        _propulsionSource.spatialBlend = 1f;
        _propulsionSource.outputAudioMixerGroup = GameAudio.SfxGroup;
    }

    void RefreshPropulsionSfx()
    {
        if (_propulsionSource == null)
            return;

        if (AnyMoveActionActive)
        {
            if (!_propulsionSource.isPlaying)
                _propulsionSource.Play();
        }
        else if (_propulsionSource.isPlaying)
        {
            _propulsionSource.Stop();
        }
    }

    static void SetParticlesPlaying(ParticleSystem particles, bool playing)
    {
        if (particles == null) return;

        if (playing)
        {
            if (!particles.isPlaying)
                particles.Play();
        }
        else if (particles.isPlaying)
        {
            particles.Stop();
        }
    }

    void FixedUpdate()
    {
        float moveNet = 0f;
        if (_moveForward) moveNet += 1f;
        if (_moveBackward) moveNet -= 1f;

        float turnNet = 0f;
        if (_rotateRight) turnNet += 1f;
        if (_rotateLeft) turnNet -= 1f;

        if (moveNet != 0f)
        {
            _lastMoveDirection = moveNet;

            if (_currentMoveForce <= 0f)
                _currentMoveForce = baseMoveForce;
            else
                _currentMoveForce = Mathf.Min(
                    maxMoveForce,
                    _currentMoveForce + moveForceIncrement * Time.fixedDeltaTime);
        }
        else if (_currentMoveForce > 0f)
        {
            _currentMoveForce = Mathf.Max(
                0f,
                _currentMoveForce - moveForceDecrement * Time.fixedDeltaTime);

            if (_currentMoveForce <= 0f)
                _lastMoveDirection = 0f;
        }

        if (_currentMoveForce > 0f && _lastMoveDirection != 0f)
            _body.AddForce(transform.forward * (_lastMoveDirection * _currentMoveForce), ForceMode.Force);

        if (turnNet != 0f)
        {
            _lastTurnDirection = turnNet;

            if (_currentTurnSpeed <= 0f)
                _currentTurnSpeed = baseTurnSpeed;
            else
                _currentTurnSpeed = Mathf.Min(
                    maxTurnSpeed,
                    _currentTurnSpeed + turnSpeedIncrement * Time.fixedDeltaTime);
        }
        else if (_currentTurnSpeed > 0f)
        {
            _currentTurnSpeed = Mathf.Max(
                0f,
                _currentTurnSpeed - turnSpeedDecrement * Time.fixedDeltaTime);

            if (_currentTurnSpeed <= 0f)
                _lastTurnDirection = 0f;
        }

        if (_currentTurnSpeed > 0f && _lastTurnDirection != 0f)
        {
            float yawDelta = _lastTurnDirection * _currentTurnSpeed * Time.fixedDeltaTime;
            _body.MoveRotation(Quaternion.Euler(0f, yawDelta, 0f) * _body.rotation);

            // Pure spin only when not thrusting or coasting thrust.
            if (moveNet == 0f && _currentMoveForce <= 0f)
                _body.linearVelocity = Vector3.zero;
        }

        Vector3 velocity = _body.linearVelocity;
        velocity.y = 0f;
        _body.linearVelocity = velocity;
        _body.angularVelocity = Vector3.zero;
    }
}
