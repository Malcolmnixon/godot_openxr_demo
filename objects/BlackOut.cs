using Godot;

namespace GodotOpenXRDemo.objects;

[Tool]
public partial class BlackOut : Node3D
{
    /// <summary>
    /// Fade value between 0 and 1.
    /// </summary>
    private float _fade;

    /// <summary>
    /// Mesh instance
    /// </summary>
    private MeshInstance3D _meshInstance3D;

    /// <summary>
    /// Fade shader material
    /// </summary>
    private ShaderMaterial _material;

    /// <summary>
    /// Fade exported property
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.1")]
    public float Fade 
    {
        get => _fade;
        set => SetFade(value);
    }

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _Ready()
    {
        // Get the mesh instance and shader material
        _meshInstance3D = GetNode<MeshInstance3D>("MeshInstance3D");
        _material = (ShaderMaterial)_meshInstance3D.MaterialOverride;

        // Update the fade
        _UpdateFade();
    }

    /// <summary>
    /// Called when the fade property is changed.
    /// </summary>
    /// <param name="newFade">New fade value</param>
    private void SetFade(float newFade)
    {
        // Save the new fade value
        _fade = newFade;

        // Update the fade if inside the scene tree
        if (IsInsideTree())
            _UpdateFade();
    }

    /// <summary>
    /// Update the fade value.
    /// </summary>
    private void _UpdateFade()
    {
        if (_fade <= 0.0F)
        {
            _meshInstance3D.Visible = false;
        }
        else
        {
            _material?.SetShaderParameter("albedo", new Color(0.0F, 0.0F, 0.0F, _fade));
            _meshInstance3D.Visible = true;
        }
    }
}