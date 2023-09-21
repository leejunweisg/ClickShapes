using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ClickShapes.ViewModel;

public partial class ShapesViewModel : ObservableObject
{
    #region Nested Classes
    public partial class Polygon : ObservableObject
    {
        public static int nextId = 0;

        /// <summary>
        /// Polygon Constructor
        /// </summary>
        /// <param name="point">The point to create the first vertex at.</param>
        public Polygon(Point point)
        {
            Name = $"Polygon{nextId++}";
            Vertices.Add(new Vertex(new Point(0, 0)));
            Vertices.CollectionChanged += (_, _) => UpdateVerticesString(); // When vertices added or removed
        }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private ObservableCollection<Vertex> _vertices = new();

        [ObservableProperty]
        private string _verticesString = string.Empty;

        [ObservableProperty]
        private Vertex _selectedVertex = null;

        [ObservableProperty]
        private bool _isSelected = false;

        [ObservableProperty]
        private bool _isClosed = false;

        public void UpdateVerticesString()
        {
            VerticesString = string.Join(" ", Vertices);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public partial class Vertex : ObservableObject
    {
        /// <summary>
        /// Vertex Constructor
        /// </summary>
        /// <param name="point">The point to create the vertex at.</param>
        public Vertex(Point point)
        {
            Point = point;
            IsFloating = true;
        }

        [ObservableProperty]
        private Point _point;

        [ObservableProperty]
        private bool _isSelected = false;

        [ObservableProperty]
        private bool _isFloating = false;

        public override string ToString()
        {
            return $"{Point.X},{Point.Y}";
        }
    }
    #endregion


    #region Constructor
    public ShapesViewModel()
    {
    }
    #endregion


    #region Properties
    [ObservableProperty]
    private ObservableCollection<Polygon> _polygons = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeletePolygonCommand))]
    private Polygon _polygonOfInterest;

    [ObservableProperty]
    private Vertex _selectedVertex;

    [ObservableProperty]
    private BitmapSource _backgroundImage;

    [ObservableProperty]
    private int _canvasWidth = 800;

    [ObservableProperty]
    private int _canvasHeight = 600;
    #endregion


    #region Methods
    /* RelayCommands */
    [RelayCommand]
    private void CanvasClicked(Canvas canvas)
    {
        Log.Debug("Canvas Clicked");

        if (PolygonOfInterest != null)
        {
            // If polygon is open, add clicked point to collection of vertices
            if (!PolygonOfInterest.IsClosed)
            {
                // Set down current vertex
                PolygonOfInterest.Vertices[PolygonOfInterest.Vertices.Count - 1].IsFloating = false;

                // Create new vertex (floating by default)
                PolygonOfInterest.Vertices.Add(new Vertex(Mouse.GetPosition(canvas)));
            }

            // If polygon is closed and a vertex is selected, deselect it (as user clicked on a blank space on the canvas)
            else
            {
                // Deselect selected vertex
                if (SelectedVertex != null)
                {
                    SelectedVertex.IsSelected = false;
                    SelectedVertex = null;
                }
                // Deselect Polygon of Interest
                else
                {
                    PolygonOfInterest = null;
                }
            }
        }
    }

    [RelayCommand]
    private void CanvasMouseMove(Canvas canvas)
    {
        if (PolygonOfInterest != null)
        {
            // If polygon is open, move the last vertex (floating) as mouse moves
            if (!PolygonOfInterest.IsClosed)
            {
                if (PolygonOfInterest.Vertices.Count > 0)
                {
                    PolygonOfInterest.Vertices[PolygonOfInterest.Vertices.Count - 1].Point = Mouse.GetPosition(canvas);
                }
            }
            // If polygon is closed, with a selected vertex, allow dragging of vertex
            else
            {
                if (SelectedVertex != null && Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    SelectedVertex.Point = Mouse.GetPosition(canvas);
                }
            }

            // Need to recompute vertices string when points are moved.
            PolygonOfInterest.UpdateVerticesString();
        }
    }

    [RelayCommand]
    private void Backspace()
    {
        Log.Debug("Backspace");

        if (PolygonOfInterest != null)
        {
            // If polygon closed and point selected: delete the selected point
            if (PolygonOfInterest.IsClosed)
            {
                // TODO: if removal of vertex causes the polygon to be invalid, remove display warning/confirmation before clearing all vertices
                if (SelectedVertex != null)
                {
                    PolygonOfInterest.Vertices.Remove(SelectedVertex);
                    SelectedVertex = null;
                }
                // If no vertex selected, ask if user wishes to delete the polygon
                else
                {
                    DeletePolygonOfInterest();
                }
            }

            // If polygon open and at least one point exists: delete the latest point
            else
            {
                // Remove the second last point
                if (PolygonOfInterest.Vertices.Count >= 2)
                {
                    PolygonOfInterest.Vertices.RemoveAt(PolygonOfInterest.Vertices.Count - 2);
                }
            }
        }
    }

    [RelayCommand]
    private void Escape()
    {
        Log.Debug("Escape");

        if (PolygonOfInterest != null)
        {
            if (PolygonOfInterest.IsClosed)
            {
                // If polygon is closed and a vertex is selected, deselect it
                if (SelectedVertex != null)
                {
                    SelectedVertex.IsSelected = false;
                    SelectedVertex = null;
                }
            }
        }
    }

    [RelayCommand]
    private void RectangleClicked(Vertex vertex)
    {
        Log.Debug("Rectangle Clicked");

        if (PolygonOfInterest != null)
        {
            // If poylgon is open, close the polygon
            if (!PolygonOfInterest.IsClosed)
            {
                // Close polygon if the clicked vertex is the first vertex
                if (PolygonOfInterest.Vertices[0] ==  vertex)
                {
                    // TODO: validate points to ensure it is a proper polygon

                    // Remove latest point, close polygon
                    PolygonOfInterest.IsClosed = true;
                    PolygonOfInterest.Vertices.RemoveAt(PolygonOfInterest.Vertices.Count - 1);

                    // Add polygon to polygons
                    Polygons.Add(PolygonOfInterest);
                    PolygonOfInterest = null;
                }
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
    }

    [RelayCommand]
    private void NewPolygon()
    {
        // If there are no selected polygon, simply create a new polygon.
        if (PolygonOfInterest == null)
        {
            PolygonOfInterest = new(new Point(0, 0));
        }

        // If a polygon is selected, check if it is still open (unsaved), or closed (saved). 
        else
        {
            // If polygon is open (unsaved), ask if user wishes to discard.
            if (!PolygonOfInterest.IsClosed)
            {
                if (MessageBox.Show("Do you wish to discard the current polygon?", "Discard Unsaved Polygon",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    PolygonOfInterest = new(new Point(0, 0));
                }
            }
            else
            {
                PolygonOfInterest = new(new Point(0, 0));
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeletePolygon))]
    private void DeletePolygon()
    {
        DeletePolygonOfInterest();
    }

    [RelayCommand]
    private void PolygonClicked(Polygon clickedPolygon)
    {
        // If there are no existing polygon of interest, simply make the clicked polygon the polygon of interest.
        if (PolygonOfInterest == null)
        {
            PolygonOfInterest = clickedPolygon;
        }

        // If there is an existing polygon of interest, ensure that it is closed before assigning it.
        else
        {
            // Make
            if (PolygonOfInterest.IsClosed)
            {
                PolygonOfInterest = clickedPolygon;

                // Deselect selected vertex 
                if (SelectedVertex != null)
                {
                    SelectedVertex.IsSelected = false;
                    SelectedVertex = null;
                }
            }
        }
    }

    [RelayCommand]
    private void ExportToJson()
    {
        // Prepare points in a nested list structure
        List<List<Point>> data = new();
        foreach (Polygon polygon in Polygons)
        {
            List<Point> points = new();
            foreach (Vertex vertex in polygon.Vertices)
            {
                points.Add(vertex.Point);
            }
            data.Add(points);
        }

        // Serialise to JSON string
        var res = JsonConvert.SerializeObject(data);

        // Save
        SaveFileDialog saveFileDialog = new();
        saveFileDialog.DefaultExt = ".txt";
        saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
        saveFileDialog.FileName = "polygons";

        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveFileDialog.FileName, res);
        }

        Log.Debug($"Polygons JSON exported to: {saveFileDialog.FileName}");
    }

    [RelayCommand]
    private void ImportFromJson()
    {
        // TODO: Import from JSON
    }

    [RelayCommand]
    private void LoadImage()
    {
        OpenFileDialog openFileDialog = new();
        if (openFileDialog.ShowDialog() == true)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(openFileDialog.FileName);
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // To release the file after loading
            bitmap.EndInit();

            BackgroundImage = bitmap;
            CanvasWidth = BackgroundImage.PixelWidth;
            CanvasHeight = BackgroundImage.PixelHeight;
        }
    }

    private void DeletePolygonOfInterest()
    {
        if (PolygonOfInterest != null)
        {
            if (MessageBox.Show("Do you wish to delete the current polygon?", "Delete Polygon",
                         MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Polygons.Remove(PolygonOfInterest);
                PolygonOfInterest = null;
            }
        }
    }

    private bool CanDeletePolygon()
    {
        return PolygonOfInterest != null;
    }
    #endregion
}
