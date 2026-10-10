using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CannonAmmoUI : MonoBehaviour
{
    public enum AmmoDisplayMode
    {
        Pips,
        Text
    }

    [SerializeField] Cannon cannon;
    [SerializeField] AmmoDisplayMode displayMode = AmmoDisplayMode.Text;
    [SerializeField] Transform pipContainer;
    [SerializeField] Image pipPrefab;
    [SerializeField] GameObject pipsRoot;
    [SerializeField] GameObject textRoot;
    [SerializeField] TMP_Text amountText;
    [SerializeField] GameObject reloadRoot;
    [SerializeField] Image reloadFill;
    [SerializeField] Color pipActiveColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] Color pipSpentColor = new Color(1f, 1f, 1f, 0.45f);

    readonly List<Image> _pips = new List<Image>();
    Camera _camera;
    bool _wasReloading;
    int _lastAmmo = -1;
    int _lastMagazineSize = -1;
    AmmoDisplayMode _lastDisplayMode;

    void Awake()
    {
        if (cannon == null)
            cannon = GetComponentInParent<Cannon>();

        if (pipPrefab != null)
            pipPrefab.gameObject.SetActive(false);

        _camera = Camera.main;
        _lastDisplayMode = displayMode;
    }

    void Start()
    {
        if (cannon == null)
            return;

        RefreshAmmoDisplay(force: true);
        SetReloadingVisual(cannon.IsReloading);
        _wasReloading = cannon.IsReloading;
    }

    void LateUpdate()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_camera != null)
            transform.rotation = _camera.transform.rotation;

        if (cannon == null)
            return;

        if (displayMode != _lastDisplayMode)
        {
            _lastDisplayMode = displayMode;
            RefreshAmmoDisplay(force: true);
            if (!_wasReloading)
                SetReloadingVisual(false);
        }

        bool reloading = cannon.IsReloading;
        if (reloading != _wasReloading)
        {
            _wasReloading = reloading;
            SetReloadingVisual(reloading);
        }

        if (reloading)
        {
            if (reloadFill != null)
                reloadFill.fillAmount = cannon.ReloadProgress;
            return;
        }

        RefreshAmmoDisplay(force: false);
    }

    void RefreshAmmoDisplay(bool force)
    {
        int ammo = cannon.CurrentAmmo;
        int magazineSize = cannon.MagazineSize;

        if (!force && ammo == _lastAmmo && magazineSize == _lastMagazineSize)
            return;

        _lastAmmo = ammo;
        _lastMagazineSize = magazineSize;

        if (displayMode == AmmoDisplayMode.Pips)
            SyncAndApplyPips(ammo, magazineSize);
        else
            ApplyAmountText(ammo, magazineSize);
    }

    void SyncAndApplyPips(int ammo, int magazineSize)
    {
        if (pipContainer == null || pipPrefab == null)
            return;

        while (_pips.Count < magazineSize)
        {
            Image pip = Instantiate(pipPrefab, pipContainer);
            pip.gameObject.SetActive(true);
            pip.name = $"Pip_{_pips.Count}";
            _pips.Add(pip);
        }

        for (int i = 0; i < _pips.Count; i++)
        {
            Image pip = _pips[i];
            if (pip == null)
                continue;

            bool inMagazine = i < magazineSize;
            pip.gameObject.SetActive(inMagazine);
            if (inMagazine)
                pip.color = i < ammo ? pipActiveColor : pipSpentColor;
        }
    }

    void ApplyAmountText(int ammo, int magazineSize)
    {
        if (amountText == null)
            return;

        amountText.text = $"{ammo}/{magazineSize}";
    }

    void SetReloadingVisual(bool reloading)
    {
        bool showAmmo = !reloading;

        if (pipsRoot != null)
            pipsRoot.SetActive(showAmmo && displayMode == AmmoDisplayMode.Pips);
        if (textRoot != null)
            textRoot.SetActive(showAmmo && displayMode == AmmoDisplayMode.Text);
        if (reloadRoot != null)
            reloadRoot.SetActive(reloading);

        if (reloading && reloadFill != null)
            reloadFill.fillAmount = 0f;

        if (!reloading)
            RefreshAmmoDisplay(force: true);
    }
}
