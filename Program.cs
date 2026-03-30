using System.Numerics;
using Raylib_cs;

struct DrawableTexture {

    Texture2D texture;
    Image image;
    bool should_update_texture;
    Vector2 image_center_screen_pos;
    public bool can_render;
    public DrawableTexture(Vector2 imagePos, Image image_ref) {
        image_center_screen_pos = imagePos;
        image = image_ref;
        texture = Raylib.LoadTextureFromImage(image);
        can_render = true;
        visitedPosition = new bool[image.Width, image.Height];
        should_fill = new bool[image.Width, image.Height];
    }

    public Texture2D getTexture() {
        return texture;
    }

    public void draw_point(brush_frame_data brush) {
        var draw_pos = get_draw_pos(brush.mouse_pos);
        Raylib.ImageDrawCircleV(ref image, draw_pos, (int)brush.brush_size, brush.color);
        
        should_update_texture = true;
    }

    public void draw_line(brush_frame_data brush, Vector2 last_mouse_pos) {
        var draw_pos = get_draw_pos(brush.mouse_pos);
        var last_draw_pos = get_draw_pos(last_mouse_pos);

        float t = 0;
        while (t < 1.0f) {
            Vector2 start = Vector2.Lerp(last_draw_pos, draw_pos, t);
            t += 0.05f;
            Raylib.ImageDrawCircleV(ref image, start, (int)brush.brush_size, brush.color);
        } 
        
        should_update_texture = true;
    }

    public void draw_fill(brush_frame_data brush) {
        var pos = get_draw_pos(brush.mouse_pos);
        int x = (int)pos.X;
        int y = (int)pos.Y;

        for (int bool_x = 0; bool_x < image.Width; bool_x++) {
            for (int bool_y = 0; bool_y < image.Height; bool_y++) {
                visitedPosition[bool_x, bool_y] = false;
                should_fill[bool_x, bool_y] = false;
            }
        }

        check_color = sample_position_color(brush.mouse_pos);
        fill_color = brush.color;
        width = image.Width;
        height = image.Height;
        try_fill_color(x,y);
        should_update_texture = true;
    }
    
    static bool[,] visitedPosition;
    static bool[,] should_fill;
    static Color check_color;
    static Color fill_color;
    static int width;
    static int height;

    void try_fill_color(int x_pos, int y_pos)
    {
        var queue = new Queue<(int, int)>();
        queue.Enqueue((x_pos, y_pos));

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (x < 0 || x >= width || y < 0 || y >= height || visitedPosition[x, y]) {
                continue;
            }

            visitedPosition[x, y] = true;
            var col = Raylib.GetImageColor(image, x, y);

            if (col.R == check_color.R && col.G == check_color.G && col.B == check_color.B && col.A == check_color.A)
            {
                Raylib.ImageDrawPixel(ref image, x, y, fill_color);
                queue.Enqueue((x + 1, y));
                queue.Enqueue((x - 1, y));
                queue.Enqueue((x, y + 1));
                queue.Enqueue((x, y - 1));
            }
        }
    }
    

    public void draw_texture() {
        if (should_update_texture) {
            unsafe {
                Raylib.UpdateTexture(texture, image.Data);
            }
            should_update_texture = false;
        }

        int x = (int)image_center_screen_pos.X - texture.Width / 2;
        int y = (int)image_center_screen_pos.Y - texture.Height / 2;
        Raylib.DrawTexture(texture, x, y, Color.White);
    }

    Vector2 get_draw_pos(Vector2 mouse_pos) {
        var scr_size = image_center_screen_pos;
        var tex_size = new Vector2(texture.Width, texture.Height);
        mouse_pos -= scr_size - tex_size / 2;
        return mouse_pos;
    }
    
    
    
    public Color sample_position_color(Vector2 mouse_pos) {
        var draw_pos = get_draw_pos(mouse_pos);
        return Raylib.GetImageColor(image, (int)draw_pos.X, (int)draw_pos.Y);
    }

    public bool within_bounds(Vector2 mouse_pos) {
        var draw_pos = get_draw_pos(mouse_pos);
        return draw_pos.X >= 0 && 
               draw_pos.X < texture.Width && 
               draw_pos.Y >= 0 &&
               draw_pos.Y<texture.Height;   
    }
}


public struct brush_frame_data {
    public Color color;
    public float brush_size;
    public Vector2 mouse_pos;
    public brush_types brush_type;
    public bool is_painting;
}

public enum brush_types {
    none,
    pencil,
    eraser,
    fill
}


public class Program {
    public static int screen_width = 1240;
    public static int screen_height = 1240;
    static DrawableTexture[] drawables = new DrawableTexture[7];
    static int idx_color_wheel = 0;
    static int idx_draw_texture = 1;
    static Color base_color = Color.White;

