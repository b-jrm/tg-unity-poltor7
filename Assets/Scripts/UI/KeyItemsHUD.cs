
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyItemsHUD : MonoBehaviour
{
    public static KeyItemsHUD Instance { get; private set; }

    [Header("Referencias del HUD")]
    [SerializeField] private Transform keyItemsContainer;
    [SerializeField] private GameObject keyItemIconPrefab;

    // Identificadores de los objetos que ya se muestran.
    private readonly HashSet<string> displayedItems =
        new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Hay más de un KeyItemsHUD en la escena.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Muestra un icono cuando el inventario registra un objeto clave.
    /// Llama este método solo para objetos que deban aparecer en KeyItems.
    /// </summary>
    public void ShowKeyItem(ItemData item)
    {
        Debug.Log($"KeyItemsHUB::ShowKeyItem(ItemData item: {item})");
        if (item == null)
        {
            Debug.LogWarning("KeyItemsHUD recibió un ItemData nulo.");
            return;
        }

        if (keyItemsContainer == null ||
            keyItemIconPrefab == null)
        {
            Debug.LogError(
                "Asigna KeyItems Container y Key Item Icon Prefab " +
                "en el Inspector."
            );
            return;
        }

        if (item.icon == null)
        {
            Debug.LogWarning(
                "El objeto " + item.displayName +
                " no tiene un icono asignado en su ItemData."
            );
            return;
        }

        string itemId = item.id;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogWarning(
                "El ItemData no tiene un ID válido."
            );
            return;
        }

        // Evita duplicados aunque se recoja varias veces.
        if (!displayedItems.Add(itemId))
            return;

        GameObject iconObject = Instantiate(
            keyItemIconPrefab,
            keyItemsContainer
        );

        iconObject.name = "KeyItem_" + itemId;

        Image image = iconObject.GetComponent<Image>();

        if (image == null)
            image = iconObject.GetComponentInChildren<Image>();

        if (image == null)
        {
            Debug.LogError(
                "El prefab de Key Item necesita un componente Image."
            );

            displayedItems.Remove(itemId);
            Destroy(iconObject);
            return;
        }

        image.sprite = item.icon;
        image.preserveAspect = true;

        // Ayuda de accesibilidad y depuración.
        image.raycastTarget = false;

        Debug.Log($"KeyItemsHUB::ShowKeyItem() => image: {image}");
        Debug.Log($"KeyItemsHUB::ShowKeyItem() => iconObject: {iconObject}");

        iconObject.SetActive(true);
    }

    public void RemoveKeyItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        // Quitar el ID del conjunto de objetos visibles.
        displayedItems.Remove(itemId);

        if (keyItemsContainer == null)
        {
            //Debug.LogWarning(
            //    "KeyItemsHUD: keyItemsContainer no está asignado."
            //);
            return;
        }

        // Buscar únicamente el icono del objeto indicado.
        string iconName = "KeyItem_" + itemId;

        for (int i = keyItemsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = keyItemsContainer.GetChild(i);

            if (child.name == iconName)
            {
                Destroy(child.gameObject);

                //Debug.Log(
                //    $"KeyItemsHUD: icono eliminado: {iconName}"
                //);

                return;
            }
        }

        //Debug.Log(
        //    $"KeyItemsHUD: no se encontró el icono {iconName}."
        //);
    }

    /// <summary>
    /// Limpia los iconos del HUD.
    /// No elimina los objetos del inventario.
    /// </summary>
    public void ClearKeyItemsHUD()
    {
        displayedItems.Clear();

        for (int i = keyItemsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(keyItemsContainer.GetChild(i).gameObject);
        }
    }
}
