using System;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Raylib_cs;


public struct DrawableTexture
{
    Texture2D texture;
    Image image;
    bool should_update_texture;
    Vector2 image_center_screen_pos;
    public bool can_render;
    public DrawableTexture(Vector2 imagePos, Image image_ref)
    {
        image_center_screen_pos = imagePos;
        image = image_ref;
        texture = Raylib.LoadTextureFromImage(image);
        can_render = true;
        visitedPosition = new bool[image.Width, image.Height];
        should_fill = new bool[image.Width, image.Height];
    }

    public Texture2D getTexture()
    {
        return texture;
    }

    public void draw_point(brush_frame_data brush)
    {
        try
        {
            var draw_pos = get_draw_pos(brush.mouse_pos);
            Raylib.ImageDrawCircleV(ref image, draw_pos, (int)brush.brush_size, brush.color);

            should_update_texture = true;
        }
        catch (Exception e)
        {
            Console.Write($"Draw failed -> {e}");
        }
    }

    public void draw_line(brush_frame_data brush, Vector2 last_mouse_pos)
    {
        var draw_pos = get_draw_pos(brush.mouse_pos);
        var last_draw_pos = get_draw_pos(last_mouse_pos);

        float t = 0;
        while (t < 1.0f)
        {
            Vector2 start = Vector2.Lerp(last_draw_pos, draw_pos, t);
            t += 0.05f;
            Raylib.ImageDrawCircleV(ref image, start, (int)brush.brush_size, brush.color);
        }

        should_update_texture = true;
    }

    public void draw_fill(brush_frame_data brush)
    {
        var pos = get_draw_pos(brush.mouse_pos);
        int x = (int)pos.X;
        int y = (int)pos.Y;

        for (int bool_x = 0; bool_x < image.Width; bool_x++)
        {
            for (int bool_y = 0; bool_y < image.Height; bool_y++)
            {
                visitedPosition[bool_x, bool_y] = false;
                should_fill[bool_x, bool_y] = false;
            }
        }

        check_color = sample_position_color(brush.mouse_pos);
        fill_color = brush.color;
        width = image.Width;
        height = image.Height;
        try_fill_color(x, y);
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
            if (x < 0 || x >= width || y < 0 || y >= height || visitedPosition[x, y])
            {
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


    public void draw_texture()
    {
        if (should_update_texture)
        {
            unsafe
            {
                Raylib.UpdateTexture(texture, image.Data);
            }
            should_update_texture = false;
        }

        int x = (int)image_center_screen_pos.X - texture.Width / 2;
        int y = (int)image_center_screen_pos.Y - texture.Height / 2;
        Raylib.DrawTexture(texture, x, y, Color.White);
    }

    Vector2 get_draw_pos(Vector2 mouse_pos)
    {
        var scr_size = image_center_screen_pos;
        var tex_size = new Vector2(texture.Width, texture.Height);
        mouse_pos -= scr_size - tex_size / 2;
        return mouse_pos;
    }



    public Color sample_position_color(Vector2 mouse_pos)
    {
        var draw_pos = get_draw_pos(mouse_pos);
        return Raylib.GetImageColor(image, (int)draw_pos.X, (int)draw_pos.Y);
    }

    public bool within_bounds(Vector2 mouse_pos)
    {
        var draw_pos = get_draw_pos(mouse_pos);
        return draw_pos.X >= 0 &&
               draw_pos.X < texture.Width &&
               draw_pos.Y >= 0 &&
               draw_pos.Y < texture.Height;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct brush_frame_data // 29 ?
{
    public Color color; // 16
    public Vector2 mouse_pos; // 8
    public float brush_size; // 4
    public bool is_painting; // 1
}

public class funky_funcs
{
    static public byte[] toByteArray(brush_frame_data structure)
    {
        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new System.IO.BinaryWriter(memoryStream))
        {
            writer.Write(structure.color.R);
            writer.Write(structure.color.G);
            writer.Write(structure.color.B);
            writer.Write(structure.color.A);
            writer.Write(structure.brush_size);

            writer.Write(structure.mouse_pos.X);
            writer.Write(structure.mouse_pos.Y);

            writer.Write(structure.is_painting);

            // byte[] keyBytes = Encoding.BigEndianUnicode.GetBytes(structure.brush_locations);
            //
            // writer.Write(BitConverter.GetBytes((ushort)keyBytes.Length).Reverse().ToArray());
            // writer.Write(structure.brush_locations);
            return memoryStream.ToArray();
        }
    }

    static public brush_frame_data fromByteArray(byte[] byteArray)
    {
        using (var memoryStream = new MemoryStream(byteArray))
        using (var reader = new BinaryReader(memoryStream))
        {
            brush_frame_data structure = new brush_frame_data();

            structure.color.R = reader.ReadByte();
            structure.color.G = reader.ReadByte();
            structure.color.B = reader.ReadByte();
            structure.color.A = reader.ReadByte();
            structure.brush_size = reader.ReadSingle();

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            structure.mouse_pos = new Vector2(x, y);

            structure.is_painting = reader.ReadBoolean();
            
            // UInt16 locationsSize = reader.ReadUInt16();
            // structure.brush_locations = [];
            // for (int i = 0; i < locationsSize; i++)
            // {
            //     float ix = reader.ReadSingle();
            //     float iy = reader.ReadSingle();
            //     structure.brush_locations.Add(new Vector2(ix, iy));
            // }

            return structure;
        }
    }
    static public byte[] toByteArray(Packet structure)
    {
        using (var memoryStream = new System.IO.MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            // writer.Write(BitConverter.GetBytes((ushort)structure.PacketType).Reverse().ToArray());
            // byte[] keyBytes = Encoding.BigEndianUnicode.GetBytes(structure.Key);
            // writer.Write(BitConverter.GetBytes((ushort)keyBytes.Length).Reverse().ToArray());
            // writer.Write(keyBytes);
            writer.Write(structure.Data);

            return memoryStream.ToArray();
        }
    }

    static public Packet packetFromByteArray(byte[] byteArray)
    {
        using (var memoryStream = new MemoryStream(byteArray))
        using (var reader = new BinaryReader(memoryStream))
        {
            Packet structure = new Packet();
            // structure.PacketType = reader.ReadUInt16();


            // UInt16 keySize = reader.ReadUInt16();
            // structure.Key = Encoding.UTF8.GetString(reader.ReadBytes(keySize));


            List<byte> totalBytes = new List<byte>();
            int size = 0;
            unsafe {
            size = sizeof(brush_frame_data);
            }
            byte[] buffer = new byte[size];
            int bytesRead;

            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalBytes.AddRange(buffer[..bytesRead]);
            }

            structure.Data = buffer;

            return structure;
        }
    }
}



public class Program
{
    public static int screen_width = 800;
    public static int screen_height = 800;
    static DrawableTexture[] drawables = new DrawableTexture[7];
    static int idx_color_wheel = 0;
    static int idx_draw_texture = 1;
    static Color base_color = Color.White;


    static string GetProjectPath(string relative_path)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relative_path);
    }

