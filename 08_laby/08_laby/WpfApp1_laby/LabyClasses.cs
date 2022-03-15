using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1_laby
{
    // DOWNLOAD http://users.nik.uni-obuda.hu/prog4/laby_files.zip
    // Uncompress to Images & Levels
    // Right click > Properties > Embedded Resource + Do Not Copy
    // size???
    class LabyModel
    {
        public bool[,] Walls { get; set; } // Tile[X, Y] true = wall
        public Point Player { get; set; } // Tile coordinates
        public Point Exit { get; set; } // Tile coordinates

        public double GameWidth { get; private set; } // Pixel size
        public double GameHeight { get; private set; } // Pixel size
        public double TileSize { get; set; } // Pixel size

        public LabyModel(double w, double h)
        {
            this.GameWidth = w; this.GameHeight = h;
        }
    }

    class LabyRenderer
    {
        LabyModel model;
        
        // minimize 'new' calls !!!
		// Drawing = Geometry + Brush + Pen
        Drawing oldBackground;
        Drawing oldWalls; 
        Drawing oldExit;
        Drawing oldPlayer;
        Point oldPlayerPosition; // Tile position
        Dictionary<string, Brush> myBrushes = new Dictionary<string, Brush>();

        public LabyRenderer(LabyModel model)
        {
            this.model = model;
        }

        public void Reset()
        {
            oldBackground = null;
            oldWalls = null;
            oldExit = null;
            oldPlayer = null;
            oldPlayerPosition = new Point(-1, -1);
            myBrushes.Clear();
        }

        Brush GetBrush(string fname, bool isTiled)
        {
            if (!myBrushes.ContainsKey(fname))
            {
                // IF content+copy always
                // ImageBrush ib = new ImageBrush(new BitmapImage(new Uri(@"Images\" + fname, UriKind.Relative)));
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                // Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();
                bmp.StreamSource = Assembly.GetExecutingAssembly().GetManifestResourceStream(fname);
                bmp.EndInit();
                ImageBrush ib = new ImageBrush(bmp);
                if (isTiled) {
                    ib.TileMode = TileMode.Tile;
                    ib.Viewport = new Rect(0, 0, model.TileSize, model.TileSize);
                    ib.ViewportUnits = BrushMappingMode.Absolute;
                }
                // ib.Viewbox // ONLY if multiple textures in one image
                myBrushes[fname] = ib;
            }
            return myBrushes[fname];
        }

        Brush PlayerBrush { get { return GetBrush("WpfApp1_laby.Images.player.bmp", false); } }
        Brush ExitBrush { get { return GetBrush("WpfApp1_laby.Images.exit.bmp", false); } }
        Brush WallBrush { get { return GetBrush("WpfApp1_laby.Images.wall.bmp", true); } }

        public Drawing BuildDrawing()
        {
            DrawingGroup dg = new DrawingGroup();
            dg.Children.Add(GetBackground());
            dg.Children.Add(GetWalls());
            dg.Children.Add(GetExit());
            dg.Children.Add(GetPlayer());
            return dg; 
        }
        private Drawing GetBackground()
        {
            if (oldBackground == null)
            {
                Geometry backgroundGeometry = new RectangleGeometry(new Rect(0, 0, model.GameWidth, model.GameHeight));
                oldBackground = new GeometryDrawing(Brushes.Black, null, backgroundGeometry);
            }
            return oldBackground;
        }
        private Drawing GetExit()
        {
            if (oldExit == null)
            {
                Geometry g = new RectangleGeometry(new Rect(
                    model.Exit.X * model.TileSize, model.Exit.Y * model.TileSize,
                    model.TileSize, model.TileSize));
                oldExit = new GeometryDrawing(ExitBrush, null, g);
            }
            return oldExit;
        }
        private Drawing GetPlayer()
        {
            if (oldPlayer == null || oldPlayerPosition!=model.Player)
            {
                Geometry g = new RectangleGeometry(new Rect(model.Player.X * model.TileSize, model.Player.Y * model.TileSize, model.TileSize, model.TileSize));
                oldPlayer = new GeometryDrawing(PlayerBrush, null, g);
                oldPlayerPosition = model.Player;
            }
            return oldPlayer;
        }
        private Drawing GetWalls()
        {
            if (oldWalls == null)
            {
                GeometryGroup g = new GeometryGroup();
                for (int x = 0; x < model.Walls.GetLength(0); x++)
                {
                    for (int y = 0; y < model.Walls.GetLength(1); y++)
                    {
                        if (model.Walls[x, y])
                        {
                            Geometry box = new RectangleGeometry(new Rect(x * model.TileSize, y * model.TileSize, model.TileSize, model.TileSize));
                            g.Children.Add(box);
                        }
                    }
                }
                oldWalls = new GeometryDrawing(WallBrush, null, g);
            }
            return oldWalls;
        }
    }

    class LabyLogic
    {
        LabyModel model;
        public LabyLogic(LabyModel model, string fname)
        {
            this.model = model;
			InitModel(fname);
        }
		
        private void InitModel(string fname)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fname);
            StreamReader sr = new StreamReader(stream);
            string[] lines = sr.ReadToEnd().Replace("\r", "").Split('\n');

            int width = int.Parse(lines[0]);
            int height = int.Parse(lines[1]);
            model.Walls = new bool[width, height];
            model.TileSize = Math.Min(model.GameWidth/width, model.GameHeight/height);
            for (int x=0; x<width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    char current = lines[y+2][x];
                    model.Walls[x, y] = (current == 'e');
                    if (current == 'S') model.Player = new Point(x, y);
                    if (current == 'F') model.Exit = new Point(x, y);
                }
            }

        }        
		
		public bool Move(int dx, int dy) // [-1..1] double???
        {
            int newx = (int)(model.Player.X + dx);
            int newy = (int)(model.Player.Y + dy);
            if (newx >= 0 && newy >= 0 &&
                newx < model.Walls.GetLength(0) &&
                newy < model.Walls.GetLength(1) &&
                !model.Walls[newx, newy])
            {
                // model.Player.X = newx; 
                // can't do this => not a variable => ChangeX, ChangeY => Pong
                model.Player = new Point(newx, newy);
            }
            return model.Player.Equals(model.Exit);
        }

        public Point GetTilePos(Point mousePos) // Pixel position => Tile position
        {
            return new Point((int)(mousePos.X / model.TileSize),
                            (int)(mousePos.Y / model.TileSize));
        }
    }

    class LabyControl : FrameworkElement
    {
        LabyLogic logic;
        LabyRenderer renderer;
        LabyModel model;
        Stopwatch stw;

        public LabyControl()
        {
            Loaded += LabyControl_Loaded;// += <TAB><ENTER>
        }
        private void LabyControl_Loaded(object sender, RoutedEventArgs e)
        {
            stw = new Stopwatch();
            model = new LabyModel(ActualWidth, ActualHeight);
            logic = new LabyLogic(model, "WpfApp1_laby.Levels.L00.lvl");
            renderer = new LabyRenderer(model);

            Window win = Window.GetWindow(this);
            if (win != null)
            {
                win.KeyDown += Win_KeyDown;
                MouseDown += LabyControl_MouseDown;
            }

            InvalidateVisual();
            stw.Start();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (renderer != null) drawingContext.DrawDrawing(renderer.BuildDrawing());
        }

        private void LabyControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            Point tilePos = logic.GetTilePos(mousePos);
            MessageBox.Show(mousePos + "\n" + tilePos);
        }

        private void Win_KeyDown(object sender, KeyEventArgs e)
        {
            bool finished = false;
            switch (e.Key)
            {
                case Key.W: finished = logic.Move(0, -1); break;
                case Key.S: finished = logic.Move(0, 1); break;
                case Key.A: finished = logic.Move(-1, 0); break;
                case Key.D: finished = logic.Move(1, 0); break;
            }
            InvalidateVisual();
            if (finished)
            {
                stw.Stop();
                MessageBox.Show("YAY! "+stw.Elapsed.ToString(@"hh\:mm\:ss\.fff"));
                // Not elegant: should use an event!
            }
        }
    }
}