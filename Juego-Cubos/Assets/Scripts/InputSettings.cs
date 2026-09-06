using UnityEngine;
using UnityEngine.InputSystem;

public static class InputSettings
{
    private const string BindingsKey =
        "PlayerInputBindings";


    // =========================================================
    // GUARDAR CONTROLES
    // =========================================================

    public static void SaveBindings(
        PlayerControls controls
    )
    {
        string bindings =
            controls.asset.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(
            BindingsKey,
            bindings
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Controles guardados correctamente."
        );
    }


    // =========================================================
    // CARGAR CONTROLES
    // =========================================================

    public static void LoadBindings(
        PlayerControls controls
    )
    {
        if (!PlayerPrefs.HasKey(
            BindingsKey
        ))
        {
            return;
        }

        string bindings =
            PlayerPrefs.GetString(
                BindingsKey
            );

        controls.asset.LoadBindingOverridesFromJson(
            bindings
        );

        Debug.Log(
            "Controles personalizados cargados."
        );
    }


    // =========================================================
    // RESTABLECER CONTROLES
    // =========================================================

    public static void ResetBindings(
        PlayerControls controls
    )
    {
        controls.asset.RemoveAllBindingOverrides();

        PlayerPrefs.DeleteKey(
            BindingsKey
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Controles restablecidos."
        );
    }
}