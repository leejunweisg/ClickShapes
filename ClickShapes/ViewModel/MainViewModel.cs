using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClickShapes.ViewModel;

partial class Vertex : ObservableObject
{
    public Vertex(Point point)
    {
        Point = point;
        IsFloating = true;
    }

    [ObservableProperty]
    private Point _point;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isFloating;
}

partial class MainViewModel : ObservableObject
{
    #region Fields

    #endregion

    #region Constructor
    public MainViewModel()
    {
        // Initialise logger
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Debug()
            .MinimumLevel.Debug()
            .CreateLogger();

        // Initialise vertices collection and add default 'floating' vertex
        Vertices = new();
        Vertices.Add(new Vertex(new Point(0, 0)));
    }
    #endregion

    #region Properties
    [ObservableProperty]
    private ObservableCollection<Vertex> _vertices;

    [ObservableProperty]
    private string _pointsString = "";

    [ObservableProperty]
    private Vertex _selectedVertex = null;

    [ObservableProperty]
    private bool _isPolygonClosed;
    #endregion

    #region Methods
    private void UpdatePoints()
    {
        string temp = "";
        foreach (var vertex in Vertices)
        {
            temp += $"{vertex.Point.X},{vertex.Point.Y} ";
        }
        PointsString = temp;
    }

    [RelayCommand]
    private void CanvasClicked(Canvas canvas)
    {
        Log.Debug("Canvas Clicked");

        // If polygon is open, add clicked point to collection of vertices
        if (!IsPolygonClosed)
        {
            // Set down current vertex
            Vertices[Vertices.Count - 1].IsFloating = false;

            // Create new vertex (floating by default)
            Vertices.Add(new Vertex(Mouse.GetPosition(canvas)));

            UpdatePoints();
        }
    }

    [RelayCommand]
    private void CanvasMouseMove(Canvas canvas)
    {
        // If polygon is open, move the last vertex (floating) as mouse moves
        if (!IsPolygonClosed)
        {
            if (Vertices.Count > 0)
            {
                Vertices[Vertices.Count - 1].Point = Mouse.GetPosition(canvas);
            }
        }

        // TODO: for a closed polygon with a selected vertex, allow dragging of vertex
        else
        {

        }

        UpdatePoints();
    }


    [RelayCommand]
    private void BackSpace()
    {
        Log.Debug("Backspace");

        // If polygon closed and point selected: delete the selected point
        if (IsPolygonClosed)
        {
            // TODO: if removal of vertex causes the polygon to be invalid, remove display warning/confirmation before clearing all vertices
            if (SelectedVertex != null)
            {
                Vertices.Remove(SelectedVertex);
                SelectedVertex = null;
            }
        }

        // If polygon open and at least one point exists: delete the latest point
        else
        {
            // Remove the second last point
            if (Vertices.Count >= 2)
            {
                Vertices.RemoveAt(Vertices.Count - 2);
            }
        }

        UpdatePoints();
    }

    [RelayCommand]
    private void Escape()
    {
        Log.Debug("Escape");

        // If polygon is closed and a vertex is selected, deselect it
        if (IsPolygonClosed)
        {
            if (SelectedVertex != null)
            {
                SelectedVertex.IsSelected = false;
                SelectedVertex = null;
            }
        }
    }

    [RelayCommand]
    private void RectangleClicked(Vertex vertex)
    {
        Log.Debug("Rectangle Clicked");

        // If poylgon is open, close the polygon
        if (!IsPolygonClosed)
        {
            // TODO: validate points to ensure it is a proper polygon

            // Remove latest point, close polygon
            IsPolygonClosed = true;
            Vertices.RemoveAt(Vertices.Count - 1);

            UpdatePoints();
        }

        // If polygon is closed, mark the clicked rectangle as selected
        else
        {
            // Deselect previous
            if (SelectedVertex != null)
            {
                SelectedVertex.IsSelected = false;
            }
            SelectedVertex = vertex;
            SelectedVertex.IsSelected = true;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Vertices.Clear();
        Vertices.Add(new Vertex(new Point(0, 0)));
        PointsString = "";
        SelectedVertex = null;
        IsPolygonClosed = false;
    }
    #endregion

    #region Events
    #endregion
}

