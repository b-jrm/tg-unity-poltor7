using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class WeaponHitscan : MonoBehaviour
{
    [Header("Identidad")]
    public string weaponItemId; // ejemplo: "weapon_p7" (vacio = siempre disponible)
    public string ammoItemId; // vacio = melee o sin municion (llave inglesa)
    [Header("Estadisticas")]
    public float damage = 18f;
    public float range = 60f;
    public float fireRate = 0.3f;
    public int magSize = 12;
    public float reloadTime = 1.4f;
    public bool automatic;
    public int pellets = 1; // escopeta = 8
    public float spread = 0f; // escopeta = 0.06
    public LayerMask hitMask = ~0; // excluir la capa Player
    [Header("Efectos")]
    public AudioSource audioSource;
    public AudioClip shotSfx;
    public ParticleSystem muzzle;
    public GameObject hitFx;
    int mag;
    float nextFire;
    bool reloading;
    Camera cam;
    PlayerInventory inv;
    public bool UsesAmmo => !string.IsNullOrEmpty(ammoItemId);
    public int Mag => mag;
    void Awake()
    {
        cam = Camera.main;
        inv = GetComponentInParent<PlayerInventory>();
        mag = magSize;
    }
    void OnDisable() { reloading = false; }
    void Update()
    {
        if (!PlayerController.InputEnabled || reloading) return;
        var mouse = Mouse.current;
        bool wantsFire = automatic ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame;
        if (wantsFire && Time.time >= nextFire) Fire();
        if (Keyboard.current.rKey.wasPressedThisFrame && UsesAmmo && mag < magSize)
            StartCoroutine(Reload());
    }
    void Fire()
    {
        if (UsesAmmo)
        {
            if (mag <= 0) { StartCoroutine(Reload()); return; }
            mag--;
        }
        nextFire = Time.time + fireRate;
        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = cam.transform.forward
            + cam.transform.right * Random.Range(-spread, spread)
            + cam.transform.up * Random.Range(-spread, spread);
            if (Physics.Raycast(cam.transform.position, dir.normalized, out var hit, range,
            hitMask, QueryTriggerInteraction.Ignore))
            {
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null) h.TakeDamage(damage);
                if (hitFx != null)
                    Destroy(Instantiate(hitFx, hit.point, Quaternion.LookRotation(hit.normal)), 2f);
            }
        }
        if (muzzle != null) muzzle.Play();
        if (audioSource != null && shotSfx != null) audioSource.PlayOneShot(shotSfx);
    }
    IEnumerator Reload()
    {
        int need = magSize - mag;
        int available = inv.Count(ammoItemId);
        if (need <= 0 || available <= 0) yield break;
        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        int taken = Mathf.Min(need, available);
        inv.Remove(ammoItemId, taken);
        mag += taken;
        reloading = false;
    }
}
