using Godot;
using Godot.Collections;

namespace StarterGame2;

/// <summary>
/// First-person city builder system. Place/remove structures on a GridMap.
/// Converted from Kenney's builder.gd, adapted for first-person perspective.
/// Active only when GameManager is in Build mode.
/// </summary>
public partial class Builder : Node3D
{
    [Export] public Array<StructureResource> Structures { get; set; } = new();

    [Export] public Node3D? Selector { get; set; }
    [Export] public Node3D? SelectorContainer { get; set; }
    [Export] public Camera3D? ViewCamera { get; set; }
    [Export] public GridMap? Gridmap { get; set; }
    [Export] public Label? CashDisplay { get; set; }

    private DataMap _map = new();
    private int _index = 0;
    private Plane _plane;

    private AudioManager _audio = null!;
    private GameManager _gameManager = null!;

    public override void _Ready()
    {
        _audio = GetNode<AudioManager>("/root/AudioManager");
        _gameManager = GetNode<GameManager>("/root/GameManager");

        _map = new DataMap();
        _plane = new Plane(Vector3.Up, 0f);

        // Create MeshLibrary dynamically from structure resources
        var meshLibrary = new MeshLibrary();

        foreach (var structure in Structures)
        {
            int id = meshLibrary.GetLastUnusedItemId();
            meshLibrary.CreateItem(id);

            var mesh = GetMeshFromScene(structure.Model);
            if (mesh != null)
            {
                meshLibrary.SetItemMesh(id, mesh);
                meshLibrary.SetItemMeshTransform(id, Transform3D.Identity);
            }
        }

        if (Gridmap != null)
            Gridmap.MeshLibrary = meshLibrary;

        UpdateStructure();
        UpdateCash();

        // Listen for mode changes to show/hide build UI
        _gameManager.ModeChanged += OnModeChanged;
        SetBuildVisible(false);
    }

    public override void _Process(double delta)
    {
        if (!_gameManager.IsBuildMode) return;
        if (ViewCamera == null || Selector == null || Gridmap == null) return;

        ActionRotate();
        ActionStructureToggle();
        ActionSave();
        ActionLoad();

        // Raycast from center of screen in first-person mode
        var viewport = GetViewport();
        var mousePos = viewport.GetMousePosition();

        var from = ViewCamera.ProjectRayOrigin(mousePos);
        var dir = ViewCamera.ProjectRayNormal(mousePos);

        var intersection = _plane.IntersectsRay(from, dir);
        if (intersection.HasValue)
        {
            var gridmapPos = new Vector3(
                Mathf.Round(intersection.Value.X),
                0f,
                Mathf.Round(intersection.Value.Z));

            Selector.Position = Selector.Position.Lerp(gridmapPos, Mathf.Min((float)delta * 40f, 1f));

            ActionBuild(new Vector3I((int)gridmapPos.X, 0, (int)gridmapPos.Z));
            ActionDemolish(new Vector3I((int)gridmapPos.X, 0, (int)gridmapPos.Z));
        }
    }

    private Mesh? GetMeshFromScene(PackedScene? packedScene)
    {
        if (packedScene == null) return null;

        var sceneState = packedScene.GetState();
        for (int i = 0; i < sceneState.GetNodeCount(); i++)
        {
            if (sceneState.GetNodeType(i) == "MeshInstance3D")
            {
                for (int j = 0; j < sceneState.GetNodePropertyCount(i); j++)
                {
                    if (sceneState.GetNodePropertyName(i, j) == "mesh")
                    {
                        var value = sceneState.GetNodePropertyValue(i, j);
                        if (value.Obj is Mesh mesh)
                            return (Mesh)mesh.Duplicate();
                    }
                }
            }
        }
        return null;
    }