    static void Main(string[] args)
    {
        if (args.Length != 2) {
            Console.WriteLine("Invalid amount of arguments, input ip and port!");
            return;
        }
        Raylib.InitWindow(screen_width, screen_height, "Fuck poop");
        const int resolution = 512;

        var color_wheel_image = Raylib.LoadImage(GetProjectPath($@"assets{Path.DirectorySeparatorChar}colorwheel.png"));
        var draw_image = Raylib.GenImageColor(resolution, resolution, base_color);
        var layer_image = Raylib.GenImageColor(resolution, resolution, new Color(0, 0, 0, 0));

        bool is_erasing = false;
        drawables[idx_color_wheel] = new DrawableTexture(new Vector2(screen_width / 2, color_wheel_image.Height / 2), color_wheel_image);
        drawables[idx_draw_texture] = new DrawableTexture(new Vector2(screen_width / 2, screen_height - draw_image.Height / 2 - 24), draw_image);
        for (int i = idx_draw_texture + 1; i < drawables.Length; i++)
        {
            drawables[i] = new DrawableTexture(new Vector2(screen_width / 2, screen_height - draw_image.Height / 2 - 24), layer_image);
        }

        brush_frame_data brush_data = new brush_frame_data()
        {
            color = Color.Black,
            brush_size = 4,
        };
        
        Console.WriteLine(args[0]);
        Console.WriteLine(args[1]);
        Networker networker = new Networker(args[0], args[1]);

        // List<Vector2> brush_locations = [];

        while (Raylib.WindowShouldClose() == false)
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(0.1f, 0.1f, 0.1f, 1f));

            // Render loop
            for (int i = 0; i < drawables.Length; i++)
            {
                if (drawables[i].can_render)
                {
                    drawables[i].draw_texture();
                }
            }


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
            if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
            {
                brush_data.color.A = (byte)Math.Clamp(brush_data.color.A + scroll_amount * 10, 0, 255);
            }
            else
            {
                brush_data.brush_size = Math.Max(brush_data.brush_size + scroll_amount * 3, 1);
            }


