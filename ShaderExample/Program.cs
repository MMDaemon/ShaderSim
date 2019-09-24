using OpenTK;
using OpenTK.Graphics;

namespace ShaderExample
{
    class Program
    {
        static void Main(string[] args)
        {
            GameWindow window = new GameWindow(800, 600, new GraphicsMode(32, 24, 0, 8));
            Model model = new Model();
            View view = new View(true);

            window.UpdateFrame += (s, e) => model.Update((float)e.Time);
            window.RenderFrame += (s, e) => view.Render(model.Entities);
            window.RenderFrame += (s, e) => window.SwapBuffers();
            window.Resize += (s, e) => view.Resize(window.Width, window.Height);

            window.Run();
        }
    }
}