    private void ActionBuild(Vector3I gridmapPos)
    {
        if (!Input.IsActionJustPressed("build")) return;
        if (Gridmap == null) return;

        int previousTile = Gridmap.GetCellItem(gridmapPos);
        Gridmap.SetCellItem(gridmapPos, _index,
            Gridmap.GetOrthogonalIndexFromBasis(Selector!.Basis));

        if (previousTile != _index)
        {
            _map.Cash -= Structures[_index].Price;
            UpdateCash();
            _audio.Play("sounds/builder/placement-a.ogg, sounds/builder/placement-b.ogg, sounds/builder/placement-c.ogg, sounds/builder/placement-d.ogg", -20);
        }
    }

    private void ActionDemolish(Vector3I gridmapPos)
    {
        if (!Input.IsActionJustPressed("demolish")) return;
        if (Gridmap == null) return;

        if (Gridmap.GetCellItem(gridmapPos) != -1)
        {
            Gridmap.SetCellItem(gridmapPos, -1);
            _audio.Play("sounds/builder/removal-a.ogg, sounds/builder/removal-b.ogg, sounds/builder/removal-c.ogg, sounds/builder/removal-d.ogg", -20);
        }
    }

    private void ActionRotate()
    {
        if (Input.IsActionJustPressed("rotate_structure") && Selector != null)
        {
            Selector.RotateY(Mathf.DegToRad(90));
            _audio.Play("sounds/builder/rotate.ogg", -30);
        }
    }

    private void ActionStructureToggle()
    {
        if (Input.IsActionJustPressed("structure_next"))
        {
            _index = (int)Mathf.Wrap(_index + 1, 0, Structures.Count);
            _audio.Play("sounds/builder/toggle.ogg", -30);
        }
        if (Input.IsActionJustPressed("structure_previous"))
        {
            _index = (int)Mathf.Wrap(_index - 1, 0, Structures.Count);
            _audio.Play("sounds/builder/toggle.ogg", -30);
        }
        UpdateStructure();
    }

    private void UpdateStructure()
    {
        if (SelectorContainer == null || Structures.Count == 0) return;

        // Clear previous preview
        foreach (var child in SelectorContainer.GetChildren())
        {
            SelectorContainer.RemoveChild(child);
            child.QueueFree();
        }

        // Create new preview
        var model = Structures[_index].Model?.Instantiate<Node3D>();
        if (model != null)
        {
            SelectorContainer.AddChild(model);
            model.Position = new Vector3(0, 0.25f, 0);
        }
    }

    private void UpdateCash()
    {
        if (CashDisplay != null)
            CashDisplay.Text = "$" + _map.Cash;

        var gm = GetNode<GameManager>("/root/GameManager");
        gm.Cash = _map.Cash;
    }

    private void ActionSave()
    {
        if (!Input.IsActionJustPressed("save")) return;
        GD.Print("Saving map...");

        _map.Structures.Clear();
        if (Gridmap == null) return;

        foreach (var cell in Gridmap.GetUsedCells())
        {
            var data = new DataStructure
            {
                Position = new Vector2I(cell.X, cell.Z),
                Orientation = Gridmap.GetCellItemOrientation(cell),
                Structure = Gridmap.GetCellItem(cell)
            };
            _map.Structures.Add(data);
        }

        ResourceSaver.Save(_map, "user://map.res");
    }

    private void ActionLoad()
    {
        if (!Input.IsActionJustPressed("load")) return;
        GD.Print("Loading map...");

        if (Gridmap == null) return;
        Gridmap.Clear();

        var loaded = ResourceLoader.Load<DataMap>("user://map.res");
        _map = loaded ?? new DataMap();

        foreach (var cell in _map.Structures)
        {
            Gridmap.SetCellItem(
                new Vector3I(cell.Position.X, 0, cell.Position.Y),
                cell.Structure,
                cell.Orientation);
        }

        UpdateCash();
    }

    private void OnModeChanged(int mode)
    {
        SetBuildVisible((GameManager.GameMode)mode == GameManager.GameMode.Build);
    }

    private void SetBuildVisible(bool visible)
    {
        if (Selector != null) Selector.Visible = visible;
        if (CashDisplay != null) CashDisplay.Visible = visible;
    }
}