            if (Raylib.IsKeyPressed(KeyboardKey.Up))
            {
                idx_draw_texture = Math.Clamp(idx_draw_texture + 1, 1, drawables.Length - 1);
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Down))
            {
                idx_draw_texture = Math.Clamp(idx_draw_texture - 1, 1, drawables.Length - 1);
            }


            // Drawing on canvas
            Vector2 last_mouse_pos = brush_data.mouse_pos;
            bool was_mouse_held = brush_data.is_painting;
            bool is_mouse_down = Raylib.IsMouseButtonDown(MouseButton.Left);
            brush_data.mouse_pos = Raylib.GetMousePosition();
            brush_data.is_painting = is_mouse_down;
            is_erasing = Raylib.IsKeyDown(KeyboardKey.E);

            if (Raylib.IsMouseButtonPressed(MouseButton.Right))
            {
                var sample_color = drawables[idx_draw_texture].sample_position_color(brush_data.mouse_pos);
                sample_color.A = brush_data.color.A;
                brush_data.color = sample_color;
            }

            if (is_mouse_down)
            {
                // brush_locations.Add(brush_data.mouse_pos);
                if (drawables[idx_color_wheel].within_bounds(brush_data.mouse_pos))
                {
                    var sample_color = drawables[idx_color_wheel].sample_position_color(brush_data.mouse_pos);
                    sample_color.A = brush_data.color.A;
                    brush_data.color = sample_color;
                }

                if (drawables[idx_draw_texture].within_bounds(brush_data.mouse_pos))
                {
                    var col = brush_data.color;

                    if (is_erasing)
                    {
                        brush_data.color = new Color(0, 0, 0, 0);
                    }

                    if (was_mouse_held)
                    {
                        drawables[idx_draw_texture].draw_line(brush_data, last_mouse_pos);
                    }
                    else
                    {
                        drawables[idx_draw_texture].draw_point(brush_data);
                    }

                    brush_data.color = col;
                    networker.WriteAsync(brush_data);

                }
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.F))
            {
                if (drawables[idx_draw_texture].within_bounds(brush_data.mouse_pos))
                {
                    drawables[idx_draw_texture].draw_fill(brush_data);
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                // networker.WriteAsync(brush_data);
                // brush_locations = [];
            }

            networker.DrawQueue(ref drawables[idx_draw_texture]);

            Raylib.DrawFPS(screen_width - 128, 8);
            Raylib.EndDrawing();
            
        }
    }
}


public class Networker : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private bool _isRunning;
    private Task _readTask;
    private Queue<brush_frame_data> draw_queue;

    public Networker(string serverAddress, string port)
    {
        _client = new TcpClient(serverAddress, int.Parse(port));
        _stream = _client.GetStream();
        draw_queue = new Queue<brush_frame_data>();
        if (!_stream.CanRead)
        {
            Console.WriteLine($"Failed to connect to server at {serverAddress}:{port}");
            return;
        }
        Console.WriteLine($"Connected to server at {serverAddress}:{port}");
        StartListening();
    }

    public void StartListening()
    {
        _isRunning = true;
        _readTask = Task.Run(async () => {
            brush_frame_data data = new brush_frame_data();
            byte[] bytes = funky_funcs.toByteArray(data);
            var handshake = new Packet
            {
                // PacketType = 1,
                // Key = "1234",
                Data = bytes,
            };


            var packet = funky_funcs.toByteArray(handshake);

            _stream.Write(packet, 0, packet.Length);

            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Connection closed by server.");
                }

                var text = System.Text.Encoding.Default.GetString(buffer);
                Console.WriteLine($"Server base: {text}");
                //draw_queue.Enqueue(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from stream: {ex.Message}");
            }

            while (_isRunning)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Connection closed by server.");
                        break;
                    }


                    brush_frame_data content = funky_funcs.fromByteArray(buffer.Take(bytesRead).ToArray());
                    draw_queue.Enqueue(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading from stream: {ex.Message}");
                }
                Console.WriteLine("Reached loop end");
            }
            Console.WriteLine("Finished listening");
        });
    }

    public void WriteAsync(brush_frame_data brush_data)
    {
        var buffer = funky_funcs.toByteArray(brush_data);

        var brushData = new Packet
        {
            // PacketType = 0,
            // Key = "1234",
            Data = buffer,
        };

        var packet = funky_funcs.toByteArray(brushData);
        _stream.WriteAsync(packet, 0, packet.Length);
    }

    public void DrawQueue(ref DrawableTexture texture)
    {
        try
        {
            var count = draw_queue.Count;

            for (int i = 0; i < count; i++)
            {

                var d = draw_queue.Dequeue();
                texture.draw_point(new brush_frame_data
                {
                    color = d.color,
                    mouse_pos = d.mouse_pos,
                    brush_size = d.brush_size,
                    is_painting = true,
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void StopListening()
    {
        Console.WriteLine("Stopped listening");
        _isRunning = false;
        _readTask?.Wait();
    }

    public void Dispose()
    {
        Console.WriteLine("Dispose");
        StopListening();
        _stream?.Dispose();
        _client?.Dispose();
    }
}
public class Packet
{
    // public UInt16 PacketType { get; set; }
    // public string Key { get; set; }
    public byte[] Data { get; set; }
}