    static string GetProjectPath(string relative_path) {
	    return Path.Combine(Directory.GetCurrentDirectory(), relative_path);
    }

    static void Main(string[] args) {
        Raylib.InitWindow(screen_width, screen_height, "Fuck poop");
        const int resolution = 1024;

        var color_wheel_image = Raylib.LoadImage(GetProjectPath($@"assets{Path.DirectorySeparatorChar}colorwheel.png"));
        var draw_image = Raylib.GenImageColor(resolution, resolution, base_color);
        var layer_image = Raylib.GenImageColor(resolution, resolution, new Color(0,0,0,0));

        bool is_erasing = false;
        drawables[idx_color_wheel] = new DrawableTexture(new Vector2(screen_width / 2, color_wheel_image.Height / 2), color_wheel_image);
        drawables[idx_draw_texture] = new DrawableTexture(new Vector2(screen_width / 2, screen_height / 2), draw_image);
        for (int i = idx_draw_texture + 1; i < drawables.Length; i++) {
            drawables[i] = new DrawableTexture(new Vector2(screen_width / 2, screen_height / 2), layer_image);
        }

        brush_frame_data brush_data = new brush_frame_data(){
            color = Color.Black,
            brush_size = 4,
            brush_type = brush_types.pencil
        };

        while (Raylib.WindowShouldClose() == false) {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Beige);
            
            // Render loop
            for (int i = 0; i < drawables.Length; i++) {
                if (drawables[i].can_render) {
                    drawables[i].draw_texture();
                }
            }
            
            Raylib.DrawText(Directory.GetCurrentDirectory(), 200, 12, 24, Color.Magenta);
            
            Raylib.DrawText($"Brush Size: {brush_data.brush_size}", 8, 8, 42, Color.DarkGreen);
            Raylib.DrawText($"Brush Opacity: {brush_data.color.A / 255.0f:F2}", 8, 42, 42, Color.DarkGreen);
            Raylib.DrawText($"Layer: {idx_draw_texture}", 8, 84, 42, Color.DarkGreen);

            /*
             * TODO : addative/color blending, network, sell $$$$ 
             * 
             * 
             */
            
            // Brush size
            float scroll_amount = Raylib.GetMouseWheelMove();
            
            // Brush opacity
            if (Raylib.IsKeyDown(KeyboardKey.LeftControl)) {
                brush_data.color.A = (byte)Math.Clamp(brush_data.color.A + scroll_amount * 10, 0, 255);
            }
            else {
                brush_data.brush_size = Math.Max(brush_data.brush_size + scroll_amount * 3, 1);
            }


            if (Raylib.IsKeyPressed(KeyboardKey.Up)) {
                idx_draw_texture = Math.Clamp(idx_draw_texture + 1, 1, drawables.Length - 1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Down)) {
                idx_draw_texture = Math.Clamp(idx_draw_texture - 1, 1, drawables.Length - 1);
            }
            
            
            // Drawing on canvas
            Vector2 last_mouse_pos = brush_data.mouse_pos;
            bool was_mouse_held = brush_data.is_painting;
            bool is_mouse_down = Raylib.IsMouseButtonDown(MouseButton.Left);
            brush_data.mouse_pos = Raylib.GetMousePosition();
            brush_data.is_painting = is_mouse_down;
            is_erasing = Raylib.IsKeyDown(KeyboardKey.E);
            
            if (Raylib.IsMouseButtonPressed(MouseButton.Right)) {
                var sample_color = drawables[idx_draw_texture].sample_position_color(brush_data.mouse_pos);
                sample_color.A = brush_data.color.A;
                brush_data.color = sample_color;
            }

            if (is_mouse_down) {
                if (drawables[idx_color_wheel].within_bounds(brush_data.mouse_pos)) {
                    var sample_color = drawables[idx_color_wheel].sample_position_color(brush_data.mouse_pos);
                    sample_color.A = brush_data.color.A;
                    brush_data.color = sample_color;
                }
                
                if (drawables[idx_draw_texture].within_bounds(brush_data.mouse_pos)) 
                {
                    var col = brush_data.color;
                    
                    if (is_erasing) {
                        brush_data.color = new Color(0,0,0,0);
                    }
                    
                    if (was_mouse_held) {
                        drawables[idx_draw_texture].draw_line(brush_data, last_mouse_pos);
                    }
                    else {
                        drawables[idx_draw_texture].draw_point(brush_data);
                    }
    
                    brush_data.color = col;
    
                }
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.F)) {
                if (drawables[idx_draw_texture].within_bounds(brush_data.mouse_pos)) {
                    drawables[idx_draw_texture].draw_fill(brush_data);
                }
            }
            
            Raylib.DrawFPS(8, 80);
            Raylib.EndDrawing();
        }
    }
}