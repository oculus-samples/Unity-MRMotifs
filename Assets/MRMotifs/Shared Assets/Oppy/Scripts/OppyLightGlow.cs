// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class OppyLightGlow : MonoBehaviour
{
    private const string EmissionColorShaderPropertyName = "_EmissionColor";
    [SerializeField] private Material oppyMaterial;
    private readonly Color _glowColor = new(1, 0.8f, 0.3f);
    private readonly Color _noGlowColor = Color.black;

    public void SetGlowActive(bool active)
    {
        if (!active)
            oppyMaterial.SetColor(EmissionColorShaderPropertyName, _noGlowColor);
        else
            oppyMaterial.SetColor(EmissionColorShaderPropertyName, _glowColor);
    }
}
